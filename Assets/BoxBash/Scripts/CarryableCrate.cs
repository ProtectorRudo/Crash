using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBash
{
    public enum CrateKind { Normal, TNT, Nitro, Heavy, Gift }

    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class CarryableCrate : MonoBehaviour
    {
        public CrateKind kind = CrateKind.Normal;
        public float impactDamage = SpaceBashTuning.NormalDamage;
        public float explosionDamage = SpaceBashTuning.TntExplosionDamage;
        public float explosionRadius = 2.15f;
        public float tileBlastRadius = 0.96f;
        public float fuseSeconds = SpaceBashTuning.TntFuse;
        public float CarryMoveMultiplier { get; private set; } = 1f;

        public bool CanBePickedUp => !carried && !primed && !spent && kind != CrateKind.Nitro && kind != CrateKind.Gift;
        public bool CanBeKicked => !carried && !spent;
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

        public void ConfigureKind(CrateKind value)
        {
            kind = value;
            impactDamage = SpaceBashTuning.NormalDamage;
            explosionDamage = SpaceBashTuning.TntExplosionDamage;
            explosionRadius = 2.15f;
            tileBlastRadius = 0.96f;
            fuseSeconds = SpaceBashTuning.TntFuse;
            CarryMoveMultiplier = 1f;
            body.mass = 1.8f;
            body.drag = 2.4f;
            body.angularDrag = 2f;

            switch (kind)
            {
                case CrateKind.TNT:
                    impactDamage = 16f;
                    explosionDamage = SpaceBashTuning.TntExplosionDamage;
                    explosionRadius = 2.15f;
                    tileBlastRadius = 0.98f;
                    fuseSeconds = SpaceBashTuning.TntFuse;
                    body.mass = 1.7f;
                    break;
                case CrateKind.Nitro:
                    impactDamage = 10f;
                    explosionDamage = SpaceBashTuning.NitroExplosionDamage;
                    explosionRadius = 2.45f;
                    tileBlastRadius = 1.55f;
                    fuseSeconds = SpaceBashTuning.NitroFuse;
                    body.mass = 1.25f;
                    break;
                case CrateKind.Heavy:
                    impactDamage = SpaceBashTuning.HeavyDamage;
                    CarryMoveMultiplier = 0.82f;
                    body.mass = 3.1f;
                    body.drag = 3.4f;
                    break;
                case CrateKind.Gift:
                    impactDamage = 0f;
                    body.mass = 2.2f;
                    body.drag = 5f;
                    break;
            }
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
            if (primed && (kind == CrateKind.TNT || kind == CrateKind.Nitro) && !carried)
            {
                float speed = kind == CrateKind.Nitro ? 22f : 12f;
                float pulse = 1f + Mathf.Sin((Time.time - primedAt) * speed) * 0.065f;
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
            transform.localRotation = Quaternion.Euler(8f, 14f, 5f);
            transform.localScale = baseScale * 1.03f;
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
            body.angularVelocity = new Vector3(4.2f, 6.2f, 3.8f);
            primed = true;
            primedAt = Time.time;
            thrownAt = Time.time;

            if (kind == CrateKind.TNT) StartCoroutine(Fuse(fuseSeconds));
            else if (kind == CrateKind.Nitro) StartCoroutine(Fuse(fuseSeconds));
            else if (kind == CrateKind.Normal) StartCoroutine(ExpireSolidAfterThrow(0.78f));
            else if (kind == CrateKind.Heavy) StartCoroutine(ExpireSolidAfterThrow(0.92f));
        }

        public void Kick(ArenaFighter owner, Vector3 direction, float speed)
        {
            if (!CanBeKicked) return;
            Owner = owner;
            ReservedBy = null;
            if (kind == CrateKind.Gift)
            {
                OpenGift();
                return;
            }
            if (kind == CrateKind.Nitro)
            {
                primed = true;
                primedAt = Time.time;
                Explode();
                return;
            }

            carried = false;
            body.isKinematic = false;
            hitbox.enabled = true;
            body.velocity = direction.normalized * speed + Vector3.up * 0.18f;
            body.angularVelocity = new Vector3(0f, 8f, 0f);
            primed = true;
            primedAt = Time.time;
            thrownAt = Time.time;
            if (kind == CrateKind.TNT)
            {
                StopAllCoroutines();
                StartCoroutine(Fuse(fuseSeconds));
            }
            else if (kind == CrateKind.Normal) StartCoroutine(ExpireSolidAfterThrow(0.72f));
            else if (kind == CrateKind.Heavy) StartCoroutine(ExpireSolidAfterThrow(0.86f));
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
            body.velocity = Vector3.zero;
        }

        private IEnumerator Fuse(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (!spent && primed) Explode();
        }

        private IEnumerator ExpireSolidAfterThrow(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (!spent && primed && (kind == CrateKind.Normal || kind == CrateKind.Heavy)) BreakSolid();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (spent) return;
            ArenaFighter fighter = collision.collider.GetComponentInParent<ArenaFighter>();

            if (!primed)
            {
                if (kind == CrateKind.Gift && fighter != null)
                {
                    OpenGift();
                    return;
                }
                if (kind == CrateKind.Nitro && fighter != null)
                {
                    Owner = null;
                    primed = true;
                    Explode();
                }
                return;
            }

            if (Time.time - thrownAt < 0.055f) return;
            if (fighter != null && fighter != Owner)
            {
                Vector3 away = fighter.transform.position - transform.position;
                away.y = 0.18f;
                fighter.ApplyDamage(impactDamage, away.normalized * 5.2f + Vector3.up * 1.15f, Owner);
                if (kind == CrateKind.TNT || kind == CrateKind.Nitro) Explode();
                else BreakSolid();
                return;
            }

            CarryableCrate other = collision.collider.GetComponentInParent<CarryableCrate>();
            if (other != null && other != this && other.kind == CrateKind.Gift) other.OpenGift();

            if (kind == CrateKind.Nitro && collision.relativeVelocity.sqrMagnitude > 4f) Explode();
        }

        public void ChainReact()
        {
            if (spent || (kind != CrateKind.TNT && kind != CrateKind.Nitro)) return;
            primed = true;
            primedAt = Time.time;
            StopAllCoroutines();
            StartCoroutine(ChainDelay());
        }

        private IEnumerator ChainDelay()
        {
            yield return new WaitForSeconds(Random.Range(0.055f, 0.12f));
            Explode();
        }

        private void BreakSolid()
        {
            if (spent) return;
            spent = true;
            PrototypeBootstrap.Feel?.CrateBurst(transform.position, kind);
            Destroy(gameObject);
        }

        public void OpenGift()
        {
            if (spent) return;
            spent = true;
            Vector3 p = transform.position;
            PrototypeBootstrap.Feel?.GiftOpen(p);
            PrototypeBootstrap.Instance?.SpawnRandomPickup(p + Vector3.up * 0.44f);
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
                    fighter.ApplyDamage(explosionDamage * Mathf.Lerp(0.58f, 1f, falloff),
                        impulseDirection * Mathf.Lerp(4.4f, 7.6f, falloff) + Vector3.up * 1.8f, Owner);
                }

                CarryableCrate other = col.GetComponentInParent<CarryableCrate>();
                if (other != null && other != this)
                {
                    if (other.kind == CrateKind.Gift) other.OpenGift();
                    else other.ChainReact();
                }
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

            PrototypeBootstrap.Feel?.Explosion(center, kind == CrateKind.Nitro ? 0.78f : 0.56f);
            Destroy(gameObject);
        }
    }
}
