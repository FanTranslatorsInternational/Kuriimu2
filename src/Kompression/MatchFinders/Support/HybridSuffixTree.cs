using System;
using System.Collections.Generic;
using System.Linq;

namespace Kompression.MatchFinders.Support
{
    /// <summary>
    /// A combination of the Ukkonen suffix tree and storing offsets to each root child.
    /// </summary>
    class HybridSuffixTree : IDisposable
    {
        private Dictionary<SuffixTreeNode, SuffixTreeNode> _suffixLinks =
            new Dictionary<SuffixTreeNode, SuffixTreeNode>();
        private Dictionary<byte, List<int>> _offsetDictionary = new Dictionary<byte, List<int>>();

        private IntValue _rootEnd;
        private IntValue _leafEnd;
        private IntValue _splitEnd;

        private SuffixTreeNode _activeNode;
        private SuffixTreeNode _lastNewNode;

        private int _activeEdge = -1;
        private int _activeLength;

        private int _remainingSuffixCount;

        /// <summary>
        /// Indicates whether the tree was already built.
        /// </summary>
        public bool IsBuilt { get; private set; }

        /// <summary>
        /// Gets the root node for the created tree.
        /// </summary>
        public SuffixTreeNode Root { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="HybridSuffixTree"/>.
        /// </summary>
        public HybridSuffixTree()
        {
            _rootEnd = new IntValue(-1);
            _leafEnd = new IntValue(-1);
        }

        public int[] GetOffsets(byte value, int position, int windowSize, int minDisplacement, int unitSize)
        {
            return _offsetDictionary[value].
                Where(x => x <= position - minDisplacement && x >= position - windowSize).
                Where(x => x % unitSize == 0).
                ToArray();
        }

        /// <summary>
        /// Builds the suffix tree.
        /// </summary>
        /// <param name="input">The input from which to build the tree.</param>
        /// <param name="position">The position in the input to start the creation from.</param>
        public void Build(byte[] input, int position)
        {
            /* Root is a special node with start and end indices as -1,
            as it has no parent from where an edge comes to root */
            _activeNode = Root = new SuffixTreeNode(-1, _rootEnd);

            for (var i = position; i < input.Length; i++)
            {
                ExtendSuffixTree(input, i);
                if (!_offsetDictionary.ContainsKey(input[i]))
                    _offsetDictionary[input[i]] = new List<int>();
                _offsetDictionary[input[i]].Insert(0, i);
            }

            _suffixLinks = null;
            _activeNode = null;
            _lastNewNode = null;

            IsBuilt = true;
        }

        private void ExtendSuffixTree(byte[] input, int position)
        {
            // Extension Rule 1, this takes care of extending all
            // leaves created so far in tree
            _leafEnd.Value = position;

            // Increment remainingSuffixCount indicating that a
            // new suffix added to the list of suffixes yet to be
            // added in tree
            _remainingSuffixCount++;

            // Set lastNewNode to nullptr while starting a new phase,
            // indicating there is no internal node waiting for
            // it's suffix link reset in current phase
            _lastNewNode = null;

            //Add all suffixes (yet to be added) one by one in tree
            while (_remainingSuffixCount > 0)
            {
                if (_activeLength == 0)
                    _activeEdge = position; //APCFALZ

                // There is no outgoing edge starting with
                // activeEdge from activeNode
                if (!_activeNode.Children.ContainsKey(input[_activeEdge]))
                {
                    //Extension Rule 2 (A new leaf edge gets created)
                    var newNode = new SuffixTreeNode(position, _leafEnd);
                    _suffixLinks[newNode] = Root;
                    _activeNode.Children[input[_activeEdge]] = newNode;

                    // A new leaf edge is created in above line starting
                    // from  an existing node (the current activeNode), and
                    // if there is any internal node waiting for it's suffix
                    // link get reset, point the suffix link from that last
                    // internal node to current activeNode. Then set lastNewNode
                    // to nullptr indicating no more node waiting for suffix link
                    // reset.
                    if (_lastNewNode != null)
                    {
                        _suffixLinks[_lastNewNode] = _activeNode;
                        _lastNewNode = null;
                    }
                }
                // There is an outgoing edge starting with activeEdge
                // from activeNode
                else
                {
                    // Get the next node at the end of edge starting
                    // with activeEdge
                    var next = _activeNode.Children[input[_activeEdge]];
                    if (WalkDown(next))
                    {
                        // Start from next node (the new activeNode)
                        continue;
                    }

                    // Extension Rule 3 (current byte being processed
                    // is already on the edge)
                    if (input[next.Start + _activeLength] == input[position])
                    {
                        // If a newly created node waiting for it's 
                        // suffix link to be set, then set suffix link 
                        // of that waiting node to current active node
                        if (_lastNewNode != null && _activeNode != Root)
                        {
                            _suffixLinks[_lastNewNode] = _activeNode;
                            _lastNewNode = null;
                        }

                        _activeLength++;
                        // STOP all further processing in this phase
                        // and move on to next phase
                        break;
                    }

                    // We will be here when activePoint is in middle of
                    // the edge being traversed and current character
                    // being processed is not  on the edge (we fall off
                    // the tree). In this case, we add a new internal node
                    // and a new leaf edge going out of that new node. This
                    // is Extension Rule 2, where a new leaf edge and a new
                    // internal node get created
                    _splitEnd = new IntValue(next.Start + _activeLength - 1);

                    // New internal node
                    var split = new SuffixTreeNode(next.Start, _splitEnd);
                    _suffixLinks[split] = Root;
                    _activeNode.Children[input[_activeEdge]] = split;

                    // New leaf coming out of new internal node
                    var newNode = new SuffixTreeNode(position, _leafEnd);
                    _suffixLinks[newNode] = split;
                    split.Children[input[position]] = newNode;

                    next.Start += _activeLength;
                    split.Children[input[next.Start]] = next;

                    // We got a new internal node here. If there is any
                    // internal node created in last extensions of same
                    // phase which is still waiting for it's suffix link
                    // reset, do it now.
                    if (_lastNewNode != null)
                    {
                        // SuffixLink of lastNewNode points to current newly
                        // created internal node
                        _suffixLinks[_lastNewNode] = split;
                    }

                    // Make the current newly created internal node waiting
                    // for it's suffix link reset (which is pointing to root
                    // at present). If we come across any other internal node
                    // (existing or newly created) in next extension of same
                    // phase, when a new leaf edge gets added (i.e. when
                    // Extension Rule 2 applies is any of the next extension
                    // of same phase) at that point, suffixLink of this node
                    // will point to that internal node.
                    _lastNewNode = split;
                }

                // One suffix got added in tree, decrement the count of
                // suffixes yet to be added.
                _remainingSuffixCount--;
                if (_activeNode == Root && _activeLength > 0)
                {
                    _activeLength--;
                    _activeEdge = position - _remainingSuffixCount + 1;
                }
                else if (_activeNode != Root)
                {
                    _activeNode = _suffixLinks[_activeNode];
                }
            }
        }

        private bool WalkDown(SuffixTreeNode node)
        {
            var length = node.Length;

            // activePoint change for walk down using
            // Skip/Count Trick  (Trick 1). If activeLength is greater
            // than current edge length, set next  internal node as
            // activeNode and adjust activeEdge and activeLength
            // accordingly to represent same activePoint
            if (_activeLength >= length)
            {
                _activeEdge += length;
                _activeLength -= length;
                _activeNode = node;
                return true;
            }

            return false;
        }

        #region Dispose

        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
            _suffixLinks = null;
            _offsetDictionary = null;

            _rootEnd = null;
            _leafEnd = null;
            _splitEnd = null;
        }

        #endregion
    }
}
