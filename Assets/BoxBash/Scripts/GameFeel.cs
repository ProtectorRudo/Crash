using System.Collections;
using UnityEngine;

namespace BoxBash
{
    public sealed class GameFeel : MonoBehaviour
    {
        private Camera cam;
        private Vector3 baseLocalPosition;
        private Coroutine shakeRoutine;
        private AudioSource audioSource;
        private AudioClip pickupClip;
        private AudioClip throwClip;
        private AudioClip hitClip;
        private AudioClip explosionClip;
        private AudioClip jumpClip;
        private AudioClip powerupClip;

        public void Bind(Camera target)
        {
            cam = target;
            if (cam != null) baseLocalPosition = cam.transform.localPosition;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.48f;
            pickupClip = Tone("pickup", 620f, 0.07f, 0.26f);
            throwClip = Tone("throw", 260f, 0.06f, 0.23f);
            hitClip = Noise("hit", 0.07f, 0.34f);
            explosionClip = Noise("explosion", 0.18f, 0.55f);
            jumpClip = Tone("jump", 410f, 0.06f, 0.20f);
            powerupClip = Tone("power", 850f, 0.11f, 0.28f);
        }

        public void Pickup(Vector3 position)
        {
            SpawnBurst(position, 7, 0.07f, 1.8f, 0.28f, new Color(1f, 0.78f, 0.30f));
            Play(pickupClip, 0.8f);
        }

        public void Throw(Vector3 position)
        {
            SpawnBurst(position, 6, 0.06f, 1.6f, 0.22f, new Color(0.75f, 0.90f, 1f));
            Shake(0.035f, 0.05f);
            Play(throwClip, 0.72f);
        }

        public void Jump(Vector3 position)
        {
            SpawnBurst(position + Vector3.down * 0.2f, 5, 0.06f, 1.1f, 0.18f, new Color(0.75f, 0.82f, 0.92f));
            Play(jumpClip, 0.65f);
        }

        public void Kick(Vector3 position)
        {
            SpawnBurst(position, 8, 0.07f, 2.2f, 0.24f, new Color(1f, 0.87f, 0.40f));
            Shake(0.055f, 0.055f);
        }

        public void Hit(Vector3 position, float intensity)
        {
            SpawnBurst(position, 12, 0.11f, 2.9f, 0.48f, new Color(1f, 0.48f, 0.24f));
            Shake(intensity, 0.10f);
            Play(hitClip, 0.82f);
        }

        public void CrateBurst(Vector3 position, CrateKind kind)
        {
            Color color = kind == CrateKind.Heavy ? new Color(0.65f, 0.70f, 0.78f) : new Color(0.92f, 0.62f, 0.24f);
            SpawnBurst(position, 14, 0.14f, 3.4f, 0.65f, color);
            Shake(0.16f, 0.08f);
        }

        public void GiftOpen(Vector3 position)
        {
            SpawnBurst(position, 20, 0.10f, 3.4f, 0.55f, new Color(0.95f, 0.38f, 1f));
            Play(powerupClip, 0.82f);
        }

        public void Powerup(Vector3 position, PowerupKind kind)
        {
            SpawnBurst(position, 18, 0.10f, 3.0f, 0.50f, new Color(0.42f, 1f, 0.86f));
            Play(powerupClip, 0.88f);
        }

        public void ShieldBlock(Vector3 position)
        {
            SpawnBurst(position, 14, 0.08f, 2.7f, 0.35f, new Color(0.30f, 0.88f, 1f));
            Shake(0.08f, 0.07f);
        }

        public void HeavyImpact(Vector3 position)
        {
            SpawnBurst(position, 22, 0.13f, 3.2f, 0.55f, new Color(0.70f, 0.72f, 0.78f));
            Shake(0.34f, 0.16f);
            Play(hitClip, 1f);
        }

        public void TileWarning(Vector3 position) => SpawnBurst(position + Vector3.up * 0.18f, 7, 0.07f, 1.2f, 0.2f, new Color(1f, 0.32f, 0.12f));

        public void Elimination(Vector3 position)
        {
            SpawnBurst(position, 28, 0.18f, 4.8f, 0.72f, new Color(1f, 0.75f, 0.26f));
            Shake(0.38f, 0.16f);
        }

        public void Explosion(Vector3 position, float intensity)
        {
            SpawnBurst(position, 42, 0.20f, 6.8f, 0.90f, new Color(1f, 0.36f, 0.12f));
            Shake(intensity, 0.22f);
            Play(explosionClip, 0.95f);
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private void Play(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, volume);
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

        private void SpawnBurst(Vector3 position, int count, float size, float speed, float lifetime, Color color)
        {
            GameObject go = new GameObject("FX Burst");
            go.transform.position = position + Vector3.up * 0.5f;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
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

        private void OnDestroy()
        {
            if (pickupClip != null) Destroy(pickupClip);
            if (throwClip != null) Destroy(throwClip);
            if (hitClip != null) Destroy(hitClip);
            if (explosionClip != null) Destroy(explosionClip);
            if (jumpClip != null) Destroy(jumpClip);
            if (powerupClip != null) Destroy(powerupClip);
        }

        private AudioClip Tone(string name, float frequency, float seconds, float amplitude)
        {
            const int rate = 22050;
            int count = Mathf.Max(64, Mathf.RoundToInt(rate * seconds));
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float fade = 1f - (float)i / count;
                data[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * amplitude * fade;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip Noise(string name, float seconds, float amplitude)
        {
            const int rate = 22050;
            int count = Mathf.Max(64, Mathf.RoundToInt(rate * seconds));
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float fade = 1f - (float)i / count;
                data[i] = Random.Range(-1f, 1f) * amplitude * fade * fade;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
