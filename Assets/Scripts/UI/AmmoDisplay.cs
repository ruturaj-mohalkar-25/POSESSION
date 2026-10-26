using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // Row of pips next to the mouse cursor (the aiming crosshair): filled = uses left, faded = spent.
    public class AmmoDisplay : MonoBehaviour
    {
        [SerializeField] private RectTransform pipRow;
        [SerializeField] private Sprite pipSprite;
        [SerializeField] private float pipSize = 22f;
        [SerializeField] private float pipSpacing = 8f;
        [SerializeField] private Vector2 cursorOffset = new Vector2(28f, -28f);
        [SerializeField] private Color fullColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color spentColor = new Color(1f, 1f, 1f, 0.2f);

        private readonly List<Image> pips = new List<Image>();
        private Canvas canvas;

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
            FollowCursor();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            FollowCursor();
        }

        // Overlay canvas works in screen pixels; the offset is scaled like the rest of the HUD
        private void FollowCursor()
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            transform.position = (Vector2)Input.mousePosition + cursorOffset * scale;
        }

        private Image CreatePip(int index)
        {
            var go = new GameObject("Pip" + index, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(pipRow, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(pipSize, pipSize);
            rect.anchoredPosition = new Vector2(index * (pipSize + pipSpacing), 0f);

            var img = go.GetComponent<Image>();
            img.sprite = pipSprite;
            img.raycastTarget = false;
            return img;
        }
    }
}
