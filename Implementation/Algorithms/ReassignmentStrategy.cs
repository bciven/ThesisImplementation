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
        private int _power2;

        public ReassignmentStrategy(Algorithm<T> algorithm)
        {
            _algorithm = algorithm;
            _previousPhantomNumberCount = 0;
            _power2 = 0;
        }

        public void KeepPhantomEvents(List<int> availableUsers, List<int> realOpenEvents, AlgorithmSpec.ReassignmentEnum reassignment, double preservePerc)
        {
            if (reassignment == AlgorithmSpec.ReassignmentEnum.Addition)
            {
                _phantomEvents = _phantomEvents?.Where(x => !_algorithm.EventIsReal(x)).ToList() ?? _algorithm.AllEvents.Where(x => !_algorithm.EventIsReal(x)).ToList();
                var events = _phantomEvents.Select(x => new UserEvent { Event = x, Utility = 0d }).ToList();
                AdditionStrategy(availableUsers, realOpenEvents, events);
            }
            else if (reassignment == AlgorithmSpec.ReassignmentEnum.Reduction)
            {
                _phantomEvents = _phantomEvents?.Where(x => !_algorithm.EventIsReal(x)).ToList() ?? _algorithm.AllEvents.Where(x => !_algorithm.EventIsReal(x)).ToList();
                var events = _phantomEvents.Select(x => new UserEvent { Event = x, Utility = 0d }).ToList();
                ReductionStrategy(availableUsers, realOpenEvents, events, preservePerc);
            }
            else if (reassignment == AlgorithmSpec.ReassignmentEnum.Power2Reduction)
            {
                _phantomEvents = _algorithm.AllEvents.Where(x => !_algorithm.EventIsReal(x)).ToList();
                var events = _phantomEvents.Select(x => new UserEvent { Event = x, Utility = 0d }).ToList();
                Power2ReductionStrategy(availableUsers, realOpenEvents, events);
            }
        }

        private void ReductionStrategy(List<int> availableUsers, List<int> realOpenEvents, List<UserEvent> phantomEvents, double preservePerc)
        {
            foreach (var @event in phantomEvents)
            {
                @event.Utility = availableUsers.Sum(user => _algorithm.InAffinities[user][@event.Event]);
            }
            phantomEvents = phantomEvents.OrderByDescending(x => x.Utility).ToList();

            int eventsToKeep = (int)Math.Round((double)(preservePerc * phantomEvents.Count) / 100);

            if (_previousPhantomNumberCount == 0 || _previousPhantomNumberCount != phantomEvents.Count)
            {
                _previousPhantomNumberCount = phantomEvents.Count;
            }
            else if (_previousPhantomNumberCount == phantomEvents.Count)
            {
                eventsToKeep = (int)Math.Floor((double)(preservePerc * eventsToKeep) / 100);
            }

            phantomEvents = phantomEvents.Take(eventsToKeep).ToList();
            _phantomEvents = phantomEvents.Select(x => x.Event).ToList();
            realOpenEvents.AddRange(phantomEvents.Select(phantomEvent => phantomEvent.Event));
        }

        private void Power2ReductionStrategy(List<int> availableUsers, List<int> realOpenEvents, List<UserEvent> phantomEvents)
        {
            foreach (var @event in phantomEvents)
            {
                @event.Utility = availableUsers.Sum(user => _algorithm.InAffinities[user][@event.Event]);
            }
            phantomEvents = phantomEvents.OrderByDescending(x => x.Utility).ToList();
            var preservePerc = 100 - Math.Pow(2, _power2);
            _power2++;

            int eventsToKeep = (int)Math.Round((double)(preservePerc * phantomEvents.Count) / 100);

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