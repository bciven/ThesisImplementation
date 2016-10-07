using System;
using System.Collections.Generic;
using System.Linq;
using Implementation.Data_Structures;

namespace Implementation.Algorithms
{
    public class ReassignmentStrategy<T>
    {
        private readonly Algorithm<T> _algorithm;
        private int _previousPhantomNumberCount;
        private List<int> _phantomEvents;

        public ReassignmentStrategy(Algorithm<T> algorithm)
        {
            _algorithm = algorithm;
            _previousPhantomNumberCount = 0;
        }

        public void KeepPhantomEvents(List<int> availableUsers, List<int> realOpenEvents, AlgorithmSpec.ReassignmentEnum reassignment)
        {
            _phantomEvents = _phantomEvents?.Where(x => !_algorithm.EventIsReal(x)).ToList() ?? _algorithm.AllEvents.Where(x => !_algorithm.EventIsReal(x)).ToList();
            var events = _phantomEvents.Select(x => new UserEvent { Event = x, Utility = 0d }).ToList();
            if (reassignment == AlgorithmSpec.ReassignmentEnum.Addition)
            {
                AdditionStrategy(availableUsers, realOpenEvents, events);
            }
            else if (reassignment == AlgorithmSpec.ReassignmentEnum.Reduction)
            {
                ReductionStrategy(availableUsers, realOpenEvents, events);
            }
        }

        private void ReductionStrategy(List<int> availableUsers, List<int> realOpenEvents, List<UserEvent> phantomEvents)
        {
            foreach (var @event in phantomEvents)
            {
                @event.Utility = availableUsers.Sum(user => _algorithm.InAffinities[user][@event.Event]);
            }
            phantomEvents = phantomEvents.OrderByDescending(x => x.Utility).ToList();

            int eventsToKeep = (int)Math.Round((double)(90 * phantomEvents.Count) / 100);

            if (_previousPhantomNumberCount == 0 || _previousPhantomNumberCount != phantomEvents.Count)
            {
                _previousPhantomNumberCount = phantomEvents.Count;
            }
            else if (_previousPhantomNumberCount == phantomEvents.Count)
            {
                eventsToKeep = (int)Math.Floor((double)(90 * eventsToKeep) / 100);
            }
            
            phantomEvents = phantomEvents.Take(eventsToKeep).ToList();
            _phantomEvents = phantomEvents.Select(x => x.Event).ToList();
            realOpenEvents.AddRange(phantomEvents.Select(phantomEvent => phantomEvent.Event));
        }

        private void AdditionStrategy(List<int> availableUsers, List<int> realOpenEvents, List<UserEvent> phantomEvents)
        {
            foreach (var @event in phantomEvents)
            {
                @event.Utility = availableUsers.Sum(user => _algorithm.InAffinities[user][@event.Event]);
            }
            phantomEvents = phantomEvents.OrderByDescending(x => x.Utility).ToList();
            var numberOfAvailableUsers = availableUsers.Count;
            foreach (var phantomEvent in phantomEvents)
            {
                if (_algorithm.EventCapacity[phantomEvent.Event].Min <= numberOfAvailableUsers)
                {
                    realOpenEvents.Add(phantomEvent.Event);
                    numberOfAvailableUsers -= _algorithm.EventCapacity[phantomEvent.Event].Min;
                    if (numberOfAvailableUsers == 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}