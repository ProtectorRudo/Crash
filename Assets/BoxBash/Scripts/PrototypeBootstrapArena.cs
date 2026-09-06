using UnityEngine;

namespace BoxBash
{
    public sealed partial class PrototypeBootstrap
    {
        private void CreateArena()
        {
            Material a = Mat(new Color(0.18f, 0.21f, 0.27f), 0.48f, 0.42f);
            Material b = Mat(new Color(0.12f, 0.15f, 0.21f), 0.42f, 0.36f);
            Material inset = Mat(new Color(0.055f, 0.065f, 0.085f), 0.34f, 0.28f);
            Material seam = Mat(new Color(0.035f, 0.042f, 0.060f), 0.58f, 0.28f);
            Material hazardYellow = GlowMat(new Color(1f, 0.72f, 0.06f));
            Material hazardDark = Mat(new Color(0.035f, 0.035f, 0.042f), 0.30f, 0.20f);
            float half = (Grid - 1) * Tile * 0.5f;

            for (int x = 0; x < Grid; x++)
            for (int z = 0; z < Grid; z++)
            {
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Deck Panel {x}-{z}";
                tile.transform.SetParent(world);
                tile.transform.position = new Vector3(x * Tile - half, 0f, z * Tile - half);
                tile.transform.localScale = new Vector3(Tile * 0.93f, 0.26f, Tile * 0.93f);
                tile.GetComponent<Renderer>().sharedMaterial = ((x + z) % 2 == 0) ? a : b;
                tile.AddComponent<BreakableTile>();
                CreatePanelInset(tile.transform, inset);

                if ((x * 3 + z * 5) % 11 == 0)
                    CreateHazardPanel(tile.transform, hazardYellow, hazardDark, (x + z) % 2 == 0);
            }

            float arena = Grid * Tile;
            CreateWall(new Vector3(0f, 0.46f, -arena * 0.5f - 0.16f), new Vector3(arena + 0.65f, 0.92f, 0.26f), seam);
            CreateWall(new Vector3(0f, 0.46f, arena * 0.5f + 0.16f), new Vector3(arena + 0.65f, 0.92f, 0.26f), seam);
            CreateWall(new Vector3(-arena * 0.5f - 0.16f, 0.46f, 0f), new Vector3(0.26f, 0.92f, arena + 0.65f), seam);
            CreateWall(new Vector3(arena * 0.5f + 0.16f, 0.46f, 0f), new Vector3(0.26f, 0.92f, arena + 0.65f), seam);

            for (int i = -4; i <= 4; i++)
            {
                Material stripe = ((i + 4) % 2 == 0) ? hazardYellow : hazardDark;
                CreateRailStripe(new Vector3(i * Tile, 0.91f, -arena * 0.5f - 0.31f), new Vector3(Tile * 0.78f, 0.10f, 0.11f), stripe);
                CreateRailStripe(new Vector3(i * Tile, 0.91f, arena * 0.5f + 0.31f), new Vector3(Tile * 0.78f, 0.10f, 0.11f), stripe);
                CreateRailStripe(new Vector3(-arena * 0.5f - 0.31f, 0.91f, i * Tile), new Vector3(0.11f, 0.10f, Tile * 0.78f), stripe);
                CreateRailStripe(new Vector3(arena * 0.5f + 0.31f, 0.91f, i * Tile), new Vector3(0.11f, 0.10f, Tile * 0.78f), stripe);
            }
        }

        private void CreatePanelInset(Transform tile, Material mat)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Recessed Deck Face";
            panel.transform.SetParent(tile, false);
            panel.transform.localPosition = new Vector3(0f, 0.525f, 0f);
            panel.transform.localScale = new Vector3(0.76f, 0.035f, 0.76f);
            panel.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(panel.GetComponent<Collider>());
        }

        private void CreateHazardPanel(Transform tile, Material yellow, Material dark, bool horizontal)
        {
            for (int i = -2; i <= 2; i++)
            {
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = "Deck Hazard Stripe";
                bar.transform.SetParent(tile, false);
                if (horizontal)
                {
                    bar.transform.localPosition = new Vector3(i * 0.145f, 0.565f, 0f);
                    bar.transform.localScale = new Vector3(0.12f, 0.035f, 0.56f);
                }
                else
                {
                    bar.transform.localPosition = new Vector3(0f, 0.565f, i * 0.145f);
                    bar.transform.localScale = new Vector3(0.56f, 0.035f, 0.12f);
                }
                bar.GetComponent<Renderer>().sharedMaterial = ((i + 2) % 2 == 0) ? yellow : dark;
                Destroy(bar.GetComponent<Collider>());
            }
        }

        private void CreateWall(Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Arena Rail";
            wall.transform.SetParent(world);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void CreateRailStripe(Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Rail Hazard Stripe";
            stripe.transform.SetParent(world);
            stripe.transform.position = pos;
            stripe.transform.localScale = scale;
            stripe.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(stripe.GetComponent<Collider>());
        }

        private void CreateFighters()
        {
            float r = Tile * 3.15f;
            Vector3[] pos =
            {
                new Vector3(-r, 0.88f, -r), new Vector3(r, 0.88f, -r),
                new Vector3(-r, 0.88f, r), new Vector3(r, 0.88f, r)
            };
            Color[] colors =
            {
                new Color(0.10f, 0.73f, 1f), new Color(1f, 0.24f, 0.32f),
                new Color(0.40f, 0.92f, 0.28f), new Color(1f, 0.68f, 0.12f)
            };
            string[] names = { "VOS", "ROJO", "VERDE", "ORO" };

            for (int i = 0; i < 4; i++)
            {
                GameObject go = new GameObject(i == 0 ? "PLAYER" : $"BOT {i}");
                go.transform.SetParent(world);
                go.transform.position = pos[i];

                CapsuleCollider cap = go.AddComponent<CapsuleCollider>();
                cap.height = 1.68f;
                cap.radius = 0.39f;
                cap.center = Vector3.zero;

                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.mass = 1.25f;
                rb.drag = 4.4f;
                rb.angularDrag = 4f;

                FighterPresentation presentation = go.AddComponent<FighterPresentation>();
                ArenaFighter fighter = go.AddComponent<ArenaFighter>();
                fighter.IsHuman = i == 0;
                fighter.displayName = names[i];
                fighter.playerColor = colors[i];

                Transform visual = CreateFighterVisual(go.transform, colors[i], i);
                presentation.Bind(fighter, visual);
                match.Register(fighter);

                if (i == 0)
                {
                    go.AddComponent<PlayerTouchController>();
                }
                else
                {
                    BotBrain bot = go.AddComponent<BotBrain>();
                    bot.skill = 0.48f + i * 0.12f;
                    bot.aggression = 0.52f + i * 0.08f;
                }
            }
        }

        private Transform CreateFighterVisual(Transform owner, Color color, int style)
        {
            PrimitiveType primitive = style == 2 ? PrimitiveType.Cube : (style == 3 ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            GameObject visual = GameObject.CreatePrimitive(primitive);
            visual.name = "Original Fighter Visual";
            visual.transform.SetParent(owner, false);
            visual.transform.localPosition = Vector3.zero;
            if (style == 0) visual.transform.localScale = new Vector3(0.76f, 0.90f, 0.76f);
            else if (style == 1) visual.transform.localScale = new Vector3(0.82f, 0.82f, 0.82f);
            else if (style == 2) visual.transform.localScale = new Vector3(0.72f, 1.05f, 0.72f);
            else visual.transform.localScale = new Vector3(0.86f, 0.86f, 0.86f);
            Destroy(visual.GetComponent<Collider>());
            Material bodyMat = Mat(color, style == 2 ? 0.38f : 0.12f, 0.68f);
            visual.GetComponent<Renderer>().sharedMaterial = bodyMat;
            CreateCharacterDetails(visual.transform, bodyMat, style);
            return visual.transform;
        }

        private void CreateCharacterDetails(Transform parent, Material bodyMat, int style)
        {
            Material white = Mat(new Color(0.96f, 0.98f, 1f), 0f, 0.55f);
            Material dark = Mat(new Color(0.018f, 0.024f, 0.038f), 0.05f, 0.18f);

            for (int s = -1; s <= 1; s += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                eye.transform.SetParent(parent, false);
                eye.transform.localPosition = new Vector3(0.18f * s, 0.28f, 0.45f);
                eye.transform.localScale = new Vector3(0.22f, 0.25f, 0.12f);
                eye.GetComponent<Renderer>().sharedMaterial = white;
                Destroy(eye.GetComponent<Collider>());

                GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pupil.name = "Pupil";
                pupil.transform.SetParent(parent, false);
                pupil.transform.localPosition = new Vector3(0.18f * s, 0.28f, 0.53f);
                pupil.transform.localScale = Vector3.one * 0.095f;
                pupil.GetComponent<Renderer>().sharedMaterial = dark;
                Destroy(pupil.GetComponent<Collider>());
            }

            for (int s = -1; s <= 1; s += 2)
            {
                GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hand.name = "Hand";
                hand.transform.SetParent(parent, false);
                hand.transform.localPosition = new Vector3(0.53f * s, -0.02f, 0.04f);
                hand.transform.localScale = new Vector3(0.25f, 0.28f, 0.25f);
                hand.GetComponent<Renderer>().sharedMaterial = bodyMat;
                Destroy(hand.GetComponent<Collider>());
            }

            if (style == 0) CreateAntenna(parent, bodyMat, 0f);
            if (style == 1)
            {
                CreateAntenna(parent, bodyMat, -0.26f);
                CreateAntenna(parent, bodyMat, 0.26f);
            }
            if (style == 2)
            {
                GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visor.name = "Robot Visor";
                visor.transform.SetParent(parent, false);
                visor.transform.localPosition = new Vector3(0f, 0.18f, 0.54f);
                visor.transform.localScale = new Vector3(0.56f, 0.12f, 0.06f);
                visor.GetComponent<Renderer>().sharedMaterial = GlowMat(new Color(0.20f, 0.88f, 1f));
                Destroy(visor.GetComponent<Collider>());
            }
            if (style == 3)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ear.name = "Ear";
                    ear.transform.SetParent(parent, false);
                    ear.transform.localPosition = new Vector3(0.47f * s, 0.28f, 0f);
                    ear.transform.localScale = new Vector3(0.26f, 0.40f, 0.22f);
                    ear.GetComponent<Renderer>().sharedMaterial = bodyMat;
                    Destroy(ear.GetComponent<Collider>());
                }
            }

            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Shadow";
            shadow.transform.SetParent(owner: parent.parent, worldPositionStays: false);
            shadow.transform.localPosition = new Vector3(0f, -0.83f, 0f);
            shadow.transform.localScale = new Vector3(0.62f, 0.018f, 0.48f);
            shadow.GetComponent<Renderer>().sharedMaterial = dark;
            Destroy(shadow.GetComponent<Collider>());
        }

        private void CreateAntenna(Transform parent, Material mat, float x)
        {
            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antenna.name = "Antenna";
            antenna.transform.SetParent(parent, false);
            antenna.transform.localPosition = new Vector3(x, 0.88f, 0f);
            antenna.transform.localScale = new Vector3(0.055f, 0.20f, 0.055f);
            antenna.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(antenna.GetComponent<Collider>());
        }
    }
}
