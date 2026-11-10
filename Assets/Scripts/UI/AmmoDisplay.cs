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
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private Sprite hammerSprite;
        [SerializeField] private float pipSize = 56f;
        [SerializeField] private float pipSpacing = 2f;
        [SerializeField] private Vector2 cursorOffset = new Vector2(32f, -32f);
        [SerializeField] private Color fullColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color spentColor = new Color(1f, 1f, 1f, 0.2f);

        private readonly List<Image> pips = new List<Image>();
        private Canvas canvas;
        private readonly Vector3[] corners = new Vector3[4];

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

        public void UseBulletSprite()
        {
            pipSprite = bulletSprite != null ? bulletSprite : pipSprite;
            fullColor = new Color(1f, 0.85f, 0.3f);
            ResizePips(48f);
        }

        public void UseHammerSprite()
        {
            pipSprite = hammerSprite != null ? hammerSprite : pipSprite;
            fullColor = Color.white;
            ResizePips(60f);
        }

        private void ResizePips(float size)
        {
            pipSize = size;
            for (int i = 0; i < pips.Count; i++)
            {
                pips[i].sprite = pipSprite;
                pips[i].color = fullColor;
                var rect = (RectTransform)pips[i].transform;
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(i * (size + pipSpacing), 0f);
            }
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
            PositionPips(Input.mousePosition);
        }

        private void PositionPips(Vector2 screenPosition)
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas == null || pipRow == null) return;
            var root = canvas.rootCanvas.transform as RectTransform;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, uiCamera, out Vector2 cursor)) return;
            transform.position = root.TransformPoint(cursor + cursorOffset);

            // Clamp the visible symbols, rather than the old fixed-size row, inside the canvas.
            Bounds bounds = new Bounds();
            bool found = false;
            foreach (var pip in pips)
            {
                if (!pip.gameObject.activeSelf) continue;
                pip.rectTransform.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector3 point = root.InverseTransformPoint(corner);
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!found) return;
            Rect safe = root.rect;
            Vector2 shift = new Vector2(
                Mathf.Max(0f, safe.xMin + 8f - bounds.min.x) - Mathf.Max(0f, bounds.max.x - safe.xMax + 8f),
                Mathf.Max(0f, safe.yMin + 8f - bounds.min.y) - Mathf.Max(0f, bounds.max.y - safe.yMax + 8f));
            transform.position += root.TransformVector(shift);
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
            img.preserveAspect = true;
            return img;
        }
    }
}
