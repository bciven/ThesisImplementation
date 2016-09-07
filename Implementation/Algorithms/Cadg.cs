using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;

namespace Implementation.Algorithms
{
    public class Cadg : Algorithm<List<UserEvent>>
    {
        private List<int> _events;
        private List<int> _users;
        private List<int> _numberOfUserAssignments;
        private List<int> _eventDeficitContribution;
        private List<List<int>> _permanentAssignments;
        private int _deficit = 0;
        private FakeHeap _queue;
        private List<int> _phantomEvents;
        private bool _init;
        private readonly IDataFeed _dataFeeder;
        private CadgConf _conf => (CadgConf)Conf;

        public Cadg(CadgConf conf, IDataFeed dataFeed)
        {
            _dataFeeder = dataFeed;
            Conf = conf;
        }

        public override void Run()
        {
            if (!_init)
                throw new Exception("Not Initialized");
            int hitcount = 0;

            while (!_queue.IsEmpty())
            {
                hitcount++;
                PrintQueue();
                var min = _queue.RemoveMax();
                var user = min.User;
                var @event = min.Event;
                var minCapacity = EventCapacity[@event].Min;
                var maxCapacity = EventCapacity[@event].Max;
                bool assignmentMade = false;
                List<int> affectedEvents = new List<int>();

                if (UserAssignments[user] == null && Assignments[@event].Count < maxCapacity && !Assignments[@event].Contains(user))
                {
                    if (_conf.PhantomAware)
                    {
                        if (Assignments[@event].Count == 0)
                        {
                            if (_eventDeficitContribution.Sum(x => x) + minCapacity <= _users.Count)
                            {
                                if (!_conf.DeficitFix)
                                {
                                    _deficit = _deficit + minCapacity - 1;
                                }
                                else
                                {
                                    _eventDeficitContribution[@event] = minCapacity - 1;
                                }
                                _phantomEvents.Add(@event);
                            }
                            else
                            {
                                PrintAssignments(assignmentMade);
                                continue;
                            }
                        }
                        else
                        {
                            if (_phantomEvents.Contains(@event))
                            {
                                if (!_conf.DeficitFix)
                                {
                                    _deficit--;
                                }
                                else
                                {
                                    _eventDeficitContribution[@event]--;
                                }
                            }
                        }
                    }

                    Assignments[@event].Add(user);
                    _numberOfUserAssignments[user]++;
                    assignmentMade = true;

                    if (_users.Contains(user))
                    {
                        _users.Remove(user);
                    }

                    if (Assignments[@event].Count > minCapacity)
                    {
                        UserAssignments[user] = @event;
                        _permanentAssignments[@event].Add(user);

                        var excludedEvents = _events.Where(x => x != @event && Assignments[x].Contains(user)).ToList();
                        foreach (var e in excludedEvents)
                        {
                            Assignments[e].Remove(user);
                            _numberOfUserAssignments[user]--;
                            if (_conf.ImmediateReaction)
                            {
                                affectedEvents.Add(e);
                            }
                        }
                    }

                    if (Assignments[@event].Count == minCapacity)
                    {
                        _phantomEvents.Remove(@event);
                        foreach (var u in Assignments[@event])
                        {
                            //permanently assign all users to real events
                            UserAssignments[u] = @event;
                            _permanentAssignments[@event].Add(u);

                            //unassign these users from all other events
                            var excludedEvents = _events.Where(x => x != @event && Assignments[x].Contains(u));
                            foreach (var e in excludedEvents)
                            {
                                if (_conf.PhantomAware && !_phantomEvents.Contains(e))
                                {
                                    Console.WriteLine("This event should be phantom!");
                                }
                                Assignments[e].Remove(u);
                                _numberOfUserAssignments[u]--;

                                //affected_evts.append(e)  # this line is not in ref paper
                                if (_conf.ImmediateReaction)
                                {
                                    affectedEvents.Add(e);
                                }
                            }
                        }
                    }

                    if (_conf.LazyAdjustment)
                    {
                        AdjustList(affectedEvents, user, @event, assignmentMade);
                    }
                }

                if (!_conf.LazyAdjustment)
                {
                    AdjustList(affectedEvents, user, @event, assignmentMade);
                }

                if (_queue.IsEmpty())
                {
                    DynamicReassign();
                }
            }

            GreedyAssign();

            Assignments = _permanentAssignments;
        }

        private void GreedyAssign()
        {
            if (!_conf.GreedyReassign)
                return;

            if (UserAssignments.Any(x => !x.HasValue))
            {
                List<int> availableUsers;
                List<int> realOpenEvents;
                PrepareReassignment(out availableUsers, out realOpenEvents);

                var queue = new FakeHeap();
                foreach (var @event in realOpenEvents)
                {
                    foreach (var availableUser in availableUsers)
                    {
                        var q = Util(@event, availableUser, _conf.CommunityAware, _users);
                        if (q > 0)
                        {
                            queue.AddOrUpdate(q, new UserEvent { User = availableUser, Event = @event });
                        }
                    }
                }

                while (!queue.IsEmpty())
                {
                    var userEvent = queue.RemoveMax();
                    if (UserAssignments[userEvent.User] == null && Assignments[userEvent.Event].Count >= EventCapacity[userEvent.Event].Min && Assignments[userEvent.Event].Count < EventCapacity[userEvent.Event].Max)
                    {
                        Assignments[userEvent.Event].Add(userEvent.User);
                        UserAssignments[userEvent.User] = userEvent.Event;
                    }
                }
            }
        }

        private void DynamicReassign()
        {
            if (!_conf.DynamicReassign)
                return;

            if (UserAssignments.Any(x => !x.HasValue))
            {
                List<int> availableUsers;
                List<int> realOpenEvents;
                PrepareReassignment(out availableUsers, out realOpenEvents);

                foreach (var @event in realOpenEvents)
                {
                    foreach (var availableUser in availableUsers)
                    {
                        var q = Util(@event, availableUser, _conf.CommunityAware, _users);
                        _queue.AddOrUpdate(q, new UserEvent { User = availableUser, Event = @event });
                    }
                }
            }
        }

        private void PrepareReassignment(out List<int> availableUsers, out List<int> realOpenEvents)
        {
            var phantomEvents = AllEvents.Where(x => Assignments[x].Count < EventCapacity[x].Min).ToList();
            realOpenEvents = AllEvents.Where(x => EventCapacity[x].Min <= Assignments[x].Count && Assignments[x].Count < EventCapacity[x].Max).ToList();
            availableUsers = new List<int>();
            for (int i = 0; i < UserAssignments.Count; i++)
            {
                if (UserAssignments[i] == null)
                {
                    availableUsers.Add(i);
                }
            }

            foreach (var phantomEvent in phantomEvents)
            {
                if (Assignments[phantomEvent].Count > 0)
                {
                    availableUsers.AddRange(Assignments[phantomEvent]);
                    Assignments[phantomEvent].RemoveAll(x => true);
                }
            }
            availableUsers = availableUsers.Distinct().OrderBy(x => x).ToList();
            availableUsers.ForEach(x => _numberOfUserAssignments[x] = 0);
        }

        private void AdjustList(List<int> affectedEvents, int user, int @event, bool assignmentMade)
        {
            if (_conf.ImmediateReaction)
            {
                foreach (var e in affectedEvents)
                {
                    ImmediateReaction(e);
                }
            }

            foreach (var u in AllUsers)
            {
                Update(user, u, @event);
            }

            PrintAssignments(assignmentMade);
            CheckValidity();
        }

        private void CheckValidity()
        {
            foreach (var assignment in Assignments)
            {
                if (assignment.Count != assignment.Distinct().Count())
                {
                    Console.WriteLine("Elements are not unique !");
                    break;
                }
            }
        }

        private void ImmediateReaction(int @event)
        {
            var userEvents = new List<UserEvent>();
            var numberOfUsers = Assignments[@event].Count;
            for (int i = 0; i < numberOfUsers; i++)
            {
                var userOfOtherEvent = Assignments[@event][i];
                _numberOfUserAssignments[userOfOtherEvent]--;
                if (_numberOfUserAssignments[userOfOtherEvent] == 0)
                {
                    _users.Add(userOfOtherEvent);
                }
                if (UserAssignments[userOfOtherEvent] != null)
                {
                    Console.WriteLine("User is already assigned?!");
                }
                if (!_phantomEvents.Contains(@event))
                {
                    Console.WriteLine("Event is not a phantom!!");
                }
                var ue = new UserEvent { Event = @event, User = userOfOtherEvent };
                userEvents.Add(ue);
            }
            Assignments[@event].Clear();

            foreach (var ue in userEvents)
            {
                var newPriority = Util(ue.Event, ue.User, _conf.CommunityAware, _users);
                _queue.AddOrUpdate(newPriority, ue);
                foreach (var user in AllUsers)
                {
                    Update(ue.User, user, ue.Event);
                }
            }

            //_affectedEvents.Add(@event);
            if (_conf.PhantomAware)
            {
                if (_phantomEvents.Contains(@event))
                {
                    _phantomEvents.Remove(@event);
                }

                if (_conf.DeficitFix)
                {
                    _eventDeficitContribution[@event] = 0;
                }
                else if (EventCapacity[@event].Min >= numberOfUsers)
                {
                    _deficit = _deficit - (EventCapacity[@event].Min - numberOfUsers);
                }
            }
        }

        private void Update(int user1, int user2, int @event)
        {
            if (SocAffinities[user1, user2] > 0 && UserAssignments[user2] == null) /* or a in affected_evts)*/
            {
                //What if this friend is already in that event, should it be aware that his friend is now assigned to this event?
                var newPriority = Util(@event, user2, _conf.CommunityAware, _users);
                if (!Assignments[@event].Contains(user2) && Assignments[@event].Count < EventCapacity[@event].Max)
                {
                    _queue.AddOrUpdate(newPriority, new UserEvent { User = user2, Event = @event });
                }

                /*if (_conf.ImmediateReaction)
                {
                    if (!Assignments[@event].Contains(user2) && Assignments[@event].Count < EventCapacity[@event].Max)
                    {
                        _queue.AddOrUpdate(newPriority, new UserEvent { User = user2, Event = @event });
                    }
                }
                else
                {
                    _queue.AddOrUpdate(newPriority, new UserEvent { User = user2, Event = @event });
                    _queue.Update(newPriority, new UserEvent { User = user2, Event = @event });
                }*/
            }
        }

        protected override void PrintQueue()
        {
            if (!_conf.PrintOutEachStep)
            {
                return;
            }

            var max = _queue.Max;
            Console.WriteLine("User {0}, Event {1}, Value {2}", (char)(max.User + 97),
                (char)(max.Event + 88), max.Utility);
        }

        public override void Initialize()
        {
            SetNullMembers();

            AllUsers = new List<int>();
            AllEvents = new List<int>();
            _init = false;
            _conf.NumberOfPhantomEvents = 0;

            if (_conf.FeedType == FeedTypeEnum.Example1 || _conf.FeedType == FeedTypeEnum.XlsxFile)
            {
                int numberOfUsers;
                int numberOfEvents;
                _dataFeeder.GetNumberOfUsersAndEvents(out numberOfUsers, out numberOfEvents);
                _conf.NumberOfUsers = numberOfUsers;
                _conf.NumberOfEvents = numberOfEvents;
            }

            for (var i = 0; i < _conf.NumberOfUsers; i++)
            {
                AllUsers.Add(i);
            }

            for (var i = 0; i < _conf.NumberOfEvents; i++)
            {
                AllEvents.Add(i);
            }

            _users = new List<int>();
            _events = new List<int>();
            Assignments = new List<List<int>>();
            _permanentAssignments = new List<List<int>>();
            UserAssignments = new List<int?>();
            _numberOfUserAssignments = new List<int>();
            _eventDeficitContribution = new List<int>();
            SocialWelfare = 0;
            _queue = new FakeHeap/*<double, UserEvent>*/();
            _phantomEvents = new List<int>();
            //_deficit = 0;
            _init = true;
            //_affectedUserEvents = new List<UserEvent>();

            for (var i = 0; i < _conf.NumberOfUsers; i++)
            {
                _users.Add(i);
                UserAssignments.Add(null);
                _numberOfUserAssignments.Add(0);
            }

            for (var i = 0; i < _conf.NumberOfEvents; i++)
            {
                _events.Add(i);
                _eventDeficitContribution.Add(0);
                Assignments.Add(new List<int>());
                _permanentAssignments.Add(new List<int>());
            }

            EventCapacity = _dataFeeder.GenerateCapacity(_users, _events);
            InAffinities = _dataFeeder.GenerateInnateAffinities(_users, _events);
            SocAffinities = _dataFeeder.GenerateSocialAffinities(_users);

            foreach (var u in _users)
            {
                foreach (var e in _events)
                {
                    var gain = 0d;
                    if (InAffinities[u][e] != 0)
                    {
                        gain = (1 - _conf.Alpha) * InAffinities[u][e];
                        //gain = Math.Round(gain, _conf.Percision);
                        var ue = new UserEvent { Event = e, User = u, Utility = gain };
                        _queue.AddOrUpdate(gain, ue);
                    }
                }
            }
        }

        private void SetNullMembers()
        {
            InAffinities = null;
            SocAffinities = null;
            _events = null;
            _users = null;
            AllEvents = null;
            AllUsers = null;
            _numberOfUserAssignments = null;
            _eventDeficitContribution = null;
            Assignments = null;
            UserAssignments = null;
            _deficit = 0;
            EventCapacity = null;
            _queue = null;
            _phantomEvents = null;
            _init = false;
        }

        private void Traceout()
        {
            var assignments = Assignments.SelectMany(x => x).ToList();
            if (assignments.Count != assignments.Distinct().Count())
            {
                foreach (var assignment in Assignments)
                {
                    foreach (var u in assignment)
                    {
                        Trace.Write(u + "-");
                    }
                    Trace.WriteLine("");
                }
                Trace.WriteLine("End");
            }
        }
    }
}
