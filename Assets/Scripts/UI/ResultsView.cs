using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // Medal row + stats line on an end panel. Stats-only panels leave the medal fields empty.
    public class ResultsView : MonoBehaviour
    {
        [SerializeField] private Image[] medalPips;
        [SerializeField] private Text medalText;
        [SerializeField] private Text statsText;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
        private static readonly Color Silver = new Color(0.8f, 0.84f, 0.9f);
        private static readonly Color Bronze = new Color(0.85f, 0.55f, 0.3f);
        private static readonly Color Empty = new Color(1f, 1f, 1f, 0.15f);

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
    }
}
