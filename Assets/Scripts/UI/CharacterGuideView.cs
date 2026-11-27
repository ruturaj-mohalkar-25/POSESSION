using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // One place to learn role appearance, purpose and controls before entering a level.
    public sealed class CharacterGuideView : MonoBehaviour
    {
        private GameObject home;
        private GameObject panel;
        private Button back;

        public void Build(GameObject main, Sprite surface)
        {
            home = main;
            panel = GoldTheme.Box(transform, "Character guide", Vector2.zero, Vector2.one, GoldTheme.Background).gameObject;
            Label(panel.transform, "Brand", "POSSESSION", 28, .3f, .92f, .7f, .98f);
            Label(panel.transform, "Heading", "KNOW YOUR HOST", 62, .2f, .80f, .8f, .93f);
            back = GoldTheme.Button(panel.transform, "CharacterGuideBackButton", "BACK", new Vector2(.025f,.89f), new Vector2(.15f,.965f));
            back.GetComponent<Image>().sprite = surface;
            back.GetComponent<Image>().type = Image.Type.Sliced;
            back.onClick.AddListener(Close);

            Card(0, "GUARD", new Color(.32f,.60f,.94f), "SHOOT TARGETS", "Aim + left-click", surface);
            Card(1, "HEAVY", new Color(.95f,.57f,.12f), "BREAK BARRIERS", "Get close + left-click", surface);
            Card(2, "ENGINEER", new Color(.16f,.70f,.73f), "OPERATE THE VAULT", "Press F at stations", surface);
            Label(panel.transform, "Shared controls", "WASD / ARROWS  Move     |     E  Possess / eject", 27, .055f, .035f, .945f, .12f);
            panel.SetActive(false);
        }

        private void Card(int index, string role, Color accent, string purpose, string control, Sprite surface)
        {
            float left = .045f + index * .31f;
            var card = GoldTheme.Box(panel.transform, role + " guide", new Vector2(left,.15f), new Vector2(left+.29f,.78f), GoldTheme.Panel);
            card.sprite = surface;
            card.type = Image.Type.Sliced;
            GoldTheme.Box(card.transform, "Role accent", new Vector2(.04f,.975f), new Vector2(.96f,.985f), accent);
            Label(card.transform, "Role name", role, 40, .05f, .86f, .95f, .97f);
            var portraitArea = GoldTheme.Rect(card.transform, "Portrait area", new Vector2(.04f,.25f), new Vector2(.96f,.86f));
            var portrait = GoldTheme.Rect(portraitArea, "Character portrait", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            portrait.texture = Resources.Load<Texture2D>("CharacterGuide/" + role);
            portrait.raycastTarget = false;
            var fit = portrait.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 1f;
            var task = Label(card.transform, "Purpose", purpose, 30, .04f, .14f, .96f, .25f);
            task.color = accent;
            Label(card.transform, "Instructions", control, 25, .04f, .035f, .96f, .14f);
        }

        private static Text Label(Transform parent, string name, string value, int size,
            float left, float bottom, float right, float top)
        {
            var label = GoldTheme.Label(parent, name, value, size, new Vector2(left,bottom), new Vector2(right,top));
            var font = Resources.Load<Font>("Menu/Barlow-Bold");
            if (font != null) { label.font = font; label.fontStyle = FontStyle.Normal; }
            return label;
        }

        public void Open()
        {
            home.SetActive(false);
            panel.SetActive(true);
            back.Select();
        }

        public void Close()
        {
            panel.SetActive(false);
            home.SetActive(true);
            var entry = home.transform.Find("CharactersButton");
            if (entry != null) entry.GetComponent<Button>().Select();
        }

        private void Update()
        {
            if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
        }
    }
}
