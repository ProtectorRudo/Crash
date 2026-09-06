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
                case PowerupKind.Shield: return 1.20f;
                case PowerupKind.SpeedBoots: return 0.95f;
                case PowerupKind.SlowZap: return 0.16f;
                case PowerupKind.Weight: return 0.10f;
                case PowerupKind.ThrowBoost: return 0.05f;
                default: return 0.5f;
            }
        }

        private void Collect(ArenaFighter fighter)
        {
            IsAvailable = false;
            switch (kind)
            {
                case PowerupKind.Wumpa:
                    fighter.Heal(SpaceBashTuning.WumpaHeal);
                    break;
                case PowerupKind.SpeedBoots:
                    fighter.ApplySpeedBoost(1.34f, SpaceBashTuning.SpeedBootsDuration);
                    break;
                case PowerupKind.Shield:
                    fighter.ApplyShield(SpaceBashTuning.ShieldDuration);
                    break;
                case PowerupKind.SlowZap:
                    fighter.ApplySlow(0.52f, SpaceBashTuning.SlowZDuration);
                    break;
                case PowerupKind.Weight:
                    fighter.GiveWeight(SpaceBashTuning.WeightDuration);
                    break;
                case PowerupKind.ThrowBoost:
                    fighter.ApplyThrowBoost(1.20f, 5f);
                    break;
            }

            PrototypeBootstrap.Feel?.Powerup(transform.position, kind);
            Destroy(gameObject);
        }
    }
}
