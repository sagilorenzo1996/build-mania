using System;
using System.Collections.Generic;
using System.Linq;
using BuildMania.Bricks;
using UnityEngine;

namespace BuildMania.Building
{
    /// <summary>
    /// Follows an <see cref="InstructionPlan"/> on a live assembly. The current step is the
    /// first one with bricks still missing, so undo, redo and detaching all move the player
    /// back and forth between steps naturally.
    /// </summary>
    public sealed class GuidedBuild
    {
        readonly List<List<BrickPlacement>> targets = new();
        readonly List<string> problems = new();
        BrickAssembly assembly;
        List<BrickPlacement> remaining = new();

        /// <param name="catalog">Resolves the model's brick ids to definitions.</param>
        public GuidedBuild(InstructionPlan plan, BrickCatalog catalog)
        {
            Plan = plan;
            foreach (var step in plan.Steps)
            {
                var placements = new List<BrickPlacement>();
                foreach (var brick in step.Bricks)
                {
                    if (catalog.TryGet(brick.BrickId, out var definition))
                        placements.Add(new BrickPlacement(definition, new Vector3Int(brick.X, brick.Y, brick.Z), brick.Rotation, brick.Color));
                    else
                        problems.Add($"Step {step.Number}: unknown brick type '{brick.BrickId}'.");
                }
                targets.Add(placements);
            }
        }

        public InstructionPlan Plan { get; }

        /// <summary>Bricks the model refers to that could not be resolved or placed.</summary>
        public IReadOnlyList<string> Problems => problems;

        /// <summary>Index into <see cref="InstructionPlan.Steps"/>; equals the step count once complete.</summary>
        public int StepIndex { get; private set; }

        public int StepCount => targets.Count;
        public bool IsComplete => StepIndex >= targets.Count;

        /// <summary>Bricks of the current step that are not placed yet.</summary>
        public IReadOnlyList<BrickPlacement> RemainingTargets => remaining;

        /// <summary>Raised when the player moves on to a new step (or back, after undo).</summary>
        public event Action<int> StepChanged;

        /// <summary>Raised when the last brick of the model is placed.</summary>
        public event Action Completed;

        /// <summary>
        /// Starts following <paramref name="target"/>. The first <paramref name="completedSteps"/>
        /// steps are built straight away, to resume saved progress.
        /// </summary>
        public void Attach(BrickAssembly target, int completedSteps = 0)
        {
            Detach();
            assembly = target;
            for (var step = 0; step < Mathf.Min(completedSteps, targets.Count); step++)
                foreach (var placement in targets[step])
                    if (!IsPresent(placement) && assembly.TryPlace(placement, out _) != PlacementResult.Ok)
                        problems.Add($"Could not restore step {Plan.Steps[step].Number}: {placement.Definition.Id} at {placement.Position}.");

            assembly.BrickAdded += OnAssemblyChanged;
            assembly.BrickRemoved += OnAssemblyChanged;
            Recalculate(notify: false);
        }

        public void Detach()
        {
            if (assembly == null)
                return;
            assembly.BrickAdded -= OnAssemblyChanged;
            assembly.BrickRemoved -= OnAssemblyChanged;
            assembly = null;
        }

        /// <summary>True if the placement is one of the current step's missing bricks.</summary>
        public bool Accepts(BrickPlacement placement)
        {
            foreach (var target in remaining)
                if (target.IsEquivalentTo(placement))
                    return true;
            return false;
        }

        void OnAssemblyChanged(PlacedBrick _) => Recalculate(notify: true);

        void Recalculate(bool notify)
        {
            var wasComplete = IsComplete;
            var previous = StepIndex;

            StepIndex = targets.Count;
            remaining = new List<BrickPlacement>();
            for (var step = 0; step < targets.Count; step++)
            {
                var missing = targets[step].Where(target => !IsPresent(target)).ToList();
                if (missing.Count > 0)
                {
                    StepIndex = step;
                    remaining = missing;
                    break;
                }
            }

            if (!notify)
                return;
            if (StepIndex != previous)
                StepChanged?.Invoke(StepIndex);
            if (IsComplete && !wasComplete)
                Completed?.Invoke();
        }

        bool IsPresent(BrickPlacement target) =>
            assembly.TryGetBrickAt(target.Cells().First(), out var brick) && brick.Placement.IsEquivalentTo(target);
    }
}
