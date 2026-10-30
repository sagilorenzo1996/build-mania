using System.IO;
using BuildMania.Building;
using BuildMania.Content;
using BuildMania.Interaction;
using UnityEditor;
using UnityEngine;

namespace BuildMania.EditorTools
{
    /// <summary>
    /// Content tooling: the model catalog, exporting a build as a new guided model, and the
    /// TextMesh Pro resources the menus need.
    /// </summary>
    public static class ContentGenerator
    {
        public const string ModelCatalogPath = "Assets/BuildMania/Data/ModelCatalog.asset";
        const string ModelsFolder = "Assets/BuildMania/Data/Models";
        const string StarterCarPath = ModelsFolder + "/starter_car.json";

        /// <summary>Creates the catalog if needed and makes sure the Starter Car is listed.</summary>
        public static ModelCatalog EnsureModelCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ModelCatalog>(ModelCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ModelCatalog>();
                AssetDatabase.CreateAsset(catalog, ModelCatalogPath);
            }

            if (!catalog.TryGet("starter_car", out _))
            {
                var json = AssetDatabase.LoadAssetAtPath<TextAsset>(StarterCarPath);
                catalog.AddOrReplace(new ModelEntry("starter_car", "Starter Car",
                    "A small car that uses every starter brick: ten bricks in five steps.", json));
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        [MenuItem("Build Mania/Export Current Build as Model", true)]
        static bool CanExport() => EditorApplication.isPlaying && Object.FindAnyObjectByType<BuildArea>() != null;

        /// <summary>
        /// Saves what is on the build area (in Play mode, for example over Quest Link) as a
        /// guided model and adds it to the catalog. Each brick becomes its own step; edit the
        /// "step" numbers in the JSON to group bricks into fewer steps.
        /// </summary>
        [MenuItem("Build Mania/Export Current Build as Model")]
        static void ExportCurrentBuild()
        {
            var assembly = Object.FindAnyObjectByType<BuildArea>().View.Assembly;
            if (assembly.Count == 0)
            {
                EditorUtility.DisplayDialog("Export model", "The build area is empty.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanelInProject("Export model", "new_model", "json",
                "Choose a file name; it becomes the model id.", ModelsFolder);
            if (string.IsNullOrEmpty(path))
                return;

            var id = Path.GetFileNameWithoutExtension(path);
            var model = ModelConverter.FromAssembly(assembly, id, ObjectNames.NicifyVariableName(id));
            File.WriteAllText(path, ModelSerializer.ToJson(model));
            AssetDatabase.ImportAsset(path);

            var catalog = EnsureModelCatalog();
            catalog.AddOrReplace(new ModelEntry(id, model.Name, "", AssetDatabase.LoadAssetAtPath<TextAsset>(path)));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Build Mania: exported {assembly.Count} bricks to {path} and added '{id}' to the model catalog.");
        }

        /// <summary>Imports TextMesh Pro's fonts and shaders into the project, once.</summary>
        [MenuItem("Build Mania/Import Text Resources")]
        public static void ImportTextResources()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
                return;
            var ugui = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            var package = Path.Combine(ugui.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            UnityEditor.AssetPackage.Package.Import(package, false);
            AssetDatabase.Refresh();
            Debug.Log("Build Mania: imported TextMesh Pro essential resources.");
        }
    }
}
