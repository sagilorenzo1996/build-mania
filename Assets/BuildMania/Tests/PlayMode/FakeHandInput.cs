using BuildMania.Interaction;
using UnityEngine;

namespace BuildMania.Tests
{
    /// <summary>Hand input driven directly by a test.</summary>
    public sealed class FakeHandInput : HandInput
    {
        public bool Tracked = true;
        public Pose Pose = Pose.identity;
        public bool Grab;
        public Vector3 Poke = new(100f, 100f, 100f);

        public override bool IsTracked => Tracked;
        public override Pose GrabPose => Pose;
        public override bool GrabPressed => Grab;
        public override Vector3 PokePoint => Poke;
    }
}
