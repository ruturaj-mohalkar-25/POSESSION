using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Possession
{
    public sealed class TutorialCoach : MonoBehaviour
    {
        private TutorialGuide guide;
        private Canvas canvas;
        private RectTransform root, focusLayer, focusCard, action, liveRing, focusRing, toast, pingRing, hpFlash, expiryRing;
        private readonly RectTransform[] shades = new RectTransform[4];
        private readonly List<RectTransform> widgets = new();
        private readonly List<Vector3> originalScales = new();
        private Text focusTitle, focusBody, focusIcon, continueText, actionKey, actionTitle, actionArrow, toastText;
        private CanvasGroup actionAlpha, toastAlpha;
        private Transform focusBodyTarget, pingTarget;
        private string previousAction = "";
        private float focusBegan, actionBegan, toastBegan = -10f, healthBegan = -10f;
        private static readonly Color Gold = new(1f, 0.8f, 0.25f);
        private static readonly Color Green = new(0.4f, 1f, 0.6f);
        private static readonly Color Red = new(1f, 0.3f, 0.25f);
        private static readonly Color Dark = new(0.035f, 0.055f, 0.09f, 0.97f);

        public void Initialize(TutorialGuide tutorial)
        {
            guide = tutorial;
            var go = new GameObject("TutorialEffects", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            root = go.GetComponent<RectTransform>();
            liveRing = Ring(root, "ActionHalo", Gold);
            pingRing = Ring(root, "ObjectiveSuccessHalo", Green);
            expiryRing = Ring(root, "AutoEjectClockHalo", Red);
            expiryRing.gameObject.SetActive(false);
            hpFlash = Panel(root, "HealthDamageFlash", Vector2.zero, Vector2.one, Color.clear);
            hpFlash.gameObject.SetActive(false);

            action = Panel(root, "ActionPrompt", Vector2.zero, new Vector2(470f, 120f), Dark);
            actionAlpha = action.gameObject.AddComponent<CanvasGroup>();
            var cap = Panel(action, "KeyCap", new Vector2(-165f, 0f), new Vector2(104f, 94f), Gold);
            actionKey = Text(cap, "RequiredKey", "W", 42, Vector2.zero, new Vector2(100f, 86f), Dark);
            actionTitle = Text(action, "Action", "MOVE FORWARD", 27, new Vector2(55f, 18f), new Vector2(310f, 50f), Color.white);
            actionArrow = Text(action, "Pointer", "↑", 31, new Vector2(55f, -27f), new Vector2(310f, 40f), Gold);

            toast = Panel(root, "ActionSuccess", new Vector2(0f, 210f), new Vector2(720f, 70f), Dark);
            toastAlpha = toast.gameObject.AddComponent<CanvasGroup>();
            toastText = Text(toast, "SuccessText", "", 30, Vector2.zero, new Vector2(700f, 60f), Green);
            toast.gameObject.SetActive(false);

            focusLayer = CreateRect(root, "Spotlight", Vector2.zero, Vector2.zero);
            focusLayer.anchorMin = Vector2.zero; focusLayer.anchorMax = Vector2.one;
            focusLayer.offsetMin = focusLayer.offsetMax = Vector2.zero;
            for (int i = 0; i < shades.Length; i++)
            {
                shades[i] = Panel(focusLayer, "DimArea" + i, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.76f));
                shades[i].GetComponent<Image>().raycastTarget = true;
            }
            focusRing = Ring(focusLayer, "SpotlightHalo", Gold);
            focusCard = Panel(focusLayer, "CoachCard", new Vector2(170f, 100f), new Vector2(850f, 350f), Dark);
            focusCard.gameObject.AddComponent<TutorialPopIn>();
            var icon = Panel(focusCard, "FeatureBadge", new Vector2(-315f, 77f), new Vector2(150f, 80f), Gold);
            focusIcon = Text(icon, "FeatureKey", "TIME", 32, Vector2.zero, new Vector2(145f, 76f), Dark);
            focusTitle = Text(focusCard, "FeatureTitle", "BEAT THE CLOCK", 34, new Vector2(90f, 93f), new Vector2(580f, 75f), Gold);
            focusBody = Text(focusCard, "FeatureExplanation", "", 28, new Vector2(0f, -5f), new Vector2(780f, 130f), Color.white);
            focusBody.fontStyle = FontStyle.Normal;
            var next = Button(focusCard, "ContinueCoach", "GOT IT", new Vector2(-120f, -120f), new Vector2(400f, 62f), guide.ContinueCoaching, Gold);
            continueText = next.GetComponentInChildren<Text>();
            Button(focusCard, "CoachMenu", "MAIN MENU", new Vector2(270f, -120f), new Vector2(230f, 62f), guide.MainMenu, new Color(0.18f, 0.24f, 0.32f));
            focusLayer.gameObject.SetActive(false);
            action.gameObject.SetActive(false);
            liveRing.gameObject.SetActive(false); pingRing.gameObject.SetActive(false);
        }

        public void Show(TutorialFocus next)
        {
            RestoreWidgets();
            focusBegan = Time.unscaledTime;
            bool brief = next == TutorialFocus.LevelClock || next == TutorialFocus.Health;
            focusCard.sizeDelta = new Vector2(850f, brief ? 270f : 350f);
            focusBodyTarget = next == TutorialFocus.GuardAbility ? guide.Guard.transform
                : next == TutorialFocus.HeavyAbility ? guide.Heavy.transform : null;
            foreach (var widget in guide.FocusWidgets(next))
                if (widget != null) { widgets.Add(widget); originalScales.Add(widget.localScale); }
            focusIcon.text = next switch { TutorialFocus.LevelClock => "TIME", TutorialFocus.Health => "HP",
                TutorialFocus.PossessionClock => Mathf.CeilToInt(guide.Possession.PossessDuration) + "s", _ => "LMB" };
            focusTitle.text = next switch
            {
                TutorialFocus.LevelClock => "BEAT THE CLOCK",
                TutorialFocus.Health => "YOUR HEALTH",
                TutorialFocus.GuardAbility => "YOU ARE THE GUARD",
                TutorialFocus.HeavyAbility => "YOU ARE THE WALL BREAKER",
                _ => "AUTO-EJECT AT ZERO"
            };
            focusBody.text = next switch
            {
                TutorialFocus.LevelClock => "You have " + Mathf.CeilToInt(guide.LevelSeconds / 60f) + " minutes to reach the exit.",
                TutorialFocus.Health => "You have " + guide.Spirit.CurrentHP + " HP. Avoid hostile enemies.",
                TutorialFocus.GuardAbility => "Aim with the mouse. LMB shoots the target and opens the door.",
                TutorialFocus.HeavyAbility => "Move close → face the brick wall with WASD → LMB to smash.",
                _ => "At zero you leave the body automatically. The enemy attacks for 5s — use WASD to escape!"
            };
            continueText.text = next == TutorialFocus.GuardAbility ? "LET'S SHOOT" : next == TutorialFocus.HeavyAbility
                ? "LET'S SMASH" : next == TutorialFocus.PossessionClock ? "WATCH IT RUN OUT" : "GOT IT";
            focusIcon.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(-315f, brief ? 72f : 77f);
            focusTitle.rectTransform.anchoredPosition = new Vector2(90f, brief ? 72f : 93f);
            focusBody.rectTransform.anchoredPosition = new Vector2(0f, -5f);
            focusBody.rectTransform.sizeDelta = new Vector2(780f, brief ? 65f : 130f);
            foreach (var button in focusCard.GetComponentsInChildren<Button>())
            { var rect = (RectTransform)button.transform; rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, brief ? -87f : -120f); }
            focusLayer.gameObject.SetActive(true);
            focusCard.GetComponent<TutorialPopIn>().Play();
            action.gameObject.SetActive(false); liveRing.gameObject.SetActive(false);
        }

        public void Hide()
        {
            RestoreWidgets();
            if (focusLayer != null) focusLayer.gameObject.SetActive(false);
            previousAction = "";
        }

        private void OnDestroy() => RestoreWidgets();
        private void RestoreWidgets()
        {
            for (int i = 0; i < widgets.Count; i++) if (widgets[i] != null) widgets[i].localScale = originalScales[i];
            widgets.Clear(); originalScales.Clear();
        }

        public void Toast(string message, Transform target, Color? color = null)
        {
            if (toast == null) return;
            toastText.text = message; toastBegan = Time.unscaledTime; pingTarget = target;
            toastText.color = color ?? Green;
            pingRing.GetComponent<TutorialRingGraphic>().color = color ?? Green;
            toast.gameObject.SetActive(true);
        }
        public void PulseHealth() => healthBegan = Time.unscaledTime;

        private void LateUpdate()
        {
            if (guide == null || canvas == null) return;
            if (guide.IsCoaching) DrawSpotlight();
            else DrawAction();
            DrawFeedback();
            DrawExpiryWarning();
        }

        private void DrawSpotlight()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - focusBegan) / 0.45f);
            float zoom = Mathf.Lerp(1f, 1.32f, Mathf.SmoothStep(0f, 1f, t));
            Rect hole = new(-120f, -50f, 240f, 180f);
            bool found = false;
            for (int i = 0; i < widgets.Count; i++)
            {
                if (widgets[i] == null) continue;
                widgets[i].localScale = originalScales[i] * zoom;
                Rect rect = UiBounds(widgets[i]); hole = found ? Union(hole, rect) : rect; found = true;
            }
            if (focusBodyTarget != null) { hole = WorldBounds(focusBodyTarget); found = true; }
            hole = Expand(hole, 20f);
            Rect screen = root.rect;
            hole = Rect.MinMaxRect(Mathf.Clamp(hole.xMin, screen.xMin, screen.xMax), Mathf.Clamp(hole.yMin, screen.yMin, screen.yMax),
                Mathf.Clamp(hole.xMax, screen.xMin, screen.xMax), Mathf.Clamp(hole.yMax, screen.yMin, screen.yMax));
            Fit(shades[0], Rect.MinMaxRect(screen.xMin, hole.yMax, screen.xMax, screen.yMax));
            Fit(shades[1], Rect.MinMaxRect(screen.xMin, screen.yMin, screen.xMax, hole.yMin));
            Fit(shades[2], Rect.MinMaxRect(screen.xMin, hole.yMin, hole.xMin, hole.yMax));
            Fit(shades[3], Rect.MinMaxRect(hole.xMax, hole.yMin, screen.xMax, hole.yMax));
            Fit(focusRing, Expand(hole, 8f + Mathf.Sin(Time.unscaledTime * 5f) * 4f));
            if (focusBodyTarget == null) focusCard.anchoredPosition = new Vector2(hole.center.x < 0f ? 180f : -180f, 30f);
            else
            {
                Vector2 half = focusCard.sizeDelta * 0.5f;
                float above = hole.yMax + 32f + half.y;
                float below = hole.yMin - 32f - half.y;
                if (above + half.y < screen.yMax - 15f) focusCard.anchoredPosition = new Vector2(0f, above);
                else if (below - half.y > screen.yMin + 15f) focusCard.anchoredPosition = new Vector2(0f, below);
                else
                {
                    bool left = hole.xMin - screen.xMin > screen.xMax - hole.xMax;
                    float x = left ? hole.xMin - 32f - half.x : hole.xMax + 32f + half.x;
                    focusCard.anchoredPosition = new Vector2(x,
                        Mathf.Clamp(hole.center.y, screen.yMin + half.y + 15f, screen.yMax - half.y - 15f));
                }
            }
        }

        private void DrawAction()
        {
            bool ended = !guide.Progress.Started || guide.Progress.Completed || guide.Progress.Failed;
            action.gameObject.SetActive(!ended); liveRing.gameObject.SetActive(!ended);
            if (ended) return;
            Transform body = guide.Possession.PossessedEnemy != null ? guide.Possession.PossessedEnemy.transform : guide.Possession.transform;
            Transform target = body;
            string key = "E", label = guide.Title, arrow = "↑";
            Color color = Gold;
            var lesson = guide.Progress.Lesson;
            switch (lesson)
            {
                case TutorialLesson.Movement:
                    key = guide.MovementKey; label = guide.MovementAction;
                    arrow = guide.Progress.NextMovement == TutorialMovement.Left ? "←" : guide.Progress.NextMovement == TutorialMovement.Right
                        ? "→" : guide.Progress.NextMovement == TutorialMovement.Back ? "↓" : "↑";
                    break;
                case TutorialLesson.FindGuard:
                case TutorialLesson.RepossessGuard:
                case TutorialLesson.FindHeavy:
                    target = lesson == TutorialLesson.FindHeavy ? guide.Heavy.transform : guide.Guard.transform;
                    label = target == guide.Guard.transform ? "GUARD · SHOOT" : "WALL BREAKER";
                    key = "WASD"; arrow = "MOVE CLOSER";
                    if (guide.Nearby(out Transform nearby, out _, out bool danger) && nearby == target)
                    { key = danger ? "RUN" : "E"; arrow = danger ? "KEEP YOUR DISTANCE" : "PRESS TO POSSESS"; color = danger ? Red : Green; }
                    if (guide.Possession.CurrentState != PlayerState.Spirit)
                    { target = body; key = "E"; label = "RETURN TO SPIRIT"; arrow = "THEN BORROW THE OTHER BODY"; }
                    break;
                case TutorialLesson.ShootTarget:
                    if (guide.Possession.CurrentState == PlayerState.Possessed_Guard)
                    {
                        if (guide.Guard.GetComponent<ShootAbility>().UsesLeft <= 0)
                        { target = guide.Guard.transform; key = "E"; label = "OUT OF SHOTS"; arrow = "EJECT & REPOSSESS"; }
                        else { target = guide.TargetBody; key = "LMB"; label = "SHOOT THIS TARGET"; arrow = "TARGET → DOOR OPENS"; }
                    }
                    else { target = guide.Guard.transform; key = "E"; label = "BORROW THE GUARD"; arrow = "GET CLOSE, THEN E"; }
                    break;
                case TutorialLesson.SmashWall:
                    if (guide.Possession.CurrentState == PlayerState.Possessed_Heavy)
                    {
                        if (guide.Heavy.GetComponent<SmashAbility>().UsesLeft <= 0)
                        { target = guide.Heavy.transform; key = "E"; label = "SMASH USED"; arrow = "EJECT & REPOSSESS"; }
                        else { target = guide.WallBody; key = "LMB"; label = "SMASH THIS WALL"; arrow = "WASD TO FACE IT"; }
                    }
                    else { target = guide.Heavy.transform; key = "E"; label = "BORROW THE HEAVY"; arrow = "GET CLOSE, THEN E"; }
                    break;
                case TutorialLesson.ManualEject: label = "EJECT BEFORE ZERO"; arrow = "PRESS E TO LEAVE"; break;
                case TutorialLesson.WaitForTimer: key = "WAIT"; label = "AUTO-EJECT IN " + Mathf.CeilToInt(Mathf.Max(0f, guide.Possession.PossessTimer)) + "s";
                    arrow = "E TO LEAVE EARLY"; break;
                case TutorialLesson.EscapeHostile: target = guide.HostileEnemy.transform; key = "WASD"; label = "AUTO-EJECTED!"; arrow = "MOVE AWAY NOW"; color = Red; break;
                case TutorialLesson.ReachExit:
                    if (guide.Possession.CurrentState == PlayerState.Spirit) { target = guide.ExitBody; key = "GO"; label = "ENTER THE GREEN EXIT"; arrow = "FINISH BEFORE ZERO"; color = Green; }
                    else { key = "E"; label = "RETURN TO SPIRIT FORM"; arrow = "THEN ENTER THE EXIT"; }
                    break;
            }
            if (guide.Possession.PossessedEnemy != null && guide.Possession.PossessTimer <= 3f)
            {
                target = body; key = Mathf.CeilToInt(Mathf.Max(0f, guide.Possession.PossessTimer)).ToString();
                label = "AUTO-EJECT IN " + key + "s"; arrow = "GET READY TO RUN"; color = Red;
            }
            string stamp = lesson + "/" + key + "/" + target.GetInstanceID();
            if (stamp != previousAction) { previousAction = stamp; actionBegan = Time.unscaledTime; }
            float age = Time.unscaledTime - actionBegan;
            float pop = Mathf.Clamp01(age / 0.35f);
            float scale = Mathf.LerpUnclamped(0.55f, 1f, 1f + 2.70158f * Mathf.Pow(pop - 1f, 3f) + 1.70158f * Mathf.Pow(pop - 1f, 2f));
            bool pressed = key switch { "W" => Input.GetKey(KeyCode.W), "A" => Input.GetKey(KeyCode.A),
                "S" => Input.GetKey(KeyCode.S), "D" => Input.GetKey(KeyCode.D), "SPACE" => Input.GetKey(KeyCode.Space),
                "E" => Input.GetKey(KeyCode.E), "LMB" => Input.GetMouseButton(0), _ => false };
            action.localScale = Vector3.one * (scale * (1f + 0.035f * Mathf.Sin(Time.unscaledTime * 6f)) * (pressed ? 1.06f : 1f));
            actionAlpha.alpha = Mathf.Clamp01(age / 0.12f);
            actionKey.text = key; actionKey.fontSize = key.Length > 2 ? 25 : 42;
            actionTitle.text = label; actionTitle.fontSize = 24;
            actionArrow.text = arrow; actionArrow.fontSize = arrow.Length > 24 ? 17 : arrow.Length > 2 ? 21 : 36;
            actionArrow.color = color;
            actionKey.transform.parent.GetComponent<Image>().color = pressed ? Green : color;
            Rect bounds = WorldBounds(target);
            Fit(liveRing, Expand(bounds, 9f + 5f * Mathf.Sin(Time.unscaledTime * 5f)));
            liveRing.GetComponent<TutorialRingGraphic>().color = color;
            Vector2 position = new(bounds.center.x, bounds.yMax + 85f + 7f * Mathf.Sin(Time.unscaledTime * 5f));
            action.anchoredPosition = Clamp(position, action.sizeDelta * 0.5f + Vector2.one * 14f);
        }

        private void DrawExpiryWarning()
        {
            bool show = !guide.IsCoaching && !guide.Progress.Completed && !guide.Progress.Failed
                && guide.Possession.PossessedEnemy != null && guide.Possession.PossessTimer <= 3f;
            expiryRing.gameObject.SetActive(show);
            if (!show) return;
            foreach (var clock in guide.FocusWidgets(TutorialFocus.PossessionClock))
                if (clock != null) Fit(expiryRing, Expand(UiBounds(clock), 8f + 5f * Mathf.Sin(Time.unscaledTime * 12f)));
        }

        private void DrawFeedback()
        {
            float age = Time.unscaledTime - toastBegan;
            bool ended = guide.Progress.Completed || guide.Progress.Failed;
            bool showing = age >= 0f && age < 1.8f && !guide.IsCoaching && !ended;
            toast.gameObject.SetActive(showing);
            pingRing.gameObject.SetActive(showing && pingTarget != null);
            if (showing)
            {
                toast.anchoredPosition = new Vector2(0f, root.rect.yMax - 200f);
                toastAlpha.alpha = Mathf.Clamp01((1.8f - age) / 0.4f);
                toast.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Mathf.Clamp01(age / 0.3f) * Mathf.PI));
                if (pingTarget != null) Fit(pingRing, Expand(WorldBounds(pingTarget), 16f + age * 12f));
            }
            float damageAge = Time.unscaledTime - healthBegan;
            hpFlash.gameObject.SetActive(damageAge >= 0f && damageAge < 0.8f && !guide.IsCoaching && !ended);
            if (hpFlash.gameObject.activeSelf)
            {
                Rect bounds = default; bool first = true;
                foreach (var widget in guide.FocusWidgets(TutorialFocus.Health))
                    if (widget != null) { bounds = first ? UiBounds(widget) : Union(bounds, UiBounds(widget)); first = false; }
                Fit(hpFlash, Expand(bounds, 12f));
                hpFlash.GetComponent<Image>().color = new Color(1f, 0.1f, 0.05f, (1f - damageAge / 0.8f) * 0.25f);
            }
        }

        private Rect UiBounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var source = rect.GetComponentInParent<Canvas>();
            Camera camera = source != null && source.renderMode != RenderMode.ScreenSpaceOverlay ? source.worldCamera : null;
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            foreach (var corner in corners)
            { var p = Local(RectTransformUtility.WorldToScreenPoint(camera, corner)); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private Rect WorldBounds(Transform target)
        {
            var camera = Camera.main;
            if (camera == null || target == null) return new Rect(-50f, -50f, 100f, 100f);
            var collider = target.GetComponent<Collider>();
            Bounds b = collider != null ? collider.bounds : new Bounds(target.position, new Vector3(1f, 2f, 1f));
            // A broken wall or disabled exit can have empty collider bounds.
            if (b.size.sqrMagnitude < 0.01f) b = new Bounds(target.position, Vector3.one * 1.5f);
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 screen = camera.WorldToScreenPoint(corner);
                var p = Local(screen); min = Vector2.Min(min, p); max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private Vector2 Local(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out Vector2 local);
            return local;
        }
        private Vector2 Clamp(Vector2 p, Vector2 margin) => new(Mathf.Clamp(p.x, root.rect.xMin + margin.x, root.rect.xMax - margin.x),
            Mathf.Clamp(p.y, root.rect.yMin + 270f + margin.y, root.rect.yMax - 130f - margin.y));
        private static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        private static Rect Expand(Rect r, float pixels) => Rect.MinMaxRect(r.xMin - pixels, r.yMin - pixels, r.xMax + pixels, r.yMax + pixels);
        private static void Fit(RectTransform rect, Rect bounds) { rect.anchoredPosition = bounds.center; rect.sizeDelta = bounds.size; }
        private static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        { var rect = CreateRect(parent, name, position, size); var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return rect; }
        private static RectTransform Ring(Transform parent, string name, Color color)
        { var rect = CreateRect(parent, name, Vector2.zero, Vector2.one); var ring = rect.gameObject.AddComponent<TutorialRingGraphic>(); ring.color = color; ring.raycastTarget = false; return rect; }
        private static Text Text(Transform parent, string name, string value, int size, Vector2 position, Vector2 bounds, Color color)
        {
            var rect = CreateRect(parent, name, position, bounds); var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.fontStyle = FontStyle.Bold;
            text.text = value; text.color = color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; return text;
        }
        private static Button Button(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityAction clicked, Color color)
        {
            var rect = Panel(parent, name, position, size, color); var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(clicked);
            Text(rect, "Text", label, 24, Vector2.zero, size - new Vector2(12f, 6f), color == Gold ? Dark : Color.white); return button;
        }
    }
}
