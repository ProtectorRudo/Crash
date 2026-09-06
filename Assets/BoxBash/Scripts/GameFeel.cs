using System.Collections;
using UnityEngine;

namespace BoxBash
{
    public sealed class GameFeel : MonoBehaviour
    {
        private Camera cam;
        private Vector3 baseLocalPosition;
        private Coroutine shakeRoutine;

        public void Bind(Camera target)
        {
            cam = target;
            if (cam != null) baseLocalPosition = cam.transform.localPosition;
        }

        public void Pickup(Vector3 position) => SpawnBurst(position, 7, 0.07f, 1.8f, 0.28f);

        public void Throw(Vector3 position)
        {
            SpawnBurst(position, 6, 0.06f, 1.6f, 0.22f);
            Shake(0.045f, 0.05f);
        }

        public void Hit(Vector3 position, float intensity)
        {
            SpawnBurst(position, 11, 0.11f, 2.8f, 0.5f);
            Shake(intensity, 0.10f);
        }

        public void CrateBurst(Vector3 position, CrateKind kind)
        {
            SpawnBurst(position, 14, 0.14f, 3.4f, 0.65f);
            Shake(0.18f, 0.08f);
        }

        public void TileWarning(Vector3 position) => SpawnBurst(position + Vector3.up * 0.18f, 5, 0.07f, 1.1f, 0.2f);

        public void Elimination(Vector3 position)
        {
            SpawnBurst(position, 28, 0.18f, 4.8f, 0.72f);
            Shake(0.38f, 0.16f);
        }

        public void Explosion(Vector3 position, float intensity)
        {
            SpawnBurst(position, 38, 0.20f, 6.7f, 0.9f);
            Shake(intensity, 0.22f);
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private void Shake(float intensity, float duration)
        {
            if (cam == null) return;
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float fade = 1f - Mathf.Clamp01(t / duration);
                cam.transform.localPosition = baseLocalPosition + Random.insideUnitSphere * intensity * fade;
                yield return null;
            }
            cam.transform.localPosition = baseLocalPosition;
            shakeRoutine = null;
        }

        private void SpawnBurst(Vector3 position, int count, float size, float speed, float lifetime)
        {
            GameObject go = new GameObject("FX Burst");
            go.transform.position = position + Vector3.up * 0.5f;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;
            ps.Emit(count);
            Destroy(go, lifetime + 0.25f);
        }
    }
}
