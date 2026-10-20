using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    public class ResultsView : MonoBehaviour
    {
        [SerializeField] private Image[] medalPips;
        [SerializeField] private Text medalText;
        [SerializeField] private Text statsText;
        [SerializeField] private RectTransform chartContainer;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
        private static readonly Color Silver = new Color(0.8f, 0.84f, 0.9f);
        private static readonly Color Bronze = new Color(0.85f, 0.55f, 0.3f);
        private static readonly Color Empty = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color GuardColor = new Color(0.3f, 0.6f, 1f);
        private static readonly Color HeavyColor = new Color(1f, 0.6f, 0.2f);

        public void ShowStats(string summary)
        {
            if (statsText != null) statsText.text = summary;
        }

        public void ShowRating(int stars)
        {
            stars = Mathf.Clamp(stars, 1, 3);
            Color color = stars == 3 ? Gold : stars == 2 ? Silver : Bronze;

            if (medalPips != null)
                for (int i = 0; i < medalPips.Length; i++)
                    if (medalPips[i] != null) medalPips[i].color = i < stars ? color : Empty;

            if (medalText != null)
            {
                medalText.text = stars == 3 ? "GOLD" : stars == 2 ? "SILVER" : "BRONZE";
                medalText.color = color;
            }
        }

        public void ShowChart(List<PossessionEntry> entries)
        {
            if (chartContainer == null || entries == null || entries.Count == 0) return;

            for (int i = chartContainer.childCount - 1; i >= 0; i--)
                Destroy(chartContainer.GetChild(i).gameObject);

            float total = 0f;
            foreach (var e in entries) total += e.duration;
            if (total <= 0f) return;

            float containerWidth = chartContainer.rect.width;
            float gap = 4f;
            float usable = containerWidth - gap * (entries.Count - 1);
            float x = 0f;

            foreach (var e in entries)
            {
                float w = (e.duration / total) * usable;
                var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)bar.transform;
                rect.SetParent(chartContainer, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(x, 0f);
                rect.sizeDelta = new Vector2(w, chartContainer.rect.height);

                var img = bar.GetComponent<Image>();
                img.color = e.enemyType == "Heavy" ? HeavyColor : GuardColor;
                img.raycastTarget = false;

                var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var lr = (RectTransform)label.transform;
                lr.SetParent(rect, false);
                lr.anchorMin = new Vector2(0.5f, 0f);
                lr.anchorMax = new Vector2(0.5f, 0f);
                lr.pivot = new Vector2(0.5f, 1f);
                lr.anchoredPosition = new Vector2(0, -4f);
                lr.sizeDelta = new Vector2(100f, 30f);
                var txt = label.GetComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 18;
                txt.alignment = TextAnchor.UpperCenter;
                txt.color = Color.white;
                txt.text = $"{e.enemyType} {e.duration:F1}s";
                txt.raycastTarget = false;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;

                x += w + gap;
            }
        }
    }
}
