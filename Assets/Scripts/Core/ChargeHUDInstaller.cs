using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Possession
{
    public static class ChargeHUDInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene() => Install(SceneManager.GetActiveScene());
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

        public static void Install(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            HUDManager hud = null;
            PossessionCharge charge = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (hud == null) hud = root.GetComponentInChildren<HUDManager>();
                if (charge == null) charge = root.GetComponentInChildren<PossessionCharge>();
            }
            if (hud == null || charge == null) return;
            var display = charge.GetComponent<ChargeHUD>();
            if (display == null) display = charge.gameObject.AddComponent<ChargeHUD>();
            display.Attach(hud);
        }
    }

    public sealed class ChargeHUD : MonoBehaviour
    {
        private PossessionCharge charge;
        private PossessionController possession;
        private SpiritController spirit;
        private Text label;
        private GameObject panel;
        public Text Label => label;

        private void Awake()
        {
            charge = GetComponent<PossessionCharge>();
            possession = GetComponent<PossessionController>();
            spirit = GetComponent<SpiritController>();
        }

        public void Attach(HUDManager hud)
        {
            if (label != null) return;
            panel = new GameObject("ChargePanel", typeof(RectTransform), typeof(Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.SetParent(hud.transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(520, 50);
            var background = panel.GetComponent<Image>();
            background.color = new Color(.04f, .07f, .09f, .85f);
            background.raycastTarget = false;
            var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 0);
            textRect.offsetMax = new Vector2(-12, 0);
            label = textObj.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            Update();
        }

        private void OnDestroy() { if (panel != null) Destroy(panel); }
        private void OnDisable() { if (panel != null) panel.SetActive(false); }

        private void Update()
        {
            if (label == null || charge == null || possession == null) return;
            bool playing = (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress)
                && (spirit == null || spirit.CurrentHP > 0);
            panel.SetActive(playing);
            if (!playing) return;
            if (possession.CurrentState != PlayerState.Spirit)
            {
                label.text = "POSSESSING · E to eject";
                label.color = new Color(.85f, .65f, .2f);
            }
            else if (charge.HasCharge)
            {
                label.text = "CHARGES " + charge.Count + "/" + charge.MaxCharges + " · E near a body";
                label.color = new Color(.45f, 1f, .7f);
            }
            else
            {
                label.text = "Find an energy shard";
                label.color = Color.white;
            }
        }
    }
}
