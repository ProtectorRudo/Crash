using System.Collections.Generic;
using UnityEngine;

namespace BoxBash
{
    public sealed class MatchManager : MonoBehaviour
    {
        private enum MatchState { Countdown, Playing, Finished }

        public float matchSeconds = 75f;
        public float countdownSeconds = 2.4f;
        public IReadOnlyList<ArenaFighter> Fighters => fighters;
        public ArenaFighter Human { get; private set; }
        public bool Finished => state == MatchState.Finished;

        private readonly List<ArenaFighter> fighters = new List<ArenaFighter>();
        private MatchState state = MatchState.Countdown;
        private float remaining;
        private float countdownRemaining;
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
            {
                PrototypeBootstrap.Instance?.RestartPrototype();
            }
        }

        private void BeginMatch()
        {
            state = MatchState.Playing;
            remaining = matchSeconds;
            foreach (ArenaFighter fighter in fighters) if (fighter != null && fighter.IsAlive) fighter.SetControlEnabled(true);
            crateDirector?.SetActive(true);
        }

        private void OnEliminated(ArenaFighter eliminated)
        {
            if (state != MatchState.Playing) return;
            int alive = 0;
            ArenaFighter survivor = null;
            foreach (ArenaFighter fighter in fighters)
            {
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
            foreach (ArenaFighter fighter in fighters)
            {
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
            result = winner == Human ? "¡VICTORIA!" : "DERROTA";
            winner?.Celebrate();
            crateDirector?.SetActive(false);
            foreach (ArenaFighter f in fighters) if (f != null) f.SetControlEnabled(false);
            canRestartAt = Time.unscaledTime + 0.45f;
        }

        private bool RestartPressed()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(0)) return true;
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }

        private int AliveCount()
        {
            int alive = 0;
            for (int i = 0; i < fighters.Count; i++) if (fighters[i] != null && fighters[i].IsAlive) alive++;
            return alive;
        }

        private void OnGUI()
        {
            float scale = Mathf.Clamp(Screen.width / 1080f, 0.68f, 1.25f);
            GUI.skin.label.fontSize = Mathf.RoundToInt(30 * scale);
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.box.fontSize = Mathf.RoundToInt(34 * scale);

            string time = Mathf.CeilToInt(remaining).ToString("00");
            GUI.Box(new Rect(Screen.width * 0.42f, 18f, Screen.width * 0.16f, 58f * scale), state == MatchState.Countdown ? "--" : time);

            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(Screen.width - 260f * scale, 20f, 230f * scale, 44f), $"VIVOS  {AliveCount()}/4");

            if (Human != null)
            {
                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                GUI.Label(new Rect(24f, 20f, 340f, 44f), $"VIDA  {Mathf.CeilToInt(Human.Health)}");
                if (state == MatchState.Playing)
                    GUI.Label(new Rect(24f, 62f, 520f, 44f), Human.CarriedCrate == null ? "Doble toque: agarrar" : "Doble toque: lanzar");
            }

            if (state == MatchState.Countdown)
            {
                GUI.skin.box.fontSize = Mathf.RoundToInt(72 * scale);
                string text = countdownRemaining > 0.7f ? Mathf.CeilToInt(countdownRemaining).ToString() : "¡YA!";
                GUI.Box(new Rect(Screen.width * 0.34f, Screen.height * 0.34f, Screen.width * 0.32f, Screen.height * 0.18f), text);
            }

            if (state == MatchState.Finished)
            {
                GUI.skin.box.fontSize = Mathf.RoundToInt(54 * scale);
                GUI.Box(new Rect(Screen.width * 0.2f, Screen.height * 0.36f, Screen.width * 0.6f, Screen.height * 0.20f), result);
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(Screen.width * 0.15f, Screen.height * 0.58f, Screen.width * 0.7f, 80f), "TOCÁ PARA REVANCHA");
            }
        }

        private void OnDestroy()
        {
            foreach (ArenaFighter fighter in fighters)
                if (fighter != null) fighter.Eliminated -= OnEliminated;
        }
    }
}
