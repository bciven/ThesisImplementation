using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class Graph
    {
        public Dictionary<int, List<int>> Edges;

        public Graph(int numberOfEdges)
        {
            Edges = new Dictionary<int, List<int>>(numberOfEdges);
        }

        public Graph()
        {
            Edges = new Dictionary<int, List<int>>();
        }
    }
}
