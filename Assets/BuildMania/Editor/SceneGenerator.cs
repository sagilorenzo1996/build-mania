using System.Collections.Generic;
using BuildMania.App;
using BuildMania.Bricks;
using BuildMania.Content;
using BuildMania.Interaction;
using BuildMania.XR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BuildMania.EditorTools
{
    /// <summary>
    /// Generates the playable scenes and the Meta project settings they need. Both scenes run
    /// the same step flow (Garage, Guided Build, Free Build, Play) on the build area:
    /// <list type="bullet">
    /// <item>MRBuild: passthrough mixed reality; the build area appears in front of you and can be carried to any table.</item>
    /// <item>VRWorkshop: the same building on a virtual workbench, and the fallback when passthrough is unavailable.</item>
    /// </list>
    /// Both scenes accept Touch controllers and hand tracking, so they also suit controller-free Meta VR Glasses.
    /// Batch mode: <c>-executeMethod BuildMania.EditorTools.SceneGenerator.GenerateScenes</c>.
    /// </summary>
    public static class SceneGenerator
    {
        const string ScenesFolder = "Assets/BuildMania/Scenes";
        public const string MixedRealityScenePath = ScenesFolder + "/MRBuild.unity";
        public const string WorkshopScenePath = ScenesFolder + "/VRWorkshop.unity";
        const string WorkshopMaterialsFolder = "Assets/BuildMania/Materials/Workshop";

        const string MetaPrefabs = "Packages/com.meta.xr.sdk.core/Prefabs/";
        const string CameraRigPrefab = MetaPrefabs + "OVRCameraRig.prefab";
        const string ControllerPrefab = MetaPrefabs + "OVRControllerPrefab.prefab";
        const string HandPrefab = MetaPrefabs + "OVRHandPrefab.prefab";

        const float TableHeight = 0.74f;

        [MenuItem("Build Mania/Generate Scenes")]
        public static void GenerateScenes()
        {
            var models = ContentGenerator.EnsureModelCatalog();
            var areaPrefab = BuildAreaGenerator.GenerateBuildAreaPrefab();
            BuildAreaGenerator.EnsureFolder(ScenesFolder);
            ConfigureMetaProject();

            CreateMixedRealityScene(areaPrefab, models);
            CreateWorkshopScene(areaPrefab, models);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MixedRealityScenePath, true),
                new EditorBuildSettingsScene(WorkshopScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Build Mania: generated MRBuild and VRWorkshop scenes.");
        }

        /// <summary>Controllers plus hands, optional passthrough, and every Quest-family device including VR Glasses.</summary>
        static void ConfigureMetaProject()
        {
            var config = OVRProjectConfig.CachedProjectConfig;
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Supported;
            foreach (var device in new[]
                     {
                         OVRProjectConfig.DeviceType.Quest2, OVRProjectConfig.DeviceType.QuestPro, OVRProjectConfig.DeviceType.Quest3,
                         OVRProjectConfig.DeviceType.Quest3S, OVRProjectConfig.DeviceType.VRGlasses,
                     })
                if (!config.targetDeviceTypes.Contains(device))
                    config.targetDeviceTypes.Add(device);
            OVRProjectConfig.CommitProjectConfig(config);
        }

        static void CreateMixedRealityScene(GameObject areaPrefab, ModelCatalog models)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);
            AddSunlight();

            var area = InstantiateArea(areaPrefab, new Vector3(0f, 0.8f, 0.85f), placeInFrontOfViewer: true);
            CreateRig(area, passthrough: true);
            CreateAppFlow(area, models);
            new GameObject("Passthrough Fallback").AddComponent<PassthroughFallback>();

            EditorSceneManager.SaveScene(scene, MixedRealityScenePath);
        }

        static void CreateWorkshopScene(GameObject areaPrefab, ModelCatalog models)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = AmbientMode.Skybox;
            AddSunlight();
            BuildWorkshop();

            // Baseplate resting on the table, tray edge about 20 cm in front of the player.
            var area = InstantiateArea(areaPrefab, new Vector3(0f, TableHeight + BrickGrid.PlateHeight, 0.87f), placeInFrontOfViewer: false);
            CreateRig(area, passthrough: false);
            CreateAppFlow(area, models);

            EditorSceneManager.SaveScene(scene, WorkshopScenePath);
        }

        static void CreateAppFlow(BuildArea area, ModelCatalog models)
        {
            var bricks = AssetDatabase.LoadAssetAtPath<BrickCatalog>("Assets/BuildMania/Bricks/StarterBricks.asset");
            var flow = new GameObject("App Flow").AddComponent<AppFlow>();
            BuildAreaGenerator.SetFields(flow,
                ("area", area), ("models", models), ("bricks", bricks), ("menu", area.GetComponentInChildren<MenuPanel>(true)));
        }

        static BuildArea InstantiateArea(GameObject areaPrefab, Vector3 position, bool placeInFrontOfViewer)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(areaPrefab);
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            var area = instance.GetComponent<BuildArea>();
            var serialized = new SerializedObject(area);
            serialized.FindProperty("placeInFrontOfViewer").boolValue = placeInFrontOfViewer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return area;
        }

        static void CreateRig(BuildArea area, bool passthrough)
        {
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefab));
            rig.name = "OVRCameraRig";

            var manager = rig.GetComponent<OVRManager>();
            if (manager == null)
                manager = rig.AddComponent<OVRManager>();
            manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            manager.isInsightPassthroughEnabled = passthrough;
            if (passthrough)
                rig.AddComponent<OVRPassthroughLayer>();

            var eye = Find(rig.transform, "CenterEyeAnchor").GetComponent<Camera>();
            eye.tag = "MainCamera";
            if (passthrough)
            {
                // Transparent clear colour lets the passthrough layer show behind the bricks.
                eye.clearFlags = CameraClearFlags.SolidColor;
                eye.backgroundColor = Color.clear;
            }
            else
            {
                eye.clearFlags = CameraClearFlags.Skybox;
            }

            foreach (var (side, controller, handType, skeletonType, meshType) in new[]
                     {
                         ("Left", OVRInput.Controller.LTouch, OVRHand.Hand.HandLeft, OVRSkeleton.SkeletonType.XRHandLeft, OVRMesh.MeshType.XRHandLeft),
                         ("Right", OVRInput.Controller.RTouch, OVRHand.Hand.HandRight, OVRSkeleton.SkeletonType.XRHandRight, OVRMesh.MeshType.XRHandRight),
                     })
            {
                var controllerAnchor = Find(rig.transform, $"{side}ControllerAnchor");
                var controllerModel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ControllerPrefab), controllerAnchor);
                controllerModel.GetComponent<OVRControllerHelper>().m_controller = controller;

                var handAnchor = Find(rig.transform, $"{side}HandAnchor");
                var handModel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HandPrefab), handAnchor);
                handModel.name = $"OVRHand ({side})";
                SetEnum(handModel.GetComponent<OVRHand>(), "HandType", (int)handType);
                SetEnum(handModel.GetComponent<OVRSkeleton>(), "_skeletonType", (int)skeletonType);
                SetEnum(handModel.GetComponent<OVRMesh>(), "_meshType", (int)meshType);

                var buildHand = new GameObject($"Build Hand ({side})");
                buildHand.transform.SetParent(rig.transform, false);
                var input = buildHand.AddComponent<MetaHandInput>();
                input.Configure(controller, controllerAnchor, handModel.GetComponent<OVRHand>(), handModel.GetComponent<OVRSkeleton>());
                buildHand.AddComponent<BrickHand>().Configure(input, area);
            }
        }

        static void AddSunlight()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static void BuildWorkshop()
        {
            BuildAreaGenerator.EnsureFolder(WorkshopMaterialsFolder);
            var floorMaterial = WorkshopMaterial("Floor", new Color(0.42f, 0.4f, 0.38f), 0.2f);
            var woodMaterial = WorkshopMaterial("Wood", new Color(0.55f, 0.38f, 0.22f), 0.35f);

            var environment = new GameObject("Workshop").transform;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(environment, false);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

            var table = new GameObject("Workbench").transform;
            table.SetParent(environment, false);
            table.localPosition = new Vector3(0f, 0f, 0.7f);
            Box(table, "Top", new Vector3(0f, TableHeight - 0.02f, 0f), new Vector3(1.0f, 0.04f, 1.0f), woodMaterial);
            foreach (var x in new[] { -0.45f, 0.45f })
            foreach (var z in new[] { -0.45f, 0.45f })
                Box(table, "Leg", new Vector3(x, (TableHeight - 0.04f) * 0.5f, z), new Vector3(0.05f, TableHeight - 0.04f, 0.05f), woodMaterial);
        }

        static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static Material WorkshopMaterial(string name, Color colour, float smoothness)
        {
            var path = $"{WorkshopMaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void SetEnum(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform Find(Transform root, string name)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.name == name)
                    return current;
                foreach (Transform child in current)
                    queue.Enqueue(child);
            }
            throw new MissingReferenceException($"'{name}' not found under {root.name}.");
        }
    }
}
