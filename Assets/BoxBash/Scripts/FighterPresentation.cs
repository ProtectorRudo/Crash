using System.Collections;
using UnityEngine;

namespace BoxBash
{
    /// <summary>Purely visual motion. Never changes the fighter collider or gameplay transform scale.</summary>
    public sealed class FighterPresentation : MonoBehaviour
    {
        private ArenaFighter fighter;
        private Transform visual;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private Coroutine reaction;
        private bool celebrating;

        public void Bind(ArenaFighter owner, Transform visualRoot)
        {
            fighter = owner;
            visual = visualRoot;
            baseScale = visual.localScale;
            basePosition = visual.localPosition;
        }

        private void Update()
        {
            if (visual == null || fighter == null || celebrating) return;
            float movement = fighter.MoveInput.magnitude;
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 11f)) * 0.055f * movement;
            visual.localPosition = Vector3.Lerp(visual.localPosition, basePosition + Vector3.up * bob, Time.deltaTime * 18f);
            if (reaction == null)
            {
                float stretch = 1f + movement * 0.025f;
                Vector3 locomotionScale = new Vector3(baseScale.x * stretch, baseScale.y / stretch, baseScale.z * stretch);
                visual.localScale = Vector3.Lerp(visual.localScale, locomotionScale, Time.deltaTime * 16f);
            }
        }

        public void Pickup() => Punch(new Vector3(1.10f, 0.90f, 1.10f), 0.15f);
        public void Throw() => Punch(new Vector3(0.88f, 1.14f, 0.88f), 0.18f);
        public void Hit() => Punch(new Vector3(1.16f, 0.82f, 1.16f), 0.18f);

        public void Eliminate()
        {
            if (visual == null) return;
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
            const float duration = 0.28f;
            Vector3 start = visual.localScale;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                visual.localScale = Vector3.Lerp(start, baseScale * 0.18f, k);
                visual.localRotation = Quaternion.Euler(0f, k * 260f, k * 48f);
                visual.localPosition = basePosition + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.55f;
                yield return null;
            }
            visual.gameObject.SetActive(false);
        }

        private IEnumerator CelebrationRoutine()
        {
            float phase = 0f;
            while (true)
            {
                phase += Time.deltaTime * 8f;
                float jump = Mathf.Abs(Mathf.Sin(phase)) * 0.42f;
                float squash = 1f + Mathf.Sin(phase * 2f) * 0.06f;
                visual.localPosition = basePosition + Vector3.up * jump;
                visual.localScale = new Vector3(baseScale.x * squash, baseScale.y / squash, baseScale.z * squash);
                visual.localRotation = Quaternion.Euler(0f, phase * 26f, 0f);
                yield return null;
            }
        }
    }
}
