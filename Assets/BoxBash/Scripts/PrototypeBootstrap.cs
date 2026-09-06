using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBash
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        public static PrototypeBootstrap Instance { get; private set; }
        public static GameFeel Feel { get; private set; }

        private readonly List<Material> mats = new List<Material>();
        private MatchManager match;
        private Transform session;
        private Transform world;
        private Camera gameCamera;
        private bool restarting;
        private const int Grid = 9;
        private const float Tile = 1.35f;

        private void Awake() => Instance = this;

        private void Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = false;
#if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
            BuildWorld();
        }

        public void RestartPrototype()
        {
            if (!restarting) StartCoroutine(RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            restarting = true;
            if (session != null) Destroy(session.gameObject);
            yield return null;
            DestroySessionMaterials();
            ArenaWorld.PruneDestroyed();
            BuildWorld();
            restarting = false;
        }

        private void BuildWorld()
        {
            session = new GameObject("BOX BASH - Session").transform;
            gameCamera = CreateCamera();
            CreateLighting();

            GameObject feelGo = new GameObject("Game Feel");
            feelGo.transform.SetParent(session);
            Feel = feelGo.AddComponent<GameFeel>();
            Feel.Bind(gameCamera);

            world = new GameObject("Arena World").transform;
            world.SetParent(session);

            GameObject directorGo = new GameObject("Crate Director");
            directorGo.transform.SetParent(session);
            CrateDirector director = directorGo.AddComponent<CrateDirector>();
            director.Bind(this);
            director.SetActive(false);

            GameObject matchGo = new GameObject("Match Manager");
            matchGo.transform.SetParent(session);
            match = matchGo.AddComponent<MatchManager>();
            match.Bind(director);

            CreateArena();
            CreateFighters();
            CreateCrates();
        }

        private Camera CreateCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.transform.position = new Vector3(0f, 12.8f, -10.6f);
            cam.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
            cam.fieldOfView = 45f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.055f, 0.075f, 0.12f);
            cam.allowHDR = true;
            return cam;
        }

        private void CreateLighting()
        {
            RenderSettings.ambientLight = new Color(0.43f, 0.47f, 0.58f);
            GameObject sun = new GameObject("Key Light");
            sun.transform.SetParent(session);
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.15f;
            l.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);

            GameObject rim = new GameObject("Arena Rim Light");
            rim.transform.SetParent(session);
            Light r = rim.AddComponent<Light>();
            r.type = LightType.Point;
            r.range = 18f;
            r.intensity = 1.8f;
            rim.transform.position = new Vector3(0f, 6f, 1f);
        }

        private Material Mat(Color color, float metallic = 0f, float smooth = 0.35f)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material m = new Material(shader);
            m.color = color;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            mats.Add(m);
            return m;
        }

        private void CreateArena()
        {
            Material a = Mat(new Color(0.20f, 0.24f, 0.34f), 0.08f, 0.55f);
            Material b = Mat(new Color(0.15f, 0.18f, 0.27f), 0.04f, 0.45f);
            float half = (Grid - 1) * Tile * 0.5f;

            for (int x = 0; x < Grid; x++)
            for (int z = 0; z < Grid; z++)
            {
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Tile {x}-{z}";
                tile.transform.SetParent(world);
                tile.transform.position = new Vector3(x * Tile - half, 0f, z * Tile - half);
                tile.transform.localScale = new Vector3(Tile * 0.94f, 0.28f, Tile * 0.94f);
                tile.GetComponent<Renderer>().material = ((x + z) % 2 == 0) ? a : b;
                tile.AddComponent<BreakableTile>();
            }

            Material edge = Mat(new Color(0.08f, 0.11f, 0.18f), 0.1f, 0.5f);
            float arena = Grid * Tile;
            CreateWall(new Vector3(0f, 0.7f, -arena * 0.5f - 0.15f), new Vector3(arena + 0.6f, 1.6f, 0.3f), edge);
            CreateWall(new Vector3(0f, 0.7f, arena * 0.5f + 0.15f), new Vector3(arena + 0.6f, 1.6f, 0.3f), edge);
            CreateWall(new Vector3(-arena * 0.5f - 0.15f, 0.7f, 0f), new Vector3(0.3f, 1.6f, arena + 0.6f), edge);
            CreateWall(new Vector3(arena * 0.5f + 0.15f, 0.7f, 0f), new Vector3(0.3f, 1.6f, arena + 0.6f), edge);
        }

        private void CreateWall(Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Arena Edge";
            wall.transform.SetParent(world);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material = mat;
        }

        private void CreateFighters()
        {
            float r = Tile * 3f;
            Vector3[] pos =
            {
                new Vector3(-r, 0.88f, -r), new Vector3(r, 0.88f, -r),
                new Vector3(-r, 0.88f, r), new Vector3(r, 0.88f, r)
            };
            Color[] colors =
            {
                new Color(0.12f, 0.75f, 1f), new Color(1f, 0.27f, 0.36f),
                new Color(0.48f, 1f, 0.35f), new Color(1f, 0.72f, 0.18f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject go = new GameObject(i == 0 ? "PLAYER" : $"BOT {i}");
                go.transform.SetParent(world);
                go.transform.position = pos[i];

                CapsuleCollider cap = go.AddComponent<CapsuleCollider>();
                cap.height = 1.72f;
                cap.radius = 0.39f;
                cap.center = Vector3.zero;

                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.mass = 1.2f;
                rb.drag = 2.5f;
                rb.angularDrag = 3f;

                FighterPresentation presentation = go.AddComponent<FighterPresentation>();
                ArenaFighter fighter = go.AddComponent<ArenaFighter>();
                fighter.IsHuman = i == 0;

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = new Vector3(0.78f, 0.88f, 0.78f);
                Destroy(visual.GetComponent<Collider>());
                Material bodyMat = Mat(colors[i], 0.1f, 0.7f);
                visual.GetComponent<Renderer>().material = bodyMat;
                CreateCharacterDetails(visual.transform, bodyMat);

                presentation.Bind(fighter, visual.transform);
                match.Register(fighter);

                if (i == 0)
                {
                    go.AddComponent<PlayerTouchController>();
                    go.AddComponent<ThrowGuide>();
                }
                else
                {
                    BotBrain bot = go.AddComponent<BotBrain>();
                    bot.skill = 0.46f + i * 0.12f;
                    bot.aggression = 0.50f + i * 0.09f;
                }
            }
        }

        private void CreateCharacterDetails(Transform parent, Material bodyMat)
        {
            Material white = Mat(new Color(0.95f, 0.97f, 1f), 0f, 0.55f);
            Material dark = Mat(new Color(0.02f, 0.025f, 0.035f), 0f, 0.18f);

            for (int s = -1; s <= 1; s += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye White";
                eye.transform.SetParent(parent, false);
                eye.transform.localPosition = new Vector3(0.18f * s, 0.29f, 0.43f);
                eye.transform.localScale = new Vector3(0.23f, 0.27f, 0.13f);
                eye.GetComponent<Renderer>().material = white;
                Destroy(eye.GetComponent<Collider>());

                GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pupil.name = "Pupil";
                pupil.transform.SetParent(parent, false);
                pupil.transform.localPosition = new Vector3(0.18f * s, 0.29f, 0.515f);
                pupil.transform.localScale = Vector3.one * 0.105f;
                pupil.GetComponent<Renderer>().material = dark;
                Destroy(pupil.GetComponent<Collider>());
            }

            for (int s = -1; s <= 1; s += 2)
            {
                GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hand.name = "Hand";
                hand.transform.SetParent(parent, false);
                hand.transform.localPosition = new Vector3(0.53f * s, -0.03f, 0.06f);
                hand.transform.localScale = new Vector3(0.25f, 0.30f, 0.25f);
                hand.GetComponent<Renderer>().material = bodyMat;
                Destroy(hand.GetComponent<Collider>());
            }

            GameObject mouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouth.name = "Mouth";
            mouth.transform.SetParent(parent, false);
            mouth.transform.localPosition = new Vector3(0f, 0.04f, 0.515f);
            mouth.transform.localScale = new Vector3(0.20f, 0.035f, 0.04f);
            mouth.GetComponent<Renderer>().material = dark;
            Destroy(mouth.GetComponent<Collider>());

            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Blob Shadow";
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0f, -1.01f, 0f);
            shadow.transform.localScale = new Vector3(0.82f, 0.025f, 0.62f);
            shadow.GetComponent<Renderer>().material = Mat(new Color(0.025f, 0.03f, 0.045f), 0f, 0.05f);
            Destroy(shadow.GetComponent<Collider>());
        }

        private void CreateCrates()
        {
            Vector3[] spots =
            {
                new Vector3(0, .62f, 0), new Vector3(-2.7f,.62f,0), new Vector3(2.7f,.62f,0),
                new Vector3(0,.62f,-2.7f), new Vector3(0,.62f,2.7f),
                new Vector3(-2.7f,.62f,-2.7f), new Vector3(2.7f,.62f,-2.7f),
                new Vector3(-2.7f,.62f,2.7f), new Vector3(2.7f,.62f,2.7f),
                new Vector3(-4.05f,.62f,0), new Vector3(4.05f,.62f,0)
            };

            for (int i = 0; i < spots.Length; i++)
            {
                CrateKind kind = i % 6 == 0 ? CrateKind.Nitro : (i % 4 == 0 ? CrateKind.TNT : CrateKind.Normal);
                SpawnCrate(spots[i], kind, false);
            }
        }

        public CarryableCrate SpawnCrate(Vector3 pos, CrateKind kind, bool popIn)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = kind + " Crate";
            crate.transform.SetParent(world);
            crate.transform.position = pos;
            crate.transform.localScale = Vector3.one * 0.92f;

            Color color = kind == CrateKind.Normal ? new Color(0.55f, 0.30f, 0.12f) :
                          kind == CrateKind.TNT ? new Color(0.90f, 0.12f, 0.10f) :
                          new Color(0.23f, 0.92f, 0.30f);
            crate.GetComponent<Renderer>().material = Mat(color, 0.02f, 0.38f);
            Rigidbody rb = crate.AddComponent<Rigidbody>();
            rb.mass = 0.8f;
            rb.drag = 0.6f;
            rb.angularDrag = 0.4f;
            CarryableCrate c = crate.AddComponent<CarryableCrate>();
            c.kind = kind;
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
            stripe.name = "Crate Accent";
            stripe.transform.SetParent(parent, false);
            stripe.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            stripe.transform.localScale = new Vector3(0.82f, 0.06f, 0.82f);
            Color accent = kind == CrateKind.Normal ? new Color(0.95f, 0.67f, 0.25f) :
                           kind == CrateKind.TNT ? new Color(1f, 0.88f, 0.18f) : Color.white;
            stripe.GetComponent<Renderer>().material = Mat(accent, 0f, 0.45f);
            Destroy(stripe.GetComponent<Collider>());
        }

        private IEnumerator PopIn(Transform target, Rigidbody body)
        {
            if (target == null) yield break;
            Vector3 finalScale = Vector3.one * 0.92f;
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
            for (int i = 0; i < mats.Count; i++)
                if (mats[i] != null) Destroy(mats[i]);
            mats.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (Material m in mats) if (m != null) Destroy(m);
        }
    }
}
