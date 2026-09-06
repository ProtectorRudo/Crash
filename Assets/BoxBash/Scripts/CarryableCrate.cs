using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBash
{
    public enum CrateKind { Normal, TNT, Nitro }

    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class CarryableCrate : MonoBehaviour
    {
        public CrateKind kind = CrateKind.Normal;
        public float impactDamage = 23f;
        public float explosionDamage = 38f;
        public float explosionRadius = 2.3f;
        public float tileBlastRadius = 1.85f;
        public float fuseSeconds = 1.35f;

        public bool CanBePickedUp => !carried && !spent;
        public bool IsPrimed => primed && !spent;
        public ArenaFighter Owner { get; private set; }
        public ArenaFighter ReservedBy { get; private set; }

        private Rigidbody body;
        private BoxCollider hitbox;
        private bool carried;
        private bool primed;
        private bool spent;
        private float thrownAt;
        private float primedAt;
        private Vector3 baseScale;
        private static readonly Collider[] explosionHits = new Collider[64];

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            hitbox = GetComponent<BoxCollider>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            baseScale = transform.localScale;
            ArenaWorld.Register(this);
        }

        private void OnDestroy() => ArenaWorld.Unregister(this);

        private void Update()
        {
            if (transform.position.y < -4.5f)
            {
                Destroy(gameObject);
                return;
            }
            if (spent) return;
            if (primed && kind != CrateKind.Normal && !carried)
            {
                float speed = kind == CrateKind.Nitro ? 19f : 11f;
                float pulse = 1f + Mathf.Sin((Time.time - primedAt) * speed) * 0.055f;
                transform.localScale = baseScale * pulse;
            }
            else if (!carried)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 12f);
            }
        }

        public bool CanBeTargetedBy(ArenaFighter seeker)
        {
            return CanBePickedUp && (ReservedBy == null || ReservedBy == seeker || !ReservedBy.IsAlive);
        }

        public bool TryReserve(ArenaFighter seeker)
        {
            if (!CanBeTargetedBy(seeker)) return false;
            ReservedBy = seeker;
            return true;
        }

        public void ReleaseReservation(ArenaFighter seeker)
        {
            if (ReservedBy == seeker) ReservedBy = null;
        }

        public bool TryPickup(ArenaFighter fighter, Transform anchor)
        {
            if (!CanBePickedUp || fighter == null) return false;
            if (ReservedBy != null && ReservedBy != fighter && !fighter.IsHuman) return false;
            carried = true;
            primed = false;
            ReservedBy = null;
            Owner = fighter;
            StopAllCoroutines();
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            hitbox.enabled = false;
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0f, 18f, 7f);
            transform.localScale = baseScale * 1.08f;
            return true;
        }

        public void Throw(ArenaFighter owner, Vector3 direction, float speed, float lift)
        {
            if (spent) return;
            carried = false;
            ReservedBy = null;
            Owner = owner;
            transform.SetParent(null, true);
            hitbox.enabled = true;
            body.isKinematic = false;
            body.velocity = direction.normalized * speed + Vector3.up * lift;
            body.angularVelocity = new Vector3(5f, 7f, 4f);
            primed = true;
            primedAt = Time.time;
            thrownAt = Time.time;

            if (kind == CrateKind.TNT) StartCoroutine(Fuse(fuseSeconds));
            else if (kind == CrateKind.Nitro) StartCoroutine(Fuse(0.42f));
        }

        public void Drop(Vector3 position)
        {
            if (spent) return;
            carried = false;
            primed = false;
            ReservedBy = null;
            Owner = null;
            StopAllCoroutines();
            transform.SetParent(null, true);
            transform.position = position;
            transform.localScale = baseScale;
            hitbox.enabled = true;
            body.isKinematic = false;
        }

        private IEnumerator Fuse(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (!spent && primed) Explode();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!primed || spent || Time.time - thrownAt < 0.06f) return;

            ArenaFighter fighter = collision.collider.GetComponentInParent<ArenaFighter>();
            if (fighter != null && fighter != Owner)
            {
                Vector3 away = fighter.transform.position - transform.position;
                away.y = 0.18f;
                fighter.ApplyDamage(impactDamage, away.normalized * 5.5f + Vector3.up * 1.4f, Owner);
                if (kind == CrateKind.Normal) BreakNormal();
                else Explode();
                return;
            }

            if (kind == CrateKind.Nitro && collision.relativeVelocity.sqrMagnitude > 8f) Explode();
        }

        public void ChainReact()
        {
            if (spent || kind == CrateKind.Normal) return;
            primed = true;
            primedAt = Time.time;
            StopAllCoroutines();
            StartCoroutine(ChainDelay());
        }

        private IEnumerator ChainDelay()
        {
            yield return new WaitForSeconds(Random.Range(0.07f, 0.16f));
            Explode();
        }

        private void BreakNormal()
        {
            if (spent) return;
            spent = true;
            PrototypeBootstrap.Feel?.CrateBurst(transform.position, kind);
            Destroy(gameObject);
        }

        public void Explode()
        {
            if (spent) return;
            spent = true;
            carried = false;
            ReservedBy = null;
            if (Owner != null && Owner.CarriedCrate == this) Owner.ForceDrop();

            Vector3 center = transform.position;
            int hitCount = Physics.OverlapSphereNonAlloc(center, explosionRadius, explosionHits);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = explosionHits[i];
                ArenaFighter fighter = col.GetComponentInParent<ArenaFighter>();
                if (fighter != null)
                {
                    Vector3 delta = fighter.transform.position - center;
                    float falloff = 1f - Mathf.Clamp01(delta.magnitude / explosionRadius);
                    Vector3 impulseDirection = delta.sqrMagnitude > 0.01f ? delta.normalized : Vector3.up;
                    fighter.ApplyDamage(explosionDamage * Mathf.Lerp(0.55f, 1f, falloff),
                        impulseDirection * Mathf.Lerp(4.5f, 8f, falloff) + Vector3.up * 2f, Owner);
                }

                CarryableCrate other = col.GetComponentInParent<CarryableCrate>();
                if (other != null && other != this) other.ChainReact();
            }

            IReadOnlyList<BreakableTile> tiles = ArenaWorld.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                BreakableTile tile = tiles[i];
                if (tile == null || tile.IsBroken) continue;
                Vector3 flat = tile.transform.position - center;
                flat.y = 0f;
                if (flat.magnitude <= tileBlastRadius) tile.Blast();
            }

            PrototypeBootstrap.Feel?.Explosion(center, kind == CrateKind.Nitro ? 0.8f : 0.6f);
            Destroy(gameObject);
        }
    }
}
