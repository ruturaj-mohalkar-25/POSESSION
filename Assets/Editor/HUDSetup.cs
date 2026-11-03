using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Possession.Editor
{
    public static class HUDSetup
    {
        static readonly Color BarBackground = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        // Objective steps per level: the bar advances on every DoorOpened / WallBroken
        static readonly string[][] LevelObjectives =
        {
            new[]
            {
                "1/3  POSSESS GUARD  •  HIT TARGET",
                "2/3  POSSESS HEAVY  •  BREAK WALL",
                "3/3  EJECT  •  REACH EXIT"
            },
            new[]
            {
                "1/3  POSSESS GUARD  •  HIT 2 TARGETS",
                "2/3  POSSESS HEAVY  •  BREAK WALL",
                "3/3  EJECT  •  REACH EXIT"
            },
            new[]
            {
                "1/4  POSSESS GUARD  •  HIT TARGET",
                "2/4  POSSESS HEAVY  •  BREAK WALL 1",
                "3/4  POSSESS HEAVY  •  BREAK WALL 2",
                "4/4  EJECT  •  REACH EXIT"
            }
        };

        [MenuItem("Possession/P1/Apply HUD Cleanup")]
        public static void ApplyHUDCleanup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("HUD Cleanup", "Stop Play mode first, then run this again.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            for (int i = 0; i < LevelPaths.Length; i++)
            {
                if (!System.IO.File.Exists(LevelPaths[i])) continue;
                var scene = EditorSceneManager.OpenScene(LevelPaths[i], OpenSceneMode.Single);
                var hud = FindInScene<HUDManager>(scene);
                if (hud == null) continue;
                StyleHUD(hud);
                SetObjectives(hud, i);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("P1 HUD cleanup applied to all available levels.");
        }

        [MenuItem("Possession/P5/Setup HUD Bars and Buttons")]
        public static void SetupHUD()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("HUD Setup", "Stop Play mode first, then run this again.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Level_01 holds the source HUD; the other levels get a copy of it
            var level1 = EditorSceneManager.OpenScene(LevelPaths[0], OpenSceneMode.Single);
            var hud = FindInScene<HUDManager>(level1);
            if (hud == null)
            {
                EditorUtility.DisplayDialog("HUD Setup", "No HUDManager found in Level_01.", "OK");
                return;
            }

            StyleHUD(hud);
            SetupLevel(level1, hud, 0);

            for (int i = 1; i < LevelPaths.Length; i++)
            {
                if (!System.IO.File.Exists(LevelPaths[i])) continue;
                var scene = EditorSceneManager.OpenScene(LevelPaths[i], OpenSceneMode.Additive);

                var old = FindInScene<HUDManager>(scene);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var copy = Object.Instantiate(hud.gameObject);
                copy.name = hud.gameObject.name;
                SceneManager.MoveGameObjectToScene(copy, scene);

                EnsureEventSystem(scene);
                SetupLevel(scene, copy.GetComponent<HUDManager>(), i);
                EditorSceneManager.CloseScene(scene, true);
            }

            AddScenesToBuild();
            SetupMainMenu();
        }

        static void StyleHUD(HUDManager hud)
        {
            var canvas = hud.transform;
            var so = new SerializedObject(hud);

            // Health text: top-left, with the bar underneath
            var healthText = so.FindProperty("healthText").objectReferenceValue as Text;
            if (healthText != null)
            {
                PlaceTopLeft(healthText.rectTransform, new Vector2(24, -18), new Vector2(280, 36));
                StyleText(healthText, 26, TextAnchor.MiddleLeft);
                healthText.text = "HP 3/3";
            }

            var healthBG = FindOrCreateImage(canvas, "HealthBarBG", BarBackground);
            PlaceTopLeft(healthBG.rectTransform, new Vector2(24, -58), new Vector2(280, 18));
            var healthFill = FindOrCreateImage(healthBG.transform, "HealthFill", new Color(0.2f, 0.85f, 0.3f));
            StretchFromLeft(healthFill.rectTransform);

            // Possession timer group: a small bar that PossessionCues keeps above the possessed body.
            // HUDManager colours the fill green -> yellow -> red as possession time runs out.
            var white = new Color(0.95f, 0.95f, 1f);
            var group = FindOrCreateRect(canvas, "PossessTimerGroup");
            group.anchorMin = group.anchorMax = new Vector2(0.5f, 0.5f);
            group.pivot = new Vector2(0.5f, 0f);
            group.anchoredPosition = Vector2.zero;
            group.sizeDelta = new Vector2(160, 14);

            // Bar only: the countdown text stays wired to HUDManager but is not shown
            var possessText = so.FindProperty("possessTimerText").objectReferenceValue as Text;
            if (possessText != null)
            {
                possessText.transform.SetParent(group, false);
                possessText.gameObject.SetActive(false);
            }

            var possessBG = FindOrCreateImage(group, "PossessBarBG", BarBackground);
            var bgRect = possessBG.rectTransform;
            bgRect.anchorMin = new Vector2(0, 0);
            bgRect.anchorMax = new Vector2(1, 0);
            bgRect.pivot = new Vector2(0.5f, 0);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(0, 14);
            var possessFill = FindOrCreateImage(possessBG.transform, "PossessFill", new Color(0.2f, 0.85f, 0.3f));
            StretchFromLeft(possessFill.rectTransform);

            // Short ability hint under the possessed body, positioned and faded by PossessionCues
            var hint = FindOrCreateText(canvas, "AbilityHint");
            var hRect = hint.rectTransform;
            hRect.anchorMin = hRect.anchorMax = hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.sizeDelta = new Vector2(300, 44);
            StyleText(hint, 30, TextAnchor.MiddleCenter);
            hint.color = white;
            hint.text = "Shoot";
            hint.gameObject.SetActive(false);

            var cues = canvas.GetComponent<PossessionCues>();
            if (cues == null) cues = canvas.gameObject.AddComponent<PossessionCues>();
            var cso = new SerializedObject(cues);
            cso.FindProperty("timerGroup").objectReferenceValue = group;
            cso.FindProperty("hintText").objectReferenceValue = hint;
            cso.FindProperty("possessedMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_Ghost.mat");
            // Guard aim line: a thick warm-gold beam matching the bullet colour
            cso.FindProperty("aimColor").colorValue = new Color(1f, 0.75f, 0.2f, 0.9f);
            cso.FindProperty("aimStartWidth").floatValue = 0.22f;
            cso.FindProperty("aimEndWidth").floatValue = 0.08f;
            cso.ApplyModifiedPropertiesWithoutUndo();

            // Centre-screen message for forced eject
            var message = FindOrCreateText(canvas, "MessageText");
            var mRect = message.rectTransform;
            mRect.anchorMin = mRect.anchorMax = mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.anchoredPosition = new Vector2(0, 90);
            mRect.sizeDelta = new Vector2(900, 70);
            StyleText(message, 44, TextAnchor.MiddleCenter);
            message.color = new Color(0.2f, 0.85f, 0.3f);
            message.alignment = TextAnchor.MiddleCenter;
            message.horizontalOverflow = HorizontalWrapMode.Overflow;
            message.text = "DOOR OPEN";
            message.gameObject.SetActive(false);

            // Level timer: top-left under the health bar, clear of the play area
            var timer = so.FindProperty("levelTimerText").objectReferenceValue as Text;
            if (timer != null)
            {
                PlaceTopLeft(timer.rectTransform, new Vector2(24, -84), new Vector2(280, 36));
                StyleText(timer, 28, TextAnchor.MiddleLeft);
                timer.text = "TIME 5:00";
            }

            // Form and controls duplicate the character/world-space cues and obscure play.
            var form = so.FindProperty("currentFormText").objectReferenceValue as Text;
            if (form != null) Object.DestroyImmediate(form.gameObject);
            so.FindProperty("currentFormText").objectReferenceValue = null;

            // [E] POSSESS prompt: bottom-centre
            var prompt = so.FindProperty("possessPrompt").objectReferenceValue as GameObject;
            var promptText = prompt != null ? prompt.GetComponent<Text>() : null;
            if (promptText != null)
            {
                promptText.rectTransform.sizeDelta = new Vector2(500, 70);
                promptText.rectTransform.anchoredPosition = new Vector2(0, 150);
                StyleText(promptText, 34, TextAnchor.MiddleCenter); // floats over the enemy at runtime
                promptText.color = new Color(0.2f, 0.85f, 0.3f);
            }

            // Objective bar: bottom-centre strip that tells the player the next step
            var objectiveBG = FindOrCreateImage(canvas, "ObjectiveBar", new Color(0f, 0f, 0f, 0.55f));
            var oRect = objectiveBG.rectTransform;
            oRect.anchorMin = oRect.anchorMax = oRect.pivot = new Vector2(0.5f, 0);
            oRect.anchoredPosition = new Vector2(0, 24);
            oRect.sizeDelta = new Vector2(820, 52);
            var objective = FindOrCreateText(objectiveBG.transform, "ObjectiveText");
            var otRect = objective.rectTransform;
            otRect.anchorMin = Vector2.zero;
            otRect.anchorMax = Vector2.one;
            otRect.offsetMin = otRect.offsetMax = Vector2.zero;
            StyleText(objective, 26, TextAnchor.MiddleCenter);
            objective.color = new Color(1f, 0.95f, 0.75f);
            objective.text = "1/3  POSSESS GUARD  •  HIT TARGET";
            so.FindProperty("objectiveText").objectReferenceValue = objective;

            // Removed full-screen damage flash; clean it up if an earlier run added it
            var oldFlash = canvas.Find("DamageFlash");
            if (oldFlash != null) Object.DestroyImmediate(oldFlash.gameObject);

            var controls = so.FindProperty("controlsText").objectReferenceValue as Text;
            var controlsPanel = canvas.Find("ControlsPanel");
            if (controlsPanel != null) Object.DestroyImmediate(controlsPanel.gameObject);
            else if (controls != null) Object.DestroyImmediate(controls.gameObject);
            so.FindProperty("controlsText").objectReferenceValue = null;

            // Ammo pips: follow the mouse cursor while possessing, positioned at runtime
            var ammoRow = FindOrCreateRect(canvas, "AmmoRow");
            ammoRow.anchorMin = ammoRow.anchorMax = ammoRow.pivot = new Vector2(0, 0.5f);
            ammoRow.sizeDelta = new Vector2(200, 30);
            var oldAmmoLabel = ammoRow.Find("Label");
            if (oldAmmoLabel != null) Object.DestroyImmediate(oldAmmoLabel.gameObject);
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
            so.FindProperty("alertColor").colorValue = new Color(0.9f, 0.2f, 0.2f);
            so.FindProperty("successColor").colorValue = new Color(0.2f, 0.85f, 0.3f);
            so.FindProperty("objectiveColor").colorValue = new Color(0.95f, 0.75f, 0.2f);
            so.ApplyModifiedPropertiesWithoutUndo();
            group.gameObject.SetActive(false);

            // End screens
            var dark = new Color(0.22f, 0.22f, 0.26f);
            var redBtn = new Color(0.6f, 0.15f, 0.2f);
            var greenBtn = new Color(0.2f, 0.65f, 0.35f);
            var goldBtn = new Color(0.75f, 0.58f, 0.15f);

            var levelComplete = so.FindProperty("levelCompletePanel").objectReferenceValue as GameObject;
            StylePanel(levelComplete,
                ":)", "LEVEL COMPLETE", new Color(0.02f, 0.10f, 0.05f, 0.88f), new Color(0.35f, 0.95f, 0.5f), true,
                ("NextButton", "NEXT LEVEL", hud.OnNextLevelButton, greenBtn),
                ("RestartButton", "RESTART", hud.OnRestartButton, dark),
                ("MenuButton", "MAIN MENU", hud.OnQuitButton, dark));
            var nextLabel = levelComplete != null ? levelComplete.transform.Find("NextButton/Text") : null;
            so.FindProperty("nextButtonLabel").objectReferenceValue = nextLabel != null ? nextLabel.GetComponent<Text>() : null;

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
        }

        // Objectives, hostile enemies and analytics for one level scene, then save it
        static void SetupLevel(Scene scene, HUDManager hud, int levelIndex)
        {
            SetObjectives(hud, levelIndex);

            // The end screen summary comes from AnalyticsTracker, which lives next to the GameManager
            var gm = FindInScene<GameManager>(scene);
            if (gm != null && FindInScene<AnalyticsTracker>(scene) == null)
                gm.gameObject.AddComponent<AnalyticsTracker>();

            // Hostile enemies need to deal damage so the health bar has something to show
            var dust = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_FXDust.mat");
            foreach (var enemy in FindAllInScene<EnemyBase>(scene))
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
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void SetObjectives(HUDManager hud, int levelIndex)
        {
            var so = new SerializedObject(hud);
            var steps = LevelObjectives[Mathf.Min(levelIndex, LevelObjectives.Length - 1)];
            var prop = so.FindProperty("objectives");
            prop.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = steps[i];
            var objective = so.FindProperty("objectiveText").objectReferenceValue as Text;
            if (objective != null) objective.text = steps[0];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        static System.Collections.Generic.List<T> FindAllInScene<T>(Scene scene) where T : Component
        {
            var all = new System.Collections.Generic.List<T>();
            foreach (var root in scene.GetRootGameObjects())
                all.AddRange(root.GetComponentsInChildren<T>(true));
            return all;
        }

        // End screen buttons need an EventSystem to receive clicks
        static void EnsureEventSystem(Scene scene)
        {
            if (FindInScene<EventSystem>(scene) != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        // ---------------- End screens ----------------

        static void StylePanel(GameObject panel, string face, string title, Color background, Color titleColor,
            bool withResults, params (string name, string label, UnityAction action, Color color)[] buttons)
        {
            if (panel == null) return;

            // Draw on top of every other HUD element (objective bar, prompt, ...)
            panel.transform.SetAsLastSibling();

            var bg = panel.GetComponent<Image>();
            if (bg != null) bg.color = background;

            float top = withResults ? 300f : 250f;
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

            float y = top - 310f;
            if (withResults)
            {
                y = AddResults(panel, top - 220f);
            }
            else
            {
                foreach (var name in new[] { "Medal", "Stats" })
                {
                    var child = panel.transform.Find(name);
                    if (child != null) Object.DestroyImmediate(child.gameObject);
                }
                var view = panel.GetComponent<ResultsView>();
                if (view != null) Object.DestroyImmediate(view);
            }

            foreach (var b in buttons)
            {
                var btn = FindOrCreateButton(panel.transform, b.name, b.label, new Vector2(0, y), b.color);
                SetSingleListener(btn.onClick, b.action);
                y -= 95f;
            }
        }

        // Medal row (gold / silver / bronze) and the analytics line; returns where buttons start
        static float AddResults(GameObject panel, float y)
        {
            var results = panel.GetComponent<ResultsView>();
            if (results == null) results = panel.AddComponent<ResultsView>();
            var rso = new SerializedObject(results);

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

            var stats = FindOrCreateText(panel.transform, "Stats");
            PlaceCentre(stats.rectTransform, new Vector2(0, y), new Vector2(1400, 44));
            StyleText(stats, 28, TextAnchor.MiddleCenter);
            stats.fontStyle = FontStyle.Normal;
            stats.color = new Color(0.85f, 0.85f, 0.85f);
            stats.text = "TIME 0:00    POSSESSIONS 0    FORCED EJECTS 0    DEATHS 0";
            rso.FindProperty("statsText").objectReferenceValue = stats;
            y -= 50f;

            var chartLabel = FindOrCreateText(panel.transform, "ChartLabel");
            PlaceCentre(chartLabel.rectTransform, new Vector2(0, y), new Vector2(1400, 30));
            StyleText(chartLabel, 22, TextAnchor.MiddleCenter);
            chartLabel.color = new Color(0.7f, 0.7f, 0.7f);
            chartLabel.text = "POSSESSION BREAKDOWN";
            y -= 30f;

            var chart = FindOrCreateRect(panel.transform, "ChartContainer");
            PlaceCentre(chart, new Vector2(0, y), new Vector2(800, 36));
            rso.FindProperty("chartContainer").objectReferenceValue = chart;
            rso.ApplyModifiedPropertiesWithoutUndo();

            return y - 80f;
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
