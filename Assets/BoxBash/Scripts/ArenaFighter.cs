using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBash
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class ArenaFighter : MonoBehaviour
    {
        public event Action<ArenaFighter> Eliminated;

        [Header("Movement")]
        public float moveSpeed = 5.4f;
        public float acceleration = 24f;
        public float turnSpeed = 20f;

        [Header("Combat")]
        public float maxHealth = 100f;
        public float pickupRadius = 1.55f;
        public float throwSpeed = 10.8f;
        public float throwLift = 2.35f;
        public float aimAssistRange = 7.2f;
        [Range(0f, 1f)] public float aimAssistStrength = 0.72f;

        public bool IsHuman { get; set; }
        public bool IsAlive { get; private set; } = true;
        public bool ControlsEnabled { get; private set; }
        public float Health { get; private set; }
        public CarryableCrate CarriedCrate { get; private set; }
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public Vector2 MoveInput { get; private set; }
        public Rigidbody Body => body;

        private Rigidbody body;
        private Transform carryAnchor;
        private FighterPresentation presentation;
        private float invulnerableUntil;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Health = maxHealth;

            carryAnchor = new GameObject("CarryAnchor").transform;
            carryAnchor.SetParent(transform, false);
            carryAnchor.localPosition = new Vector3(0f, 1.18f, 0.68f);
            presentation = GetComponent<FighterPresentation>();
            ArenaWorld.Register(this);
        }

        private void OnDestroy() => ArenaWorld.Unregister(this);

        public void SetControlEnabled(bool enabled)
        {
            ControlsEnabled = enabled && IsAlive;
            if (!ControlsEnabled) SetMoveInput(Vector2.zero, true);
        }

        public void SetMoveInput(Vector2 input) => SetMoveInput(input, false);

        private void SetMoveInput(Vector2 input, bool bypassGate)
        {
            if (!bypassGate && !ControlsEnabled) return;
            MoveInput = Vector2.ClampMagnitude(input, 1f);
            Vector3 world = new Vector3(MoveInput.x, 0f, MoveInput.y);
            if (world.sqrMagnitude > 0.04f) Facing = world.normalized;
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;

            Vector3 desired = new Vector3(MoveInput.x, 0f, MoveInput.y) * moveSpeed;
            Vector3 planar = new Vector3(body.velocity.x, 0f, body.velocity.z);
            Vector3 next = Vector3.MoveTowards(planar, desired, acceleration * Time.fixedDeltaTime);
            body.velocity = new Vector3(next.x, body.velocity.y, next.z);

            if (Facing.sqrMagnitude > 0.1f)
            {
                Quaternion target = Quaternion.LookRotation(Facing, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.fixedDeltaTime);
            }

            if (transform.position.y < -2.65f) Eliminate();
        }

        public void ContextAction()
        {
            if (!IsAlive || !ControlsEnabled) return;
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
                if (dot < 0.42f) continue;
                float score = distance * (1.7f - dot);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = other;
                }
            }

            if (best == null) return baseDirection;

            Vector3 target = best.transform.position;
            Vector3 velocity = best.Body != null ? best.Body.velocity : Vector3.zero;
            velocity.y = 0f;
            float travel = Mathf.Clamp(Vector3.Distance(transform.position, target) / Mathf.Max(throwSpeed, 0.1f), 0.08f, 0.55f);
            target += velocity * travel * 0.58f;
            Vector3 assisted = target - transform.position;
            assisted.y = 0f;
            if (assisted.sqrMagnitude < 0.01f) return baseDirection;
            return Vector3.Slerp(baseDirection, assisted.normalized, aimAssistStrength).normalized;
        }

        public void ThrowCarried()
        {
            if (CarriedCrate == null || !IsAlive) return;
            CarryableCrate crate = CarriedCrate;
            CarriedCrate = null;
            Vector3 direction = GetAssistedThrowDirection();
            Facing = direction;
            crate.Throw(this, direction, throwSpeed, throwLift);
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
            invulnerableUntil = Time.time + 0.18f;
            Health = Mathf.Max(0f, Health - amount);
            body.AddForce(impulse, ForceMode.Impulse);
            presentation?.Hit();
            PrototypeBootstrap.Feel?.Hit(transform.position, amount >= 30f ? 0.45f : 0.2f);
            if (Health <= 0f) Eliminate();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            Health = Mathf.Clamp(Health + amount, 0f, maxHealth);
        }

        public void Celebrate() => presentation?.Celebrate();

        public void Eliminate()
        {
            if (!IsAlive) return;
            IsAlive = false;
            ControlsEnabled = false;
            ForceDrop();
            SetMoveInput(Vector2.zero, true);
            body.velocity = Vector3.zero;
            body.isKinematic = true;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            PrototypeBootstrap.Feel?.Elimination(transform.position);
            presentation?.Eliminate();
            Eliminated?.Invoke(this);
        }
    }
}
