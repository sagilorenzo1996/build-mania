using BuildMania.Bricks;
using BuildMania.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static BuildMania.Bricks.StarterBrickSet;

namespace BuildMania.EditorTools
{
    /// <summary>
    /// Generates the portable build area prefab: a 16x16 baseplate with the brick tray,
    /// colour swatches, undo/redo buttons and two carry handles in front of it.
    /// The player stands on the -Z side; the prefab origin is the centre of the baseplate surface.
    /// </summary>
    public static class BuildAreaGenerator
    {
        public const string PrefabPath = "Assets/BuildMania/Prefabs/BuildArea.prefab";
        const string MeshesFolder = "Assets/BuildMania/Bricks/Meshes";
        const string CatalogPath = "Assets/BuildMania/Bricks/StarterBricks.asset";
        const string PalettePath = "Assets/BuildMania/Materials/Realistic/RealisticPalette.asset";
        const string GhostMaterialPath = "Assets/BuildMania/Materials/SnapGhost.mat";

        const int BaseplateStuds = 16;
        const float P = BrickGrid.Pitch;
        const float H = BrickGrid.PlateHeight;

        // Tray layout, in studs from the baseplate centre (negative Z is towards the player).
        const float TrayNear = -14.75f;
        const float TrayFar = -8.5f;
        const float RowA = -9.75f;
        const float RowB = -12.25f;
        const float RowC = -14f;
        const float HandleX = 9.25f;

        // Realistic palette indices.
        const int White = 0, Black = 2, Blue = 4, LightGrey = 5, DarkGrey = 6, Green = 7;

        [MenuItem("Build Mania/Generate Build Area Prefab")]
        public static GameObject GenerateBuildAreaPrefab()
        {
            BrickAssetGenerator.GenerateStarterBricks();
            EnsureFolder("Assets/BuildMania/Prefabs");

            var catalog = AssetDatabase.LoadAssetAtPath<BrickCatalog>(CatalogPath);
            var palette = AssetDatabase.LoadAssetAtPath<BrickPalette>(PalettePath);
            var ghost = GenerateGhostMaterial();

            var root = new GameObject("BuildArea");
            try
            {
                var area = root.AddComponent<BuildArea>();

                var grid = Child(root.transform, "Grid", new Vector3(-(BaseplateStuds - 1) * 0.5f * P, 0f, -(BaseplateStuds - 1) * 0.5f * P));
                var view = grid.gameObject.AddComponent<BrickAssemblyView>();
                SetFields(view, ("baseplateSize", new Vector2Int(BaseplateStuds, BaseplateStuds)), ("palette", palette));
                AddVisual(grid, "Baseplate", Vector3.zero, SaveMesh(BaseplateMesh(), "baseplate_16x16"), palette.Get(Green));

                BuildTray(root.transform, area, catalog, palette);

                foreach (var side in new[] { -1f, 1f })
                {
                    var handle = Child(root.transform, side < 0 ? "Handle (Left)" : "Handle (Right)",
                        new Vector3(side * HandleX * P, 0f, (TrayNear + TrayFar) * 0.5f * P));
                    AddVisual(handle, "Knob", Vector3.zero, SaveMesh(KnobMesh(), "area_handle"), palette.Get(Blue));
                    var capsule = handle.gameObject.AddComponent<CapsuleCollider>();
                    capsule.center = new Vector3(0f, 0.05f, 0f);
                    capsule.radius = 0.025f;
                    capsule.height = 0.1f;
                    handle.gameObject.AddComponent<AreaHandle>().Configure(area);
                }

                SetFields(area, ("assemblyView", view), ("palette", palette), ("ghostMaterial", ghost));
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Build Mania: build area prefab written to {PrefabPath}.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void BuildTray(Transform root, BuildArea area, BrickCatalog catalog, BrickPalette palette)
        {
            var tray = Child(root, "Tray", Vector3.zero);
            var panelMin = new Vector3(-BaseplateStuds * 0.5f * P, -H, TrayNear * P);
            var panelMax = new Vector3(BaseplateStuds * 0.5f * P, 0f, TrayFar * P);
            var panel = new MeshBuilder();
            panel.AddBox(panelMin, panelMax);
            AddVisual(tray, "Panel", Vector3.zero, SaveMesh(panel.ToMesh("tray_panel"), "tray_panel"), palette.Get(DarkGrey));

            // Long bricks are turned a quarter so they lie across the tray.
            AddSlot(tray, area, catalog, Brick1x2, -6.5f, RowA, 0f);
            AddSlot(tray, area, catalog, Brick2x2, -3.5f, RowA, 0f);
            AddSlot(tray, area, catalog, Brick2x4, 0.5f, RowA, 90f);
            AddSlot(tray, area, catalog, Plate2x4, 5.5f, RowA, 90f);
            AddSlot(tray, area, catalog, Slope2x2, -6f, RowB, 0f);
            AddSlot(tray, area, catalog, AxlePlate2x2, -3f, RowB, 0f);
            AddSlot(tray, area, catalog, Wheel, -0.5f, RowB, 0f);

            AddHistoryButton(tray, area, palette, HistoryButton.Kind.Undo, 3f);
            AddHistoryButton(tray, area, palette, HistoryButton.Kind.Redo, 6.25f);

            // Each colour tile sits on a white frame so dark colours still stand out from the tray.
            const float frameHeight = 0.2f * H;
            const float tileHeight = 0.4f * H;
            var frameMesh = new MeshBuilder();
            frameMesh.AddBox(new Vector3(-0.75f * P, 0f, -0.75f * P), new Vector3(0.75f * P, frameHeight, 0.75f * P));
            var swatchFrame = SaveMesh(frameMesh.ToMesh("colour_swatch_frame"), "colour_swatch_frame");
            var tileMesh = new MeshBuilder();
            tileMesh.AddBox(new Vector3(-0.6f * P, 0f, -0.6f * P), new Vector3(0.6f * P, tileHeight, 0.6f * P));
            var swatchMesh = SaveMesh(tileMesh.ToMesh("colour_swatch"), "colour_swatch");
            for (var i = 0; i < palette.Count; i++)
            {
                var swatch = Child(tray, $"Swatch {i}", new Vector3((-7f + 2f * i) * P, 0f, RowC * P));
                AddVisual(swatch, "Frame", Vector3.zero, swatchFrame, palette.Get(White));
                var tile = AddVisual(swatch, "Tile", new Vector3(0f, frameHeight, 0f), swatchMesh, palette.Get(i));
                var button = swatch.gameObject.AddComponent<ColorSwatch>();
                button.Configure(area, i, tile);
                button.ConfigureZone(new Vector3(1.8f * P, 0.04f, 1.8f * P), new Vector3(0f, frameHeight + tileHeight + 0.02f, 0f));
            }
        }

        static void AddSlot(Transform tray, BuildArea area, BrickCatalog catalog, string brickId, float x, float z, float yaw)
        {
            catalog.TryGet(brickId, out var definition);
            var slot = Child(tray, $"Slot {brickId}", new Vector3(x * P, 0f, z * P));
            slot.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // Same box TraySlot sizes at runtime, so the prefab is right before play too.
            var bounds = definition.Prefab.GetComponent<MeshFilter>().sharedMesh.bounds;
            var grabBox = slot.gameObject.AddComponent<BoxCollider>();
            grabBox.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
            grabBox.size = bounds.size + Vector3.one * 0.01f;
            slot.gameObject.AddComponent<TraySlot>().Configure(definition, area);
        }

        static void AddHistoryButton(Transform tray, BuildArea area, BrickPalette palette, HistoryButton.Kind kind, float x)
        {
            const float size = 2.2f * P;
            const float baseHeight = 0.6f * H;
            var button = Child(tray, kind.ToString(), new Vector3(x * P, 0f, RowB * P));

            var baseMesh = new MeshBuilder();
            baseMesh.AddBox(new Vector3(-size * 0.5f, 0f, -size * 0.5f), new Vector3(size * 0.5f, baseHeight, size * 0.5f));
            AddVisual(button, "Base", Vector3.zero, SaveMesh(baseMesh.ToMesh("button_base"), "button_base"), palette.Get(LightGrey));

            // Arrow pointing -X for undo; turned around for redo.
            var icon = AddVisual(button, "Icon", new Vector3(0f, baseHeight, 0f), SaveMesh(ArrowMesh(size * 0.3f, 0.3f * H), "button_arrow"), palette.Get(Black));
            if (kind == HistoryButton.Kind.Redo)
                icon.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var historyButton = button.gameObject.AddComponent<HistoryButton>();
            historyButton.Configure(area, kind);
            historyButton.ConfigureZone(new Vector3(size, 0.04f, size), new Vector3(0f, baseHeight + 0.02f, 0f));
        }

        static Mesh BaseplateMesh()
        {
            var builder = new MeshBuilder();
            const float gap = BrickGrid.SeamGap;
            builder.AddBox(
                new Vector3(-0.5f * P + gap, -H, -0.5f * P + gap),
                new Vector3((BaseplateStuds - 0.5f) * P - gap, 0f, (BaseplateStuds - 0.5f) * P - gap));
            for (var x = 0; x < BaseplateStuds; x++)
            for (var z = 0; z < BaseplateStuds; z++)
                builder.AddCylinder(new Vector3(x * P, 0f, z * P), Vector3.up, BrickGrid.StudRadius, BrickGrid.StudHeight, 12, capStart: false, capEnd: true);
            return builder.ToMesh("baseplate_16x16");
        }

        static Mesh KnobMesh()
        {
            var builder = new MeshBuilder();
            builder.AddCylinder(Vector3.zero, Vector3.up, 0.03f, 0.008f, 24, capStart: true, capEnd: true);
            builder.AddCylinder(new Vector3(0f, 0.008f, 0f), Vector3.up, 0.012f, 0.07f, 16, capStart: false, capEnd: false);
            builder.AddCylinder(new Vector3(0f, 0.078f, 0f), Vector3.up, 0.022f, 0.025f, 24, capStart: true, capEnd: true);
            return builder.ToMesh("area_handle");
        }

        /// <summary>Flat triangular prism pointing towards -X.</summary>
        static Mesh ArrowMesh(float halfSize, float height)
        {
            var builder = new MeshBuilder();
            var top = new[] { new Vector3(-halfSize, height, 0f), new Vector3(halfSize, height, halfSize), new Vector3(halfSize, height, -halfSize) };
            var bottom = new[] { new Vector3(-halfSize, 0f, 0f), new Vector3(halfSize, 0f, halfSize), new Vector3(halfSize, 0f, -halfSize) };
            builder.AddPolygon(top[0], top[1], top[2]);
            builder.AddPolygon(bottom[0], bottom[2], bottom[1]);
            for (var i = 0; i < 3; i++)
            {
                var j = (i + 1) % 3;
                builder.AddPolygon(bottom[j], top[j], top[i], bottom[i]);
            }
            return builder.ToMesh("button_arrow");
        }

        static Material GenerateGhostMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GhostMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, GhostMaterialPath);
            }

            // URP Unlit, transparent with alpha blending.
            material.SetColor("_BaseColor", new Color(0.55f, 0.9f, 1f, 0.45f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        static Transform AddVisual(Transform parent, string name, Vector3 localPosition, Mesh mesh, Material material)
        {
            var visual = Child(parent, name, localPosition);
            visual.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            visual.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return visual;
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            var path = $"{MeshesFolder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static void SetFields(Object target, params (string name, object value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (var (name, value) in fields)
            {
                var property = serialized.FindProperty(name);
                switch (value)
                {
                    case Object reference:
                        property.objectReferenceValue = reference;
                        break;
                    case Vector2Int vector:
                        property.vector2IntValue = vector;
                        break;
                    case bool flag:
                        property.boolValue = flag;
                        break;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
