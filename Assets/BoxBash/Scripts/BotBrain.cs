using UnityEngine;

namespace BoxBash
{
    [RequireComponent(typeof(ArenaFighter))]
    public sealed class BotBrain : MonoBehaviour
    {
        [Range(0f, 1f)] public float aggression = 0.65f;
        [Range(0f, 1f)] public float skill = 0.62f;
        public float decisionInterval = 0.13f;

        private ArenaFighter fighter;
        private float nextDecision;
        private Vector2 wander;
        private float wanderUntil;
        private CarryableCrate reservedCrate;
        private float personalBias;
        private float nextJumpThought;

        private void Awake()
        {
            fighter = GetComponent<ArenaFighter>();
            personalBias = Random.value;
        }

        private void OnDisable() => ReleaseCrate();
        private void OnDestroy() => ReleaseCrate();

        private void Update()
        {
            if (!fighter.IsAlive || !fighter.ControlsEnabled) return;
            if (Time.time < nextDecision) return;
            nextDecision = Time.time + decisionInterval * Random.Range(0.82f, 1.18f);
            Think();
        }

        private void Think()
        {
            float localThreat = ArenaWorld.ThreatAt(transform.position);
            if (localThreat > Mathf.Lerp(0.82f, 0.34f, skill))
            {
                FleeDanger();
                if (Time.time >= nextJumpThought && Random.value < Mathf.Lerp(0.06f, 0.22f, skill))
                {
                    nextJumpThought = Time.time + 0.8f;
                    fighter.Jump();
                }
                return;
            }

            if (fighter.CarriedCrate != null)
            {
                ReleaseCrate();
                AttackWithCrate();
                return;
            }

            ArenaPickup pickup = ArenaWorld.BestPickup(fighter, fighter.Health < 48f ? 8f : 5.2f);
            if (pickup != null && (fighter.Health < 58f || Random.value < 0.18f + skill * 0.22f))
            {
                Vector3 pd = pickup.transform.position - transform.position;
                Vector2 towardPickup = new Vector2(pd.x, pd.z).normalized;
                fighter.SetMoveInput(ArenaWorld.MakeDirectionSafe(transform.position, towardPickup) * 0.9f);
                return;
            }

            CarryableCrate kickable = ArenaWorld.NearestKickableCrate(fighter, 1.18f);
            if (kickable != null && !kickable.CanBePickedUp && Random.value < 0.62f)
            {
                Vector3 d = kickable.transform.position - transform.position;
                fighter.SetMoveInput(new Vector2(d.x, d.z).normalized * 0.35f);
                fighter.Kick();
                return;
            }

            if (reservedCrate == null || !reservedCrate.CanBeTargetedBy(fighter))
            {
                ReleaseCrate();
                CarryableCrate candidate = ArenaWorld.BestFreeCrate(fighter, Mathf.Lerp(0.08f, 0.82f, skill));
                if (candidate != null && candidate.TryReserve(fighter)) reservedCrate = candidate;
            }

            if (reservedCrate != null)
            {
                Vector3 delta3 = reservedCrate.transform.position - transform.position;
                delta3.y = 0f;
                if (delta3.magnitude <= fighter.pickupRadius * 0.90f)
                {
                    fighter.ContextAction();
                    if (fighter.CarriedCrate != null) reservedCrate = null;
                    return;
                }

                Vector2 desired = new Vector2(delta3.x, delta3.z).normalized;
                fighter.SetMoveInput(ArenaWorld.MakeDirectionSafe(transform.position, desired));
                return;
            }

            ArenaFighter opponent = PickOpponent();
            if (opponent != null && Random.value < aggression)
            {
                Vector3 d = opponent.transform.position - transform.position;
                Vector2 desired = new Vector2(d.x, d.z).normalized * 0.70f;
                fighter.SetMoveInput(ArenaWorld.MakeDirectionSafe(transform.position, desired));
                return;
            }

            WanderSafely();
        }

        private void AttackWithCrate()
        {
            ArenaFighter target = PickOpponent();
            if (target == null)
            {
                WanderSafely();
                return;
            }

            Vector3 d = target.transform.position - transform.position;
            d.y = 0f;
            float distance = d.magnitude;
            Vector2 toward = new Vector2(d.x, d.z).normalized;
            float idealRange = Mathf.Lerp(2.7f, 4.7f, skill);
            if (fighter.CarriedCrate != null && fighter.CarriedCrate.kind == CrateKind.Heavy) idealRange *= 0.82f;

            if (distance > idealRange + 0.65f)
            {
                fighter.SetMoveInput(ArenaWorld.MakeDirectionSafe(transform.position, toward) * 0.88f);
                return;
            }

            fighter.SetMoveInput(ArenaWorld.MakeDirectionSafe(transform.position, toward) * 0.12f);
            float chance = Mathf.Lerp(0.43f, 0.94f, skill);
            if (distance <= idealRange + 1.0f && Random.value < chance)
            {
                fighter.SetMoveInput(toward);
                fighter.ThrowCarried();
            }
        }

        private ArenaFighter PickOpponent()
        {
            ArenaFighter nearest = ArenaWorld.NearestOpponent(fighter, true);
            if (nearest == null) return null;
            if (personalBias > 0.72f && Random.value < 0.30f)
            {
                ArenaFighter alternative = null;
                float best = float.MaxValue;
                var fighters = ArenaWorld.Fighters;
                for (int i = 0; i < fighters.Count; i++)
                {
                    ArenaFighter other = fighters[i];
                    if (other == null || other == fighter || other == nearest || !other.IsAlive) continue;
                    float score = (other.transform.position - transform.position).sqrMagnitude;
                    if (score < best) { best = score; alternative = other; }
                }
                if (alternative != null) return alternative;
            }
            return nearest;
        }

        private void FleeDanger()
        {
            Vector2 best = Vector2.zero;
            float bestThreat = float.MaxValue;
            const int samples = 12;
            for (int i = 0; i < samples; i++)
            {
                float angle = i * Mathf.PI * 2f / samples;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 probe = transform.position + new Vector3(dir.x, 0f, dir.y) * 1.2f;
                float threat = ArenaWorld.HasSafeFloor(probe) ? ArenaWorld.ThreatAt(probe) : 100f;
                threat += Random.value * (1f - skill) * 0.12f;
                if (threat < bestThreat) { bestThreat = threat; best = dir; }
            }
            fighter.SetMoveInput(best);
        }

        private void WanderSafely()
        {
            if (Time.time >= wanderUntil)
            {
                wander = Random.insideUnitCircle.normalized;
                wanderUntil = Time.time + Random.Range(0.58f, 1.20f);
            }
            fighter.SetMoveInput(ArenaWorld.MakeDirectionSafe(transform.position, wander) * 0.60f);
        }

        private void ReleaseCrate()
        {
            if (reservedCrate == null) return;
            reservedCrate.ReleaseReservation(fighter);
            reservedCrate = null;
        }
    }
}
