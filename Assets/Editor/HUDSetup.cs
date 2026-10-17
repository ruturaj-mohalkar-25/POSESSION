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
            if (hud == null && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(LevelPaths[0]);
                hud = Object.FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);
            }
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

            // Removed full-screen damage flash; clean it up if an earlier run added it
            var oldFlash = canvas.Find("DamageFlash");
            if (oldFlash != null) Object.DestroyImmediate(oldFlash.gameObject);

            // Controls overlay: bottom-right, text changes with the current form
            var controlsBG = FindOrCreateImage(canvas, "ControlsPanel", new Color(0f, 0f, 0f, 0.45f));
            var cRect = controlsBG.rectTransform;
            cRect.anchorMin = cRect.anchorMax = cRect.pivot = new Vector2(1, 0);
            cRect.anchoredPosition = new Vector2(-30, 110);
            cRect.sizeDelta = new Vector2(520, 170);
            var controls = FindOrCreateText(controlsBG.transform, "ControlsText");
            var ctRect = controls.rectTransform;
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.offsetMin = new Vector2(22, 12);
            ctRect.offsetMax = new Vector2(-22, -12);
            StyleText(controls, 28, TextAnchor.MiddleLeft);
            controls.lineSpacing = 1.15f;
            controls.color = new Color(0.92f, 0.92f, 0.95f);
            controls.text = "WASD  Move\nSPACE  Jump\nE  Possess (near an enemy)";
            so.FindProperty("controlsText").objectReferenceValue = controls;

            // Ammo pips: under the possession timer, top-right
            var ammoRow = FindOrCreateRect(canvas, "AmmoRow");
            ammoRow.anchorMin = ammoRow.anchorMax = ammoRow.pivot = new Vector2(1, 1);
            ammoRow.anchoredPosition = new Vector2(-30, -134);
            ammoRow.sizeDelta = new Vector2(460, 40);
            var ammoLabel = FindOrCreateText(ammoRow, "Label");
            var alRect = ammoLabel.rectTransform;
            alRect.anchorMin = alRect.anchorMax = alRect.pivot = new Vector2(1, 0.5f);
            alRect.anchoredPosition = new Vector2(-130, 0);
            alRect.sizeDelta = new Vector2(200, 40);
            StyleText(ammoLabel, 28, TextAnchor.MiddleRight);
            ammoLabel.text = "AMMO";
            var pips = FindOrCreateRect(ammoRow, "Pips");
            pips.anchorMin = Vector2.zero;
            pips.anchorMax = Vector2.one;
            pips.offsetMin = pips.offsetMax = Vector2.zero;
            var ammo = ammoRow.GetComponent<AmmoDisplay>();
            if (ammo == null) ammo = ammoRow.gameObject.AddComponent<AmmoDisplay>();
            var ammoSo = new SerializedObject(ammo);
            ammoSo.FindProperty("pipRow").objectReferenceValue = pips;
            ammoSo.FindProperty("pipSprite").objectReferenceValue = KnobSprite();
            ammoSo.ApplyModifiedPropertiesWithoutUndo();
            so.FindProperty("ammoDisplay").objectReferenceValue = ammo;
            ammoRow.gameObject.SetActive(false);

            // Wire references
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("possessTimerGroup").objectReferenceValue = group.gameObject;
            so.FindProperty("possessTimerFill").objectReferenceValue = possessFill;
            so.FindProperty("messageText").objectReferenceValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();
            group.gameObject.SetActive(false);

            // End screens
            var dark = new Color(0.22f, 0.22f, 0.26f);
            var greenBtn = new Color(0.2f, 0.65f, 0.35f);
            var redBtn = new Color(0.6f, 0.15f, 0.2f);
            var goldBtn = new Color(0.75f, 0.58f, 0.15f);

            StylePanel(so.FindProperty("levelCompletePanel").objectReferenceValue as GameObject,
                ":)", "LEVEL COMPLETE", new Color(0.02f, 0.10f, 0.05f, 0.88f), new Color(0.35f, 0.95f, 0.5f), true,
                ("NextButton", "NEXT LEVEL", hud.OnNextLevelButton, greenBtn),
                ("RestartButton", "RESTART", hud.OnRestartButton, dark),
                ("MenuButton", "MAIN MENU", hud.OnQuitButton, dark));

            StylePanel(so.FindProperty("gameOverPanel").objectReferenceValue as GameObject,
                ":(", "GAME OVER", new Color(0.08f, 0.02f, 0.03f, 0.92f), new Color(0.85f, 0.2f, 0.25f), false,
                ("RestartButton", "RESTART", hud.OnRestartButton, redBtn),
                ("MenuButton", "MAIN MENU", hud.OnQuitButton, dark));

            var gameComplete = FindOrCreatePanel(canvas, "GameCompletePanel");
            StylePanel(gameComplete,
                "", "GAME COMPLETE", new Color(0.09f, 0.07f, 0.02f, 0.92f), new Color(1f, 0.82f, 0.25f), true,
                ("PlayAgainButton", "PLAY AGAIN", hud.OnPlayAgainButton, goldBtn),
                ("MenuButton", "MAIN MENU", hud.OnQuitButton, dark));
            gameComplete.SetActive(false);
            so.FindProperty("gameCompletePanel").objectReferenceValue = gameComplete;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Hostile enemies need to deal damage so the health bar has something to show
            int enemies = 0;
            var dust = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_FXDust.mat");
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var hostile = enemy.GetComponent<EnemyContactDamage>();
                if (hostile == null) hostile = enemy.gameObject.AddComponent<EnemyContactDamage>();

                // A hostile Guard fires the same bullet the player uses when possessing it
                var hso = new SerializedObject(hostile);
                var shoot = enemy.GetComponent<ShootAbility>();
                if (shoot != null)
                {
                    var sso = new SerializedObject(shoot);
                    hso.FindProperty("projectilePrefab").objectReferenceValue = sso.FindProperty("projectilePrefab").objectReferenceValue;
                    hso.FindProperty("firePoint").objectReferenceValue = sso.FindProperty("firePoint").objectReferenceValue;
                }
                hso.FindProperty("dustMaterial").objectReferenceValue = dust;
                hso.FindProperty("shootRange").floatValue = 4.5f;
                hso.FindProperty("smashRange").floatValue = 3f;
                hso.ApplyModifiedPropertiesWithoutUndo();
                enemies++;
            }
            Debug.Log($"[P5] Hostile attacks set up on {enemies} enemies.");

            var scene = hud.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AddScenesToBuild();
            SetupMainMenu();
            Debug.Log("[P5] HUD, controls, ammo, end screens, build scenes and level select set up and saved.");
        }

        // ---------------- End screens ----------------

        static void StylePanel(GameObject panel, string face, string title, Color background, Color titleColor,
            bool withMedal, params (string name, string label, UnityAction action, Color color)[] buttons)
        {
            if (panel == null) return;

            // Draw on top of every other HUD element (objective bar, prompt, ...)
            panel.transform.SetAsLastSibling();

            var bg = panel.GetComponent<Image>();
            if (bg != null) bg.color = background;

            float top = withMedal ? 300f : 250f;
            if (string.IsNullOrEmpty(face))
            {
                var oldFace = panel.transform.Find("Face");
                if (oldFace != null) Object.DestroyImmediate(oldFace.gameObject);
            }
            else
            {
                var faceText = FindOrCreateText(panel.transform, "Face");
                PlaceCentre(faceText.rectTransform, new Vector2(0, top), new Vector2(300, 150));
                StyleText(faceText, 120, TextAnchor.MiddleCenter);
                faceText.text = face;
                faceText.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0.6f);
            }

            var titleText = FindOrCreateText(panel.transform, "Title");
            PlaceCentre(titleText.rectTransform, new Vector2(0, top - 125f), new Vector2(1200, 120));
            StyleText(titleText, 100, TextAnchor.MiddleCenter);
            titleText.text = title;
            titleText.color = titleColor;

            var oldSub = panel.transform.Find("Subtitle");
            if (oldSub != null) Object.DestroyImmediate(oldSub.gameObject);

            var results = panel.GetComponent<ResultsView>();
            if (results == null) results = panel.AddComponent<ResultsView>();
            var rso = new SerializedObject(results);

            float y = top - 220f;
            if (withMedal)
            {
                var medal = FindOrCreateRect(panel.transform, "Medal");
                PlaceCentre(medal, new Vector2(0, y), new Vector2(360, 50));
                var pipsProp = rso.FindProperty("medalPips");
                pipsProp.arraySize = 3;
                for (int i = 0; i < 3; i++)
                {
                    var pip = FindOrCreateImage(medal, "Pip" + i, new Color(1f, 1f, 1f, 0.15f));
                    pip.sprite = KnobSprite();
                    PlaceCentre(pip.rectTransform, new Vector2(-150 + i * 50, 0), new Vector2(40, 40));
                    pipsProp.GetArrayElementAtIndex(i).objectReferenceValue = pip;
                }
                var medalText = FindOrCreateText(medal, "MedalText");
                var mt = medalText.rectTransform;
                mt.anchorMin = mt.anchorMax = new Vector2(0.5f, 0.5f);
                mt.pivot = new Vector2(0, 0.5f);
                mt.anchoredPosition = new Vector2(-10, 0);
                mt.sizeDelta = new Vector2(200, 50);
                StyleText(medalText, 40, TextAnchor.MiddleLeft);
                medalText.text = "GOLD";
                rso.FindProperty("medalText").objectReferenceValue = medalText;
                y -= 60f;
            }

            var stats = FindOrCreateText(panel.transform, "Stats");
            PlaceCentre(stats.rectTransform, new Vector2(0, y), new Vector2(1400, 44));
            StyleText(stats, 28, TextAnchor.MiddleCenter);
            stats.fontStyle = FontStyle.Normal;
            stats.color = new Color(0.85f, 0.85f, 0.85f);
            stats.text = "TIME 0:00    POSSESSIONS 0    FORCED EJECTS 0    DEATHS 0";
            rso.FindProperty("statsText").objectReferenceValue = stats;
            rso.ApplyModifiedPropertiesWithoutUndo();

            y -= 90f;
            foreach (var b in buttons)
            {
                var btn = FindOrCreateButton(panel.transform, b.name, b.label, new Vector2(0, y), b.color);
                SetSingleListener(btn.onClick, b.action);
                y -= 95f;
            }
        }

        static GameObject FindOrCreatePanel(Transform parent, string name)
        {
            var rect = FindOrCreateRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            if (rect.GetComponent<Image>() == null) rect.gameObject.AddComponent<Image>();
            return rect.gameObject;
        }

        static Sprite KnobSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }

        // ---------------- Build scenes + main menu ----------------

        const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        static readonly string[] LevelPaths =
        {
            "Assets/Scenes/Level_01.unity", "Assets/Scenes/Level_02.unity", "Assets/Scenes/Level_03.unity"
        };

        static void AddScenesToBuild()
        {
            // MainMenu first, then levels in order (Next Level uses build index + 1).
            // Any other scenes already in the list are kept after them.
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainMenuPath, true)
            };
            foreach (var path in LevelPaths)
                if (System.IO.File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            foreach (var existing in EditorBuildSettings.scenes)
                if (System.IO.File.Exists(existing.path) && !scenes.Exists(s => s.path == existing.path))
                    scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
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
                var cso = new SerializedObject(controller);

                // Scale with the window so every button fits (editor Game view and browser alike)
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                var title = canvas.Find("Title") as RectTransform;
                if (title != null)
                {
                    PlaceCentre(title, new Vector2(0, 280), new Vector2(1400, 180));
                    var titleText = title.GetComponent<Text>();
                    if (titleText != null) titleText.fontSize = 140;
                }

                // PLAY + QUIT share a container so level select can swap them out together
                var mainButtons = FindOrCreateRect(canvas, "MainButtons");
                mainButtons.anchorMin = Vector2.zero;
                mainButtons.anchorMax = Vector2.one;
                mainButtons.offsetMin = mainButtons.offsetMax = Vector2.zero;
                cso.FindProperty("mainButtons").objectReferenceValue = mainButtons.gameObject;

                if (play != null)
                {
                    play.SetParent(mainButtons, false);
                    var btn = StyleButton(play.GetComponent<RectTransform>(), "PLAY",
                        new Vector2(0, 40), new Color(0.3f, 0.45f, 0.85f), new Vector2(420, 90));
                    SetSingleListener(btn.onClick, controller.OnPlayButton);
                }
                if (quit != null)
                {
                    quit.SetParent(mainButtons, false);
                    var btn = StyleButton(quit.GetComponent<RectTransform>(), "QUIT",
                        new Vector2(0, -80), new Color(0.22f, 0.22f, 0.26f), new Vector2(420, 90));
                    SetSingleListener(btn.onClick, controller.OnQuitButton);
                    cso.FindProperty("quitButton").objectReferenceValue = quit.gameObject;
                }

                var select = FindOrCreateRect(canvas, "LevelSelectPanel");
                select.anchorMin = Vector2.zero;
                select.anchorMax = Vector2.one;
                select.offsetMin = select.offsetMax = Vector2.zero;
                var buttonsProp = cso.FindProperty("levelButtons");
                buttonsProp.arraySize = 3;
                for (int i = 0; i < 3; i++)
                {
                    var btn = StyleButton(FindOrCreateRect(select, $"Level{i + 1}Button"), $"LEVEL {i + 1}",
                        new Vector2(0, 80 - i * 105), new Color(0.3f, 0.45f, 0.85f), new Vector2(480, 85));
                    ClearListeners(btn.onClick);
                    UnityEventTools.AddIntPersistentListener(btn.onClick, controller.OnLevelButton, i);
                    buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = btn;
                }
                var back = StyleButton(FindOrCreateRect(select, "BackButton"), "BACK",
                    new Vector2(0, -260), new Color(0.22f, 0.22f, 0.26f), new Vector2(300, 75));
                SetSingleListener(back.onClick, controller.OnBackButton);
                cso.FindProperty("levelSelectPanel").objectReferenceValue = select.gameObject;
                select.gameObject.SetActive(false);

                cso.ApplyModifiedPropertiesWithoutUndo();
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
            ClearListeners(evt);
            UnityEventTools.AddPersistentListener(evt, action);
        }

        static void ClearListeners(UnityEvent evt)
        {
            for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(evt, i);
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
        static Button StyleButton(RectTransform rect, string label, Vector2 pos, Color color, Vector2? size = null)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size ?? new Vector2(380, 80);

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
            StyleText(txt, size.HasValue ? 30 : 34, TextAnchor.MiddleCenter);
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
