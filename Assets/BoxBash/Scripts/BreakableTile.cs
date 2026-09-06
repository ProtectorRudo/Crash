using System.Collections;
using UnityEngine;

namespace BoxBash
{
    public sealed class BreakableTile : MonoBehaviour
    {
        public bool IsBroken { get; private set; }
        private Vector3 startScale;

        private void Awake()
        {
            startScale = transform.localScale;
            ArenaWorld.Register(this);
        }

        private void OnDestroy() => ArenaWorld.Unregister(this);

        public void Blast()
        {
            if (IsBroken) return;
            IsBroken = true;
            StartCoroutine(BreakRoutine());
        }

        private IEnumerator BreakRoutine()
        {
            PrototypeBootstrap.Feel?.TileWarning(transform.position);
            const float warning = 0.12f;
            float w = 0f;
            while (w < warning)
            {
                w += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(w * 90f) * 0.055f;
                transform.localScale = new Vector3(startScale.x * pulse, startScale.y, startScale.z * pulse);
                yield return null;
            }

            const float duration = 0.18f;
            float t = 0f;
            Vector3 start = transform.localScale;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / duration);
                transform.localScale = new Vector3(start.x * Mathf.Lerp(0.45f, 1f, k), start.y * k, start.z * Mathf.Lerp(0.45f, 1f, k));
                transform.Rotate(0f, 210f * Time.deltaTime, 0f, Space.World);
                yield return null;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = false;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = false;
        }
    }
}
