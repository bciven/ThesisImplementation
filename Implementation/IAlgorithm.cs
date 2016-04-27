using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation
{
    interface IAlgorithm<T>
    {
        T Run();
        void Initialize();
    }
}
