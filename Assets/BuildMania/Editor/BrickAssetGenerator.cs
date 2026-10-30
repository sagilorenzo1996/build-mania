using System.Collections.Generic;
using BuildMania.Bricks;
using UnityEditor;
using UnityEngine;

namespace BuildMania.EditorTools
{
    /// <summary>
    /// Generates the starter brick assets: meshes, prefabs, definitions, the catalog,
    /// and the realistic colour palette. Safe to re-run; existing assets keep their GUIDs.
    /// Batch mode: <c>-executeMethod BuildMania.EditorTools.BrickAssetGenerator.GenerateStarterBricks</c>.
    /// </summary>
    public static class BrickAssetGenerator
    {
        const string Root = "Assets/BuildMania";
        const string DefinitionsFolder = Root + "/Bricks/Definitions";
        const string MeshesFolder = Root + "/Bricks/Meshes";
        const string PrefabsFolder = Root + "/Bricks/Prefabs";
        const string CatalogPath = Root + "/Bricks/StarterBricks.asset";
        const string MaterialsFolder = Root + "/Materials/Realistic";
        const string PalettePath = MaterialsFolder + "/RealisticPalette.asset";

        const int StudSegments = 12;
        const int PinSegments = 10;
        const int WheelSegments = 32;
        const float PinRadius = BrickGrid.Pitch * 0.15f;
        const float PinLength = BrickGrid.Pitch * 0.3f;
        const float PlasticSmoothness = 0.75f;

        // Palette order is shared by every theme; models store these indices.
        static readonly (string name, Color color)[] RealisticColours =
        {
            ("White", new Color(0.95f, 0.95f, 0.93f)),
            ("Red", new Color(0.78f, 0.07f, 0.07f)),
            ("Black", new Color(0.08f, 0.08f, 0.09f)),
            ("Yellow", new Color(0.98f, 0.78f, 0.05f)),
            ("Blue", new Color(0.05f, 0.30f, 0.75f)),
            ("LightGrey", new Color(0.63f, 0.65f, 0.66f)),
            ("DarkGrey", new Color(0.35f, 0.36f, 0.37f)),
            ("Green", new Color(0.10f, 0.55f, 0.20f)),
        };
        const int DefaultColour = 5;
        const int WheelColour = 2;

        [MenuItem("Build Mania/Generate Starter Bricks")]
        public static void GenerateStarterBricks()
        {
            foreach (var folder in new[] { DefinitionsFolder, MeshesFolder, PrefabsFolder, MaterialsFolder })
                EnsureFolder(folder);

            var materials = GeneratePalette();
            var definitions = new List<BrickDefinition>();
            foreach (var spec in StarterBrickSet.All)
                definitions.Add(GenerateBrick(spec, materials[spec.Shape == BrickShape.Wheel ? WheelColour : DefaultColour]));

            var catalog = LoadOrCreate<BrickCatalog>(CatalogPath);
            catalog.SetBricks(definitions);
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            Debug.Log($"Build Mania: generated {definitions.Count} starter bricks and {materials.Count} palette materials.");
        }

        static List<Material> GeneratePalette()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new System.InvalidOperationException("URP Lit shader not found; is the Universal Render Pipeline package installed?");

            var materials = new List<Material>();
            foreach (var (colourName, colour) in RealisticColours)
            {
                var path = $"{MaterialsFolder}/Brick_{colourName}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.shader = shader;
                material.SetColor("_BaseColor", colour);
                material.SetFloat("_Smoothness", PlasticSmoothness);
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
                materials.Add(material);
            }

            var palette = LoadOrCreate<BrickPalette>(PalettePath);
            palette.SetMaterials(materials);
            EditorUtility.SetDirty(palette);
            return materials;
        }

        static BrickDefinition GenerateBrick(BrickSpec spec, Material material)
        {
            var definitionPath = $"{DefinitionsFolder}/{spec.Id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<BrickDefinition>(definitionPath);
            if (definition == null)
            {
                definition = BrickDefinition.Create(spec);
                AssetDatabase.CreateAsset(definition, definitionPath);
            }

            var mesh = SaveMesh(BuildMesh(spec), $"{MeshesFolder}/{spec.Id}.asset");
            var prefab = SavePrefab(spec, definition, mesh, material);

            definition.Configure(spec, prefab);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        static Bounds BodyBounds(BrickSpec spec)
        {
            const float gap = BrickGrid.SeamGap;
            var min = new Vector3(-0.5f * BrickGrid.Pitch + gap, 0f, -0.5f * BrickGrid.Pitch + gap);
            var max = new Vector3(
                (spec.Size.x - 0.5f) * BrickGrid.Pitch - gap,
                spec.Size.y * BrickGrid.PlateHeight,
                (spec.Size.z - 0.5f) * BrickGrid.Pitch - gap);
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        static Mesh BuildMesh(BrickSpec spec)
        {
            var builder = new MeshBuilder();
            var body = BodyBounds(spec);

            switch (spec.Shape)
            {
                case BrickShape.Box:
                    builder.AddBox(body.min, body.max);
                    break;
                case BrickShape.Slope:
                    AddSlope(builder, body.min, body.max,
                        ridgeZ: (spec.Size.z - 1.5f) * BrickGrid.Pitch,
                        lipY: body.min.y + BrickGrid.PlateHeight);
                    break;
                case BrickShape.Wheel:
                    var radius = Mathf.Min(body.size.y, body.size.z) * 0.5f;
                    var start = new Vector3(body.min.x, body.center.y, body.center.z);
                    builder.AddCylinder(start, Vector3.right, radius, body.size.x, WheelSegments, capStart: true, capEnd: true);
                    break;
            }

            foreach (var connector in spec.Connectors)
            {
                var position = BrickGrid.HalfToLocal(connector.HalfPosition);
                var direction = (Vector3)connector.Facing.ToVector();
                if (connector.Kind == ConnectorKind.Stud)
                    builder.AddCylinder(position, direction, BrickGrid.StudRadius, BrickGrid.StudHeight, StudSegments, capStart: false, capEnd: true);
                else if (connector.Kind == ConnectorKind.Pin)
                    builder.AddCylinder(position, direction, PinRadius, PinLength, PinSegments, capStart: false, capEnd: true);
            }

            return builder.ToMesh(spec.Id);
        }

        /// <summary>Full height behind <paramref name="ridgeZ"/>, sloping down to a one-plate lip at the front (-Z).</summary>
        static void AddSlope(MeshBuilder builder, Vector3 min, Vector3 max, float ridgeZ, float lipY)
        {
            float x0 = min.x, x1 = max.x, y0 = min.y, yTop = max.y, z0 = min.z, z1 = max.z;

            builder.AddPolygon(new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1));            // bottom
            builder.AddPolygon(new Vector3(x1, y0, z1), new Vector3(x1, yTop, z1), new Vector3(x0, yTop, z1), new Vector3(x0, y0, z1));        // back
            builder.AddPolygon(new Vector3(x0, yTop, ridgeZ), new Vector3(x0, yTop, z1), new Vector3(x1, yTop, z1), new Vector3(x1, yTop, ridgeZ)); // flat top
            builder.AddPolygon(new Vector3(x0, lipY, z0), new Vector3(x0, yTop, ridgeZ), new Vector3(x1, yTop, ridgeZ), new Vector3(x1, lipY, z0)); // slope
            builder.AddPolygon(new Vector3(x0, y0, z0), new Vector3(x0, lipY, z0), new Vector3(x1, lipY, z0), new Vector3(x1, y0, z0));        // front lip
            builder.AddPolygon(                                                                                                                // left side
                new Vector3(x0, y0, z1), new Vector3(x0, yTop, z1), new Vector3(x0, yTop, ridgeZ),
                new Vector3(x0, lipY, z0), new Vector3(x0, y0, z0));
            builder.AddPolygon(                                                                                                                // right side
                new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), new Vector3(x1, lipY, z0),
                new Vector3(x1, yTop, ridgeZ), new Vector3(x1, yTop, z1));
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
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

        static GameObject SavePrefab(BrickSpec spec, BrickDefinition definition, Mesh mesh, Material material)
        {
            var root = new GameObject(spec.Id);
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                root.AddComponent<MeshRenderer>().sharedMaterial = material;

                var body = BodyBounds(spec);
                var collider = root.AddComponent<BoxCollider>();
                collider.center = body.center;
                collider.size = body.size;

                var marker = new SerializedObject(root.AddComponent<BrickInstance>());
                marker.FindProperty("definition").objectReferenceValue = definition;
                marker.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{spec.Id}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
