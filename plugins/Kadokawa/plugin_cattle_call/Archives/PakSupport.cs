namespace plugin_cattle_call.Archives
{
    class PakHeader
    {
        public short fileCount;
        public int entryOffset;
        public short nameTable;
    }

    class PakEntry
    {
        public int size;
        public int offset;
    }

    class StringNode
    {
        public string Text { get; private set; }

        public IList<StringNode> Nodes { get; private set; }

        public StringNode()
        {
            Nodes = new List<StringNode>();
        }

        public void Add(string input)
        {
            Add(input, 0);
        }

        public void AddRange(IList<string> inputs)
        {
            foreach (var input in inputs)
                Add(input);
        }

        private void Add(string input, int position)
        {
            // If no nodes exist yet, add substring without processing it
            if (Nodes.Count < 0)
            {
                Nodes.Add(new StringNode { Text = input.Substring(position, input.Length - position) });
                return;
            }

            // Determine best matching node
            StringNode matchingNode = null;
            foreach (var node in Nodes)
            {
                if (node.Text[0] == input[position])
                {
                    matchingNode = node;
                    break;
                }
            }

            // If no node matched, create a new one with substring
            if (matchingNode == null)
            {
                Nodes.Add(new StringNode { Text = input.Substring(position, input.Length - position) });
                return;
            }

            // Match node until end or difference
            var length = Math.Min(matchingNode.Text.Length, input.Length - position);
            for (var i = 1; i < length; i++)
            {
                if (input[position + i] != matchingNode.Text[i])
                {
                    // Split and relocate nodes
                    var splitNode = new StringNode() { Text = matchingNode.Text.Substring(i, matchingNode.Text.Length - i) };
                    var newNode = new StringNode() { Text = input.Substring(position + i, input.Length - position - i) };

                    matchingNode.Text = matchingNode.Text.Substring(0, i);

                    splitNode.Nodes = matchingNode.Nodes;
                    matchingNode.Nodes = new List<StringNode> { splitNode, newNode };

                    return;
                }
            }

            // If strings only differed in length
            if (matchingNode.Text.Length < input.Length - position)
            {
                matchingNode.Add(input, position + matchingNode.Text.Length);
            }

            if (input.Length - position < matchingNode.Text.Length)
            {
                var splitNode = new StringNode() { Text = matchingNode.Text.Substring(input.Length - position, matchingNode.Text.Length - (input.Length - position)) };
                matchingNode.Text = matchingNode.Text.Substring(0, input.Length - position);

                splitNode.Nodes = matchingNode.Nodes;
                matchingNode.Nodes = new List<StringNode> { splitNode };
            }
        }
    }

}
