using System;
using System.Collections.Generic;

namespace BuildMania.Bricks
{
    /// <summary>A reversible change to an assembly.</summary>
    public interface IBuildCommand
    {
        /// <summary>Applies the change. Returns false, changing nothing, if it is not allowed.</summary>
        bool Execute(BrickAssembly assembly);

        void Undo(BrickAssembly assembly);
    }

    public sealed class PlaceBrickCommand : IBuildCommand
    {
        readonly BrickPlacement placement;
        int placedId;

        public PlaceBrickCommand(BrickPlacement placement) => this.placement = placement;

        public PlacementResult LastResult { get; private set; }
        public int PlacedId => placedId;

        public bool Execute(BrickAssembly assembly)
        {
            if (placedId != 0)
            {
                // Redo: put the brick back under the id later commands refer to.
                assembly.Insert(placement, placedId);
                return true;
            }

            LastResult = assembly.TryPlace(placement, out var placed);
            if (placed == null)
                return false;
            placedId = placed.Id;
            return true;
        }

        public void Undo(BrickAssembly assembly) => assembly.Delete(placedId);
    }

    public sealed class RemoveBrickCommand : IBuildCommand
    {
        readonly int brickId;
        BrickPlacement removed;

        public RemoveBrickCommand(int brickId) => this.brickId = brickId;

        public RemovalResult LastResult { get; private set; }

        public bool Execute(BrickAssembly assembly)
        {
            if (!assembly.TryGetBrick(brickId, out var brick))
            {
                LastResult = RemovalResult.NotFound;
                return false;
            }

            removed = brick.Placement;
            LastResult = assembly.TryRemove(brickId);
            return LastResult == RemovalResult.Ok;
        }

        public void Undo(BrickAssembly assembly) => assembly.Insert(removed, brickId);
    }

    /// <summary>Undo and redo for build commands.</summary>
    public sealed class BuildHistory
    {
        readonly BrickAssembly assembly;
        readonly List<IBuildCommand> undoStack = new();
        readonly Stack<IBuildCommand> redoStack = new();
        readonly int limit;

        public BuildHistory(BrickAssembly assembly, int limit = 200)
        {
            this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            this.limit = Math.Max(1, limit);
        }

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        public event Action Changed;

        /// <summary>Runs a command and records it if it succeeded. Clears the redo stack.</summary>
        public bool Execute(IBuildCommand command)
        {
            if (!command.Execute(assembly))
                return false;

            undoStack.Add(command);
            if (undoStack.Count > limit)
                undoStack.RemoveAt(0);
            redoStack.Clear();
            Changed?.Invoke();
            return true;
        }

        public bool Undo()
        {
            if (!CanUndo)
                return false;

            var command = undoStack[^1];
            undoStack.RemoveAt(undoStack.Count - 1);
            command.Undo(assembly);
            redoStack.Push(command);
            Changed?.Invoke();
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo)
                return false;

            var command = redoStack.Pop();
            command.Execute(assembly);
            undoStack.Add(command);
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            Changed?.Invoke();
        }
    }
}
