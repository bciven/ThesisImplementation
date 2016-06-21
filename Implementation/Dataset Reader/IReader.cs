using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Dataset_Reader
{
    public interface IReader
    {
        void FillInnateInterests();

        void FillSocialInterests();
    }
}
