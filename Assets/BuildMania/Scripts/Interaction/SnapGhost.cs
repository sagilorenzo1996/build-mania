using BuildMania.Bricks;
using UnityEngine;

namespace BuildMania.Interaction
{
    /// <summary>A see-through copy of the held brick showing where it will land.</summary>
    public sealed class SnapGhost
    {
        readonly GameObject root;
        readonly MeshFilter meshFilter;

        public SnapGhost(Material material)
        {
            root = new GameObject("Snap Ghost");
            meshFilter = root.AddComponent<MeshFilter>();
            var meshRenderer = root.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            root.SetActive(false);
        }

        public bool IsVisible => root.activeSelf;

        public void Show(BrickDefinition definition, Pose pose)
        {
            var source = definition.Prefab != null ? definition.Prefab.GetComponent<MeshFilter>() : null;
            if (source == null)
                return;
            meshFilter.sharedMesh = source.sharedMesh;
            root.transform.SetPositionAndRotation(pose.position, pose.rotation);
            root.SetActive(true);
        }

        public void Hide() => root.SetActive(false);

        public void Destroy() => Object.Destroy(root);
    }
}
