using System.Collections.Generic;
using UnityEngine;

namespace BoxBash
{
    public sealed class MatchManager : MonoBehaviour
    {
        private enum MatchState { Countdown, Playing, Finished }

        public float matchSeconds = SpaceBashTuning.MatchSeconds;
        public float countdownSeconds = SpaceBashTuning.CountdownSeconds;
        public IReadOnlyList<ArenaFighter> Fighters => fighters;
        public ArenaFighter Human { get; private set; }
        public bool Finished => state == MatchState.Finished;
        public float Remaining => remaining;

        private readonly List<ArenaFighter> fighters = new List<ArenaFighter>();
        private MatchState state = MatchState.Countdown;
        private float remaining;
        private float countdownRemaining;
        private float matchStartedAt;
        private string result = string.Empty;
        private float canRestartAt;
        private CrateDirector crateDirector;

        public void Bind(CrateDirector director)
        {
            crateDirector = director;
            remaining = matchSeconds;
            countdownRemaining = countdownSeconds;
        }

        public void Register(ArenaFighter fighter)
        {
            if (fighter == null || fighters.Contains(fighter)) return;
            fighters.Add(fighter);
            fighter.SetControlEnabled(false);
            if (fighter.IsHuman) Human = fighter;
            fighter.Eliminated += OnEliminated;
        }

        private void Update()
        {
            if (state == MatchState.Countdown)
            {
                countdownRemaining -= Time.deltaTime;
                if (countdownRemaining <= 0f) BeginMatch();
                return;
            }

            if (state == MatchState.Playing)
            {
                remaining = Mathf.Max(0f, remaining - Time.deltaTime);
                if (remaining <= 0f) FinishByHealth();
                return;
            }

            if (state == MatchState.Finished && Time.unscaledTime >= canRestartAt && RestartPressed())
                PrototypeBootstrap.Instance?.RestartPrototype();
        }

        private void BeginMatch()
        {
            state = MatchState.Playing;
            remaining = matchSeconds;
            matchStartedAt = Time.time;
            for (int i = 0; i < fighters.Count; i++)
                if (fighters[i] != null && fighters[i].IsAlive) fighters[i].SetControlEnabled(true);
            crateDirector?.SetActive(true);
        }

        private void OnEliminated(ArenaFighter eliminated)
        {
            if (state != MatchState.Playing) return;
            int alive = 0;
            ArenaFighter survivor = null;
            for (int i = 0; i < fighters.Count; i++)
            {
                ArenaFighter fighter = fighters[i];
                if (fighter == null || !fighter.IsAlive) continue;
                alive++;
                survivor = fighter;
            }
            if (alive <= 1) Finish(survivor);
        }

        private void FinishByHealth()
        {
            ArenaFighter best = null;
            float bestHealth = -1f;
            for (int i = 0; i < fighters.Count; i++)
            {
                ArenaFighter fighter = fighters[i];
                if (fighter == null || !fighter.IsAlive) continue;
                if (fighter.Health > bestHealth)
                {
                    bestHealth = fighter.Health;
                    best = fighter;
                }
            }
            Finish(best);
        }

        private void Finish(ArenaFighter winner)
        {
            if (state == MatchState.Finished) return;
            state = MatchState.Finished;
            result = winner == Human ? "¡GANASTE!" : "ELIMINADO";
            winner?.Celebrate();
            crateDirector?.SetActive(false);
            for (int i = 0; i < fighters.Count; i++) if (fighters[i] != null) fighters[i].SetControlEnabled(false);
            canRestartAt = Time.unscaledTime + 0.48f;
        }

        private bool RestartPressed()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(0)) return true;
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }

        private void OnGUI()
        {
            float s = Mathf.Clamp(Screen.width / 1080f, 0.66f, 1.35f);
            DrawTimer(s);
            DrawFighterHud(s);

            if (state == MatchState.Countdown)
            {
                GUI.skin.box.fontSize = Mathf.RoundToInt(68 * s);
                string text = countdownRemaining > 0.55f ? Mathf.CeilToInt(countdownRemaining).ToString() : "¡YA!";
                GUI.Box(new Rect(Screen.width * 0.37f, Screen.height * 0.34f, Screen.width * 0.26f, Screen.height * 0.16f), text);
            }

            if (state == MatchState.Playing && Time.time - matchStartedAt < 6.5f)
            {
                GUI.skin.label.fontSize = Mathf.RoundToInt(22 * s);
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(Screen.width * 0.15f, Screen.height - 64f * s, Screen.width * 0.70f, 42f * s),
                    "Arrastrá: mover   •   toque: saltar   •   doble toque: agarrar/lanzar   •   flick corto: patear");
            }

            if (state == MatchState.Finished)
            {
                GUI.skin.box.fontSize = Mathf.RoundToInt(52 * s);
                GUI.Box(new Rect(Screen.width * 0.23f, Screen.height * 0.36f, Screen.width * 0.54f, Screen.height * 0.19f), result);
                GUI.skin.label.fontSize = Mathf.RoundToInt(25 * s);
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(Screen.width * 0.18f, Screen.height * 0.57f, Screen.width * 0.64f, 70f * s), "TOCÁ PARA REVANCHA");
            }
        }

        private void DrawTimer(float s)
        {
            GUI.skin.box.fontSize = Mathf.RoundToInt(34 * s);
            string time = state == MatchState.Countdown ? "90" : Mathf.CeilToInt(remaining).ToString("00");
            GUI.Box(new Rect(Screen.width * 0.445f, 14f, Screen.width * 0.11f, 54f * s), time);
        }

        private void DrawFighterHud(float s)
        {
            for (int i = 0; i < fighters.Count; i++)
            {
                ArenaFighter f = fighters[i];
                if (f == null) continue;
                float w = 238f * s;
                float h = 54f * s;
                float x = (i % 2 == 0) ? 20f * s : Screen.width - w - 20f * s;
                float y = (i < 2) ? 14f : Screen.height - h - 14f * s;
                DrawOneHud(f, new Rect(x, y, w, h), s, i % 2 == 1);
            }
        }

        private void DrawOneHud(ArenaFighter f, Rect rect, float s, bool rightAligned)
        {
            GUI.color = new Color(0.06f, 0.07f, 0.10f, 0.90f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;

            GUI.skin.label.fontSize = Mathf.RoundToInt(18 * s);
            GUI.skin.label.alignment = rightAligned ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            GUI.Label(new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, 22f * s), f.displayName + (f.IsAlive ? "" : "  X"));

            Rect back = new Rect(rect.x + 9f, rect.y + 29f * s, rect.width - 18f, 13f * s);
            GUI.color = new Color(0.16f, 0.17f, 0.19f, 1f);
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = f.IsAlive ? f.playerColor : new Color(0.25f, 0.25f, 0.25f);
            float ratio = Mathf.Clamp01(f.Health / Mathf.Max(1f, f.maxHealth));
            GUI.DrawTexture(new Rect(back.x, back.y, back.width * ratio, back.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(f.StatusText))
            {
                GUI.skin.label.fontSize = Mathf.RoundToInt(12 * s);
                GUI.Label(new Rect(rect.x + 8f, rect.y + 39f * s, rect.width - 16f, 14f * s), f.StatusText);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < fighters.Count; i++)
                if (fighters[i] != null) fighters[i].Eliminated -= OnEliminated;
        }
    }
}
