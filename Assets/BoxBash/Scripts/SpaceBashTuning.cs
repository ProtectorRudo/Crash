namespace BoxBash
{
    /// <summary>
    /// Central tuning for the Space-Bash-style ruleset. Values live here so the first phone playtest
    /// can be tuned without hunting through many scripts.
    /// </summary>
    public static class SpaceBashTuning
    {
        public const float MatchSeconds = 90f;
        public const float CountdownSeconds = 3f;
        public const float MaxHealth = 100f;
        public const float MoveSpeed = 5.65f;
        public const float MoveAcceleration = 46f;
        public const float JumpImpulse = 5.25f;
        public const float ThrowSpeed = 12.2f;
        public const float ThrowLift = 1.55f;
        public const float PickupRadius = 1.45f;
        public const float AimAssistRange = 7.0f;
        public const float AimAssistStrength = 0.58f;
        public const float HitInvulnerability = 0.22f;
        public const float HitStun = 0.17f;
        public const float NormalDamage = 24f;
        public const float HeavyDamage = 34f;
        public const float TntExplosionDamage = 42f;
        public const float NitroExplosionDamage = 48f;
        public const float TntFuse = 1.55f;
        public const float NitroFuse = 0.32f;
    }
}
