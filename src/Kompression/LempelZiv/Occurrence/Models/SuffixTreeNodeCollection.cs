using System;
using System.Collections.Generic;
using System.Linq;

namespace Kompression.LempelZiv.Occurrence.Models
{
    class SuffixTreeNodeCollection
    {
        private readonly Lazy<Dictionary<int, SuffixTreeNode>> _children2 = new Lazy<Dictionary<int, SuffixTreeNode>>(() => new Dictionary<int, SuffixTreeNode>());
        private readonly Lazy<List<SuffixTreeChild>> _children = new Lazy<List<SuffixTreeChild>>(() => new List<SuffixTreeChild>());

        private readonly int _collectionMode;

        public SuffixTreeNode this[int i]
        {
            get
            {
                if (_collectionMode == 0)
                    return _children.Value.FirstOrDefault(x => x.Index == i)?.Node;

                return _children2.Value.ContainsKey(i) ? _children2.Value[i] : null;
            }
            set
            {
                if (_collectionMode == 0)
                {
                    if (_children.Value.Any(x => x.Index == i))
                        _children.Value.First(x => x.Index == i).Node = value;
                    else
                        _children.Value.Add(new SuffixTreeChild(i, value));
                }
                else
                    _children2.Value[i] = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collectionMode">Describes the list type used. 0 being lazy list, 1 being lazy dictionary.</param>
        public SuffixTreeNodeCollection(int collectionMode)
        {
            if (collectionMode != 0 && collectionMode != 1)
                throw new ArgumentOutOfRangeException(nameof(collectionMode));
            _collectionMode = collectionMode;
        }
    }
}
