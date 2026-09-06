using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBash
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class ArenaFighter : MonoBehaviour
    {
        public event Action<ArenaFighter> Eliminated;

        [Header("Identity")]
        public string displayName = "PLAYER";
        public Color playerColor = Color.white;

        [Header("Movement")]
        public float moveSpeed = SpaceBashTuning.MoveSpeed;
        public float acceleration = SpaceBashTuning.MoveAcceleration;
        public float turnSpeed = 24f;
        public float jumpImpulse = SpaceBashTuning.JumpImpulse;

        [Header("Combat")]
        public float maxHealth = SpaceBashTuning.MaxHealth;
        public float pickupRadius = SpaceBashTuning.PickupRadius;
        public float throwSpeed = SpaceBashTuning.ThrowSpeed;
        public float throwLift = SpaceBashTuning.ThrowLift;
        public float aimAssistRange = SpaceBashTuning.AimAssistRange;
        [Range(0f, 1f)] public float aimAssistStrength = SpaceBashTuning.AimAssistStrength;

        public bool IsHuman { get; set; }
        public bool IsAlive { get; private set; } = true;
        public bool ControlsEnabled { get; private set; }
        public float Health { get; private set; }
        public CarryableCrate CarriedCrate { get; private set; }
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public Vector2 MoveInput { get; private set; }
        public Rigidbody Body => body;
        public bool IsGrounded => GroundedCheck();
        public bool Shielded => shieldCharges > 0 && Time.time < shieldUntil;
        public bool HasWeight => weightDeadline > Time.time;
        public float WeightRemaining => HasWeight ? Mathf.Max(0f, weightDeadline - Time.time) : 0f;

        public string StatusText
        {
            get
            {
                if (HasWeight) return "PESA " + Mathf.CeilToInt(WeightRemaining);
                if (Shielded) return "ESCUDO 1";
                if (Time.time < speedBoostUntil) return "VELOCIDAD";
                if (Time.time < slowUntil) return "LENTO";
                return string.Empty;
            }
        }

        private Rigidbody body;
        private Transform carryAnchor;
        private FighterPresentation presentation;
        private float invulnerableUntil;
        private float stunnedUntil;
        private float nextJumpAt;
        private float nextKickAt;
        private float speedMultiplier = 1f;
        private float speedBoostUntil;
        private float throwMultiplier = 1f;
        private float throwBoostUntil;
        private int shieldCharges;
        private float shieldUntil;
        private float slowMultiplier = 1f;
        private float slowUntil;
        private float weightDeadline;
        private float weightTransferLockUntil;
        private static readonly RaycastHit[] groundHits = new RaycastHit[8];

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Health = maxHealth;

            carryAnchor = new GameObject("CarryAnchor").transform;
            carryAnchor.SetParent(transform, false);
            carryAnchor.localPosition = new Vector3(0f, 1.52f, 0.12f);
            presentation = GetComponent<FighterPresentation>();
            ArenaWorld.Register(this);
        }

        private void OnDestroy() => ArenaWorld.Unregister(this);

        private void Update()
        {
            if (Time.time >= speedBoostUntil) speedMultiplier = 1f;
            if (Time.time >= throwBoostUntil) throwMultiplier = 1f;
            if (Time.time >= slowUntil) slowMultiplier = 1f;
            if (Time.time >= shieldUntil) shieldCharges = 0;

            if (IsAlive && weightDeadline > 0f && Time.time >= weightDeadline)
            {
                weightDeadline = 0f;
                presentation?.SetWeight(false);
                PrototypeBootstrap.Instance?.DropCrushingWeight(this);
                Eliminate();
            }
        }

        public void SetControlEnabled(bool enabled)
        {
            ControlsEnabled = enabled && IsAlive;
            if (!ControlsEnabled) SetMoveInput(Vector2.zero, true);
        }

        public void SetMoveInput(Vector2 input) => SetMoveInput(input, false);

        private void SetMoveInput(Vector2 input, bool bypassGate)
        {
            if (!bypassGate && (!ControlsEnabled || Time.time < stunnedUntil)) return;
            MoveInput = Vector2.ClampMagnitude(input, 1f);
            Vector3 world = new Vector3(MoveInput.x, 0f, MoveInput.y);
            if (world.sqrMagnitude > 0.04f) Facing = world.normalized;
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;

            float carryPenalty = CarriedCrate != null ? CarriedCrate.CarryMoveMultiplier : 1f;
            float activeSlow = Time.time < slowUntil ? slowMultiplier : 1f;
            float effectiveSpeed = moveSpeed * speedMultiplier * activeSlow * carryPenalty;
            Vector3 desired = Time.time < stunnedUntil ? Vector3.zero : new Vector3(MoveInput.x, 0f, MoveInput.y) * effectiveSpeed;
            Vector3 planar = new Vector3(body.velocity.x, 0f, body.velocity.z);
            Vector3 next = Vector3.MoveTowards(planar, desired, acceleration * Time.fixedDeltaTime);
            body.velocity = new Vector3(next.x, body.velocity.y, next.z);

            if (activeSlow < 0.999f && !GroundedCheck())
            {
                float gravityCompensation = -Physics.gravity.y * body.mass * (1f - activeSlow);
                body.AddForce(Vector3.up * gravityCompensation, ForceMode.Force);
            }

            if (Facing.sqrMagnitude > 0.1f)
            {
                Quaternion target = Quaternion.LookRotation(Facing, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.fixedDeltaTime);
            }

            if (transform.position.y < -1.65f) Eliminate();
        }

        public void ContextAction()
        {
            if (!CanAct()) return;
            if (CarriedCrate != null)
            {
                ThrowCarried();
                return;
            }

            CarryableCrate best = null;
            float bestDist = pickupRadius * pickupRadius;
            IReadOnlyList<CarryableCrate> crates = ArenaWorld.Crates;
            for (int i = 0; i < crates.Count; i++)
            {
                CarryableCrate crate = crates[i];
                if (crate == null || !crate.CanBePickedUp) continue;
                float sqr = (crate.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestDist)
                {
                    bestDist = sqr;
                    best = crate;
                }
            }

            if (best != null && best.TryPickup(this, carryAnchor))
            {
                CarriedCrate = best;
                presentation?.Pickup();
                PrototypeBootstrap.Feel?.Pickup(transform.position + Vector3.up);
            }
        }

        public bool Jump()
        {
            if (!CanAct() || Time.time < nextJumpAt || !GroundedCheck()) return false;
            nextJumpAt = Time.time + 0.34f;
            Vector3 v = body.velocity;
            v.y = Mathf.Max(0f, v.y);
            body.velocity = v;
            float activeSlow = Time.time < slowUntil ? slowMultiplier : 1f;
            float bootJumpMultiplier = Time.time < speedBoostUntil ? 1.18f : 1f;
            body.AddForce(Vector3.up * jumpImpulse * activeSlow * bootJumpMultiplier, ForceMode.Impulse);
            presentation?.Jump();
            PrototypeBootstrap.Feel?.Jump(transform.position);
            return true;
        }

        public bool Kick()
        {
            if (!CanAct() || Time.time < nextKickAt) return false;

            CarryableCrate crate = ArenaWorld.NearestKickableCrate(this, 1.35f);
            if (crate != null)
            {
                nextKickAt = Time.time + 0.42f;
                crate.Kick(this, Facing, 8.6f);
                presentation?.Kick();
                PrototypeBootstrap.Feel?.Kick(transform.position + Facing * 0.7f);
                return true;
            }

            ArenaFighter opponent = NearestKickableOpponent(1.28f);
            if (opponent == null) return false;

            nextKickAt = Time.time + 0.42f;
            Vector3 impulse = Facing.normalized * 3.4f + Vector3.up * 0.62f;
            opponent.ApplyDamage(8f, impulse, this);
            presentation?.Kick();
            PrototypeBootstrap.Feel?.Kick(opponent.transform.position + Vector3.up * 0.5f);
            return true;
        }

        private ArenaFighter NearestKickableOpponent(float radius)
        {
            ArenaFighter best = null;
            float bestSqr = radius * radius;
            IReadOnlyList<ArenaFighter> fighters = ArenaWorld.Fighters;
            Vector3 facing = Facing.sqrMagnitude > 0.01f ? Facing.normalized : transform.forward;

            for (int i = 0; i < fighters.Count; i++)
            {
                ArenaFighter other = fighters[i];
                if (other == null || other == this || !other.IsAlive) continue;
                Vector3 delta = other.transform.position - transform.position;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr < 0.01f || sqr > bestSqr) continue;
                if (Vector3.Dot(facing, delta.normalized) < 0.18f) continue;
                bestSqr = sqr;
                best = other;
            }
            return best;
        }

        private bool CanAct() => IsAlive && ControlsEnabled && Time.time >= stunnedUntil;

        private bool GroundedCheck()
        {
            int count = Physics.RaycastNonAlloc(transform.position + Vector3.up * 0.12f, Vector3.down, groundHits, 1.15f);
            for (int i = 0; i < count; i++)
            {
                Collider c = groundHits[i].collider;
                if (c == null || c.transform == transform || c.transform.IsChildOf(transform)) continue;
                BreakableTile tile = c.GetComponentInParent<BreakableTile>();
                if (tile != null && !tile.IsBroken) return true;
            }
            return false;
        }

        public Vector3 GetAssistedThrowDirection()
        {
            Vector3 baseDirection = Facing.sqrMagnitude > 0.01f ? Facing.normalized : transform.forward;
            ArenaFighter best = null;
            float bestScore = float.MaxValue;
            IReadOnlyList<ArenaFighter> fighters = ArenaWorld.Fighters;

            for (int i = 0; i < fighters.Count; i++)
            {
                ArenaFighter other = fighters[i];
                if (other == null || other == this || !other.IsAlive) continue;
                Vector3 delta = other.transform.position - transform.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance > aimAssistRange || distance < 0.15f) continue;
                float dot = Vector3.Dot(baseDirection, delta / distance);
                if (dot < 0.48f) continue;
                float score = distance * (1.65f - dot);
                if (score < bestScore) { bestScore = score; best = other; }
            }

            if (best == null) return baseDirection;
            Vector3 target = best.transform.position;
            Vector3 velocity = best.Body != null ? best.Body.velocity : Vector3.zero;
            velocity.y = 0f;
            float speed = throwSpeed * throwMultiplier;
            float travel = Mathf.Clamp(Vector3.Distance(transform.position, target) / Mathf.Max(speed, 0.1f), 0.06f, 0.45f);
            target += velocity * travel * 0.48f;
            Vector3 assisted = target - transform.position;
            assisted.y = 0f;
            if (assisted.sqrMagnitude < 0.01f) return baseDirection;
            return Vector3.Slerp(baseDirection, assisted.normalized, aimAssistStrength).normalized;
        }

        public void ThrowCarried()
        {
            if (CarriedCrate == null || !CanAct()) return;
            CarryableCrate crate = CarriedCrate;
            CarriedCrate = null;
            Vector3 direction = GetAssistedThrowDirection();
            Facing = direction;
            crate.Throw(this, direction, throwSpeed * throwMultiplier, throwLift);
            presentation?.Throw();
            PrototypeBootstrap.Feel?.Throw(transform.position + Vector3.up);
        }

        internal void ForceDrop()
        {
            if (CarriedCrate == null) return;
            CarryableCrate crate = CarriedCrate;
            CarriedCrate = null;
            crate.Drop(transform.position + Vector3.up * 0.7f);
        }

        public void ApplyDamage(float amount, Vector3 impulse, ArenaFighter attacker = null)
        {
            if (!IsAlive || Time.time < invulnerableUntil) return;

            if (Time.time < speedBoostUntil)
            {
                speedBoostUntil = 0f;
                speedMultiplier = 1f;
            }

            if (Shielded)
            {
                shieldCharges = Mathf.Max(0, shieldCharges - 1);
                shieldUntil = 0f;
                invulnerableUntil = Time.time + 0.16f;
                PrototypeBootstrap.Feel?.ShieldBlock(transform.position);
                return;
            }

            invulnerableUntil = Time.time + SpaceBashTuning.HitInvulnerability;
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + SpaceBashTuning.HitStun);
            Health = Mathf.Max(0f, Health - amount);
            ForceDropOnHardHit(amount);
            body.AddForce(impulse, ForceMode.Impulse);
            presentation?.Hit();
            PrototypeBootstrap.Feel?.Hit(transform.position, amount >= 30f ? 0.42f : 0.20f);
            if (Health <= 0f) Eliminate();
        }

        private void ForceDropOnHardHit(float amount)
        {
            if (amount >= 32f && CarriedCrate != null) ForceDrop();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            Health = Mathf.Clamp(Health + amount, 0f, maxHealth);
        }

        public void ApplySpeedBoost(float multiplier, float duration)
        {
            speedMultiplier = Mathf.Max(speedMultiplier, multiplier);
            speedBoostUntil = Mathf.Max(speedBoostUntil, Time.time + duration);
        }

        public void ApplyThrowBoost(float multiplier, float duration)
        {
            throwMultiplier = Mathf.Max(throwMultiplier, multiplier);
            throwBoostUntil = Mathf.Max(throwBoostUntil, Time.time + duration);
        }

        public void ApplyShield(float duration)
        {
            shieldCharges = 1;
            shieldUntil = Time.time + duration;
            presentation?.Shield(duration);
        }

        public void ApplySlow(float multiplier, float duration)
        {
            slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        public void GiveWeight(float duration)
        {
            ReceiveWeight(Time.time + duration);
        }

        private void ReceiveWeight(float deadline)
        {
            if (!IsAlive) return;
            weightDeadline = deadline;
            weightTransferLockUntil = Time.time + 0.42f;
            presentation?.SetWeight(true);
        }

        private void PassWeightTo(ArenaFighter other)
        {
            if (!HasWeight || other == null || !other.IsAlive || other.HasWeight || Time.time < weightTransferLockUntil) return;
            float deadline = weightDeadline;
            weightDeadline = 0f;
            presentation?.SetWeight(false);
            other.ReceiveWeight(deadline);
            PrototypeBootstrap.Feel?.WeightPass(other.transform.position);
        }

        public void Flatten()
        {
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + 0.75f);
            presentation?.Flatten();
        }

        public void Celebrate() => presentation?.Celebrate();

        public void Eliminate()
        {
            if (!IsAlive) return;
            IsAlive = false;
            ControlsEnabled = false;
            weightDeadline = 0f;
            presentation?.SetWeight(false);
            ForceDrop();
            SetMoveInput(Vector2.zero, true);
            body.velocity = Vector3.zero;
            body.isKinematic = true;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            PrototypeBootstrap.Feel?.Elimination(transform.position);
            presentation?.Eliminate();
            Eliminated?.Invoke(this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsAlive) return;

            ArenaFighter otherFighter = collision.collider.GetComponentInParent<ArenaFighter>();
            if (otherFighter != null && otherFighter != this)
            {
                if (HasWeight) PassWeightTo(otherFighter);
                else if (otherFighter.HasWeight) otherFighter.PassWeightTo(this);
            }

            if (CarriedCrate != null || MoveInput.magnitude < 0.78f || Time.time < nextKickAt) return;
            CarryableCrate crate = collision.collider.GetComponentInParent<CarryableCrate>();
            if (crate == null || !crate.CanBeKicked) return;
            nextKickAt = Time.time + 0.38f;
            crate.Kick(this, Facing, 6.8f);
            presentation?.Kick();
        }
    }
}
