using System;
using System.Collections.Generic;

namespace Landmarks.Scripts.Progress
{
    public class ProgressNode
    {
        private readonly TaskList value;
        private readonly List<ProgressNode> children = new List<ProgressNode>();

        public ProgressNode(TaskList value)
        {
            this.value = value;
        }

        public void AddChild(ProgressNode node)
        {
            children.Add(node);
        }

        public ProgressNode this[int i] => children[i];

        public List<ProgressNode> GetChildren()
        {
            return children;
        }

        public TaskList GetValue()
        {
            return value;
        }

        public override string ToString()
        {
            return value.name;
        }

        public void Traverse(Action<TaskList> action)
        {
            action(value);
            foreach (var node in children)
            {
                node.Traverse(action);
            }
        }

    }
}
