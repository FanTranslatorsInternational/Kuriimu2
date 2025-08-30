using Konnect.Contract.Management.Files;
using Kuriimu2.Cmd.Contexts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Konnect.Extensions;

namespace Kuriimu2.Cmd.Models.Contexts
{
    [DebuggerDisplay("{StateInfo.FilePath}")]
    class ContextNode
    {
        private readonly IContext? _parentContext;
        private ContextNode? _parentNode;

        public IFileState? StateInfo { get; }

        public IContext? Root => GetRootContext();

        public IList<ContextNode> Children { get; } = [];

        public ContextNode()
        {
        }

        public ContextNode(IContext parentContext, ContextNode parentNode, IFileState stateInfo)
        {
            _parentContext = parentContext;
            _parentNode = parentNode;

            StateInfo = stateInfo;
        }

        public ContextNode Add(IContext parentContext, IFileState stateInfo)
        {
            var newNode = new ContextNode(parentContext, this, stateInfo);
            Children.Add(newNode);

            return newNode;
        }

        public void ListFiles()
        {
            ListFilesInternal();
        }

        public void Remove()
        {
            _parentNode?.Children.Remove(this);
            _parentNode = null;
        }

        private void ListFilesInternal(int iteration = 0)
        {
            var prefix = new string(' ', iteration * 2);

            for (var i = 0; i < Children.Count; i++)
            {
                if (Children[i].StateInfo is null)
                    continue;

                if (iteration is 0)
                    prefix = $"[{i}] ";

                if (Children[i].StateInfo!.StateChanged)
                    prefix += "* ";

                Console.WriteLine(prefix + Children[i].StateInfo!.FilePath.ToRelative());

                Children[i].ListFilesInternal(iteration + 1);
            }
        }

        private IContext? GetRootContext()
        {
            if (_parentNode == null)
                throw new InvalidOperationException("Can't get root context of the root.");

            ContextNode? currentNode = _parentNode;
            IContext? currentContext = _parentContext;

            while (currentNode is { _parentContext: not null, _parentNode: not null })
            {
                currentContext = currentNode._parentContext;
                currentNode = currentNode._parentNode;
            }

            return currentContext;
        }
    }
}
