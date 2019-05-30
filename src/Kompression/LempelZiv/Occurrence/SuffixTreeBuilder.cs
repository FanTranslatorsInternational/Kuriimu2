using System.IO;
using Kompression.LempelZiv.Occurrence.Models;

/* A suffix tree parses a file into patterns which are then directly accessible through a root node.
   The time used for finding a pattern is then O(n).
   See this article series for explanation and implementation of Ukkonen's suffix tree creation:
   https://www.geeksforgeeks.org/ukkonens-suffix-tree-construction-part-6/ */

namespace Kompression.LempelZiv.Occurrence
{
    unsafe class SuffixTreeBuilder
    {
        private byte[] _inputArray;

        SuffixTreeNode _root;
        SuffixTreeNode _lastNewNode;
        SuffixTreeNode _activeNode;

        // Position in input
        int _activeEdge = -1;
        int _activeLength;

        int _remainingSuffixCount;
        IntValue _leafEnd = new IntValue(-1);
        int _size = -1;

        readonly IntValue _rootEnd = new IntValue(-1);
        IntValue _splitEnd = new IntValue(-1);

        public SuffixTreeNode Build(Stream input)
        {
            var bkPos = input.Position;
            _inputArray = new byte[input.Length];
            input.Read(_inputArray, 0, _inputArray.Length);
            input.Position = bkPos;

            _size = (int)input.Length;
            _root = new SuffixTreeNode(-1, _rootEnd, null);

            _activeNode = _root;
            for (int i = 0; i < _size; i++)
                ExtendTree(i);

            var labelHeight = 0;
            SetSuffixIndexByDFS(_root, labelHeight);

            return _root;
        }

        private void ExtendTree(int pos)
        {
            _leafEnd.Value = pos;
            _remainingSuffixCount++;
            _lastNewNode = null;

            while (_remainingSuffixCount > 0)
            {
                if (_activeLength == 0)
                    _activeEdge = pos;

                if (_activeNode.Children[_inputArray[_activeEdge]] == null)
                {
                    _activeNode.Children[_inputArray[_activeEdge]] = new SuffixTreeNode(pos, _leafEnd, _root);

                    if (_lastNewNode != null)
                    {
                        _lastNewNode.SuffixLink = _activeNode;
                        _lastNewNode = null;
                    }
                }
                else
                {
                    var next = _activeNode.Children[_inputArray[_activeEdge]];
                    if (TryWalkDown(next))
                        continue;

                    if (_inputArray[next.Start + _activeLength] == _inputArray[pos])
                    {
                        if (_lastNewNode != null && _activeNode != _root)
                        {
                            _lastNewNode.SuffixLink = _activeNode;
                            _lastNewNode = null;
                        }

                        _activeLength++;
                        break;
                    }

                    _splitEnd = new IntValue(next.Start + _activeLength - 1);

                    var split = new SuffixTreeNode(next.Start, _splitEnd, _root);
                    _activeNode.Children[_inputArray[_activeEdge]] = split;

                    split.Children[_inputArray[pos]] = new SuffixTreeNode(pos, _leafEnd, _root);
                    next.Start += _activeLength;
                    split.Children[_inputArray[next.Start]] = next;

                    if (_lastNewNode != null)
                        _lastNewNode.SuffixLink = split;

                    _lastNewNode = split;
                }

                _remainingSuffixCount--;
                if (_activeNode == _root && _activeLength > 0)
                {
                    _activeLength--;
                    _activeEdge = pos - _remainingSuffixCount + 1;
                }
                else if (_activeNode != _root)
                    _activeNode = _activeNode.SuffixLink;
            }
        }

        private bool TryWalkDown(SuffixTreeNode node)
        {
            if (_activeLength < node.Length)
                return false;

            _activeEdge += node.Length;
            _activeLength -= node.Length;
            _activeNode = node;
            return true;
        }

        private void SetSuffixIndexByDFS(SuffixTreeNode node, int labelHeight)
        {
            if (node == null) return;

            var leaf = true;
            for (int i = 0; i < 256; i++)
            {
                if (node.Children[i] != null)
                {
                    leaf = false;
                    SetSuffixIndexByDFS(node.Children[i], labelHeight + node.Children[i].Length);
                }
            }

            if (leaf)
                node.SuffixIndex = _size - labelHeight;
        }
    }
}
