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

        // Level
        static readonly Color FloorRoom1 = new Color(0.42f, 0.32f, 0.36f); // muted dusty pink
        static readonly Color FloorRoom2 = new Color(0.40f, 0.31f, 0.37f);
        static readonly Color FloorRoom3 = new Color(0.43f, 0.33f, 0.34f);
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

            EnsureFolder("Assets/Art", "Materials");
            EnsureFolder("Assets/Art", "Textures");

            // Rooms
            Paint("Room1_Floor", Mat("P5_FloorRoom1", FloorRoom1));
            Paint("Room2_Floor", Mat("P5_FloorRoom2", FloorRoom2));
            Paint("Room3_Floor", Mat("P5_FloorRoom3", FloorRoom3));
            var wallMat = Mat("P5_Wall", WallColor);
            foreach (var name in new[] { "Wall_Left", "Wall_Right", "Wall_Back", "Wall_Front",
                "Divider1_Left", "Divider1_Right", "Divider2_Left", "Divider2_Right",
                "WallPuzzle_Left", "WallPuzzle_Right" })
                Paint(name, wallMat);

            BuildSpirit();
            BuildGuard();
            BuildHeavy();
            BuildTarget();
            var padlock = BuildDoor();
            BuildBrickWall();
            BuildExit();

            BuildEffects();
            BuildLabels(padlock);
            UpdateFloorSign();

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            }

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[P5] Minimal visuals applied and scene saved.");
        }

        // ---------------- Characters ----------------

        static void BuildSpirit()
        {
            var spirit = Find("Spirit");
            if (spirit == null) return;

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
        static void BuildGuard()
        {
            var guard = Find("Guard");
            if (guard == null) return;
            ShowBody(guard, Mat("P5_PoliceBlue", PoliceBlue));
            var model = NewModel(guard.transform);
            var black = Mat("P5_Black", Black);

            // Police cap
            Part(model, PrimitiveType.Cylinder, "Cap", new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 0.07f, 0.7f), Mat("P5_PoliceDark", PoliceDark));
            Part(model, PrimitiveType.Cube, "CapBrim", new Vector3(0f, 0.9f, 0.36f), new Vector3(0.45f, 0.04f, 0.3f), black);
            // Pistol = can shoot
            Part(model, PrimitiveType.Cube, "Pistol", new Vector3(0.5f, -0.05f, 0.3f), new Vector3(0.12f, 0.15f, 0.42f), black);
        }

        static void BuildHeavy()
        {
            var heavy = Find("Heavy");
            if (heavy == null) return;
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

        static void BuildTarget()
        {
            var target = Find("Target");
            if (target == null) return;
            Paint(target, Mat("P5_Board", BoardColor));
            var model = NewModel(target.transform);

            // Bullseye rings on the side facing the player (-z). Discs are cylinders turned to face -z.
            var red = Mat("P5_BullRed", BullRed);
            var white = Mat("P5_Board", BoardColor);
            Disc(model, "Ring1", 0.95f, -0.52f, red);
            Disc(model, "Ring2", 0.70f, -0.54f, white);
            Disc(model, "Ring3", 0.45f, -0.56f, red);
            Disc(model, "Ring4", 0.20f, -0.58f, white);
        }

        static GameObject BuildDoor()
        {
            var door = Find("Door");
            if (door == null) return null;
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

        static void BuildBrickWall()
        {
            var wall = Find("BreakableWall");
            if (wall == null) return;

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
            var checker = CheckerTexture();

            // Checkered finish mat on the corridor floor (between the brick wall and the back wall)
            var finishMat = Mat("P5_CheckerMat", Color.white);
            finishMat.mainTexture = checker;
            finishMat.mainTextureScale = new Vector2(4f, 3f);
            Part(exit, PrimitiveType.Cube, "FinishMat", new Vector3(x, 0.11f, 34.7f), new Vector3(3.1f, 0.02f, 2.5f), finishMat);

            // Goal flag on the back wall: pole + checkered flag
            var flagMat = Mat("P5_CheckerFlag", Color.white);
            flagMat.mainTexture = checker;
            flagMat.mainTextureScale = new Vector2(2f, 1.5f);
            Part(exit, PrimitiveType.Cylinder, "FlagPole", new Vector3(x - 0.6f, 1.6f, 35.6f), new Vector3(0.08f, 1.6f, 0.08f), Mat("P5_Metal", Metal));
            Part(exit, PrimitiveType.Sphere, "PoleTop", new Vector3(x - 0.6f, 3.25f, 35.6f), Vector3.one * 0.18f, Mat("P5_HardHat", HardHat));
            Part(exit, PrimitiveType.Cube, "Flag", new Vector3(x - 0.05f, 2.75f, 35.6f), new Vector3(1.0f, 0.7f, 0.04f), flagMat);
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

        static void BuildEffects()
        {
            var flash = UnlitMat("P5_FXFlash", new Color(1f, 0.9f, 0.4f));
            var dust = UnlitMat("P5_FXDust", new Color(0.85f, 0.78f, 0.7f));

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

            // Brick wall: chunks + dust when smashed
            var wall = Find("BreakableWall");
            if (wall != null)
            {
                var wallFx = wall.GetComponent<WallBreakFX>();
                if (wallFx == null) wallFx = wall.AddComponent<WallBreakFX>();
                var so = new SerializedObject(wallFx);
                so.FindProperty("chunkMaterial").objectReferenceValue = Mat("P5_BrickChunk", new Color(0.68f, 0.32f, 0.24f));
                so.FindProperty("dustMaterial").objectReferenceValue = dust;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
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

        static void BuildLabels(GameObject padlock)
        {
            var old = Find("P5_Labels");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("P5_Labels").transform;

            Label(root, "GUARD", "Guard", new Vector3(0f, 1.7f, 0f), new Color(0.55f, 0.7f, 1f), "", null);
            Label(root, "HEAVY", "Heavy", new Vector3(0f, 2.1f, 0f), WorkerOrange, "", null);
            Label(root, "SHOOT ME", "Target", new Vector3(0f, 1.3f, 0f), new Color(1f, 0.35f, 0.3f), "HIT!", null);
            Label(root, "LOCKED", "Door", new Vector3(0f, 3.4f, -0.6f), DoorColor, "OPEN", padlock);
            Label(root, "BREAKABLE", "BreakableWall", new Vector3(0f, 3.4f, -0.6f), Color.white, "", null);
            Label(root, "EXIT", "ExitZone", new Vector3(0f, 3.0f, -0.8f), new Color(1f, 0.85f, 0.2f), "", null, 0.2f, true, true);
        }

        static void Label(Transform root, string text, string followName, Vector3 offset, Color color,
            string doneText, GameObject hideWhenDone, float size = 0.1f, bool hiddenUntilWallBroken = false,
            bool withBackground = false)
        {
            var follow = Find(followName);
            if (follow == null) return;

            var go = new GameObject("Label_" + followName);
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

        static void UpdateFloorSign()
        {
            var sign = Find("StartInstructions");
            var mesh = sign != null ? sign.GetComponent<TextMesh>() : null;
            if (mesh == null) return;
            mesh.text = "POSSESSION\nWASD move   |   SPACE jump   |   E possess / eject\n"
                + "GUARD (police): mouse aim + LMB shoot\n"
                + "HEAVY (worker): face wall + LMB smash\n"
                + "Reach the EXIT as the SPIRIT";
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

        static void Paint(string name, Material mat)
        {
            var go = Find(name);
            if (go != null) Paint(go, mat);
            else Debug.LogWarning($"[P5] {name} not found, skipped.");
        }

        static void Paint(GameObject go, Material mat)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
        }
    }
}
