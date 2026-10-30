using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BuildMania.EditorTools
{
    /// <summary>
    /// Creates a URP pipeline asset tuned for Quest and makes it the project's render pipeline.
    /// Safe to re-run; it reapplies the settings to the existing asset.
    /// Batch mode: <c>-executeMethod BuildMania.EditorTools.QuestRenderSetup.Apply</c>.
    /// </summary>
    public static class QuestRenderSetup
    {
        const string Folder = "Assets/BuildMania/Settings";
        const string PipelinePath = Folder + "/QuestURP.asset";
        const string RendererPath = Folder + "/QuestURP_Renderer.asset";
        const string PostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

        [MenuItem("Build Mania/Set Up URP for Quest")]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/BuildMania", "Settings");

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(PostProcessDataPath);
                AssetDatabase.CreateAsset(renderer, RendererPath);

                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            // Mobile VR settings: no HDR or extra camera textures, 4x MSAA instead of post AA.
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 4;
            pipeline.renderScale = 1f;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.shadowDistance = 4f;
            pipeline.useSRPBatcher = true;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log($"Build Mania: render pipeline set to {PipelinePath}.");
        }
    }
}
