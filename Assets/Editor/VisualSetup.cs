using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    // Minimal, readable look built from primitives:
    //   Spirit = purple ghost, Guard = police uniform + pistol, Heavy = construction worker + hammer,
    //   Target = bullseye, Door = amber gate with padlock, BreakableWall = bricks, Exit = green.
    // All extra parts have no colliders, so gameplay and physics are unchanged.
    public static class VisualSetup
    {
        const string MaterialFolder = "Assets/Art/Materials";
        const string TextureFolder = "Assets/Art/Textures";
        const string ModelName = "P5_Model";

        static readonly string[] LevelPaths =
        {
            "Assets/Scenes/Level_01.unity", "Assets/Scenes/Level_02.unity", "Assets/Scenes/Level_03.unity"
        };

        // Level: each level has its own floor colour so it is recognisable at a glance,
        // with three close shades so neighbouring rooms stay distinguishable
        static readonly string[] FloorMaterialPrefix = { "P5_FloorRoom", "P5_FloorL2_", "P5_FloorL3_" };
        static readonly Color[][] FloorPalettes =
        {
            new[] // Level_01: muted dusty pink
            {
                new Color(0.42f, 0.32f, 0.36f), new Color(0.40f, 0.31f, 0.37f), new Color(0.43f, 0.33f, 0.34f)
            },
            new[] // Level_02: muted sage green
            {
                new Color(0.33f, 0.42f, 0.36f), new Color(0.31f, 0.40f, 0.35f), new Color(0.35f, 0.43f, 0.34f)
            },
            new[] // Level_03: warm sand
            {
                new Color(0.47f, 0.41f, 0.31f), new Color(0.45f, 0.39f, 0.30f), new Color(0.48f, 0.42f, 0.29f)
            }
        };
        static readonly Color WallColor = new Color(0.55f, 0.72f, 0.90f);  // light blue

        // Characters
        static readonly Color GhostColor = Color.white;
        static readonly Color PoliceBlue = new Color(0.12f, 0.18f, 0.40f);
        static readonly Color PoliceDark = new Color(0.07f, 0.09f, 0.16f);
        static readonly Color WorkerOrange = new Color(0.95f, 0.50f, 0.10f);
        static readonly Color HardHat = new Color(1f, 0.85f, 0.10f);
        static readonly Color Metal = new Color(0.35f, 0.36f, 0.40f);
        static readonly Color Wood = new Color(0.55f, 0.36f, 0.20f);
        static readonly Color Black = new Color(0.06f, 0.06f, 0.08f);

        // Puzzle
        static readonly Color BoardColor = new Color(0.92f, 0.90f, 0.85f);
        static readonly Color BullRed = new Color(0.85f, 0.15f, 0.15f);
        static readonly Color DoorColor = new Color(0.85f, 0.62f, 0.15f);

        [MenuItem("Possession/P5/Apply Minimal Visuals")]
        public static void ApplyVisuals()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Visual Setup", "Stop Play mode first, then run this again.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder("Assets/Art", "Materials");
            EnsureFolder("Assets/Art", "Textures");
            BuildProjectileEffects();

            string startScene = EditorSceneManager.GetActiveScene().path;
            for (int i = 0; i < LevelPaths.Length; i++)
            {
                if (!System.IO.File.Exists(LevelPaths[i])) continue;
                var scene = EditorSceneManager.OpenScene(LevelPaths[i], OpenSceneMode.Single);
                ApplyToLevel(i);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(startScene) && System.IO.File.Exists(startScene))
                EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
        }

        static void ApplyToLevel(int level)
        {
            // Rooms: every *_Floor gets the level's palette, walls are the shared light blue
            var palette = FloorPalettes[Mathf.Min(level, FloorPalettes.Length - 1)];
            var prefix = FloorMaterialPrefix[Mathf.Min(level, FloorMaterialPrefix.Length - 1)];
            var floors = new System.Collections.Generic.List<GameObject>();
            foreach (var t in AllInScene<Transform>())
                if (t.name.EndsWith("_Floor") && t.GetComponent<Renderer>() != null) floors.Add(t.gameObject);
            floors.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < floors.Count; i++)
                Paint(floors[i], Mat(prefix + (i % palette.Length + 1), palette[i % palette.Length]));

            var wallMat = Mat("P5_Wall", WallColor);
            foreach (var name in new[] { "Wall_Left", "Wall_Right", "Wall_Back", "Wall_Front",
                "Divider1_Left", "Divider1_Right", "Divider2_Left", "Divider2_Right",
                "WallPuzzle_Left", "WallPuzzle_Right" })
            {
                var wall = Find(name);
                if (wall != null) Paint(wall, wallMat);
            }
            // P4 levels keep their walls under a "Walls" group
            var walls = Find("Walls");
            if (walls != null)
                foreach (var r in walls.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = wallMat;

            foreach (var spirit in AllInScene<SpiritController>()) BuildSpirit(spirit.gameObject);
            foreach (var guard in AllInScene<GuardEnemy>()) BuildGuard(guard.gameObject);
            foreach (var heavy in AllInScene<HeavyEnemy>()) BuildHeavy(heavy.gameObject);
            foreach (var target in AllInScene<Target>()) BuildTarget(target.gameObject);
            var padlocks = new System.Collections.Generic.Dictionary<Door, GameObject>();
            foreach (var door in AllInScene<Door>()) padlocks[door] = BuildDoor(door.gameObject);
            foreach (var wall in AllInScene<BreakableWall>())
            {
                BuildBrickWall(wall.gameObject);
                BuildWallEffects(wall.gameObject);
            }
            BuildExit();

            BuildLabels(padlocks);
            HideFloorSigns();

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            }
        }

        // ---------------- Characters ----------------

        static void BuildSpirit(GameObject spirit)
        {

            // The capsule itself is the ghost body (SpiritController hides it while possessing)
            Paint(spirit, Mat("P5_Ghost", GhostColor, emission: new Color(0.45f, 0.45f, 0.5f)));
            var model = NewModel(spirit.transform);
            var ghost = Mat("P5_Ghost", GhostColor, emission: new Color(0.45f, 0.45f, 0.5f));
            var black = Mat("P5_Black", Black);

            // Eyes and mouth face the camera (-z)
            Part(model, PrimitiveType.Sphere, "EyeL", new Vector3(-0.17f, 0.45f, -0.42f), new Vector3(0.16f, 0.22f, 0.1f), black);
            Part(model, PrimitiveType.Sphere, "EyeR", new Vector3(0.17f, 0.45f, -0.42f), new Vector3(0.16f, 0.22f, 0.1f), black);
            Part(model, PrimitiveType.Sphere, "Mouth", new Vector3(0f, 0.15f, -0.46f), new Vector3(0.12f, 0.12f, 0.06f), black);

            // Wavy hem: a ring of bumps around the bottom
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                Part(model, PrimitiveType.Sphere, "Hem" + i,
                    new Vector3(Mathf.Cos(a) * 0.32f, -0.9f, Mathf.Sin(a) * 0.32f), Vector3.one * 0.32f, ghost);
            }
            // Little arms
            Part(model, PrimitiveType.Sphere, "ArmL", new Vector3(-0.52f, 0.0f, 0f), new Vector3(0.22f, 0.4f, 0.22f), ghost);
            Part(model, PrimitiveType.Sphere, "ArmR", new Vector3(0.52f, 0.0f, 0f), new Vector3(0.22f, 0.4f, 0.22f), ghost);

            if (spirit.GetComponent<SpiritVisualSync>() == null)
                spirit.AddComponent<SpiritVisualSync>();
        }

        // Minimal: coloured body + hat + the tool that tells you its ability
        static void BuildGuard(GameObject guard)
        {
            ShowBody(guard, Mat("P5_PoliceBlue", PoliceBlue));
            var model = NewModel(guard.transform);
            var black = Mat("P5_Black", Black);

            // Police cap
            Part(model, PrimitiveType.Cylinder, "Cap", new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 0.07f, 0.7f), Mat("P5_PoliceDark", PoliceDark));
            Part(model, PrimitiveType.Cube, "CapBrim", new Vector3(0f, 0.9f, 0.36f), new Vector3(0.45f, 0.04f, 0.3f), black);
            // Pistol = can shoot
            Part(model, PrimitiveType.Cube, "Pistol", new Vector3(0.5f, -0.05f, 0.3f), new Vector3(0.12f, 0.15f, 0.42f), black);
        }

        static void BuildHeavy(GameObject heavy)
        {
            ShowBody(heavy, Mat("P5_WorkerOrange", WorkerOrange));
            var model = NewModel(heavy.transform);
            var hat = Mat("P5_HardHat", HardHat);

            // Hard hat
            Part(model, PrimitiveType.Sphere, "HardHat", new Vector3(0f, 0.88f, 0f), new Vector3(0.85f, 0.45f, 0.85f), hat);
            Part(model, PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 0.8f, 0.05f), new Vector3(1.0f, 0.02f, 1.0f), hat);
            // Sledgehammer = can smash
            Part(model, PrimitiveType.Cylinder, "HammerHandle", new Vector3(0.62f, -0.1f, 0.25f), new Vector3(0.08f, 0.45f, 0.08f), Mat("P5_Wood", Wood));
            Part(model, PrimitiveType.Cube, "HammerHead", new Vector3(0.62f, 0.38f, 0.25f), new Vector3(0.22f, 0.22f, 0.5f), Mat("P5_Metal", Metal));
        }

        // ---------------- Puzzle pieces ----------------

        static void BuildTarget(GameObject target)
        {
            Paint(target, Mat("P5_Board", BoardColor));
            var model = NewModel(target.transform);

            // Bullseye rings on both faces, so it reads from whichever side the Guard aims.
            // Discs are cylinders turned to face along z.
            var red = Mat("P5_BullRed", BullRed);
            var white = Mat("P5_Board", BoardColor);
            foreach (float side in new[] { -1f, 1f })
            {
                string suffix = side < 0f ? "" : "_Back";
                Disc(model, "Ring1" + suffix, 0.95f, 0.52f * side, red);
                Disc(model, "Ring2" + suffix, 0.70f, 0.54f * side, white);
                Disc(model, "Ring3" + suffix, 0.45f, 0.56f * side, red);
                Disc(model, "Ring4" + suffix, 0.20f, 0.58f * side, white);
            }
        }

        static GameObject BuildDoor(GameObject door)
        {
            Paint(door, Mat("P5_Door", DoorColor));
            var model = NewModel(door.transform);

            // Door is a stretched cube (3.2 x 6 x 0.5), so local sizes are divided by that
            var metal = Mat("P5_Metal", Metal);
            var dark = Mat("P5_Black", Black);
            var lockGO = new GameObject("Padlock");
            lockGO.transform.SetParent(model, false);
            Part(lockGO.transform, PrimitiveType.Cube, "Body", new Vector3(0f, -0.12f, -0.6f), new Vector3(0.7f / 3.2f, 0.55f / 6f, 0.2f), metal);
            Part(lockGO.transform, PrimitiveType.Cube, "Keyhole", new Vector3(0f, -0.125f, -0.72f), new Vector3(0.12f / 3.2f, 0.22f / 6f, 0.1f), dark);
            Part(lockGO.transform, PrimitiveType.Cube, "ShackleL", new Vector3(-0.2f / 3.2f, -0.05f, -0.6f), new Vector3(0.09f / 3.2f, 0.4f / 6f, 0.1f), metal);
            Part(lockGO.transform, PrimitiveType.Cube, "ShackleR", new Vector3(0.2f / 3.2f, -0.05f, -0.6f), new Vector3(0.09f / 3.2f, 0.4f / 6f, 0.1f), metal);
            Part(lockGO.transform, PrimitiveType.Cube, "ShackleTop", new Vector3(0f, -0.018f, -0.6f), new Vector3(0.49f / 3.2f, 0.09f / 6f, 0.1f), metal);
            return lockGO;
        }

        static void BuildBrickWall(GameObject wall)
        {
            var mat = Mat("P5_Brick", Color.white);
            mat.mainTexture = BrickTexture();
            // Wall is 3.2 x 6 m and the texture is 1 x 1 m (2 bricks wide, 4 rows)
            mat.mainTextureScale = new Vector2(wall.transform.localScale.x, wall.transform.localScale.y);
            EditorUtility.SetDirty(mat);
            Paint(wall, mat);
        }

        static Texture2D BrickTexture()
        {
            string path = $"{TextureFolder}/P5_Brick.png";
            const int size = 256, rows = 4, perRow = 2, mortar = 6;
            var tex = new Texture2D(size, size);
            var mortarColor = new Color(0.86f, 0.83f, 0.78f);
            var rng = new System.Random(7);
            int brickH = size / rows, brickW = size / perRow;

            for (int row = 0; row < rows; row++)
            {
                int shift = row % 2 == 0 ? 0 : brickW / 2;
                for (int b = -1; b <= perRow; b++)
                {
                    float shade = 0.85f + (float)rng.NextDouble() * 0.2f;
                    var brick = new Color(0.72f * shade, 0.33f * shade, 0.24f * shade);
                    for (int y = row * brickH; y < (row + 1) * brickH; y++)
                    for (int x = b * brickW + shift; x < (b + 1) * brickW + shift; x++)
                    {
                        if (x < 0 || x >= size) continue;
                        bool isMortar = y - row * brickH < mortar || x - (b * brickW + shift) < mortar;
                        tex.SetPixel(x, y, isMortar ? mortarColor : brick);
                    }
                }
            }
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void BuildExit()
        {
            // Replace P4's plain green block with a finish mat + goal flag (its trigger is kept)
            var marker = Find("ExitMarker");
            if (marker != null)
            {
                var pulse = marker.GetComponent<PulseGlow>();
                if (pulse != null) Object.DestroyImmediate(pulse);
                var r = marker.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }

            var zone = Find("ExitZone");
            if (zone == null) return;
            var old = Find("P5_ExitPad");
            if (old != null) Object.DestroyImmediate(old);
            old = Find("P5_Exit");
            if (old != null) Object.DestroyImmediate(old);

            var exit = new GameObject("P5_Exit").transform;
            float x = zone.transform.position.x;
            float z = zone.transform.position.z;
            float back = BackWallSide(zone.transform.position); // +1: back wall is further along z
            var checker = CheckerTexture();

            // Checkered finish mat on the floor in front of the exit
            var finishMat = Mat("P5_CheckerMat", Color.white);
            finishMat.mainTexture = checker;
            finishMat.mainTextureScale = new Vector2(4f, 3f);
            Part(exit, PrimitiveType.Cube, "FinishMat", new Vector3(x, 0.11f, z - 0.8f * back), new Vector3(3.1f, 0.02f, 2.5f), finishMat);

            // Goal flag against the back wall: pole + checkered flag
            var flagMat = Mat("P5_CheckerFlag", Color.white);
            flagMat.mainTexture = checker;
            flagMat.mainTextureScale = new Vector2(2f, 1.5f);
            float flagZ = z + 0.1f * back;
            Part(exit, PrimitiveType.Cylinder, "FlagPole", new Vector3(x - 0.6f, 1.6f, flagZ), new Vector3(0.08f, 1.6f, 0.08f), Mat("P5_Metal", Metal));
            Part(exit, PrimitiveType.Sphere, "PoleTop", new Vector3(x - 0.6f, 3.25f, flagZ), Vector3.one * 0.18f, Mat("P5_HardHat", HardHat));
            Part(exit, PrimitiveType.Cube, "Flag", new Vector3(x - 0.05f, 2.75f, flagZ), new Vector3(1.0f, 0.7f, 0.04f), flagMat);
        }

        // The exit sits at a dead end: the nearer solid wall along z is the one behind it
        static float BackWallSide(Vector3 zonePos)
        {
            Physics.SyncTransforms();
            float ahead = Physics.Raycast(zonePos, Vector3.forward, out var hitAhead, 50f, ~0, QueryTriggerInteraction.Ignore)
                ? hitAhead.distance : float.MaxValue;
            float behind = Physics.Raycast(zonePos, Vector3.back, out var hitBehind, 50f, ~0, QueryTriggerInteraction.Ignore)
                ? hitBehind.distance : float.MaxValue;
            return behind < ahead ? -1f : 1f;
        }

        static Texture2D CheckerTexture()
        {
            string path = $"{TextureFolder}/P5_Checker.png";
            const int size = 64, cells = 2;
            var tex = new Texture2D(size, size);
            var light = new Color(0.95f, 0.95f, 0.95f);
            var dark = new Color(0.12f, 0.12f, 0.14f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool odd = (x * cells / size + y * cells / size) % 2 == 1;
                tex.SetPixel(x, y, odd ? dark : light);
            }
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            // Crisp squares, no blur
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void AddOnce<T>(GameObject go) where T : Component
        {
            if (go.GetComponent<T>() == null) go.AddComponent<T>();
        }

        // ---------------- Effects ----------------

        const string ProjectilePrefab = "Assets/Prefabs/Projectile.prefab";

        static void BuildProjectileEffects()
        {
            var flash = UnlitMat("P5_FXFlash", new Color(1f, 0.9f, 0.4f));

            // Projectile: glowing bullet + short trail + muzzle flash / impact puff (behaviour untouched)
            var root = PrefabUtility.LoadPrefabContents(ProjectilePrefab);
            try
            {
                root.GetComponent<Renderer>().sharedMaterial =
                    Mat("P5_Bullet", new Color(1f, 0.85f, 0.3f), emission: new Color(1f, 0.7f, 0.2f));

                var trail = root.GetComponent<TrailRenderer>();
                if (trail == null) trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.12f;
                trail.startWidth = 0.14f;
                trail.endWidth = 0f;
                trail.minVertexDistance = 0.05f;
                trail.sharedMaterial = flash;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var fx = root.GetComponent<ProjectileFX>();
                if (fx == null) fx = root.AddComponent<ProjectileFX>();
                var fxSo = new SerializedObject(fx);
                fxSo.FindProperty("flashMaterial").objectReferenceValue = flash;
                fxSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Brick wall: chunks + dust when smashed
        static void BuildWallEffects(GameObject wall)
        {
            var wallFx = wall.GetComponent<WallBreakFX>();
            if (wallFx == null) wallFx = wall.AddComponent<WallBreakFX>();
            var so = new SerializedObject(wallFx);
            so.FindProperty("chunkMaterial").objectReferenceValue = Mat("P5_BrickChunk", new Color(0.68f, 0.32f, 0.24f));
            so.FindProperty("dustMaterial").objectReferenceValue = UnlitMat("P5_FXDust", new Color(0.85f, 0.78f, 0.7f));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material UnlitMat(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Unlit/Color"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------------- Labels + sign ----------------

        static void BuildLabels(System.Collections.Generic.Dictionary<Door, GameObject> padlocks)
        {
            var old = Find("P5_Labels");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("P5_Labels").transform;

            // Objects P4 already labelled keep their label; it only learns to hide the padlock
            var labelled = new System.Collections.Generic.HashSet<Transform>();
            foreach (var existing in AllInScene<WorldLabel>())
            {
                var so = new SerializedObject(existing);
                var follow = so.FindProperty("follow").objectReferenceValue as Transform;
                if (follow == null) continue;
                labelled.Add(follow);
                var door = follow.GetComponent<Door>();
                if (door != null && padlocks.TryGetValue(door, out var padlock) && padlock != null)
                {
                    so.FindProperty("hideWhenDone").objectReferenceValue = padlock;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            foreach (var guard in AllInScene<GuardEnemy>())
                Label(root, labelled, "GUARD", guard.gameObject, new Vector3(0f, 1.7f, 0f), new Color(0.55f, 0.7f, 1f), "", null);
            foreach (var heavy in AllInScene<HeavyEnemy>())
                Label(root, labelled, "HEAVY", heavy.gameObject, new Vector3(0f, 2.1f, 0f), WorkerOrange, "", null);
            foreach (var target in AllInScene<Target>())
                Label(root, labelled, "SHOOT ME", target.gameObject, new Vector3(0f, 1.3f, 0f), new Color(1f, 0.35f, 0.3f), "HIT!", null);
            foreach (var pair in padlocks)
                Label(root, labelled, "LOCKED", pair.Key.gameObject, new Vector3(0f, 3.4f, -0.6f), DoorColor, "OPEN", pair.Value);
            foreach (var wall in AllInScene<BreakableWall>())
                Label(root, labelled, "BREAKABLE", wall.gameObject, new Vector3(0f, 3.4f, -0.6f), Color.white, "", null);
            var zone = Find("ExitZone");
            if (zone != null)
                Label(root, labelled, "EXIT", zone, new Vector3(0f, 3.0f, -0.8f * BackWallSide(zone.transform.position)),
                    new Color(1f, 0.85f, 0.2f), "", null, 0.2f, true, true);
        }

        static void Label(Transform root, System.Collections.Generic.HashSet<Transform> labelled, string text,
            GameObject follow, Vector3 offset, Color color, string doneText, GameObject hideWhenDone,
            float size = 0.1f, bool hiddenUntilWallBroken = false, bool withBackground = false)
        {
            if (labelled.Contains(follow.transform)) return;

            var go = new GameObject("Label_" + follow.name);
            go.transform.SetParent(root, false);
            go.transform.position = follow.transform.position + offset;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = font;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;

            Renderer background = null;
            if (withBackground)
            {
                // Dark box behind the text; WorldLabel sizes it to the text every frame
                var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bg.name = "Background";
                Object.DestroyImmediate(bg.GetComponent<Collider>());
                bg.transform.SetParent(go.transform, false);
                background = bg.GetComponent<Renderer>();
                background.sharedMaterial = UnlitMat("P5_LabelBG", new Color(0.08f, 0.08f, 0.1f));
                background.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var label = go.AddComponent<WorldLabel>();
            label.Init(follow.transform, offset, follow.GetComponent<Target>(), follow.GetComponent<Door>(),
                doneText, hideWhenDone, hiddenUntilWallBroken, background);
        }

        // Floor text (start instructions, Sign_* hints) repeats the HUD's controls and objective bar,
        // so it is switched off; the objects stay in the scene
        static void HideFloorSigns()
        {
            foreach (var t in AllInScene<Transform>())
                if (t.name == "StartInstructions" || t.name.StartsWith("Sign_"))
                    t.gameObject.SetActive(false);
        }

        // ---------------- Helpers ----------------

        static Transform NewModel(Transform parent)
        {
            // Rebuild from scratch each run; also clears parts from older versions of this tool
            foreach (var oldName in new[] { ModelName, "Visor", "Shoulders" })
            {
                var old = parent.Find(oldName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            var model = new GameObject(ModelName).transform;
            model.SetParent(parent, false);
            return model;
        }

        static void ShowBody(GameObject go, Material mat)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.enabled = true; // an earlier version of this tool hid it
            renderer.sharedMaterial = mat;
        }

        static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 localScale,
            Material mat, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = localScale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void Disc(Transform parent, string name, float diameter, float z, Material mat)
        {
            // Cylinder turned 90 deg on X so its flat face points along -z
            Part(parent, PrimitiveType.Cylinder, name, new Vector3(0f, 0f, z),
                new Vector3(diameter, 0.02f, diameter), mat, Quaternion.Euler(90f, 0f, 0f));
        }

        static Material Mat(string name, Color color, Color? emission = null)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            mat.SetFloat("_Glossiness", 0.15f); // matte
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }

        static GameObject Find(string name)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindDeep(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
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

        static System.Collections.Generic.List<T> AllInScene<T>() where T : Component
        {
            var all = new System.Collections.Generic.List<T>();
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                all.AddRange(root.GetComponentsInChildren<T>(true));
            return all;
        }

        static void Paint(GameObject go, Material mat)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
        }
    }
}
