using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class UndirectedGraph
    {
        public Dictionary<int, List<int>> Edges;

        public UndirectedGraph(int numberOfEdges)
        {
            Edges = new Dictionary<int, List<int>>(numberOfEdges);
        }

        public UndirectedGraph()
        {
            Edges = new Dictionary<int, List<int>>();
        }
    }
}
