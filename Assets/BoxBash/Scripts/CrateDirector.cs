using UnityEngine;

namespace BoxBash
{
    public sealed class CrateDirector : MonoBehaviour
    {
        public int targetCrates = 14;
        public int targetPickups = 2;
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

            int missingCrates = targetCrates - ArenaWorld.Crates.Count;
            if (missingCrates > 0)
            {
                int spawnNow = Mathf.Min(2, missingCrates);
                for (int i = 0; i < spawnNow; i++) TrySpawnCrate();
            }

            int missingPickups = targetPickups - ArenaWorld.Pickups.Count;
            if (missingPickups > 0) TrySpawnPickup();
        }

        private void TrySpawnCrate()
        {
            var tiles = ArenaWorld.Tiles;
            if (tiles.Count == 0) return;
            for (int attempt = 0; attempt < 28; attempt++)
            {
                BreakableTile tile = tiles[Random.Range(0, tiles.Count)];
                if (tile == null || tile.IsBroken) continue;
                Vector3 point = tile.transform.position + Vector3.up * 0.62f;
                if (TooCloseToFighter(point) || TooCloseToCrate(point) || TooCloseToPickup(point)) continue;

                float roll = Random.value;
                CrateKind kind = roll < 0.10f ? CrateKind.Nitro :
                                 roll < 0.34f ? CrateKind.TNT : CrateKind.Normal;
                bootstrap.SpawnCrate(point, kind, true);
                return;
            }
        }

        private void TrySpawnPickup()
        {
            var tiles = ArenaWorld.Tiles;
            if (tiles.Count == 0) return;
            for (int attempt = 0; attempt < 28; attempt++)
            {
                BreakableTile tile = tiles[Random.Range(0, tiles.Count)];
                if (tile == null || tile.IsBroken) continue;
                Vector3 point = tile.transform.position + Vector3.up * 0.58f;
                if (TooCloseToFighter(point) || TooCloseToCrate(point) || TooCloseToPickup(point)) continue;
                bootstrap.SpawnRandomPickup(point);
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

        private bool TooCloseToPickup(Vector3 point)
        {
            const float sqrLimit = 1.2f * 1.2f;
            var pickups = ArenaWorld.Pickups;
            for (int i = 0; i < pickups.Count; i++)
            {
                ArenaPickup pickup = pickups[i];
                if (pickup != null && pickup.IsAvailable && (pickup.transform.position - point).sqrMagnitude < sqrLimit) return true;
            }
            return false;
        }
    }
}
