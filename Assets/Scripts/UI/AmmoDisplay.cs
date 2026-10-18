using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // Row of pips under the possession timer: filled = uses left, faded = spent.
    public class AmmoDisplay : MonoBehaviour
    {
        [SerializeField] private RectTransform pipRow;
        [SerializeField] private Sprite pipSprite;
        [SerializeField] private float pipSize = 26f;
        [SerializeField] private float pipSpacing = 10f;
        [SerializeField] private Color fullColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color spentColor = new Color(1f, 1f, 1f, 0.2f);

        private readonly List<Image> pips = new List<Image>();

        public void Show(int left, int max)
        {
            gameObject.SetActive(max > 0);
            if (max <= 0 || pipRow == null) return;

            while (pips.Count < max) pips.Add(CreatePip(pips.Count));
            for (int i = 0; i < pips.Count; i++)
            {
                pips[i].gameObject.SetActive(i < max);
                pips[i].color = i < left ? fullColor : spentColor;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private Image CreatePip(int index)
        {
            var go = new GameObject("Pip" + index, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(pipRow, false);
            // Right-aligned so pips line up under the right-aligned timer text
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(pipSize, pipSize);
            rect.anchoredPosition = new Vector2(-index * (pipSize + pipSpacing), 0f);

            var img = go.GetComponent<Image>();
            img.sprite = pipSprite;
            img.raycastTarget = false;
            return img;
        }
    }
}
