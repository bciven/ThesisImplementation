using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class Watches
    {
        public Stopwatch _watch;
        public Stopwatch _assignmentWatch;
        public Stopwatch _userSubstitueWatch;
        public Stopwatch _eventSwitchWatch;
        public Watches()
        {
            _watch = new Stopwatch();
            _assignmentWatch = new Stopwatch();
            _userSubstitueWatch = new Stopwatch();
            _eventSwitchWatch = new Stopwatch();
        }
    }
}
