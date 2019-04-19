using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Models
{
    internal class TaskModel
    {
        public Color[] Colors { get; }
        public int[] Indices { get; }
        public int Start { get; }
        public int Length { get; }

        public TaskModel(Color[] inputColors, int[] indices, int start, int length)
        {
            Colors = inputColors;
            Indices = indices;
            Start = start;
            Length = length;
        }
    }
}
