using UnityEngine;

namespace BoxBash
{
    public enum PowerupKind { Wumpa, SpeedBoots, ThrowBoost, Shield, SlowZap, Weight }

    [RequireComponent(typeof(SphereCollider))]
    public sealed class ArenaPickup : MonoBehaviour
    {
        public PowerupKind kind;
        public bool IsAvailable { get; private set; } = true;

        private Vector3 basePosition;
        private float phase;

        private void Awake()
        {
            SphereCollider col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.48f;
            basePosition = transform.position;
            phase = Random.value * 6.28f;
            ArenaWorld.Register(this);
        }

        private void OnDestroy() => ArenaWorld.Unregister(this);

        private void Update()
        {
            if (!IsAvailable) return;
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3.6f + phase) * 0.10f + 0.10f);
        }

        private void OnTriggerEnter(Collider other)
        {
            ArenaFighter fighter = other.GetComponentInParent<ArenaFighter>();
            if (fighter == null || !fighter.IsAlive || !IsAvailable) return;
            Collect(fighter);
        }

        public float UtilityFor(ArenaFighter fighter)
        {
            if (!IsAvailable || fighter == null) return -1f;
            switch (kind)
            {
                case PowerupKind.Wumpa: return 0.25f + (1f - fighter.Health / fighter.maxHealth) * 1.6f;
                case PowerupKind.Shield: return 1.15f;
                case PowerupKind.SpeedBoots: return 0.88f;
                case PowerupKind.ThrowBoost: return 0.82f;
                case PowerupKind.SlowZap: return 0.70f;
                case PowerupKind.Weight: return 0.76f;
                default: return 0.5f;
            }
        }

        private void Collect(ArenaFighter fighter)
        {
            IsAvailable = false;
            switch (kind)
            {
                case PowerupKind.Wumpa:
                    fighter.Heal(30f);
                    break;
                case PowerupKind.SpeedBoots:
                    fighter.ApplySpeedBoost(1.34f, 6.5f);
                    break;
                case PowerupKind.ThrowBoost:
                    fighter.ApplyThrowBoost(1.30f, 7.5f);
                    break;
                case PowerupKind.Shield:
                    fighter.ApplyShield(5.5f);
                    break;
                case PowerupKind.SlowZap:
                    fighter.ApplySlowToNearestOpponent(0.62f, 4.2f);
                    break;
                case PowerupKind.Weight:
                    PrototypeBootstrap.Instance?.DropWeightOnOpponent(fighter);
                    break;
            }

            PrototypeBootstrap.Feel?.Powerup(transform.position, kind);
            Destroy(gameObject);
        }
    }
}
