using System.Collections.Generic;
using System.Linq;

namespace BuildMania.Building
{
    /// <summary>One instruction step: the bricks the player adds together.</summary>
    public sealed class InstructionStep
    {
        public InstructionStep(int number, IReadOnlyList<ModelBrick> bricks)
        {
            Number = number;
            Bricks = bricks;
        }

        /// <summary>The step number stored in the model file.</summary>
        public int Number { get; }
        public IReadOnlyList<ModelBrick> Bricks { get; }
    }

    /// <summary>A model's bricks grouped into ordered instruction steps.</summary>
    public sealed class InstructionPlan
    {
        public InstructionPlan(ModelDefinition model)
        {
            Model = model;
            Steps = model.Bricks
                .GroupBy(brick => brick.Step)
                .OrderBy(group => group.Key)
                .Select(group => new InstructionStep(group.Key, group.ToList()))
                .ToList();
        }

        public ModelDefinition Model { get; }
        public IReadOnlyList<InstructionStep> Steps { get; }
        public int BrickCount => Model.Bricks.Count;
    }
}
