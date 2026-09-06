using UnityEngine;

namespace BoxBash
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class FallingWeight : MonoBehaviour
    {
        public ArenaFighter owner;
        private bool spent;
        private float armedAt;

        private void Awake()
        {
            armedAt = Time.time + 0.18f;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.mass = 4.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (spent || Time.time < armedAt) return;
            ArenaFighter fighter = collision.collider.GetComponentInParent<ArenaFighter>();
            if (fighter != null && fighter != owner)
            {
                spent = true;
                Vector3 away = fighter.transform.position - transform.position;
                away.y = 0f;
                fighter.ApplyDamage(36f, away.normalized * 2.2f + Vector3.up * 1.0f, owner);
                fighter.ApplySlow(0.52f, 2.1f);
                fighter.Flatten();
                PrototypeBootstrap.Feel?.HeavyImpact(fighter.transform.position);
                Destroy(gameObject, 0.05f);
                return;
            }

            if (collision.collider.GetComponentInParent<BreakableTile>() != null)
            {
                spent = true;
                PrototypeBootstrap.Feel?.HeavyImpact(transform.position);
                Destroy(gameObject, 0.12f);
            }
        }

        private void Update()
        {
            if (transform.position.y < -6f) Destroy(gameObject);
        }
    }
}
