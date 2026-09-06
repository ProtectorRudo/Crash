using UnityEngine;

namespace BoxBash
{
    [RequireComponent(typeof(ArenaFighter))]
    public sealed class ThrowGuide : MonoBehaviour
    {
        public int points = 12;
        public float previewSeconds = 0.75f;
        public float width = 0.045f;

        private ArenaFighter fighter;
        private LineRenderer line;

        private void Awake()
        {
            fighter = GetComponent<ArenaFighter>();
            line = gameObject.AddComponent<LineRenderer>();
            line.enabled = false;
            line.useWorldSpace = true;
            line.positionCount = points;
            line.startWidth = width;
            line.endWidth = width * 0.35f;
            line.numCapVertices = 4;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) line.material = new Material(shader);
            line.startColor = new Color(0.85f, 0.95f, 1f, 0.75f);
            line.endColor = new Color(0.85f, 0.95f, 1f, 0.08f);
        }

        private void Update()
        {
            bool show = fighter.IsAlive && fighter.ControlsEnabled && fighter.CarriedCrate != null;
            line.enabled = show;
            if (!show) return;

            Vector3 origin = transform.position + Vector3.up * 1.35f + fighter.GetAssistedThrowDirection() * 0.65f;
            Vector3 velocity = fighter.GetAssistedThrowDirection() * fighter.throwSpeed + Vector3.up * fighter.throwLift;
            for (int i = 0; i < points; i++)
            {
                float t = previewSeconds * i / Mathf.Max(1, points - 1);
                Vector3 p = origin + velocity * t + Physics.gravity * (0.5f * t * t);
                line.SetPosition(i, p);
            }
        }

        private void OnDestroy()
        {
            if (line != null && line.material != null) Destroy(line.material);
        }
    }
}
