using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;

namespace Implementation.Algorithms
{
    public class CADG : Algorithm<List<UserEvent>>
    {
        private List<int> _events;
        private List<int> _users;
        private List<int> _numberOfUserAssignments;
        private List<int> _eventDeficitContribution;
        private int _deficit = 0;
        private IHeap _queue;
        private List<int> _phantomEvents;
        private bool _init;
        private readonly IDataFeed _dataFeeder;
        private CADGConf _conf => (CADGConf)Conf;

        public CADG(CADGConf conf, IDataFeed dataFeed, int index) : base(index)
        {
            _dataFeeder = dataFeed;
            Conf = conf;
        }

        //protected void WriteQueue(int hitCount, FileInfo output)
        //{
        //    if (hitCount % 10 != 0)
        //    {
        //        return;
        //    }

        //    var list = _queue._sortedSet.OrderByDescending(x => x.Value.Utility).ToList();
        //    if (output != null)
        //    {
        //        var dir = Directory.CreateDirectory(output.DirectoryName + @"\" + Conf.AlgorithmName + "-" + _index);
        //        var path = dir.FullName + @"\" + hitCount + ".csv";
        //        var file = new StreamWriter(path);
        //        foreach (var item in list)
        //        {
        //            file.WriteLine("{0}, {1}, {2}", item.Value.Utility, item.Value.User, item.Value.Event);
        //        }
        //        file.Close();
        //    }
        //}

        public override void Run(FileInfo output)
        {
            if (!_init)
                throw new Exception("Not Initialized");
            int hitcount = 0;

            while (!_queue.IsEmpty())
            {
                hitcount++;
                //WriteQueue(hitcount, output);
                PrintQueue();
                var userEvent = _queue.RemoveMax();
                var user = userEvent.User;
                var @event = userEvent.Event;
                var minCapacity = EventCapacity[@event].Min;
                var maxCapacity = EventCapacity[@event].Max;
                bool assignmentMade = false;
                List<int> affectedEvents = new List<int>();

                //if (IgnorePair(min))
                //{
                //    continue;
                //}

                if (UserAssignments[user] == null && Assignments[@event].Count < maxCapacity && !Assignments[@event].Contains(user))
                {
                    if (_conf.PhantomAware)
                    {
                        if (Assignments[@event].Count == 0)
                        {
                            if ((_conf.DeficitFix && _eventDeficitContribution.Sum(x => x) + minCapacity <= _users.Count) || (_deficit + minCapacity <= _users.Count))
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
                                if (_conf.ReuseDisposedPairs && !DisposeUserEvents.ContainsKey(userEvent.Key))
                                {
                                    DisposeUserEvents.Add(userEvent.Key, userEvent);
                                }

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
                    if (_conf.LazyAdjustment && UserAssignments[user] == null)
                    {
                        UserAssignments[user] = @event;
                    }

                    _numberOfUserAssignments[user]++;
                    assignmentMade = true;

                    if (_conf.SetType == SetType.Fibonacci)
                    {
                        UserAssignments[user] = @event;
                    }

                    if (_users.Contains(user))
                    {
                        _users.Remove(user);
                    }

                    if (Assignments[@event].Count > minCapacity)
                    {
                        UserAssignments[user] = @event;

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
                        RealizePhantomEvent(Assignments, @event, affectedEvents);
                    }

                    if (_conf.LazyAdjustment)
                    {
                        AdjustList(affectedEvents, user, @event, assignmentMade);
                    }
                }
                else if (_conf.ReuseDisposedPairs && !DisposeUserEvents.ContainsKey(userEvent.Key))
                {
                    DisposeUserEvents.Add(userEvent.Key, userEvent);
                }
                //else if(exhaustive && UserAssignments[user] == null && Assignments[@event].Count < maxCapacity && !Assignments[@event].Contains(user))
                //{

                //}

                if (!_conf.LazyAdjustment)
                {
                    AdjustList(affectedEvents, user, @event, assignmentMade);
                }

                if (_queue.IsEmpty())
                {
                    DefaultReassign();
                    Reassign();
                }
            }

            GreedyAssign();
            Assignments = RealizePhantomEvents(Assignments, _numberOfUserAssignments);
            RemovePhantomEvents();
            Assignments = Swap(Assignments);
            Assignments = ReuseDisposedPairs(Assignments);
            UserMultiAssignmentFault(Assignments);
        }

        protected override void RealizePhantomEvent(List<List<int>> assignments, int @event, List<int> affectedEvents)
        {
            _phantomEvents.Remove(@event);
            foreach (var u in assignments[@event])
            {
                //permanently assign all users to real events
                UserAssignments[u] = @event;

                //unassign these users from all other events
                var excludedEvents = _events.Where(x => x != @event && assignments[x].Contains(u));
                foreach (var e in excludedEvents)
                {
                    if (_conf.PhantomAware && !_phantomEvents.Contains(e))
                    {
                        throw new Exception("This event should be phantom!");
                    }
                    assignments[e].Remove(u);
                    _numberOfUserAssignments[u]--;

                    //affected_evts.append(e)  # this line is not in ref paper
                    if (_conf.ImmediateReaction)
                    {
                        affectedEvents.Add(e);
                    }
                }
            }
        }

        private List<UserEvent> userEvents1 = new List<UserEvent>();
        private List<UserEvent> userEvents2 = new List<UserEvent>();
        /*private bool IgnorePair(UserEvent userEvent)
        {
            //assignment is not permanent
            if (_conf.Exhaustive == 0 || Assignments.Count + 1 < EventCapacity[userEvent.Event].Min || _queue.IsEmpty())
            {
                return false;
            }

            if (userEvents2.Exists(x => x.Event == userEvent.Event && x.User == userEvent.User))
            {
                return false;
            }

            var eventWelfares = new Dictionary<int, Welfare>();
            for (int i = 0; i < Assignments.Count; i++)
            {
                if (Assignments[i].Contains(userEvent.User))
                {
                    var welfare = GetUserWelfare(userEvent, Assignments[i]);
                    eventWelfares.Add(i, welfare);
                }
            }
            //if it is not among the best choices out there, just ignore this pair!
            var numberOfImportantEvents = Convert.ToInt32(Math.Ceiling(_conf.Exhaustive * eventWelfares.Count));
            var welfares = eventWelfares.OrderByDescending(x => x.Value.TotalWelfare).Take(numberOfImportantEvents).ToList();
            if (eventWelfares.Count > 0 && !welfares.Exists(x => x.Key == userEvent.Event))
            {
                userEvents1.Add(userEvent);
                return true;
            }
            else
            {
                var ue = userEvents1.FirstOrDefault();
                if (ue != null)
                {
                    AddToQueue(ue.Event, ue.User);
                    userEvents2.Add(ue);
                    userEvents1.RemoveAt(0);
                }
            }

            return false;
        }*/

        private void GreedyAssign()
        {
            if (_conf.Reassignment != AlgorithmSpec.ReassignmentEnum.Greedy)
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
                        AddToQueue(@event, availableUser, true);
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

        protected override void RefillQueue(List<int> realOpenEvents, List<int> availableUsers)
        {
            //foreach (var @event in realOpenEvents)
            //{
            //    foreach (var availableUser in availableUsers)
            //    {
            //        AddToQueue(@event, availableUser);
            //    }
            //}
            InitializeQueue(availableUsers, realOpenEvents);
        }

        protected override void PhantomAware(List<int> availableUsers, List<int> phantomEvents)
        {
            _users.AddRange(availableUsers);
            availableUsers.ForEach(x => _numberOfUserAssignments[x] = 0);
            if (_conf.PhantomAware)
            {
                _deficit = 0;
                phantomEvents.ForEach(x =>
                {
                    if (Assignments[x].Count > 0)
                    {
                        _eventDeficitContribution[x] = 0;
                    }
                });
            }
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
            //CheckValidity();
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
                AddToQueue(ue.Event, ue.User);
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
                if (!_conf.LazyAdjustment)
                {
                    if (!Assignments[@event].Contains(user2) && Assignments[@event].Count < EventCapacity[@event].Max)
                    {
                        AddToQueue(@event, user2);
                    }
                }
                else
                {
                    AddToQueue(@event, user2);
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

        private void AddToQueue(int @event, int user, bool addOnPositiveUtility = false)
        {
            var q = Util(@event, user, _conf.CommunityAware, _conf.CommunityFix, _users);
            if (!addOnPositiveUtility || q.Utility > 0)
            {
                var userEvent = new UserEvent { User = user, Event = @event, Utility = q.Utility };
                _queue.AddOrUpdate(q.Utility, new UserEvent { User = user, Event = @event });
                MaxInterest = Math.Max(userEvent.Utility, MaxInterest);
                UserEventsInit[userEvent.Key].Utility = q.Utility;
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
            DisposeUserEvents = new Dictionary<string, UserEvent>();

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
            UserAssignments = new List<int?>();
            _numberOfUserAssignments = new List<int>();
            _eventDeficitContribution = new List<int>();
            Welfare = new Welfare();
            if (_conf.SetType == SetType.Fibonacci)
            {
                _queue = new FiboHeap();
            }
            else
            {
                _queue = new FakeHeap();
            }

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
            }

            EventCapacity = _dataFeeder.GenerateCapacity(_users, _events);
            InAffinities = _dataFeeder.GenerateInnateAffinities(_users, _events);
            SocAffinities = _dataFeeder.GenerateSocialAffinities(_users);

            InitializeQueue(AllUsers, AllEvents);
        }

        private void InitializeQueue(List<int> users, List<int> events)
        {
            List<UserEvent> userEvents;
            if (_conf.CommunityFix.HasFlag(CommunityFixEnum.PredictiveInitialization))
            {
                userEvents = PredictiveInitialization(InitStrategyEnum.ProbabilisticSort, users, events);
            }
            else
            {
                userEvents = DefaultQueueInitialization(users, events);
            }

            InitializeQueue(userEvents);
        }

        private List<UserEvent> DefaultQueueInitialization(List<int> users, List<int> events)
        {
            var userEvents = new List<UserEvent>();
            UserEventsInit = new Dictionary<string, UserEvent>();
            foreach (var u in users)
            {
                foreach (var e in events)
                {
                    var ue = new UserEvent { Event = e, User = u, Utility = 0d };

                    if (InAffinities[u][e] != 0)
                    {
                        ue.Utility += (1 - _conf.Alpha) * InAffinities[u][e];
                        //gain = Math.Round(gain, _conf.Percision);
                    }

                    if (_conf.CommunityAware && _conf.CommunityFix.HasFlag(CommunityFixEnum.InitializationFix))
                    {
                        var denomDeduction = 1;
                        if (_conf.CommunityFix.HasFlag(CommunityFixEnum.DenomFix))
                        {
                            denomDeduction = 0;
                        }

                        if (users.Count - denomDeduction > 0)
                        {
                            ue.Utility += _conf.Alpha*(EventCapacity[e].Max*
                                          users.Sum(
                                              x => SocAffinities[u, x] + (Conf.Asymmetric ? SocAffinities[x, u] : 0d))/
                                          (users.Count - denomDeduction));
                        }
                    }

                    if (Double.IsNaN(ue.Utility))
                    {
                        throw new Exception("Utility is not a number.");
                    }

                    UserEventsInit.Add(ue.Key, ue);
                    if (ue.Utility != 0)
                    {
                        userEvents.Add(ue);
                    }
                }
            }

            return userEvents;
        }

        private void InitializeQueue(List<UserEvent> userEvents)
        {
            foreach (var ue in userEvents)
            {
                _queue.AddOrUpdate(ue.Utility, ue);
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
