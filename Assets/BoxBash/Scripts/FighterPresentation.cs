using System.Collections;
using UnityEngine;

namespace BoxBash
{
    public sealed class FighterPresentation : MonoBehaviour
    {
        private ArenaFighter fighter;
        private Transform visual;
        private Transform groundShadow;
        private Renderer groundShadowRenderer;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private Coroutine reaction;
        private bool celebrating;
        private float shieldPulseUntil;
        private GameObject weightMarker;

        public void Bind(ArenaFighter owner, Transform visualRoot)
        {
            fighter = owner;
            visual = visualRoot;
            baseScale = visual.localScale;
            basePosition = visual.localPosition;
            groundShadow = transform.Find("Shadow");
            if (groundShadow != null) groundShadowRenderer = groundShadow.GetComponent<Renderer>();
        }

        private void Update()
        {
            UpdateGroundShadow();

            if (weightMarker != null && weightMarker.activeSelf)
            {
                weightMarker.transform.localPosition = new Vector3(0f, 2.05f + Mathf.Sin(Time.time * 8f) * 0.08f, 0f);
                weightMarker.transform.localRotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
            }

            if (visual == null || fighter == null || celebrating) return;
            float movement = fighter.MoveInput.magnitude;
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.045f * movement;
            float jumpLean = fighter.IsGrounded ? 0f : 0.04f;
            visual.localPosition = Vector3.Lerp(visual.localPosition, basePosition + Vector3.up * (bob + jumpLean), Time.deltaTime * 18f);
            if (reaction == null)
            {
                float stretch = 1f + movement * 0.018f;
                if (Time.time < shieldPulseUntil) stretch += Mathf.Sin(Time.time * 20f) * 0.018f;
                Vector3 locomotionScale = new Vector3(baseScale.x * stretch, baseScale.y / stretch, baseScale.z * stretch);
                visual.localScale = Vector3.Lerp(visual.localScale, locomotionScale, Time.deltaTime * 18f);
            }
        }

        private void UpdateGroundShadow()
        {
            if (groundShadow == null || fighter == null) return;
            Vector3 p = groundShadow.position;
            p.x = transform.position.x;
            p.y = 0.16f;
            p.z = transform.position.z;
            groundShadow.position = p;
            if (groundShadowRenderer != null)
                groundShadowRenderer.enabled = fighter.IsAlive && ArenaWorld.HasSafeFloor(transform.position, 0.82f);
        }

        public void Pickup() => Punch(new Vector3(1.08f, 0.91f, 1.08f), 0.13f);
        public void Throw() => Punch(new Vector3(0.86f, 1.16f, 0.86f), 0.16f);
        public void Hit() => Punch(new Vector3(1.18f, 0.80f, 1.18f), 0.17f);
        public void Jump() => Punch(new Vector3(0.90f, 1.12f, 0.90f), 0.15f);
        public void Kick() => Punch(new Vector3(1.10f, 0.93f, 0.90f), 0.13f);
        public void Shield(float duration) => shieldPulseUntil = Time.time + duration;
        public void Flatten() => Punch(new Vector3(1.36f, 0.54f, 1.36f), 0.52f);

        public void SetWeight(bool active)
        {
            if (active && weightMarker == null)
            {
                weightMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                weightMarker.name = "Weight Countdown Marker";
                weightMarker.transform.SetParent(transform, false);
                weightMarker.transform.localScale = new Vector3(0.46f, 0.24f, 0.46f);
                Renderer renderer = weightMarker.GetComponent<Renderer>();
                renderer.material.color = new Color(0.34f, 0.37f, 0.42f);
                Destroy(weightMarker.GetComponent<Collider>());

                GameObject label = new GameObject("500");
                label.transform.SetParent(weightMarker.transform, false);
                label.transform.localPosition = new Vector3(0f, 0f, -0.56f);
                label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                TextMesh mesh = label.AddComponent<TextMesh>();
                mesh.text = "500";
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.fontSize = 48;
                mesh.characterSize = 0.13f;
                mesh.color = Color.white;
            }
            if (weightMarker != null) weightMarker.SetActive(active);
        }

        public void Eliminate()
        {
            if (visual == null) return;
            if (weightMarker != null) weightMarker.SetActive(false);
            if (groundShadowRenderer != null) groundShadowRenderer.enabled = false;
            StopAllCoroutines();
            StartCoroutine(EliminationRoutine());
        }

        public void Celebrate()
        {
            if (visual == null) return;
            StopAllCoroutines();
            celebrating = true;
            StartCoroutine(CelebrationRoutine());
        }

        private void Punch(Vector3 multiplier, float duration)
        {
            if (visual == null || celebrating) return;
            if (reaction != null) StopCoroutine(reaction);
            reaction = StartCoroutine(PunchRoutine(multiplier, duration));
        }

        private IEnumerator PunchRoutine(Vector3 multiplier, float duration)
        {
            Vector3 target = Vector3.Scale(baseScale, multiplier);
            float half = duration * 0.42f;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                visual.localScale = Vector3.Lerp(visual.localScale, target, Mathf.Clamp01(t / half));
                yield return null;
            }
            t = 0f;
            while (t < duration - half)
            {
                t += Time.deltaTime;
                visual.localScale = Vector3.Lerp(target, baseScale, Mathf.Clamp01(t / Mathf.Max(0.01f, duration - half)));
                yield return null;
            }
            visual.localScale = baseScale;
            reaction = null;
        }

        private IEnumerator EliminationRoutine()
        {
            float t = 0f;
            const float duration = 0.34f;
            Vector3 start = visual.localScale;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                visual.localScale = Vector3.Lerp(start, baseScale * 0.16f, k);
                visual.localRotation = Quaternion.Euler(k * 20f, k * 320f, k * 55f);
                visual.localPosition = basePosition + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.62f;
                yield return null;
            }
            visual.gameObject.SetActive(false);
        }

        private IEnumerator CelebrationRoutine()
        {
            float phase = 0f;
            while (true)
            {
                phase += Time.deltaTime * 8.6f;
                float jump = Mathf.Abs(Mathf.Sin(phase)) * 0.46f;
                float squash = 1f + Mathf.Sin(phase * 2f) * 0.055f;
                visual.localPosition = basePosition + Vector3.up * jump;
                visual.localScale = new Vector3(baseScale.x * squash, baseScale.y / squash, baseScale.z * squash);
                visual.localRotation = Quaternion.Euler(0f, phase * 30f, 0f);
                yield return null;
            }
        }
    }
}
