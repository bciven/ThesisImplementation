using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class UserEvent
    {
        public string Key => User + "-" + Event;

        public UserEvent(int user, int @event, double utility, double priority)
        {
            User = user;
            Event = @event;
            Utility = utility;
            Priority = priority;
        }

        public UserEvent(int user, int @event)
        {
            User = user;
            Event = @event;
        }

        public UserEvent(int user, int @event, double utility)
        {
            User = user;
            Event = @event;
            Utility = utility;
        }

        public UserEvent()
        {
            
        }

        public int User { get; set; }
        public int Event { get; set; }
        public double Utility { get; set; }
        public double Priority { get; set; }
    }
}
