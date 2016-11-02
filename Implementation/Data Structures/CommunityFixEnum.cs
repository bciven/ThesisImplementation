using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    [Flags]
    public enum CommunityFixEnum
    {
        None = 0,
        Version1 = 1,
        Version2 = 2,
        Version3 = 4,
        Version4 = 8,
        InitializationFix = 16,
        DenomFix = 32
    }
}
