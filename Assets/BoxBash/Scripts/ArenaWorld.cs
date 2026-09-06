using System.Collections.Generic;
using UnityEngine;

namespace BoxBash
{
    /// <summary>
    /// Tiny runtime registry used by gameplay code so bots and combat do not scan the entire scene every decision.
    /// It also centralizes safe-floor and explosive-threat queries for the destructible arena.
    /// </summary>
    public static class ArenaWorld
    {
        private static readonly List<ArenaFighter> fighters = new List<ArenaFighter>(8);
        private static readonly List<CarryableCrate> crates = new List<CarryableCrate>(32);
        private static readonly List<BreakableTile> tiles = new List<BreakableTile>(128);

        public static IReadOnlyList<ArenaFighter> Fighters => fighters;
        public static IReadOnlyList<CarryableCrate> Crates => crates;
        public static IReadOnlyList<BreakableTile> Tiles => tiles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            fighters.Clear();
            crates.Clear();
            tiles.Clear();
        }

        public static void Register(ArenaFighter value)
        {
            if (value != null && !fighters.Contains(value)) fighters.Add(value);
        }

        public static void Unregister(ArenaFighter value) => fighters.Remove(value);

        public static void Register(CarryableCrate value)
        {
            if (value != null && !crates.Contains(value)) crates.Add(value);
        }

        public static void Unregister(CarryableCrate value) => crates.Remove(value);

        public static void Register(BreakableTile value)
        {
            if (value != null && !tiles.Contains(value)) tiles.Add(value);
        }

        public static void Unregister(BreakableTile value) => tiles.Remove(value);

        public static void PruneDestroyed()
        {
            fighters.RemoveAll(x => x == null);
            crates.RemoveAll(x => x == null);
            tiles.RemoveAll(x => x == null);
        }

        public static ArenaFighter NearestOpponent(ArenaFighter self, bool softenHumanFocus = false)
        {
            ArenaFighter best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < fighters.Count; i++)
            {
                ArenaFighter other = fighters[i];
                if (other == null || other == self || !other.IsAlive) continue;
                float score = (other.transform.position - self.transform.position).sqrMagnitude;
                // Prevent the three bots from mechanically dog-piling the human every round.
                if (softenHumanFocus && other.IsHuman) score *= 1.18f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = other;
                }
            }
            return best;
        }

        public static CarryableCrate BestFreeCrate(ArenaFighter seeker, float explosivePreference = 0.15f)
        {
            CarryableCrate best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < crates.Count; i++)
            {
                CarryableCrate crate = crates[i];
                if (crate == null || !crate.CanBeTargetedBy(seeker)) continue;
                Vector3 delta = crate.transform.position - seeker.transform.position;
                float score = delta.sqrMagnitude;
                if (crate.kind != CrateKind.Normal) score *= Mathf.Lerp(1f, 0.7f, explosivePreference);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = crate;
                }
            }
            return best;
        }

        public static bool HasSafeFloor(Vector3 worldPosition, float maxHorizontalDistance = 0.82f)
        {
            float maxSqr = maxHorizontalDistance * maxHorizontalDistance;
            for (int i = 0; i < tiles.Count; i++)
            {
                BreakableTile tile = tiles[i];
                if (tile == null || tile.IsBroken) continue;
                Vector3 delta = tile.transform.position - worldPosition;
                delta.y = 0f;
                if (delta.sqrMagnitude <= maxSqr) return true;
            }
            return false;
        }

        public static Vector3 NearestSafeTilePosition(Vector3 from)
        {
            Vector3 best = from;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < tiles.Count; i++)
            {
                BreakableTile tile = tiles[i];
                if (tile == null || tile.IsBroken) continue;
                Vector3 delta = tile.transform.position - from;
                delta.y = 0f;
                if (delta.sqrMagnitude < bestSqr)
                {
                    bestSqr = delta.sqrMagnitude;
                    best = tile.transform.position + Vector3.up * 0.75f;
                }
            }
            return best;
        }

        public static float ThreatAt(Vector3 position)
        {
            float threat = HasSafeFloor(position) ? 0f : 10f;
            for (int i = 0; i < crates.Count; i++)
            {
                CarryableCrate crate = crates[i];
                if (crate == null || !crate.IsPrimed || crate.kind == CrateKind.Normal) continue;
                Vector3 flat = crate.transform.position - position;
                flat.y = 0f;
                float radius = crate.explosionRadius + 0.8f;
                float distance = flat.magnitude;
                if (distance < radius) threat += 1f - distance / radius;
            }
            return threat;
        }

        public static Vector2 MakeDirectionSafe(Vector3 origin, Vector2 desired, float step = 1.05f)
        {
            if (desired.sqrMagnitude < 0.001f) return Vector2.zero;
            Vector2 normalized = desired.normalized;
            Vector3 direct = origin + new Vector3(normalized.x, 0f, normalized.y) * step;
            if (HasSafeFloor(direct) && ThreatAt(direct) < 0.62f) return normalized;

            Vector2 left = new Vector2(-normalized.y, normalized.x);
            Vector2 right = -left;
            Vector3 leftPoint = origin + new Vector3(left.x, 0f, left.y) * step;
            Vector3 rightPoint = origin + new Vector3(right.x, 0f, right.y) * step;
            float leftThreat = HasSafeFloor(leftPoint) ? ThreatAt(leftPoint) : 99f;
            float rightThreat = HasSafeFloor(rightPoint) ? ThreatAt(rightPoint) : 99f;

            if (leftThreat >= 99f && rightThreat >= 99f)
            {
                Vector3 safe = NearestSafeTilePosition(origin) - origin;
                return new Vector2(safe.x, safe.z).normalized;
            }
            return leftThreat <= rightThreat ? left : right;
        }
    }
}
