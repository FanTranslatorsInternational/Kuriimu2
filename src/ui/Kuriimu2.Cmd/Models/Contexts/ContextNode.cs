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
        private readonly IContext _currentContext;
        private ContextNode? _parentNode;

        public IFileState? StateInfo { get; }

        public IContext? Root => GetRootNode()._currentContext;

        public IList<ContextNode> Children { get; } = [];

        public ContextNode(IContext currentContext)
        {
            _currentContext = currentContext;
        }

        public ContextNode(IContext currentContext, ContextNode parentNode, IFileState stateInfo)
        {
            _currentContext = currentContext;
            _parentNode = parentNode;

            StateInfo = stateInfo;
        }

        public IContext? GetLoadedContext(IFileState fileState)
        {
            return GetLoadedContextInternal(GetRootNode(), fileState);
        }

        public ContextNode Add(IContext context, IFileState stateInfo)
        {
            var newNode = new ContextNode(context, this, stateInfo);
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

        private static IContext? GetLoadedContextInternal(ContextNode node, IFileState fileState)
        {
            if (node.StateInfo == fileState)
                return node._currentContext;

            foreach (ContextNode child in node.Children)
            {
                IContext? context = GetLoadedContextInternal(child, fileState);
                if (context is null)
                    continue;

                return context;
            }

            return null;
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

        private ContextNode GetRootNode()
        {
            if (_parentNode == null)
                return this;

            ContextNode? currentNode = _parentNode;

            while (currentNode is { _parentNode: not null })
                currentNode = currentNode._parentNode;

            return currentNode;
        }

        //private IContext? GetRootContext()
        //{
        //    if (_parentNode == null)
        //        throw new InvalidOperationException("Can't get root context of the root.");

        //    ContextNode? currentNode = _parentNode;
        //    IContext? currentContext = _parentContext;

        //    while (currentNode is { _parentContext: not null, _parentNode: not null })
        //    {
        //        currentContext = currentNode._parentContext;
        //        currentNode = currentNode._parentNode;
        //    }

        //    return currentContext;
        //}
    }
}
