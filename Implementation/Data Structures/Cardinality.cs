using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class Cardinality
    {
        public Cardinality(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public Cardinality()
        {
            
        }

        public int Min { get; set; }

        public int Max { get; set; }
    }
}
