using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Data_Structures
{
    public class UserEvents
    {
        private readonly bool _doublePriority;
        public int User { get; set; }
        private Dictionary<int, EventInterest> EventInterests { get; set; }

        public UserEvents(int user, int numberOfEvents, bool doublePriority)
        {
            _doublePriority = doublePriority;
            User = user;
            EventInterests = new Dictionary<int, EventInterest>(numberOfEvents);
        }

        public void AddEvent(int @event, double utility, double priority)
        {
            EventInterests.Add(@event, new EventInterest
            {
                Event = @event,
                Utility = utility,
                Priority = priority
            });
        }

        public EventInterest GetBestEvent(bool probabilisticApproach)
        {
            if (probabilisticApproach)
            {
                
            }
            double? maxVal1 = null; //nullable so this works even if you have all super-low negatives
            double? maxVal2 = null; //nullable so this works even if you have all super-low negatives
            int index = -1;
            foreach (var eventInterest in EventInterests)
            {
                if (!maxVal1.HasValue || eventInterest.Value.Utility > maxVal1.Value)
                {
                    maxVal1 = eventInterest.Value.Utility;
                    index = eventInterest.Key;
                }
            }

            if (index == -1)
            {
                return null;
            }

            if (_doublePriority)
            {
                foreach (var eventInterest in EventInterests)
                {
                    if (Math.Abs(eventInterest.Value.Utility - maxVal1.Value) < 0.001 && (maxVal2 == null || eventInterest.Value.Priority > maxVal2.Value))
                    {
                        maxVal2 = eventInterest.Value.Priority;
                        index = eventInterest.Key;
                    }
                }
            }

            var ei = EventInterests[index];
            EventInterests.Remove(ei.Event);
            return ei;
        }

        public void UpdateUserInterest(int @event, double newPriority)
        {
            if (EventInterests.ContainsKey(@event))
            {
                EventInterests[@event].Utility = newPriority;
            }
        }

        public void DevideAll(double utility)
        {
            foreach (var keyValuePair in EventInterests)
            {
                keyValuePair.Value.Utility = keyValuePair.Value.Utility/utility;
            }
        }
    }
}
