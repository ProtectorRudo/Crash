using UnityEngine;

namespace BoxBash
{
    public static class BoxBashRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (Object.FindObjectOfType<PrototypeBootstrap>() != null) return;
            var go = new GameObject("BoxBash Runtime Bootstrap");
            go.AddComponent<PrototypeBootstrap>();
        }
    }
}
