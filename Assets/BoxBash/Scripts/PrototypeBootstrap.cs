using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBash
{
    public sealed partial class PrototypeBootstrap : MonoBehaviour
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
        private const float Tile = 1.28f;

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
            session = new GameObject("SPACE CRATE BASH - Session").transform;
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

            CreateSpaceBackdrop();
            CreateArena();
            CreateFighters();
            CreateCrates();
        }

        private Camera CreateCamera()
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.SetParent(session);
            Camera cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            cam.transform.position = new Vector3(0f, 13.8f, -12.2f);
            cam.transform.rotation = Quaternion.Euler(51.8f, 0f, 0f);
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.035f, 0.20f);
            cam.allowHDR = true;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 90f;
            return cam;
        }

        private void CreateLighting()
        {
            RenderSettings.ambientLight = new Color(0.36f, 0.31f, 0.48f);
            GameObject sun = new GameObject("Cool Rooftop Key");
            sun.transform.SetParent(session);
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(0.76f, 0.84f, 1f);
            l.intensity = 1.18f;
            l.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -34f, 0f);

            GameObject rim = new GameObject("Neon Arena Rim");
            rim.transform.SetParent(session);
            Light r = rim.AddComponent<Light>();
            r.type = LightType.Point;
            r.color = new Color(1f, 0.26f, 0.58f);
            r.range = 18f;
            r.intensity = 1.18f;
            rim.transform.position = new Vector3(0f, 5.2f, 5f);
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

        private Material GlowMat(Color color)
        {
            Material m = Mat(color, 0.05f, 0.5f);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 1.5f);
            }
            return m;
        }

        private void CreateSpaceBackdrop()
        {
            Material sky = Mat(new Color(0.14f, 0.035f, 0.22f), 0f, 0.08f);
            GameObject skyWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skyWall.name = "Purple Skyline Backdrop";
            skyWall.transform.SetParent(session);
            skyWall.transform.position = new Vector3(0f, 8f, 35f);
            skyWall.transform.localScale = new Vector3(52f, 26f, 0.4f);
            skyWall.GetComponent<Renderer>().material = sky;
            Destroy(skyWall.GetComponent<Collider>());

            Material buildingA = Mat(new Color(0.045f, 0.050f, 0.085f), 0.18f, 0.22f);
            Material buildingB = Mat(new Color(0.070f, 0.050f, 0.105f), 0.12f, 0.18f);
            Material windowCyan = GlowMat(new Color(0.18f, 0.82f, 1f));
            Material windowPink = GlowMat(new Color(1f, 0.23f, 0.66f));
            Material windowWarm = GlowMat(new Color(1f, 0.68f, 0.18f));

            Random.State state = Random.state;
            Random.InitState(20412);
            for (int i = 0; i < 24; i++)
            {
                float width = Random.Range(1.2f, 3.2f);
                float depth = Random.Range(1.3f, 3.8f);
                float height = Random.Range(4.0f, 13.0f);
                float x = Mathf.Lerp(-20f, 20f, i / 23f) + Random.Range(-0.85f, 0.85f);
                float z = Random.Range(17f, 29f);

                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "Future Tower";
                building.transform.SetParent(session);
                building.transform.position = new Vector3(x, height * 0.5f - 3.6f, z);
                building.transform.localScale = new Vector3(width, height, depth);
                building.GetComponent<Renderer>().material = (i % 2 == 0) ? buildingA : buildingB;
                Destroy(building.GetComponent<Collider>());

                Material windowMat = i % 3 == 0 ? windowPink : (i % 3 == 1 ? windowCyan : windowWarm);
                int rows = Mathf.Clamp(Mathf.RoundToInt(height / 2.2f), 2, 5);
                for (int row = 0; row < rows; row++)
                {
                    GameObject windows = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    windows.name = "Neon Windows";
                    windows.transform.SetParent(building.transform, false);
                    float localY = -0.38f + (row + 0.75f) / rows * 0.76f;
                    windows.transform.localPosition = new Vector3(0f, localY, -0.505f);
                    windows.transform.localScale = new Vector3(0.72f, 0.045f, 0.025f);
                    windows.GetComponent<Renderer>().material = windowMat;
                    Destroy(windows.GetComponent<Collider>());
                }

                if (i % 5 == 0)
                {
                    GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    mast.name = "Tower Mast";
                    mast.transform.SetParent(building.transform, false);
                    mast.transform.localPosition = new Vector3(0f, 0.62f, 0f);
                    mast.transform.localScale = new Vector3(0.035f, 0.28f, 0.035f);
                    mast.GetComponent<Renderer>().material = windowPink;
                    Destroy(mast.GetComponent<Collider>());
                }
            }
            Random.state = state;
        }
    }
}
