using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class UserEvents
    {
        private readonly int _numberOfEvents;
        public int User { get; set; }
        private EventInterest[] EventInterests { get; set; }

        public UserEvents(int user, int numberOfEvents)
        {
            _numberOfEvents = numberOfEvents;
            User = user;
            EventInterests = new EventInterest[numberOfEvents];
        }

        public void AddEvent(int @event, double utility)
        {
            EventInterests[@event] = new EventInterest
            {
                Event = @event,
                Utility = utility
            };
        }

        public EventInterest GetBestEvent()
        {
            double? maxVal = null; //nullable so this works even if you have all super-low negatives
            int index = -1;
            for (int i = 0; i < EventInterests.Length; i++)
            {
                var eventInterest = EventInterests[i];
                if (!maxVal.HasValue || eventInterest.Utility > maxVal.Value)
                {
                    maxVal = eventInterest.Utility;
                    index = i;
                }
            }
            return EventInterests[index];
        }

        public void UpdateUserInterest(int @event, double newPriority)
        {
            EventInterests[@event].Utility = newPriority;
        }
    }
}
