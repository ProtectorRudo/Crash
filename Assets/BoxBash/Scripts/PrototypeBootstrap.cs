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
            cam.transform.position = new Vector3(0f, 13.4f, -11.2f);
            cam.transform.rotation = Quaternion.Euler(50.5f, 0f, 0f);
            cam.fieldOfView = 42f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.018f, 0.048f);
            cam.allowHDR = true;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 80f;
            return cam;
        }

        private void CreateLighting()
        {
            RenderSettings.ambientLight = new Color(0.34f, 0.39f, 0.53f);
            GameObject sun = new GameObject("Cold Key Light");
            sun.transform.SetParent(session);
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.22f;
            l.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -34f, 0f);

            GameObject rim = new GameObject("Warm Arena Rim");
            rim.transform.SetParent(session);
            Light r = rim.AddComponent<Light>();
            r.type = LightType.Point;
            r.color = new Color(1f, 0.42f, 0.16f);
            r.range = 17f;
            r.intensity = 1.25f;
            rim.transform.position = new Vector3(0f, 4.2f, 2f);
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
            Material stars = GlowMat(new Color(0.74f, 0.88f, 1f));
            Random.State state = Random.state;
            Random.InitState(9421);
            for (int i = 0; i < 46; i++)
            {
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = "Star";
                star.transform.SetParent(session);
                star.transform.position = new Vector3(Random.Range(-18f, 18f), Random.Range(3f, 15f), Random.Range(7f, 31f));
                float scale = Random.Range(0.025f, 0.075f);
                star.transform.localScale = Vector3.one * scale;
                star.GetComponent<Renderer>().material = stars;
                Destroy(star.GetComponent<Collider>());
            }
            Random.state = state;

            GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "Distant Planet";
            planet.transform.SetParent(session);
            planet.transform.position = new Vector3(9.2f, 7.8f, 23f);
            planet.transform.localScale = Vector3.one * 4.5f;
            planet.GetComponent<Renderer>().material = Mat(new Color(0.22f, 0.30f, 0.52f), 0f, 0.34f);
            Destroy(planet.GetComponent<Collider>());
        }
    }
}
