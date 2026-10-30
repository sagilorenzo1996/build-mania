using System.Collections;
using System.Linq;
using BuildMania.Bricks;
using BuildMania.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static BuildMania.Bricks.StarterBrickSet;

namespace BuildMania.Tests
{
    /// <summary>Drives the real build area prefab with a simulated hand.</summary>
    public class BuildAreaInteractionTests
    {
        const string BuildAreaPrefabPath = "Assets/BuildMania/Prefabs/BuildArea.prefab";
        static readonly Vector3 Jitter = new(0.006f, 0.004f, -0.005f);

        BuildArea area;
        BrickHand hand;
        FakeHandInput input;

        BrickAssembly Assembly => area.View.Assembly;

        [UnitySetUp]
        public IEnumerator CreateAreaAndHand()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BuildAreaPrefabPath);
#else
            GameObject prefab = null;
#endif
            Assert.That(prefab, Is.Not.Null, $"Generate {BuildAreaPrefabPath} first (Build Mania > Generate Scenes).");
            area = Object.Instantiate(prefab, new Vector3(0f, 0.8f, 0.8f), Quaternion.identity).GetComponent<BuildArea>();
            area.PlaceInFrontOfViewer = false;

            var handObject = new GameObject("Test Hand");
            input = handObject.AddComponent<FakeHandInput>();
            hand = handObject.AddComponent<BrickHand>();
            hand.Configure(input, area);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator DestroyAreaAndHand()
        {
            Object.Destroy(hand.gameObject);
            Object.Destroy(area.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BrickFromTray_SnapsOntoBaseplate()
        {
            area.SelectedColor = 3;
            yield return Grab(Slot(Brick2x2).transform.position + Vector3.up * 0.02f);
            Assert.That(hand.HeldDefinition, Is.Not.Null);
            Assert.That(hand.HeldDefinition.Id, Is.EqualTo(Brick2x2));

            yield return MoveHeldBrickTo(new Vector3Int(4, 0, 4), rotation: 1);
            yield return Release();

            Assert.That(Assembly.TryGetBrickAt(new Vector3Int(4, 0, 4), out var placed), Is.True);
            Assert.That(placed.Definition.Id, Is.EqualTo(Brick2x2));
            Assert.That(placed.Rotation, Is.EqualTo(1));
            Assert.That(placed.ColorIndex, Is.EqualTo(3));
            Assert.That(hand.HeldObject, Is.Null);
        }

        [UnityTest]
        public IEnumerator BrickReleasedAwayFromBuild_IsDiscarded()
        {
            yield return Grab(Slot(Brick2x4).transform.position + Vector3.up * 0.02f);
            input.Pose = new Pose(area.transform.position + Vector3.up, Quaternion.identity);
            yield return null;
            yield return Release();

            Assert.That(Assembly.Count, Is.Zero);
            Assert.That(hand.HeldObject, Is.Null);
        }

        [UnityTest]
        public IEnumerator GrabbingPlacedBrick_DetachesIt_AndUndoPutsItBack()
        {
            var id = PlaceDirectly(Brick2x2, new Vector3Int(2, 0, 2));
            yield return null;

            yield return Grab(CentreOf(id));
            Assert.That(Assembly.Count, Is.Zero);
            Assert.That(hand.HeldDefinition.Id, Is.EqualTo(Brick2x2));

            input.Pose = new Pose(area.transform.position + Vector3.up, Quaternion.identity);
            yield return null;
            yield return Release();
            Assert.That(Assembly.Count, Is.Zero);

            area.Undo();
            Assert.That(Assembly.TryGetBrick(id, out _), Is.True);
        }

        [UnityTest]
        public IEnumerator SupportingBrick_CannotBeDetached()
        {
            var bottom = PlaceDirectly(Brick2x2, new Vector3Int(2, 0, 2));
            PlaceDirectly(Brick2x2, new Vector3Int(2, 3, 2));
            yield return null;

            yield return Grab(CentreOf(bottom));

            Assert.That(Assembly.Count, Is.EqualTo(2));
            Assert.That(hand.HeldObject, Is.Null);
        }

        [UnityTest]
        public IEnumerator PokingSwatch_SelectsColour()
        {
            var swatch = area.GetComponentsInChildren<ColorSwatch>().Single(s => s.ColorIndex == 4);

            input.Poke = swatch.transform.position + Vector3.up * 0.012f;
            yield return null;
            yield return null;

            Assert.That(area.SelectedColor, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator PokingUndo_RemovesLastBrick()
        {
            PlaceDirectly(Brick2x2, new Vector3Int(2, 0, 2));
            var undo = area.GetComponentsInChildren<HistoryButton>().Single(b => b.name == "Undo");

            input.Poke = undo.transform.position + Vector3.up * 0.015f;
            yield return null;
            yield return null;

            Assert.That(Assembly.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CarryingHandle_MovesAndTurnsAreaButKeepsItLevel()
        {
            var handle = area.GetComponentInChildren<AreaHandle>();
            var grabStart = handle.transform.position + Vector3.up * 0.05f;
            var offset = area.transform.position - grabStart;

            yield return Grab(grabStart);
            var grabEnd = grabStart + new Vector3(0.3f, 0.1f, 0.2f);
            input.Pose = new Pose(grabEnd, Quaternion.Euler(25f, 90f, 10f));
            yield return null;
            yield return Release();

            Assert.That(Vector3.Angle(area.transform.up, Vector3.up), Is.LessThan(0.1f));
            Assert.That(Mathf.DeltaAngle(area.transform.eulerAngles.y, 90f), Is.EqualTo(0f).Within(0.1f));
            var expected = grabEnd + Quaternion.Euler(0f, 90f, 0f) * offset;
            Assert.That(Vector3.Distance(area.transform.position, expected), Is.LessThan(0.001f));
        }

        TraySlot Slot(string brickId) =>
            area.GetComponentsInChildren<TraySlot>().Single(slot => slot.Definition.Id == brickId);

        int PlaceDirectly(string brickId, Vector3Int cell)
        {
            var command = new PlaceBrickCommand(new BrickPlacement(Definition(brickId), cell, 0, 1));
            Assert.That(area.View.History.Execute(command), Is.True);
            return command.PlacedId;
        }

        BrickDefinition Definition(string brickId) => Slot(brickId).Definition;

        Vector3 CentreOf(int brickId)
        {
            Assert.That(area.View.TryGetInstance(brickId, out var instance), Is.True);
            var bounds = instance.GetComponent<MeshFilter>().sharedMesh.bounds;
            return instance.transform.TransformPoint(bounds.center);
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

        /// <summary>Moves the hand so the held brick's pivot sits just off the given cell.</summary>
        IEnumerator MoveHeldBrickTo(Vector3Int cell, int rotation)
        {
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
