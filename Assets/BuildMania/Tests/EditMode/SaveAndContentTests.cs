using System;
using System.IO;
using BuildMania.Bricks;
using BuildMania.Building;
using BuildMania.Content;
using NUnit.Framework;
using UnityEditor;

namespace BuildMania.Tests
{
    public class SaveStoreTests
    {
        string root;

        [SetUp]
        public void CreateFolder() => root = Path.Combine(Path.GetTempPath(), "buildmania-tests-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }

        [Test]
        public void Progress_SurvivesANewStoreAndCanBeCleared()
        {
            new FileSaveStore(root).SaveProgress(new BuildProgress { ModelId = "starter_car", CompletedSteps = 3 });

            var reopened = new FileSaveStore(root);
            Assert.That(reopened.LoadProgress("starter_car").CompletedSteps, Is.EqualTo(3));
            Assert.That(reopened.LoadProgress("other_model"), Is.Null);

            reopened.ClearProgress("starter_car");
            Assert.That(reopened.LoadProgress("starter_car"), Is.Null);
        }

        [Test]
        public void Creations_AreListedAndLoaded()
        {
            var store = new FileSaveStore(root);
            var creation = new ModelDefinition { Id = "creation-1", Name = "My Car" };
            creation.Bricks.Add(new ModelBrick { BrickId = StarterBrickSet.Brick2x2, Step = 1 });

            store.SaveCreation(creation);

            Assert.That(store.CreationIds(), Is.EqualTo(new[] { "creation-1" }));
            Assert.That(store.LoadCreation("creation-1").Name, Is.EqualTo("My Car"));
        }

        [Test]
        public void UnsafeIds_StayInsideTheSaveFolder()
        {
            new FileSaveStore(root).SaveProgress(new BuildProgress { ModelId = "../escape" });

            Assert.That(File.Exists(Path.Combine(root, "escape.json")), Is.False);
            Assert.That(Directory.GetFiles(Path.Combine(root, "progress")), Has.Length.EqualTo(1));
        }
    }

    public class ContentTests
    {
        [Test]
        public void EveryCatalogModel_LoadsAndBuildsCompletely()
        {
            var models = AssetDatabase.LoadAssetAtPath<ModelCatalog>("Assets/BuildMania/Data/ModelCatalog.asset");
            var bricks = AssetDatabase.LoadAssetAtPath<BrickCatalog>("Assets/BuildMania/Bricks/StarterBricks.asset");
            Assert.That(models, Is.Not.Null, "Run Build Mania > Generate Scenes to create the model catalog.");
            Assert.That(models.Models, Is.Not.Empty);

            foreach (var entry in models.Models)
            {
                var model = entry.Load();
                var guided = new GuidedBuild(new InstructionPlan(model), bricks);
                var assembly = ModelConverter.CreateAssembly(model);
                guided.Attach(assembly, guided.StepCount);

                Assert.That(guided.Problems, Is.Empty, entry.Id);
                Assert.That(guided.IsComplete, Is.True, entry.Id);
                Assert.That(assembly.Count, Is.EqualTo(model.Bricks.Count), entry.Id);
            }
        }
    }
}
