using System.Collections;
using System.Linq;
using BuildMania.App;
using BuildMania.Bricks;
using BuildMania.Building;
using BuildMania.Content;
using BuildMania.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static BuildMania.Bricks.StarterBrickSet;

namespace BuildMania.Tests
{
    /// <summary>Walks the player journey (Garage, Guided Build, Free Build) on the real prefab with a simulated hand.</summary>
    public class AppFlowTests
    {
        const string StarterCar = "starter_car";
        static readonly Vector3 Jitter = new(0.006f, 0.004f, -0.005f);

        BuildArea area;
        MenuPanel menu;
        AppFlow flow;
        BrickHand hand;
        FakeHandInput input;
        MemorySaveStore saves;

        GuidedBuildStep Guided => flow.Get<GuidedBuildStep>();

        [UnitySetUp]
        public IEnumerator CreateApp()
        {
#if UNITY_EDITOR
            var areaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BuildMania/Prefabs/BuildArea.prefab");
            var models = UnityEditor.AssetDatabase.LoadAssetAtPath<ModelCatalog>("Assets/BuildMania/Data/ModelCatalog.asset");
            var bricks = UnityEditor.AssetDatabase.LoadAssetAtPath<BrickCatalog>("Assets/BuildMania/Bricks/StarterBricks.asset");
#else
            GameObject areaPrefab = null;
            ModelCatalog models = null;
            BrickCatalog bricks = null;
#endif
            Assert.That(areaPrefab != null && models != null && bricks != null, "Run Build Mania > Generate Scenes first.");

            area = Object.Instantiate(areaPrefab, new Vector3(0f, 0.8f, 0.8f), Quaternion.identity).GetComponent<BuildArea>();
            area.PlaceInFrontOfViewer = false;
            menu = area.GetComponentInChildren<MenuPanel>(true);
            saves = new MemorySaveStore();
            flow = new GameObject("App Flow").AddComponent<AppFlow>();
            flow.Configure(area, models, bricks, menu, saves);

            var handObject = new GameObject("Test Hand");
            input = handObject.AddComponent<FakeHandInput>();
            hand = handObject.AddComponent<BrickHand>();
            hand.Configure(input, area);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator DestroyApp()
        {
            Object.Destroy(flow.gameObject);
            Object.Destroy(hand.gameObject);
            Object.Destroy(area.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Garage_OffersTheStarterCarAndFreeBuild()
        {
            Assert.That(flow.Current, Is.TypeOf<GarageStep>());
            Assert.That(menu.Buttons.Select(b => b.Label), Is.SupersetOf(new[] { "Build Starter Car", "Free build" }));
            Assert.That(area.TraySlots.Any(slot => slot.gameObject.activeSelf), Is.False);
            yield break;
        }

        [UnityTest]
        public IEnumerator GuidedBuild_OffersOnlyTheStepsBricks_AndSavesEachStep()
        {
            yield return Press("Build Starter Car");
            Assert.That(flow.Current, Is.TypeOf<GuidedBuildStep>());
            Assert.That(Guided.OfferedBricks, Is.EqualTo(new[] { Plate2x4 }));

            // Take the chassis plate from the tray and put it where the hint shows.
            var slot = area.TraySlots.Single(s => s.gameObject.activeSelf);
            yield return Grab(slot.transform.position + Vector3.up * 0.01f);
            var target = Guided.RemainingTargets[0];
            yield return MoveHeldBrickTo(target.Position, target.Rotation);
            yield return Release();

            Assert.That(Guided.Guided.StepIndex, Is.EqualTo(1));
            Assert.That(saves.LoadProgress(StarterCar).CompletedSteps, Is.EqualTo(1));
            Assert.That(Guided.OfferedBricks, Is.EqualTo(new[] { AxlePlate2x2 }));
            StringAssert.Contains("step 2 of 5", menu.Title);
        }

        [UnityTest]
        public IEnumerator LeavingAndComingBack_ResumesAtTheSavedStep()
        {
            yield return Press("Build Starter Car");
            PlaceCurrentStepDirectly();
            yield return null;

            flow.GoTo<GarageStep>();
            yield return null;
            yield return Press("Continue Starter Car");

            Assert.That(Guided.Guided.StepIndex, Is.EqualTo(1));
            Assert.That(area.View.Assembly.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FinishingTheModel_LetsThePlayerCustomiseIt()
        {
            yield return Press("Build Starter Car");
            while (!Guided.Guided.IsComplete)
            {
                PlaceCurrentStepDirectly();
                yield return null;
            }

            Assert.That(saves.LoadProgress(StarterCar).Finished, Is.True);
            yield return Press("Customise it");

            Assert.That(flow.Current, Is.TypeOf<FreeBuildStep>());
            Assert.That(area.View.Assembly.Count, Is.EqualTo(10));
            Assert.That(area.TraySlots.All(slot => slot.gameObject.activeSelf), Is.True);
        }

        void PlaceCurrentStepDirectly()
        {
            foreach (var target in Guided.RemainingTargets.ToList())
                Assert.That(area.View.History.Execute(new PlaceBrickCommand(target)), Is.True);
        }

        IEnumerator Press(string label)
        {
            var button = menu.Buttons.SingleOrDefault(b => b.Label == label);
            Assert.That(button, Is.Not.Null, $"No '{label}' button; menu shows: {string.Join(", ", menu.Buttons.Select(b => b.Label))}");

            // Touch from outside, as a finger would.
            input.Poke = new Vector3(100f, 100f, 100f);
            yield return null;
            input.Poke = button.transform.position + Vector3.up * 0.015f;
            yield return null;
            input.Poke = new Vector3(100f, 100f, 100f);
            yield return null;
        }

        IEnumerator Grab(Vector3 position)
        {
            input.Pose = new Pose(position, Quaternion.identity);
            input.Grab = true;
            yield return null;
        }

        IEnumerator Release()
        {
            input.Grab = false;
            yield return null;
        }

        IEnumerator MoveHeldBrickTo(Vector3Int cell, int rotation)
        {
            Assert.That(hand.HeldObject, Is.Not.Null, "nothing in hand");
            var held = hand.HeldObject.transform;
            var inverseGrab = Quaternion.Inverse(input.Pose.rotation);
            var offsetPosition = inverseGrab * (held.position - input.Pose.position);
            var offsetRotation = inverseGrab * held.rotation;

            var target = area.View.GridToWorld(cell, rotation);
            var grabRotation = target.rotation * Quaternion.Inverse(offsetRotation);
            input.Pose = new Pose(target.position - grabRotation * offsetPosition + Jitter, grabRotation);
            yield return null;
        }
    }
}
