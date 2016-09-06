using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class UserEvents
    {
        public int User { get; set; }
        private Dictionary<int, EventInterest> EventInterests { get; set; }

        public UserEvents(int user, int numberOfEvents)
        {
            User = user;
            EventInterests = new Dictionary<int, EventInterest>(numberOfEvents);
        }

        public void AddEvent(int @event, double utility)
        {
            EventInterests.Add(@event, new EventInterest
            {
                Event = @event,
                Utility = utility
            });
        }

        public EventInterest GetBestEvent()
        {
            double? maxVal = null; //nullable so this works even if you have all super-low negatives
            int index = -1;
            foreach (var eventInterest in EventInterests)
            {
                if (!maxVal.HasValue || eventInterest.Value.Utility > maxVal.Value)
                {
                    maxVal = eventInterest.Value.Utility;
                    index = eventInterest.Key;
                }
            }
            return EventInterests[index];
        }

        public void UpdateUserInterest(int @event, double newPriority)
        {
            if (EventInterests.ContainsKey(@event))
            {
                EventInterests[@event].Utility = newPriority;
            }
        }
    }
}
