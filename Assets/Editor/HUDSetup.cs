using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Possession.Editor
{
    public static class HUDSetup
    {
        static readonly Color BarBackground = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        [MenuItem("Possession/P5/Setup HUD Bars and Buttons")]
        public static void SetupHUD()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("HUD Setup", "Stop Play mode first, then run this again.", "OK");
                return;
            }

            var hud = Object.FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);
            if (hud == null)
            {
                EditorUtility.DisplayDialog("HUD Setup", "No HUDManager found. Open Level_01 first.", "OK");
                return;
            }

            var canvas = hud.transform;
            var so = new SerializedObject(hud);

            // Health text: top-left, with the bar underneath
            var healthText = so.FindProperty("healthText").objectReferenceValue as Text;
            if (healthText != null)
            {
                PlaceTopLeft(healthText.rectTransform, new Vector2(30, -24), new Vector2(460, 56));
                StyleText(healthText, 44, TextAnchor.MiddleLeft);
                healthText.text = "HP 3/3";
            }

            var healthBG = FindOrCreateImage(canvas, "HealthBarBG", BarBackground);
            PlaceTopLeft(healthBG.rectTransform, new Vector2(30, -86), new Vector2(460, 36));
            var healthFill = FindOrCreateImage(healthBG.transform, "HealthFill", new Color(0.2f, 0.85f, 0.3f));
            StretchFromLeft(healthFill.rectTransform);

            // Possession timer group: top-right, text above a draining bar
            var group = FindOrCreateRect(canvas, "PossessTimerGroup");
            group.anchorMin = group.anchorMax = group.pivot = new Vector2(1, 1);
            group.anchoredPosition = new Vector2(-30, -24);
            group.sizeDelta = new Vector2(460, 98);

            var possessText = so.FindProperty("possessTimerText").objectReferenceValue as Text;
            if (possessText != null)
            {
                possessText.transform.SetParent(group, false);
                possessText.gameObject.SetActive(true); // the group handles visibility now
                var r = possessText.rectTransform;
                r.anchorMin = new Vector2(0, 1);
                r.anchorMax = new Vector2(1, 1);
                r.pivot = new Vector2(1, 1);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(0, 56);
                StyleText(possessText, 40, TextAnchor.MiddleRight);
                possessText.text = "POSSESSED 15s";
            }

            var possessBG = FindOrCreateImage(group, "PossessBarBG", BarBackground);
            var bgRect = possessBG.rectTransform;
            bgRect.anchorMin = new Vector2(0, 0);
            bgRect.anchorMax = new Vector2(1, 0);
            bgRect.pivot = new Vector2(0.5f, 0);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(0, 30);
            var possessFill = FindOrCreateImage(possessBG.transform, "PossessFill", new Color(0.3f, 0.8f, 1f));
            StretchFromLeft(possessFill.rectTransform);

            // Centre-screen message for forced eject
            var message = FindOrCreateText(canvas, "MessageText");
            var mRect = message.rectTransform;
            mRect.anchorMin = mRect.anchorMax = mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.anchoredPosition = new Vector2(0, 150);
            mRect.sizeDelta = new Vector2(1200, 80);
            StyleText(message, 52, TextAnchor.MiddleCenter);
            message.color = new Color(1f, 0.6f, 0.15f);
            message.alignment = TextAnchor.MiddleCenter;
            message.horizontalOverflow = HorizontalWrapMode.Overflow;
            message.text = "FORCED OUT! Enemy is hostile";
            message.gameObject.SetActive(false);

            // Level timer: top-left under the health bar, clear of the play area
            var timer = so.FindProperty("levelTimerText").objectReferenceValue as Text;
            if (timer != null)
            {
                PlaceTopLeft(timer.rectTransform, new Vector2(30, -140), new Vector2(460, 56));
                StyleText(timer, 44, TextAnchor.MiddleLeft);
                timer.text = "TIME 5:00";
            }

            // Form text: bottom-left
            var form = so.FindProperty("currentFormText").objectReferenceValue as Text;
            if (form != null)
            {
                var f = form.rectTransform;
                f.anchorMin = f.anchorMax = f.pivot = Vector2.zero;
                f.anchoredPosition = new Vector2(30, 110); // above the objective bar
                f.sizeDelta = new Vector2(500, 60);
                StyleText(form, 44, TextAnchor.MiddleLeft);
            }

            // [E] POSSESS prompt: bottom-centre
            var prompt = so.FindProperty("possessPrompt").objectReferenceValue as GameObject;
            var promptText = prompt != null ? prompt.GetComponent<Text>() : null;
            if (promptText != null)
            {
                promptText.rectTransform.sizeDelta = new Vector2(500, 70);
                promptText.rectTransform.anchoredPosition = new Vector2(0, 150);
                StyleText(promptText, 34, TextAnchor.MiddleCenter); // floats over the enemy at runtime
                promptText.color = new Color(1f, 0.9f, 0.3f);
            }

            // Objective bar: bottom-centre strip that tells the player the next step
            var objectiveBG = FindOrCreateImage(canvas, "ObjectiveBar", new Color(0f, 0f, 0f, 0.55f));
            var oRect = objectiveBG.rectTransform;
            oRect.anchorMin = oRect.anchorMax = oRect.pivot = new Vector2(0.5f, 0);
            oRect.anchoredPosition = new Vector2(0, 30);
            oRect.sizeDelta = new Vector2(1100, 64);
            var objective = FindOrCreateText(objectiveBG.transform, "ObjectiveText");
            var otRect = objective.rectTransform;
            otRect.anchorMin = Vector2.zero;
            otRect.anchorMax = Vector2.one;
            otRect.offsetMin = otRect.offsetMax = Vector2.zero;
            StyleText(objective, 32, TextAnchor.MiddleCenter);
            objective.color = new Color(1f, 0.95f, 0.75f);
            objective.text = "1/3  Possess the GUARD and shoot the TARGET to unlock the door";
            so.FindProperty("objectiveText").objectReferenceValue = objective;

            // Wire references
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("possessTimerGroup").objectReferenceValue = group.gameObject;
            so.FindProperty("possessTimerFill").objectReferenceValue = possessFill;
            so.FindProperty("messageText").objectReferenceValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();
            group.gameObject.SetActive(false);

            // End screens
            StylePanel(so.FindProperty("levelCompletePanel").objectReferenceValue as GameObject, hud,
                ":)", "LEVEL COMPLETE", "The spirit is finally free!",
                new Color(0.02f, 0.10f, 0.05f, 0.88f), new Color(0.35f, 0.95f, 0.5f), new Color(0.2f, 0.65f, 0.35f));
            StylePanel(so.FindProperty("gameOverPanel").objectReferenceValue as GameObject, hud,
                ":(", "GAME OVER", "Your spirit faded into the dark...",
                new Color(0.08f, 0.02f, 0.03f, 0.92f), new Color(0.85f, 0.2f, 0.25f), new Color(0.6f, 0.15f, 0.2f));

            // Hostile enemies need to deal damage so the health bar has something to show
            int enemies = 0;
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (enemy.GetComponent<EnemyContactDamage>() == null)
                    enemy.gameObject.AddComponent<EnemyContactDamage>();
                enemies++;
            }
            Debug.Log($"[P5] EnemyContactDamage on {enemies} enemies.");

            var scene = hud.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AddScenesToBuild();
            SetupMainMenu();
            Debug.Log("[P5] HUD, end screens, build scenes and main menu set up and saved.");
        }

        // ---------------- End screens ----------------

        static void StylePanel(GameObject panel, HUDManager hud, string face, string title, string subtitle,
            Color background, Color titleColor, Color primaryButton)
        {
            if (panel == null) return;

            // Draw on top of every other HUD element (objective bar, prompt, ...)
            panel.transform.SetAsLastSibling();

            var bg = panel.GetComponent<Image>();
            if (bg != null) bg.color = background;

            var faceText = FindOrCreateText(panel.transform, "Face");
            PlaceCentre(faceText.rectTransform, new Vector2(0, 250), new Vector2(300, 160));
            StyleText(faceText, 140, TextAnchor.MiddleCenter);
            faceText.text = face;
            faceText.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0.6f);

            var titleText = FindOrCreateText(panel.transform, "Title");
            PlaceCentre(titleText.rectTransform, new Vector2(0, 110), new Vector2(1200, 130));
            StyleText(titleText, 110, TextAnchor.MiddleCenter);
            titleText.text = title;
            titleText.color = titleColor;

            var oldSub = panel.transform.Find("Subtitle");
            if (oldSub != null) Object.DestroyImmediate(oldSub.gameObject);

            var restart = FindOrCreateButton(panel.transform, "RestartButton", "RESTART", new Vector2(0, -110), primaryButton);
            SetSingleListener(restart.onClick, hud.OnRestartButton);

            var menu = FindOrCreateButton(panel.transform, "MenuButton", "MAIN MENU", new Vector2(0, -210),
                new Color(0.22f, 0.22f, 0.26f));
            SetSingleListener(menu.onClick, hud.OnQuitButton);
        }

        // ---------------- Build scenes + main menu ----------------

        const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        const string LevelPath = "Assets/Scenes/Level_01.unity";

        static void AddScenesToBuild()
        {
            // MainMenu first so a build starts on the menu; Restart uses the active scene's own index
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(LevelPath, true)
            };
        }

        static void SetupMainMenu()
        {
            var menuScene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Additive);

            Transform canvas = null, play = null, quit = null;
            foreach (var root in menuScene.GetRootGameObjects())
            {
                if (root.GetComponent<Canvas>() != null) canvas = root.transform;
                play ??= FindDeep(root.transform, "PlayButton");
                quit ??= FindDeep(root.transform, "QuitButton");
            }

            if (canvas != null)
            {
                var controller = canvas.GetComponent<MainMenuController>();
                if (controller == null) controller = canvas.gameObject.AddComponent<MainMenuController>();

                if (play != null)
                {
                    var btn = StyleButton(play.GetComponent<RectTransform>(), "PLAY",
                        new Vector2(0, -40), new Color(0.3f, 0.45f, 0.85f));
                    SetSingleListener(btn.onClick, controller.OnPlayButton);
                }
                if (quit != null)
                {
                    var btn = StyleButton(quit.GetComponent<RectTransform>(), "QUIT",
                        new Vector2(0, -140), new Color(0.22f, 0.22f, 0.26f));
                    SetSingleListener(btn.onClick, controller.OnQuitButton);
                }
            }
            else
            {
                Debug.LogWarning("[P5] MainMenu has no Canvas; buttons not wired.");
            }

            EditorSceneManager.MarkSceneDirty(menuScene);
            EditorSceneManager.SaveScene(menuScene);
            EditorSceneManager.CloseScene(menuScene, true);
        }

        static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        static void SetSingleListener(UnityEvent evt, UnityAction action)
        {
            for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(evt, i);
            UnityEventTools.AddPersistentListener(evt, action);
        }

        // ---------------- Helpers ----------------

        static RectTransform FindOrCreateRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static Image FindOrCreateImage(Transform parent, string name, Color color)
        {
            var rect = FindOrCreateRect(parent, name);
            var img = rect.GetComponent<Image>();
            if (img == null) img = rect.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text FindOrCreateText(Transform parent, string name)
        {
            var rect = FindOrCreateRect(parent, name);
            var txt = rect.GetComponent<Text>();
            if (txt == null) txt = rect.gameObject.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.raycastTarget = false;
            return txt;
        }

        static Button FindOrCreateButton(Transform parent, string name, string label, Vector2 pos, Color color)
        {
            return StyleButton(FindOrCreateRect(parent, name), label, pos, color);
        }

        // Rounded button with hover / pressed tints
        static Button StyleButton(RectTransform rect, string label, Vector2 pos, Color color)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(380, 80);

            var img = rect.GetComponent<Image>();
            if (img == null) img = rect.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = Color.white; // tint comes from the button colours below
            img.raycastTarget = true;

            var btn = rect.GetComponent<Button>();
            if (btn == null) btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(color, Color.black, 0.3f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var txt = FindOrCreateText(rect, "Text");
            var tRect = txt.rectTransform;
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            tRect.anchoredPosition = Vector2.zero;
            StyleText(txt, 34, TextAnchor.MiddleCenter);
            txt.text = label;
            txt.color = Color.white;
            return btn;
        }

        static void PlaceCentre(RectTransform r, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        // Bold with a dark outline so text stays readable over the light floor
        static void StyleText(Text txt, int size, TextAnchor alignment)
        {
            txt.fontSize = size;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = txt.GetComponent<Outline>();
            if (outline == null) outline = txt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        static void PlaceTopLeft(RectTransform r, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        // Fills the parent; pivot on the left so scaling X shrinks it toward the left edge
        static void StretchFromLeft(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0, 0.5f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            r.localScale = Vector3.one;
        }
    }
}
