using UnityEngine;

namespace BoxBash
{
    [RequireComponent(typeof(ArenaFighter))]
    public sealed class PlayerTouchController : MonoBehaviour
    {
        public float dragDeadZonePixels = 18f;
        public float doubleTapWindow = 0.28f;
        public float maxDragPixels = 110f;

        private ArenaFighter fighter;
        private Vector2 pointerStart;
        private bool pointerDown;
        private float lastTapTime = -10f;

        private void Awake() => fighter = GetComponent<ArenaFighter>();

        private void Update()
        {
            Vector2 keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (keyboard.sqrMagnitude > 0.01f)
            {
                fighter.SetMoveInput(Vector2.ClampMagnitude(keyboard, 1f));
                if (Input.GetKeyDown(KeyCode.Space)) fighter.ContextAction();
                return;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                HandlePointer(touch.position, touch.phase == TouchPhase.Began,
                    touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled,
                    touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary);
            }
            else
            {
                Vector2 pos = Input.mousePosition;
                HandlePointer(pos, Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0), Input.GetMouseButton(0));
            }
        }

        private void HandlePointer(Vector2 position, bool began, bool ended, bool held)
        {
            if (began)
            {
                pointerDown = true;
                pointerStart = position;
                float now = Time.unscaledTime;
                if (now - lastTapTime <= doubleTapWindow)
                {
                    fighter.ContextAction();
                    lastTapTime = -10f;
                }
                else lastTapTime = now;
            }

            if (pointerDown && held)
            {
                Vector2 delta = position - pointerStart;
                if (delta.magnitude < dragDeadZonePixels) fighter.SetMoveInput(Vector2.zero);
                else fighter.SetMoveInput(Vector2.ClampMagnitude(delta / maxDragPixels, 1f));
            }

            if (ended)
            {
                pointerDown = false;
                fighter.SetMoveInput(Vector2.zero);
            }
        }
    }
}
