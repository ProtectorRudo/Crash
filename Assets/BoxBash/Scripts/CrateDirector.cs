using UnityEngine;

namespace BoxBash
{
    /// <summary>Keeps the arena supplied so a broken crate never turns the match into dead time.</summary>
    public sealed class CrateDirector : MonoBehaviour
    {
        public int targetCrates = 11;
        public float checkInterval = 0.55f;
        public float minimumFighterDistance = 1.45f;

        private PrototypeBootstrap bootstrap;
        private float nextCheck;
        private bool active;

        public void Bind(PrototypeBootstrap owner)
        {
            bootstrap = owner;
            active = true;
        }

        public void SetActive(bool value) => active = value;

        private void Update()
        {
            if (!active || bootstrap == null || Time.time < nextCheck) return;
            nextCheck = Time.time + checkInterval;
            ArenaWorld.PruneDestroyed();
            int missing = targetCrates - ArenaWorld.Crates.Count;
            if (missing <= 0) return;

            int spawnNow = Mathf.Min(2, missing);
            for (int i = 0; i < spawnNow; i++) TrySpawnOne();
        }

        private void TrySpawnOne()
        {
            var tiles = ArenaWorld.Tiles;
            if (tiles.Count == 0) return;

            for (int attempt = 0; attempt < 24; attempt++)
            {
                BreakableTile tile = tiles[Random.Range(0, tiles.Count)];
                if (tile == null || tile.IsBroken) continue;
                Vector3 point = tile.transform.position + Vector3.up * 0.62f;
                if (TooCloseToFighter(point) || TooCloseToCrate(point)) continue;

                float roll = Random.value;
                CrateKind kind = roll < 0.12f ? CrateKind.Nitro : (roll < 0.34f ? CrateKind.TNT : CrateKind.Normal);
                bootstrap.SpawnCrate(point, kind, true);
                return;
            }
        }

        private bool TooCloseToFighter(Vector3 point)
        {
            float sqrLimit = minimumFighterDistance * minimumFighterDistance;
            var fighters = ArenaWorld.Fighters;
            for (int i = 0; i < fighters.Count; i++)
            {
                ArenaFighter fighter = fighters[i];
                if (fighter == null || !fighter.IsAlive) continue;
                if ((fighter.transform.position - point).sqrMagnitude < sqrLimit) return true;
            }
            return false;
        }

        private bool TooCloseToCrate(Vector3 point)
        {
            const float sqrLimit = 1.25f * 1.25f;
            var crates = ArenaWorld.Crates;
            for (int i = 0; i < crates.Count; i++)
            {
                CarryableCrate crate = crates[i];
                if (crate != null && (crate.transform.position - point).sqrMagnitude < sqrLimit) return true;
            }
            return false;
        }
    }
}
