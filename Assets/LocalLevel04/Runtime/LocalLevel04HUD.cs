using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Possession.LocalLevel04
{
    public sealed class LocalLevel04HUD : MonoBehaviour
    {
        public static readonly Color Mint = new Color(.29f, .96f, .83f);
        public static readonly Color Blue = new Color(.36f, .69f, 1);
        public static readonly Color Amber = new Color(1, .68f, .28f);
        public static readonly Color Red = new Color(1, .32f, .39f);
        private static readonly Color Muted = new Color(.54f, .65f, .71f);
        private static readonly Color Ink = new Color(.025f, .045f, .065f, .95f);
        private static readonly string[] Titles = { "TAKE A GUARD", "BREACH THE CACHE", "ACTIVATE THE RELAY", "BREAK THE FINAL GATE", "MAKE YOUR ESCAPE" };
        private static readonly string[] Instructions = {
            "Find the blue Guard beyond the cover. Possess it and shoot the west target.",
            "Take the central Heavy. Face the amber cache gate and smash with LMB.",
            "Bring a Guard into the opened cache. Shoot the red relay target inside.",
            "Go around the east baffle. Use the second Heavy to breach the final gate.",
            "Eject from your host. Enter the green finish zone as Spirit." };

        private LocalLevel04Challenge level;
        private SpiritController spirit;
        private PossessionController player;
        private RectTransform root, mapCard, abilityCard, context, overlay, resumeButton, retryButton;
        private Text formText, healthText, clock, timerCaption, stageTitle, stageBody, stageCount;
        private Text abilityTitle, abilityStatus, linkText, contextText, toastTitle, toastBody;
        private Text endTitle, endBody, endStats, endEyebrow;
        private LocalLevel04Stars rating;
        private Image formStripe, abilityStripe, linkFill;
        private Image[] health = new Image[3], steps = new Image[5], ammo = new Image[3], edges = new Image[4];
        private LocalLevel04Ring timerRing;
        private CanvasGroup toast;
        private Font font;
        private float totalTime, toastUntil, effectUntil;
        private Color effectColor = Mint;
        private int previousStage, swaps, stars;
        private bool paused, mapVisible = true, ending;
        private bool spiritWasEnabled, playerWasEnabled;
        private bool[] hostsWereEnabled;
        public bool IsPaused => paused;
        public int HostSwaps => swaps;

        public void Bind(LocalLevel04Challenge challenge)
        {
            level = challenge;
            spirit = FindFirstObjectByType<SpiritController>();
            player = spirit.GetComponent<PossessionController>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            totalTime = Mathf.Max(1, GameManager.Instance.LevelTimer);
            previousStage = level.Stage;
            Build();
            ShowToast("THE RELAY RUN", "Shoot. Smash. Switch. Escape.", Mint);
            Refresh();
        }
        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
            EventBus.SpiritDamaged += OnDamage;
            EventBus.LevelCompletedWithStats += OnStats;
        }
        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.SpiritDamaged -= OnDamage;
            EventBus.LevelCompletedWithStats -= OnStats;
        }
        private void OnDestroy()
        {
            if (paused) Time.timeScale = 1;
            if (root != null) Destroy(root.gameObject);
        }
        private void OnPossessed(EnemyBase host, PlayerState state)
        {
            swaps++;
            Color c = host is HeavyEnemy ? Amber : Blue;
            ShowToast("VESSEL LINKED / " + (host is HeavyEnemy ? "HEAVY" : "GUARD"), "Ten seconds. Make your move.", c);
            Flash(c);
        }
        private void OnEjected(EnemyBase host, bool forced)
        {
            ShowToast(forced ? "LINK LOST" : "SPIRIT RESTORED", forced ? "Host hostile for 5 seconds. Keep moving." : "Find your next vessel.", forced ? Red : Mint);
            Flash(forced ? Red : Mint);
        }
        private void OnDamage(int hp, int max) { Flash(Red); ShowToast("SPIRIT DAMAGED", Mathf.Max(0, hp) + " integrity remaining. Get clear of the host.", Red); }
        private void OnStats(int count, float elapsed) => stars = count;
        private void Flash(Color c) { effectColor = c; effectUntil = Time.unscaledTime + .65f; }
        private void ShowToast(string heading, string message, Color c)
        {
            if (toast == null) return;
            toastTitle.text = heading; toastTitle.color = c; toastBody.text = message;
            toastUntil = Time.unscaledTime + 2.8f;
        }

        private void Update()
        {
            if (level == null || player == null || root == null) return;
            if (Input.GetKeyDown(KeyCode.M)) { mapVisible = !mapVisible; mapCard.gameObject.SetActive(mapVisible); }
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            Refresh();
        }
        public void TogglePause()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != LevelState.InProgress) return;
            paused = !paused;
            if (paused)
            {
                spiritWasEnabled = spirit.enabled; playerWasEnabled = player.enabled;
                spirit.enabled = player.enabled = false;
                hostsWereEnabled = new bool[level.hosts.Length];
                for (int i = 0; i < level.hosts.Length; i++)
                { hostsWereEnabled[i] = level.hosts[i].enabled; level.hosts[i].enabled = false; }
            }
            else
            {
                spirit.enabled = spiritWasEnabled; player.enabled = playerWasEnabled;
                for (int i = 0; i < level.hosts.Length; i++) level.hosts[i].enabled = hostsWereEnabled[i];
            }
            Time.timeScale = paused ? 0 : 1;
            Refresh();
        }
        private void Refresh()
        {
            var manager = GameManager.Instance;
            if (manager == null) return;
            totalTime = Mathf.Max(totalTime, manager.LevelTimer);
            bool possessing = player.PossessedEnemy != null;
            bool heavy = player.PossessedEnemy is HeavyEnemy;
            Color formColor = possessing ? (heavy ? Amber : Blue) : Mint;
            formText.text = possessing ? (heavy ? "HEAVY" : "GUARD") : "SPIRIT";
            formText.color = formStripe.color = abilityStripe.color = formColor;
            healthText.text = "INTEGRITY  " + Mathf.Max(0, spirit.CurrentHP) + "/" + spirit.MaxHP;
            for (int i = 0; i < health.Length; i++) health[i].color = i < spirit.CurrentHP ? (spirit.CurrentHP <= 1 ? Red : Mint) : new Color(.16f, .22f, .26f);

            int seconds = Mathf.CeilToInt(Mathf.Max(0, manager.LevelTimer));
            clock.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            Color urgency = seconds <= 20 ? Red : seconds <= 45 ? Amber : Mint;
            clock.color = timerRing.color = urgency;
            timerCaption.text = seconds <= 20 ? "LOW TIME / RUN" : "ESCAPE TIMER";
            timerRing.value = Mathf.Clamp01(manager.LevelTimer / totalTime); timerRing.SetVerticesDirty();
            int stage = level.Stage;
            stageCount.text = "MISSION  /  " + (stage + 1).ToString("00") + " OF 05";
            stageTitle.text = Titles[stage]; stageBody.text = Instructions[stage];
            for (int i = 0; i < steps.Length; i++) steps[i].color = i < stage ? Mint : i == stage ? Color.white : new Color(.17f, .25f, .3f);
            if (stage != previousStage)
            {
                previousStage = stage;
                ShowToast("RELAY " + stage.ToString("00") + " COMPLETE", Titles[stage], Mint);
                Flash(Mint);
            }
            abilityTitle.text = possessing ? (heavy ? "KINETIC SMASH" : "PROJECTILE LINK") : "SPECTRAL MOVEMENT";
            var ability = possessing ? player.PossessedEnemy.GetComponent<Ability>() : null;
            abilityStatus.text = ability == null ? "SPACE  Jump    /    E  Possess" : ability.UsesLeft == 0
                ? "SPENT  /  Eject, then possess to recharge" : ability.CanUse ? "LMB  " + (heavy ? "Smash in facing direction" : "Aim at a target and fire") : "RECOVERING  /  Ability cooldown";
            linkText.text = possessing ? "HOST LINK   " + Mathf.Max(0, player.PossessTimer).ToString("0.0") + "s"
                : "FREE FORM   /   FIND A VESSEL";
            linkText.color = possessing && player.PossessTimer <= 3 ? Red : Muted;
            linkFill.color = possessing && player.PossessTimer <= 3 ? Red : formColor;
            linkFill.rectTransform.anchorMax = new Vector2(possessing ? Mathf.Clamp01(player.PossessTimer / player.PossessDuration) : 1, 1);
            for (int i = 0; i < ammo.Length; i++)
            {
                ammo[i].gameObject.SetActive(ability != null && i < ability.MaxUses);
                ammo[i].color = i < (ability == null ? 0 : ability.UsesLeft) ? formColor : new Color(.17f, .23f, .29f);
            }
            context.gameObject.SetActive(!ending && !paused && (possessing || level.PossessPrompt));
            contextText.text = possessing ? (player.PossessTimer <= 3 ? "EJECT NOW  /  HOST LINK EXPIRING" : "EJECT TO SPIRIT") : "POSSESS NEARBY VESSEL";
            contextText.color = possessing && player.PossessTimer <= 3 ? Red : formColor;
            toast.alpha = Mathf.Clamp01((toastUntil - Time.unscaledTime) * 2);
            float effect = Mathf.Clamp01((effectUntil - Time.unscaledTime) / .65f);
            if (seconds <= 20 && manager.CurrentState == LevelState.InProgress && !paused)
            { effect = Mathf.Max(effect, .15f + .12f * Mathf.Sin(Time.unscaledTime * 5)); effectColor = Red; }
            foreach (var edge in edges) { Color c = effectColor; c.a = effect * .75f; edge.color = c; }

            ending = manager.CurrentState != LevelState.InProgress;
            overlay.gameObject.SetActive(ending || paused);
            if (!ending && !paused) return;
            bool won = manager.CurrentState == LevelState.Complete;
            endEyebrow.text = paused ? "SIGNAL ON HOLD" : won ? "EXTRACTION CONFIRMED" : "SIGNAL TERMINATED";
            endTitle.text = paused ? "RUN PAUSED" : won ? "RELAY COMPLETE" : "RUN FAILED";
            endTitle.color = paused ? Mint : won ? Mint : Red;
            endBody.text = paused ? "Take a breath. The clock is stopped." : won ? "One Spirit. Many vessels. A clean escape." : spirit.CurrentHP <= 0 ? "Spirit integrity lost. Plan your next switch." : "The clock beat you. Find a faster route.";
            endStats.text = "TIME   " + manager.ElapsedTime.ToString("0.0") + "s     RELAYS   " + Mathf.Min(4, stage) + "/4     HOST LINKS   " + swaps;
            rating.gameObject.SetActive(won); rating.count = Mathf.Max(1, stars); rating.SetVerticesDirty();
            resumeButton.gameObject.SetActive(paused);
            retryButton.anchoredPosition = new Vector2(paused ? 362 : 205, -315);
        }

        private void Build()
        {
            root = new GameObject("Local04_SpectralHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<RectTransform>();
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 80;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Local04_UIInput", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(root);

            edges[0] = StretchEdge("Alert_Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -5), new Vector2(0, 5));
            edges[1] = StretchEdge("Alert_Bottom", Vector2.zero, new Vector2(1, 0), new Vector2(0, -5), new Vector2(0, 5));
            edges[2] = StretchEdge("Alert_Left", Vector2.zero, new Vector2(0, 1), new Vector2(-5, 0), new Vector2(5, 0));
            edges[3] = StretchEdge("Alert_Right", new Vector2(1, 0), Vector2.one, new Vector2(-5, 0), new Vector2(5, 0));

            var integrity = Card("Integrity", new Vector2(0, 1), new Vector2(24, -24), new Vector2(292, 116));
            formStripe = Box("FormStripe", integrity, 0, 0, 4, 116, Mint);
            Label(integrity, "VESSEL  /  04", 20, 12, 250, 22, 16, Muted);
            formText = Label(integrity, "SPIRIT", 20, 35, 250, 38, 32, Mint, true);
            healthText = Label(integrity, "INTEGRITY  3/3", 20, 81, 180, 22, 16, Muted);
            for (int i = 0; i < 3; i++) health[i] = Box("Integrity_" + i, integrity, 202 + i * 24, 87, 18, 8, Mint);

            var timer = Card("EscapeClock", new Vector2(.5f, 1), new Vector2(0, -24), new Vector2(264, 116));
            var track = At("TimerTrack", timer, 16, 17, 82, 82).gameObject.AddComponent<LocalLevel04Ring>();
            track.color = new Color(.12f, .21f, .25f); track.raycastTarget = false;
            timerRing = At("TimerRing", timer, 16, 17, 82, 82).gameObject.AddComponent<LocalLevel04Ring>(); timerRing.raycastTarget = false;
            Label(timer, "04", 30, 38, 54, 40, 28, Mint, true, TextAnchor.MiddleCenter);
            timerCaption = Label(timer, "ESCAPE TIMER", 113, 20, 140, 28, 14, Muted);
            clock = Label(timer, "02:00", 110, 45, 146, 48, 42, Color.white, true);

            var mission = Card("Mission", new Vector2(1, 1), new Vector2(-24, -24), new Vector2(360, 190));
            stageCount = Label(mission, "MISSION / 01 OF 05", 20, 12, 320, 25, 16, Mint);
            stageTitle = Label(mission, "TAKE A GUARD", 20, 42, 320, 31, 24, Color.white, true);
            stageBody = Label(mission, "", 20, 82, 320, 62, 19, Muted);
            for (int i = 0; i < 5; i++) steps[i] = Box("RelayStep_" + i, mission, 20 + i * 65, 164, 56, 4, Muted);

            abilityCard = Card("Ability", new Vector2(0, 0), new Vector2(24, 24), new Vector2(370, 128));
            abilityStripe = Box("AbilityStripe", abilityCard, 0, 0, 4, 128, Mint);
            abilityTitle = Label(abilityCard, "SPECTRAL MOVEMENT", 20, 12, 330, 25, 20, Color.white, true);
            abilityStatus = Label(abilityCard, "", 20, 44, 330, 36, 17, Muted);
            linkText = Label(abilityCard, "", 20, 85, 286, 22, 15, Muted);
            var bar = Box("HostLinkTrack", abilityCard, 20, 114, 330, 3, new Color(.13f, .21f, .25f)).rectTransform;
            linkFill = new GameObject("HostLinkRemaining", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            linkFill.transform.SetParent(bar, false); linkFill.raycastTarget = false;
            linkFill.rectTransform.anchorMin = Vector2.zero; linkFill.rectTransform.anchorMax = Vector2.one;
            linkFill.rectTransform.offsetMin = linkFill.rectTransform.offsetMax = Vector2.zero;
            for (int i = 0; i < 3; i++) ammo[i] = Box("Ammo_" + i, abilityCard, 304 + i * 16, 90, 9, 13, Mint);

            mapCard = Card("SignalMap", new Vector2(1, 0), new Vector2(-24, 24), new Vector2(320, 244));
            Label(mapCard, "SIGNAL MAP", 16, 9, 220, 26, 17, Mint, true);
            Label(mapCard, "M  HIDE", 244, 11, 65, 22, 14, Muted);
            var map = At("LiveMap", mapCard, 16, 45, 288, 174).gameObject.AddComponent<LocalLevel04SignalMap>();
            map.level = level; map.player = player; map.spirit = spirit.transform; map.raycastTarget = false;
            Label(mapCard, "BLUE  GUARD     AMBER  HEAVY     +  GOAL", 16, 223, 300, 18, 12, Muted);

            var control = Anchor("Controls", root, new Vector2(.5f, 0), new Vector2(0, 18), new Vector2(560, 28));
            Label(control, "WASD MOVE   SPACE JUMP   LMB ABILITY   ESC PAUSE   R RETRY", 0, 0, 560, 28, 15, Muted, false, TextAnchor.MiddleCenter);
            context = Card("PossessContext", new Vector2(.5f, 0), new Vector2(0, 68), new Vector2(420, 58));
            Box("KeyBadge", context, 12, 10, 38, 38, new Color(.13f, .26f, .29f));
            Label(context, "E", 12, 10, 38, 38, 24, Mint, true, TextAnchor.MiddleCenter);
            contextText = Label(context, "POSSESS NEARBY VESSEL", 65, 11, 348, 36, 17, Mint, true);

            var banner = Card("Transmission", new Vector2(.5f, 1), new Vector2(0, -168), new Vector2(500, 78));
            toast = banner.gameObject.AddComponent<CanvasGroup>(); toast.blocksRaycasts = false;
            toastTitle = Label(banner, "", 10, 8, 480, 31, 22, Mint, true, TextAnchor.MiddleCenter);
            toastBody = Label(banner, "", 10, 43, 480, 25, 17, Muted, false, TextAnchor.MiddleCenter);

            overlay = new GameObject("RunOverlay", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            overlay.SetParent(root, false); overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero; overlay.GetComponent<Image>().color = new Color(.01f, .025f, .04f, .88f);
            var dialog = Card("RunSummary", new Vector2(.5f, .5f), Vector2.zero, new Vector2(680, 410), overlay);
            endEyebrow = Label(dialog, "", 30, 26, 620, 27, 16, Muted, false, TextAnchor.MiddleCenter);
            endTitle = Label(dialog, "", 30, 69, 620, 64, 46, Mint, true, TextAnchor.MiddleCenter);
            rating = At("StarRating", dialog, 265, 133, 150, 40).gameObject.AddComponent<LocalLevel04Stars>(); rating.raycastTarget = false;
            endBody = Label(dialog, "", 40, 179, 600, 43, 21, Muted, false, TextAnchor.MiddleCenter);
            endStats = Label(dialog, "", 30, 240, 620, 30, 17, Color.white, false, TextAnchor.MiddleCenter);
            retryButton = Button(dialog, "RETRY RUN  [ R ]", 362, 315, 270, 55, () => level.Restart());
            resumeButton = Button(dialog, "RESUME  [ ESC ]", 48, 315, 270, 55, TogglePause);
            overlay.gameObject.SetActive(false);
        }

        private Image StretchEdge(string name, Vector2 min, Vector2 max, Vector2 lower, Vector2 upper)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(root, false); image.raycastTarget = false; image.color = Color.clear;
            image.rectTransform.anchorMin = min; image.rectTransform.anchorMax = max;
            image.rectTransform.offsetMin = lower; image.rectTransform.offsetMax = upper; return image;
        }
        private RectTransform Card(string name, Vector2 anchor, Vector2 position, Vector2 size, RectTransform parent = null)
        {
            var rect = Anchor(name, parent == null ? root : parent, anchor, position, size);
            var panel = rect.gameObject.AddComponent<Image>(); panel.color = Ink; panel.raycastTarget = false;
            Box("BorderTop", rect, 0, 0, size.x, 1, new Color(.18f, .31f, .35f));
            Box("BorderBottom", rect, 0, size.y - 1, size.x, 1, new Color(.1f, .2f, .24f));
            return rect;
        }
        private static RectTransform Anchor(string name, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor;
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        private static RectTransform At(string name, RectTransform parent, float x, float y, float width, float height) =>
            Anchor(name, parent, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
        private static Image Box(string name, RectTransform parent, float x, float y, float width, float height, Color color)
        {
            var image = At(name, parent, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        private Text Label(RectTransform parent, string text, float x, float y, float width, float height, int size, Color color,
            bool bold = false, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var label = At(text.Length > 0 ? text : "Readout", parent, x, y, width, height).gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = color;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false; return label;
        }
        private RectTransform Button(RectTransform parent, string caption, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var image = Box(caption, parent, x, y, width, height, new Color(.08f, .22f, .24f));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            var colors = button.colors; colors.highlightedColor = new Color(.5f, 1, .9f); colors.pressedColor = new Color(.3f, .75f, .65f); button.colors = colors;
            Label(image.rectTransform, caption, 0, 0, width, height, 20, Mint, true, TextAnchor.MiddleCenter);
            return image.rectTransform;
        }
    }
}
