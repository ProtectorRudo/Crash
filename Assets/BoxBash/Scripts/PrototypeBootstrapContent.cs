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
                case CrateKind.Nitro: color = new Color(0.16f, 0.82f, 0.20f); break;
                case CrateKind.Heavy: color = new Color(0.42f, 0.46f, 0.52f); break;
                case CrateKind.Gift: color = new Color(0.63f, 0.20f, 0.82f); break;
                default: color = new Color(0.46f, 0.47f, 0.49f); break;
            }

            float metallic = kind == CrateKind.Normal ? 0.05f : 0.08f;
            float smoothness = kind == CrateKind.Normal ? 0.16f : 0.32f;
            crate.GetComponent<Renderer>().sharedMaterial = Mat(color, metallic, smoothness);
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
            Color accent = kind == CrateKind.Normal ? new Color(0.17f, 0.18f, 0.20f) :
                           kind == CrateKind.TNT ? new Color(1f, 0.82f, 0.06f) :
                           kind == CrateKind.Nitro ? new Color(0.93f, 1f, 0.93f) :
                           kind == CrateKind.Heavy ? new Color(0.18f, 0.20f, 0.23f) : new Color(1f, 0.72f, 0.16f);
            Material accentMat = Mat(accent, 0.05f, 0.34f);

            for (int axis = 0; axis < 2; axis++)
            {
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.name = "Crate Reinforcement";
                band.transform.SetParent(parent, false);
                band.transform.localPosition = new Vector3(0f, 0.44f - axis * 0.88f, 0f);
                band.transform.localScale = new Vector3(0.84f, 0.06f, 0.84f);
                band.GetComponent<Renderer>().sharedMaterial = accentMat;
                Destroy(band.GetComponent<Collider>());
            }

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
            mesh.fontStyle = FontStyle.Bold;
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
            PrimitiveType shape = kind == PowerupKind.Weight ? PrimitiveType.Cube :
                                  kind == PowerupKind.Shield ? PrimitiveType.Cylinder : PrimitiveType.Sphere;
            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = kind + " Powerup";
            go.transform.SetParent(world);
            go.transform.position = position;
            go.transform.localScale = kind == PowerupKind.Weight ? new Vector3(0.54f, 0.38f, 0.54f) :
                                      kind == PowerupKind.Shield ? new Vector3(0.48f, 0.13f, 0.48f) : Vector3.one * 0.52f;
            Color color = kind == PowerupKind.Wumpa ? new Color(1f, 0.32f, 0.14f) :
                          kind == PowerupKind.SpeedBoots ? new Color(0.18f, 0.76f, 1f) :
                          kind == PowerupKind.Shield ? new Color(0.18f, 1f, 0.72f) :
                          kind == PowerupKind.SlowZap ? new Color(0.62f, 0.38f, 1f) :
                          kind == PowerupKind.Weight ? new Color(0.62f, 0.64f, 0.68f) :
                          new Color(1f, 0.78f, 0.14f);
            go.GetComponent<Renderer>().sharedMaterial = GlowMat(color);

            Collider old = go.GetComponent<Collider>();
            if (!(old is SphereCollider))
            {
                Destroy(old);
                go.AddComponent<SphereCollider>();
            }

            string icon = kind == PowerupKind.Wumpa ? "+" :
                          kind == PowerupKind.SpeedBoots ? ">>" :
                          kind == PowerupKind.Shield ? "1" :
                          kind == PowerupKind.SlowZap ? "Z" :
                          kind == PowerupKind.Weight ? "500" : "";
            CreatePowerupIcon(go.transform, icon);

            if (kind == PowerupKind.Wumpa)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = "Health Leaf";
                leaf.transform.SetParent(go.transform, false);
                leaf.transform.localPosition = new Vector3(0.25f, 0.52f, 0f);
                leaf.transform.localScale = new Vector3(0.22f, 0.10f, 0.34f);
                leaf.GetComponent<Renderer>().sharedMaterial = GlowMat(new Color(0.22f, 0.90f, 0.25f));
                Destroy(leaf.GetComponent<Collider>());
            }

            ArenaPickup pickup = go.AddComponent<ArenaPickup>();
            pickup.kind = kind;
            return pickup;
        }

        private void CreatePowerupIcon(Transform parent, string icon)
        {
            if (string.IsNullOrEmpty(icon)) return;
            GameObject text = new GameObject("Powerup Icon");
            text.transform.SetParent(parent, false);
            text.transform.localPosition = new Vector3(0f, 0f, -0.58f);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh mesh = text.AddComponent<TextMesh>();
            mesh.text = icon;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = icon.Length > 1 ? 0.075f : 0.10f;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = Color.white;
        }

        public void DropCrushingWeight(ArenaFighter target)
        {
            if (target == null) return;
            GameObject weight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weight.name = "500 Weight";
            weight.transform.SetParent(world);
            weight.transform.position = target.transform.position + Vector3.up * 2.2f;
            weight.transform.localScale = new Vector3(0.88f, 0.55f, 0.88f);
            weight.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.26f, 0.29f, 0.34f), 0.70f, 0.34f);
            CreatePowerupIcon(weight.transform, "500");
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
