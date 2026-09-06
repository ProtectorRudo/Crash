using UnityEngine;

namespace BoxBash
{
    [RequireComponent(typeof(ArenaFighter))]
    public sealed class PlayerTouchController : MonoBehaviour
    {
        public float dragDeadZonePixels = 18f;
        public float doubleTapWindow = 0.24f;
        public float maxDragPixels = 105f;
        public float tapMoveCancelPixels = 26f;
        public float flickMinPixels = 64f;
        public float flickMaxPixels = 170f;

        private ArenaFighter fighter;
        private Vector2 pointerStart;
        private bool pointerDown;
        private float pointerBeganAt;
        private bool tapPending;
        private float pendingTapAt;
        private float lastTapTime = -10f;

        private float PixelScale => Mathf.Clamp(Screen.height / 1080f, 0.68f, 1.45f);

        private void Awake() => fighter = GetComponent<ArenaFighter>();

        private void Update()
        {
            bool keyboardMoving = HandleKeyboard();

            if (tapPending && Time.unscaledTime >= pendingTapAt)
            {
                tapPending = false;
                fighter.Jump();
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                HandlePointer(touch.position, touch.phase == TouchPhase.Began,
                    touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled,
                    touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary);
            }
            else if (!keyboardMoving)
            {
                Vector2 pos = Input.mousePosition;
                HandlePointer(pos, Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0), Input.GetMouseButton(0));
                if (!pointerDown && !Input.GetMouseButton(0)) fighter.SetMoveInput(Vector2.zero);
            }
        }

        private bool HandleKeyboard()
        {
            Vector2 keyboard = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyboard.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyboard.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyboard.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyboard.y += 1f;
            if (keyboard.sqrMagnitude > 0.01f) fighter.SetMoveInput(Vector2.ClampMagnitude(keyboard, 1f));
            if (Input.GetKeyDown(KeyCode.Space)) fighter.ContextAction();
            if (Input.GetKeyDown(KeyCode.J)) fighter.Jump();
            if (Input.GetKeyDown(KeyCode.K)) fighter.Kick();
            return keyboard.sqrMagnitude > 0.01f;
        }

        private void HandlePointer(Vector2 position, bool began, bool ended, bool held)
        {
            float scale = PixelScale;
            float dragDeadZone = dragDeadZonePixels * scale;
            float maxDrag = maxDragPixels * scale;
            float tapCancel = tapMoveCancelPixels * scale;

            if (began)
            {
                pointerDown = true;
                pointerStart = position;
                pointerBeganAt = Time.unscaledTime;

                float now = Time.unscaledTime;
                if (now - lastTapTime <= doubleTapWindow)
                {
                    tapPending = false;
                    fighter.ContextAction();
                    lastTapTime = -10f;
                }
                else
                {
                    lastTapTime = now;
                    tapPending = true;
                    pendingTapAt = now + doubleTapWindow;
                }
            }

            if (pointerDown && held)
            {
                Vector2 delta = position - pointerStart;
                if (delta.magnitude > tapCancel)
                {
                    tapPending = false;
                    lastTapTime = -10f;
                }

                if (delta.magnitude < dragDeadZone) fighter.SetMoveInput(Vector2.zero);
                else fighter.SetMoveInput(Vector2.ClampMagnitude(delta / Mathf.Max(1f, maxDrag), 1f));
            }

            if (ended && pointerDown)
            {
                Vector2 delta = position - pointerStart;
                float distance = delta.magnitude;
                float heldFor = Time.unscaledTime - pointerBeganAt;
                pointerDown = false;
                fighter.SetMoveInput(Vector2.zero);

                bool deliberateFlick = heldFor >= 0.045f && heldFor < 0.22f &&
                                      distance >= flickMinPixels * scale && distance <= flickMaxPixels * scale;
                if (deliberateFlick)
                {
                    tapPending = false;
                    lastTapTime = -10f;
                    fighter.Kick();
                }
            }
        }
    }
}
