using System;
using System.Collections.Generic;
using System.IO;

namespace Kore.XFont.Compression
{
    public sealed class HuffSupport
    {
        public class SimpleReversedPrioQueue<TPrio, TValue>
        {
            private SortedDictionary<TPrio, LinkedList<TValue>> items;
            private int itemCount;

            /// <summary>
            /// Gets the number of items in this queue.
            /// </summary>
            public int Count { get { return this.itemCount; } }

            /// <summary>
            /// Creates a new, empty reverse priority queue.
            /// </summary>
            public SimpleReversedPrioQueue()
            {
                this.items = new SortedDictionary<TPrio, LinkedList<TValue>>();
                this.itemCount = 0;
            }

            /// <summary>
            /// Enqueues the given value, using the given priority.
            /// </summary>
            /// <param name="priority">The priority of the value.</param>
            /// <param name="value">The value to enqueue.</param>
            public void Enqueue(TPrio priority, TValue value)
            {
                if (!this.items.ContainsKey(priority))
                    this.items.Add(priority, new LinkedList<TValue>());
                this.items[priority].AddLast(value);
                this.itemCount++;
            }

            /// <summary>
            /// Gets the current value with the lowest priority from this queue, without dequeueing the value.
            /// </summary>
            /// <param name="priority">The priority of the returned value.</param>
            /// <returns>The current value with the lowest priority.</returns>
            /// <exception cref="IndexOutOfRangeException">If there are no items left in this queue.</exception>
            public TValue Peek(out TPrio priority)
            {
                if (this.itemCount == 0)
                    throw new IndexOutOfRangeException();
                foreach (KeyValuePair<TPrio, LinkedList<TValue>> kvp in this.items)
                {
                    priority = kvp.Key;
                    return kvp.Value.First.Value;
                }
                throw new IndexOutOfRangeException();
            }

            /// <summary>
            /// Dequeues the current value at the head of thisreverse priority queue.
            /// </summary>
            /// <param name="priority">The priority of the dequeued value.</param>
            /// <returns>The dequeued value, that used to be at the head of this queue.</returns>
            /// <exception cref="IndexOutOfRangeException">If this queue does not contain any items.</exception>
            public TValue Dequeue(out TPrio priority)
            {
                if (this.itemCount == 0)
                    throw new IndexOutOfRangeException();
                LinkedList<TValue> lowestLL = null;
                priority = default(TPrio);
                foreach (KeyValuePair<TPrio, LinkedList<TValue>> kvp in this.items)
                {
                    lowestLL = kvp.Value;
                    priority = kvp.Key;
                    break;
                }

                TValue returnValue = lowestLL.First.Value;
                lowestLL.RemoveFirst();
                // remove unused linked lists. priorities will only grow.
                if (lowestLL.Count == 0)
                {
                    this.items.Remove(priority);
                }
                this.itemCount--;
                return returnValue;
            }
        }

        public class HuffTreeNode
        {
            #region Fields & Properties: Data & IsData
            /// <summary>
            /// The data contained in this node. May not mean anything when <code>isData == false</code>
            /// </summary>
            private byte data;
            /// <summary>
            /// A flag indicating if this node has been filled.
            /// </summary>
            private bool isFilled;
            /// <summary>
            /// The data contained in this node. May not mean anything when <code>isData == false</code>.
            /// Throws a NullReferenceException when this node has not been defined (ie: reference was outside the
            /// bounds of the tree definition)
            /// </summary>
            public byte Data
            {
                get
                {
                    if (!this.isFilled) throw new NullReferenceException("Reference to an undefined node in the huffman tree.");
                    return this.data;
                }
            }
            /// <summary>
            /// A flag indicating if this node contains data. If not, this is not a leaf node.
            /// </summary>
            private bool isData;
            /// <summary>
            /// Returns true if this node represents data.
            /// </summary>
            public bool IsData { get { return this.isData; } }
            #endregion

            #region Field & Properties: Children & Parent
            /// <summary>
            /// The child of this node at side 0
            /// </summary>
            private HuffTreeNode child0;
            /// <summary>
            /// The child of this node at side 0
            /// </summary>
            public HuffTreeNode Child0 { get { return this.child0; } }
            /// <summary>
            /// The child of this node at side 1
            /// </summary>
            private HuffTreeNode child1;
            /// <summary>
            /// The child of this node at side 1
            /// </summary>
            public HuffTreeNode Child1 { get { return this.child1; } }
            /// <summary>
            /// The parent node of this node.
            /// </summary>
            public HuffTreeNode Parent { get; private set; }
            /// <summary>
            /// Determines if this is the Child0 of the parent node. Assumes there is a parent.
            /// </summary>
            public bool IsChild0 { get { return this.Parent.child0 == this; } }
            /// <summary>
            /// Determines if this is the Child1 of the parent node. Assumes there is a parent.
            /// </summary>
            public bool IsChild1 { get { return this.Parent.child1 == this; } }
            #endregion

            #region Field & Property: Depth
            private int depth;
            /// <summary>
            /// Get or set the depth of this node. Will not be set automatically, but
            /// will be set recursively (the depth of all child nodes will be updated when this is set).
            /// </summary>
            public int Depth
            {
                get { return this.depth; }
                set
                {
                    this.depth = value;
                    // recursively set the depth of the child nodes.
                    if (!this.isData)
                    {
                        this.child0.Depth = this.depth + 1;
                        this.child1.Depth = this.depth + 1;
                    }
                }
            }
            #endregion

            #region Property: Size
            /// <summary>
            /// Calculates the size of the sub-tree with this node as root.
            /// </summary>
            public int Size
            {
                get
                {
                    if (this.IsData)
                        return 1;
                    return 1 + this.child0.Size + this.child1.Size;
                }
            }
            #endregion

            /// <summary>
            /// The index of this node in the array for building the proper ordering.
            /// If -1, this node has not yet been placed in the array.
            /// </summary>
            internal int index = -1;

            #region Constructor(data, isData, child0, child1)
            /// <summary>
            /// Manually creates a new node for a huffman tree.
            /// </summary>
            /// <param name="data">The data for this node.</param>
            /// <param name="isData">If this node represents data.</param>
            /// <param name="child0">The child of this node on the 0 side.</param>
            /// <param name="child1">The child of this node on the 1 side.</param>
            public HuffTreeNode(byte data, bool isData, HuffTreeNode child0, HuffTreeNode child1)
            {
                this.data = data;
                this.isData = isData;
                this.child0 = child0;
                this.child1 = child1;
                this.isFilled = true;
                if (!isData)
                {
                    this.child0.Parent = this;
                    this.child1.Parent = this;
                }
            }
            #endregion

            #region Constructor(Stream, isData, relOffset, maxStreamPos)
            /// <summary>
            /// Creates a new node in the Huffman tree.
            /// </summary>
            /// <param name="stream">The stream to read from. It is assumed that there is (at least)
            /// one more byte available to read.</param>
            /// <param name="isData">If this node is a data-node.</param>
            /// <param name="relOffset">The offset of this node in the source data, relative to the start
            /// of the compressed file.</param>
            /// <param name="maxStreamPos">The indicated end of the huffman tree. If the stream is past
            /// this position, the tree is invalid.</param>
            public HuffTreeNode(Stream stream, bool isData, long relOffset, long maxStreamPos)
            {
                /*
                 Tree Table (list of 8bit nodes, starting with the root node)
                    Root Node and Non-Data-Child Nodes are:
                    Bit0-5   Offset to next child node,
                            Next child node0 is at (CurrentAddr AND NOT 1)+Offset*2+2
                            Next child node1 is at (CurrentAddr AND NOT 1)+Offset*2+2+1
                    Bit6     Node1 End Flag (1=Next child node is data)
                    Bit7     Node0 End Flag (1=Next child node is data)
                    Data nodes are (when End Flag was set in parent node):
                    Bit0-7   Data (upper bits should be zero if Data Size is less than 8)
                 */

                if (stream.Position >= maxStreamPos)
                {
                    // this happens when part of the tree is unused.
                    this.isFilled = false;
                    return;
                }
                this.isFilled = true;
                int readData = stream.ReadByte();
                if (readData < 0)
                    throw new Exception("Stream too short");
                this.data = (byte)readData;

                this.isData = isData;

                if (!this.isData)
                {
                    int offset = this.data & 0x3F;
                    bool zeroIsData = (this.data & 0x80) > 0;
                    bool oneIsData = (this.data & 0x40) > 0;

                    // off AND NOT 1 == off XOR (off AND 1)
                    long zeroRelOffset = (relOffset ^ (relOffset & 1)) + offset * 2 + 2;

                    long currStreamPos = stream.Position;
                    // position the stream right before the 0-node
                    stream.Position += (zeroRelOffset - relOffset) - 1;
                    // read the 0-node
                    this.child0 = new HuffTreeNode(stream, zeroIsData, zeroRelOffset, maxStreamPos);
                    this.child0.Parent = this;
                    // the 1-node is directly behind the 0-node
                    this.child1 = new HuffTreeNode(stream, oneIsData, zeroRelOffset + 1, maxStreamPos);
                    this.child1.Parent = this;

                    // reset the stream position to right behind this node's data
                    stream.Position = currStreamPos;
                }
            }
            #endregion

            /// <summary>
            /// Generates and returns a string-representation of the huffman tree starting at this node.
            /// </summary>
            public override string ToString()
            {
                if (this.isData)
                {
                    return "<" + this.data.ToString("X2") + ">";
                }
                else
                {
                    return "[" + this.child0.ToString() + "," + this.child1.ToString() + "]";
                }
            }
        }

        public static HuffTreeNode GetLowest(SimpleReversedPrioQueue<int, HuffTreeNode> leafQueue, SimpleReversedPrioQueue<int, HuffTreeNode> nodeQueue, out int prio)
        {
            if (leafQueue.Count == 0)
                return nodeQueue.Dequeue(out prio);
            else if (nodeQueue.Count == 0)
                return leafQueue.Dequeue(out prio);
            else
            {
                int leafPrio, nodePrio;
                leafQueue.Peek(out leafPrio);
                nodeQueue.Peek(out nodePrio);
                // pick a node from the leaf queue when the priorities are equal.
                if (leafPrio <= nodePrio)
                    return leafQueue.Dequeue(out prio);
                else
                    return nodeQueue.Dequeue(out prio);
            }
        }

        public static void Insert(HuffTreeNode node, HuffTreeNode[] array, int maxOffset)
        {
            // if the node has two data-children, insert it as far to the end as possible.
            if (node.Child0.IsData && node.Child1.IsData)
            {
                for (int i = array.Length - 1; i >= 0; i--)
                {
                    if (array[i] == null)
                    {
                        array[i] = node;
                        node.index = i;
                        break;
                    }
                }
            }
            else
            {
                // if the node is not data, insert it as far left as possible.
                // we know that both children are already present.
                int offset = Math.Max(node.Child0.index - maxOffset, node.Child1.index - maxOffset);
                offset = Math.Max(0, offset);
                if (offset >= node.Child0.index || offset >= node.Child1.index)
                {
                    // it may be that the childen are too far apart, with lots of empty entries in-between.
                    // shift the bottom child right until the node fits in its left-most place for the top child.
                    // (there should be more than enough room in the array)
                    while (offset >= Math.Min(node.Child0.index, node.Child1.index))
                        ShiftRight(array, Math.Min(node.Child0.index, node.Child1.index), maxOffset);
                    while (array[offset] != null)
                        ShiftRight(array, offset, maxOffset);
                    array[offset] = node;
                    node.index = offset;
                }
                else
                {
                    for (int i = offset; i < node.Child0.index && i < node.Child1.index; i++)
                    {
                        if (array[i] == null)
                        {
                            array[i] = node;
                            node.index = i;
                            break;
                        }
                    }
                }
            }

            if (node.index < 0)
                throw new Exception("Node could not be inserted!");

            // if the insertion of this node means that the parent has both children inserted, insert the parent.
            if (node.Parent != null)
            {
                if ((node.Parent.Child0.index >= 0 || node.Parent.Child0.IsData)
                    && (node.Parent.Child1.index >= 0 || node.Parent.Child1.IsData))
                    Insert(node.Parent, array, maxOffset);
            }
        }

        private static void ShiftRight(HuffTreeNode[] array, int idx, int maxOffset)
        {
            HuffTreeNode node = array[idx];
            if (array[idx + 1] != null)
                ShiftRight(array, idx + 1, maxOffset);
            if (node.Parent.index > 0 && node.index - maxOffset + 1 > node.Parent.index)
                ShiftRight(array, node.Parent.index, maxOffset);
            if (node != array[idx])
                return; // already done indirectly.
            array[idx + 1] = array[idx];
            array[idx] = null;
            node.index++;
        }
    }
}
