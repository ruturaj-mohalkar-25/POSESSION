using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    public static class ChargeHUDInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindFirstObjectByType<Level02Director>() != null) return;
            var charge = Object.FindFirstObjectByType<PossessionCharge>();
            if (charge == null) return;
            charge.gameObject.AddComponent<ChargeHUD>();
        }
    }

    public sealed class ChargeHUD : MonoBehaviour
    {
        private PossessionCharge charge;
        private PossessionController possession;
        private Text label;

        private void Awake()
        {
            charge = GetComponent<PossessionCharge>();
            possession = GetComponent<PossessionController>();

            var canvas = new GameObject("ChargeHUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 15;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("ChargePanel", typeof(RectTransform), typeof(Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(320, 50);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.09f, 0.85f);

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            label = textObj.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        private void Update()
        {
            if (label == null || charge == null) return;
            if (possession.CurrentState != PlayerState.Spirit)
            {
                label.text = "POSSESSING · E to eject";
                label.color = new Color(0.85f, 0.65f, 0.2f);
            }
            else if (charge.HasCharge)
            {
                label.text = "CHARGES " + charge.Count + "/" + charge.MaxCharges + " · E near a body";
                label.color = new Color(0.45f, 1f, 0.7f);
            }
            else
            {
                label.text = "Find an energy shard";
                label.color = Color.white;
            }
        }
    }
}
