using System;
using System.Collections.Generic;
using BuildMania.Building;
using UnityEngine;

namespace BuildMania.Content
{
    /// <summary>One buildable model in the catalog. Later: thumbnail, pack, price and unlock state.</summary>
    [Serializable]
    public sealed class ModelEntry
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField, TextArea] string description;
        [Tooltip("The model's JSON file (see ModelDefinition).")]
        [SerializeField] TextAsset model;

        public ModelEntry(string id, string displayName, string description, TextAsset model)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.model = model;
        }

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public TextAsset ModelFile => model;

        public ModelDefinition Load() => ModelSerializer.FromJson(model.text);
    }

    /// <summary>The models the Garage offers. Model packs will add their own catalogs.</summary>
    [CreateAssetMenu(fileName = "ModelCatalog", menuName = "Build Mania/Model Catalog")]
    public sealed class ModelCatalog : ScriptableObject
    {
        [SerializeField] List<ModelEntry> models = new();

        public IReadOnlyList<ModelEntry> Models => models;

        public bool TryGet(string id, out ModelEntry entry)
        {
            entry = models.Find(model => model.Id == id);
            return entry != null;
        }

        /// <summary>Adds the entry, or replaces the one with the same id.</summary>
        public void AddOrReplace(ModelEntry entry)
        {
            var index = models.FindIndex(model => model.Id == entry.Id);
            if (index >= 0)
                models[index] = entry;
            else
                models.Add(entry);
        }
    }
}
