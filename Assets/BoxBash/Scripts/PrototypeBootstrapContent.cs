using System.Collections;
using UnityEngine;

namespace BoxBash
{
    public sealed partial class PrototypeBootstrap
    {
        private void CreateCrates()
        {
            Vector3[] spots =
            {
                new Vector3(0,.62f,0), new Vector3(-2.56f,.62f,0), new Vector3(2.56f,.62f,0),
                new Vector3(0,.62f,-2.56f), new Vector3(0,.62f,2.56f),
                new Vector3(-2.56f,.62f,-2.56f), new Vector3(2.56f,.62f,-2.56f),
                new Vector3(-2.56f,.62f,2.56f), new Vector3(2.56f,.62f,2.56f),
                new Vector3(-3.84f,.62f,0), new Vector3(3.84f,.62f,0),
                new Vector3(0,.62f,-3.84f), new Vector3(0,.62f,3.84f),
                new Vector3(-3.84f,.62f,-2.56f), new Vector3(3.84f,.62f,2.56f)
            };
            CrateKind[] kinds =
            {
                CrateKind.Normal, CrateKind.Normal, CrateKind.Normal, CrateKind.TNT, CrateKind.Normal,
                CrateKind.Normal, CrateKind.Nitro, CrateKind.Normal, CrateKind.Normal, CrateKind.TNT,
                CrateKind.Normal, CrateKind.Normal, CrateKind.Normal, CrateKind.Nitro, CrateKind.TNT
            };
            for (int i = 0; i < spots.Length; i++) SpawnCrate(spots[i], kinds[i], false);

            SpawnPowerup(new Vector3(-1.28f, 0.58f, 1.28f), PowerupKind.Wumpa);
            SpawnPowerup(new Vector3(1.28f, 0.58f, -1.28f), PowerupKind.SpeedBoots);
        }

        public CarryableCrate SpawnCrate(Vector3 pos, CrateKind kind, bool popIn)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = kind + " Crate";
            crate.transform.SetParent(world);
            crate.transform.position = pos;
            crate.transform.localScale = Vector3.one * 0.90f;

            Color color;
            switch (kind)
            {
                case CrateKind.TNT: color = new Color(0.90f, 0.11f, 0.08f); break;
                case CrateKind.Nitro: color = new Color(0.18f, 0.87f, 0.23f); break;
                case CrateKind.Heavy: color = new Color(0.42f, 0.46f, 0.52f); break;
                case CrateKind.Gift: color = new Color(0.63f, 0.20f, 0.82f); break;
                default: color = new Color(0.47f, 0.50f, 0.56f); break;
            }
            crate.GetComponent<Renderer>().material = Mat(color, kind == CrateKind.Normal ? 0.48f : 0.08f, 0.34f);
            Rigidbody rb = crate.AddComponent<Rigidbody>();
            CarryableCrate c = crate.AddComponent<CarryableCrate>();
            c.ConfigureKind(kind);
            CreateCrateAccent(crate.transform, kind);
            if (popIn)
            {
                rb.isKinematic = true;
                crate.transform.localScale = Vector3.one * 0.12f;
                StartCoroutine(PopIn(crate.transform, rb));
            }
            return c;
        }

        private void CreateCrateAccent(Transform parent, CrateKind kind)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Crate Band";
            stripe.transform.SetParent(parent, false);
            stripe.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            stripe.transform.localScale = new Vector3(0.84f, 0.065f, 0.84f);
            Color accent = kind == CrateKind.Normal ? new Color(0.18f, 0.20f, 0.24f) :
                           kind == CrateKind.TNT ? new Color(1f, 0.85f, 0.10f) :
                           kind == CrateKind.Nitro ? new Color(0.92f, 1f, 0.92f) :
                           kind == CrateKind.Heavy ? new Color(0.18f, 0.20f, 0.23f) : new Color(1f, 0.72f, 0.16f);
            stripe.GetComponent<Renderer>().material = Mat(accent, 0.05f, 0.42f);
            Destroy(stripe.GetComponent<Collider>());

            string label = kind == CrateKind.TNT ? "TNT" : kind == CrateKind.Nitro ? "N" : "";
            if (!string.IsNullOrEmpty(label)) CreateCrateLabel(parent, label);
        }

        private void CreateCrateLabel(Transform parent, string label)
        {
            GameObject text = new GameObject("Crate Label");
            text.transform.SetParent(parent, false);
            text.transform.localPosition = new Vector3(0f, 0.02f, -0.505f);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh mesh = text.AddComponent<TextMesh>();
            mesh.text = label;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = 0.09f;
            mesh.color = Color.white;
        }

        public void SpawnRandomPickup(Vector3 position)
        {
            float roll = Random.value;
            PowerupKind kind = roll < 0.34f ? PowerupKind.Wumpa :
                               roll < 0.56f ? PowerupKind.SpeedBoots :
                               roll < 0.74f ? PowerupKind.Shield :
                               roll < 0.88f ? PowerupKind.SlowZap : PowerupKind.Weight;
            SpawnPowerup(position, kind);
        }

        public ArenaPickup SpawnPowerup(Vector3 position, PowerupKind kind)
        {
            GameObject go = GameObject.CreatePrimitive(kind == PowerupKind.Weight ? PrimitiveType.Cube : PrimitiveType.Sphere);
            go.name = kind + " Powerup";
            go.transform.SetParent(world);
            go.transform.position = position;
            go.transform.localScale = kind == PowerupKind.Weight ? Vector3.one * 0.46f : Vector3.one * 0.52f;
            Color color = kind == PowerupKind.Wumpa ? new Color(1f, 0.36f, 0.18f) :
                          kind == PowerupKind.SpeedBoots ? new Color(0.20f, 0.78f, 1f) :
                          kind == PowerupKind.Shield ? new Color(0.25f, 1f, 0.75f) :
                          kind == PowerupKind.SlowZap ? new Color(0.62f, 0.45f, 1f) :
                          kind == PowerupKind.Weight ? new Color(0.72f, 0.74f, 0.80f) :
                          new Color(1f, 0.78f, 0.14f);
            go.GetComponent<Renderer>().material = GlowMat(color);
            Collider old = go.GetComponent<Collider>();
            if (!(old is SphereCollider))
            {
                Destroy(old);
                go.AddComponent<SphereCollider>();
            }
            ArenaPickup pickup = go.AddComponent<ArenaPickup>();
            pickup.kind = kind;
            return pickup;
        }

        public void DropCrushingWeight(ArenaFighter target)
        {
            if (target == null) return;
            GameObject weight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weight.name = "500 Weight";
            weight.transform.SetParent(world);
            weight.transform.position = target.transform.position + Vector3.up * 2.2f;
            weight.transform.localScale = new Vector3(0.88f, 0.55f, 0.88f);
            weight.GetComponent<Renderer>().material = Mat(new Color(0.26f, 0.29f, 0.34f), 0.70f, 0.34f);
            Rigidbody rb = weight.AddComponent<Rigidbody>();
            rb.mass = 4.5f;
            rb.velocity = Vector3.down * 8f;
            Destroy(weight, 1.4f);
            PrototypeBootstrap.Feel?.HeavyImpact(target.transform.position);
        }

        public void DropWeightOnOpponent(ArenaFighter owner)
        {
            ArenaFighter target = ArenaWorld.NearestOpponent(owner, false);
            if (target != null) DropCrushingWeight(target);
        }

        private IEnumerator PopIn(Transform target, Rigidbody body)
        {
            if (target == null) yield break;
            Vector3 finalScale = Vector3.one * 0.90f;
            float t = 0f;
            const float duration = 0.18f;
            while (target != null && t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float overshoot = Mathf.Sin(k * Mathf.PI) * 0.12f;
                target.localScale = Vector3.Lerp(Vector3.one * 0.12f, finalScale, k) * (1f + overshoot);
                yield return null;
            }
            if (target != null) target.localScale = finalScale;
            if (body != null) body.isKinematic = false;
        }

        private void DestroySessionMaterials()
        {
            for (int i = 0; i < mats.Count; i++) if (mats[i] != null) Destroy(mats[i]);
            mats.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            for (int i = 0; i < mats.Count; i++) if (mats[i] != null) Destroy(mats[i]);
        }
    }
}
