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
            result = winner == Human ? "¡GANASTE!" : (Human != null && Human.IsAlive ? "PERDISTE" : "ELIMINADO");
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
            DrawFighterHud(s);
            DrawTimer(s);

            if (state == MatchState.Countdown)
            {
                GUI.skin.box.fontSize = Mathf.RoundToInt(68 * s);
                string text = countdownRemaining > 0.55f ? Mathf.CeilToInt(countdownRemaining).ToString() : "¡YA!";
                GUI.Box(new Rect(Screen.width * 0.37f, Screen.height * 0.34f, Screen.width * 0.26f, Screen.height * 0.16f), text);
            }

            if (state == MatchState.Playing && Time.time - matchStartedAt < 5.0f)
            {
                GUI.skin.label.fontSize = Mathf.RoundToInt(20 * s);
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(Screen.width * 0.12f, Screen.height - 58f * s, Screen.width * 0.76f, 38f * s),
                    "Arrastrá: mover   •   toque: saltar   •   doble toque: agarrar/lanzar   •   flick: patear");
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
            float w = 104f * s;
            float h = 58f * s;
            Rect rect = new Rect((Screen.width - w) * 0.5f, 10f * s, w, h);
            GUI.color = new Color(0.055f, 0.040f, 0.075f, 0.96f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = new Color(1f, 0.52f, 0.08f, 1f);
            GUI.skin.label.fontSize = Mathf.RoundToInt(36 * s);
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            string time = state == MatchState.Countdown ? "90" : Mathf.CeilToInt(remaining).ToString("00");
            GUI.Label(rect, time);
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = Color.white;
        }

        private void DrawFighterHud(float s)
        {
            float gap = 7f * s;
            float centerGap = 122f * s;
            float outer = 10f * s;
            float available = Screen.width - outer * 2f - centerGap - gap * 2f;
            float w = Mathf.Min(218f * s, available * 0.25f);
            float h = 60f * s;
            float left1 = outer;
            float left2 = left1 + w + gap;
            float right2 = Screen.width - outer - w;
            float right1 = right2 - gap - w;
            float[] xs = { left1, left2, right1, right2 };

            for (int i = 0; i < fighters.Count && i < 4; i++)
            {
                ArenaFighter f = fighters[i];
                if (f == null) continue;
                DrawOneHud(f, new Rect(xs[i], 10f * s, w, h), s);
            }
        }

        private void DrawOneHud(ArenaFighter f, Rect rect, float s)
        {
            GUI.color = new Color(0.035f, 0.040f, 0.060f, 0.94f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;

            float portrait = 34f * s;
            Rect portraitRect = new Rect(rect.x + 7f * s, rect.y + 7f * s, portrait, portrait);
            GUI.color = f.IsAlive ? f.playerColor : new Color(0.22f, 0.22f, 0.24f);
            GUI.DrawTexture(portraitRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.skin.label.fontSize = Mathf.RoundToInt(15 * s);
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.Label(new Rect(portraitRect.xMax + 7f * s, rect.y + 3f * s, rect.width - portrait - 18f * s, 21f * s),
                f.displayName + (f.IsAlive ? "" : "  X"));
            GUI.skin.label.fontStyle = FontStyle.Normal;

            Rect back = new Rect(portraitRect.xMax + 7f * s, rect.y + 27f * s, rect.width - portrait - 18f * s, 12f * s);
            GUI.color = new Color(0.10f, 0.10f, 0.12f, 1f);
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = f.IsAlive ? new Color(0.35f, 0.95f, 0.22f, 1f) : new Color(0.22f, 0.22f, 0.22f, 1f);
            float ratio = Mathf.Clamp01(f.Health / Mathf.Max(1f, f.maxHealth));
            GUI.DrawTexture(new Rect(back.x, back.y, back.width * ratio, back.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(f.StatusText))
            {
                GUI.skin.label.fontSize = Mathf.RoundToInt(11 * s);
                GUI.Label(new Rect(portraitRect.xMax + 7f * s, rect.y + 40f * s, rect.width - portrait - 18f * s, 16f * s), f.StatusText);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < fighters.Count; i++)
                if (fighters[i] != null) fighters[i].Eliminated -= OnEliminated;
        }
    }
}
