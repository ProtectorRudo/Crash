using UnityEngine;

namespace BoxBash
{
    public sealed class CrateDirector : MonoBehaviour
    {
        public int targetCrates = 14;
        public float checkInterval = 0.58f;
        public float minimumFighterDistance = 1.4f;

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
            for (int attempt = 0; attempt < 28; attempt++)
            {
                BreakableTile tile = tiles[Random.Range(0, tiles.Count)];
                if (tile == null || tile.IsBroken) continue;
                Vector3 point = tile.transform.position + Vector3.up * 0.62f;
                if (TooCloseToFighter(point) || TooCloseToCrate(point)) continue;

                float roll = Random.value;
                CrateKind kind = roll < 0.08f ? CrateKind.Nitro :
                                 roll < 0.25f ? CrateKind.TNT :
                                 roll < 0.36f ? CrateKind.Gift :
                                 roll < 0.47f ? CrateKind.Heavy : CrateKind.Normal;
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
            const float sqrLimit = 1.12f * 1.12f;
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
