using UnityEngine;

namespace BuildMania.Interaction
{
    /// <summary>
    /// A grab point for carrying the whole build area. The area stays level and only turns
    /// about the vertical axis, so it can be set down on any table or surface.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class AreaHandle : MonoBehaviour
    {
        [SerializeField] BuildArea area;

        public BuildArea Area => area;

        public void Configure(BuildArea owner) => area = owner;
    }
}
