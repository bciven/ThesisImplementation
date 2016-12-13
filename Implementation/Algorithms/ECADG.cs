using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;

namespace Implementation.Algorithms
{
    public class ECADG : Algorithm<List<UserEvent>>
    {
        private List<int> _events;
        private List<int> _users;
        private List<int> _numberOfUserAssignments;
        private List<int> _eventDeficitContribution;
        private int _deficit = 0;
        private FakeHeap _queue;
        private List<int> _phantomEvents;
        private bool _init;
        private readonly IDataFeed _dataFeeder;
        private Dictionary<int, List<int>> _userTempAssignments;
        private ECADGConf _conf => (ECADGConf)Conf;
        private Dictionary<string, bool> _allCandidates;

        public ECADG(ECADGConf conf, IDataFeed dataFeed, int index) : base(index)
        {
            _dataFeeder = dataFeed;
            Conf = conf;
        }

        protected void WriteQueue(int hitCount, FileInfo output)
        {
            if (hitCount % 10 != 0)
            {
                return;
            }

            var list = _queue._sortedSet.OrderByDescending(x => x.Value.Utility).ToList();
            if (output != null)
            {
                var dir = Directory.CreateDirectory(output.DirectoryName + @"\" + Conf.AlgorithmName + "-" + _index);
                var path = dir.FullName + @"\" + hitCount + ".csv";
                var file = new StreamWriter(path);
                foreach (var item in list)
                {
                    file.WriteLine("{0}, {1}, {2}", item.Value.Utility, item.Value.User, item.Value.Event);
                }
                file.Close();
            }
        }

        public override void Run(FileInfo output)
        {
            if (!_init)
                throw new Exception("Not Initialized");

            int hitcount = 0;
            while (!_queue.IsEmpty())
            {
                //WriteQueue(hitcount, output);
                PrintQueue();
                var userEvent = _queue.RemoveMax();
                var user = userEvent.User;
                var @event = userEvent.Event;
                var minCapacity = EventCapacity[@event].Min;
                var maxCapacity = EventCapacity[@event].Max;
                bool assignmentMade = false;
                List<int> affectedEvents = new List<int>();

                if (UserAssignments[user] == null /*&& Assignments[@event].Count < maxCapacity*/ && !Assignments[@event].Contains(user))
                {
                    hitcount++;
                    Assignments[@event].Add(user);
                    _userTempAssignments[user].Add(@event);
                }

                AdjustList(affectedEvents, user, @event, assignmentMade);

                if (_queue.IsEmpty())
                {
                    RemovalProcess();
                    hitcount = 0;
                    if (_queue.Count() == 0)
                    {
                        DefaultReassign();
                        Reassign();
                    }
                }
            }

            GreedyAssign();
            RemovePhantomEvents();
            Assignments = Swap(Assignments);
            Assignments = ReuseDisposedPairs(Assignments);
            UserMultiAssignmentFault(Assignments);
        }

        private void RemovalProcess()
        {
            var ordered = _userTempAssignments.OrderBy(x => x.Value.Count).Where(x => x.Value.Count > 1).ToDictionary(x=> x.Key, x=> x.Value);
            var candidates = new List<UserEvent>();
            foreach (var userAssignment in ordered)
            {
                var userEvents = new List<UserEvent>();
                var user = userAssignment.Key;
                foreach (var @event in userAssignment.Value)
                {
                    var welfare = CalculateSocialWelfare(Assignments[@event], user, @event);
                    var numberOfPermanentAssignment = Assignments[@event].Count(x => UserAssignments[x].HasValue);
                    if (UserAssignments[user] == null && EventCapacity[@event].Min < numberOfPermanentAssignment)
                    {
                        userEvents.Add(new UserEvent(user, @event, welfare.TotalWelfare, numberOfPermanentAssignment));
                    }
                }

                var numberToRemove = (int)Math.Max(Math.Ceiling(userEvents.Count / 2d), 1);
                var candidatesToRemove = userEvents.OrderBy(x => (0.7 * x.Utility) + 0.3 * (x.Priority - EventCapacity[x.Event].Min)).Take(numberToRemove).ToList();
                candidates.AddRange(candidatesToRemove);
            }

            foreach (var userEvent in candidates)
            {
                _allCandidates[userEvent.Key] = true;
            }

            foreach (var candidate in candidates)
            {
                Assignments[candidate.Event].Remove(candidate.User);
                _userTempAssignments[candidate.User].Remove(candidate.Event);
            }

            var determinedUsers = _userTempAssignments.Where(x => x.Value.Count == 1);

            foreach (var determinedUser in determinedUsers)
            {
                UserAssignments[determinedUser.Key] = determinedUser.Value[0];
            }

            foreach (var @event in _events)
            {
                foreach (var user in _users)
                {
                    var ue = new UserEvent(user, @event);
                    if (UserAssignments[user] == null && !_userTempAssignments[user].Contains(@event)
                        && Assignments[@event].Count < EventCapacity[@event].Max && !_allCandidates[ue.Key])
                    {
                        ue.Utility = Util(@event, user, _conf.CommunityAware, _conf.CommunityFix, _users).Utility;
                        _queue.AddOrUpdate(ue.Utility, ue);
                    }
                }
            }
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
                        var q = Util(@event, availableUser, _conf.CommunityAware, _conf.CommunityFix, _users);
                        if (q.Utility > 0)
                        {
                            queue.AddOrUpdate(q.Utility, new UserEvent { User = availableUser, Event = @event });
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

        protected override void RefillQueue(List<int> realOpenEvents, List<int> availableUsers)
        {
            foreach (var @event in realOpenEvents)
            {
                foreach (var availableUser in availableUsers)
                {
                    AddToQueue(@event, availableUser);
                }
            }
        }

        private void AddToQueue(int @event, int user)
        {
            var q = Util(@event, user, _conf.CommunityAware, _conf.CommunityFix, _users);
            _queue.AddOrUpdate(q.Utility, new UserEvent { User = user, Event = @event });
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
                var newPriority = Util(ue.Event, ue.User, _conf.CommunityAware, _conf.CommunityFix, _users);
                _queue.AddOrUpdate(newPriority.Utility, ue);
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

                _eventDeficitContribution[@event] = 0;
            }
        }

        private void Update(int user1, int user2, int @event)
        {
            if (SocAffinities[user1, user2] > 0 && UserAssignments[user2] == null) /* or a in affected_evts)*/
            {
                //What if this friend is already in that event, should it be aware that his friend is now assigned to this event?
                var newPriority = Util(@event, user2, _conf.CommunityAware, _conf.CommunityFix, _users);
                if (!Assignments[@event].Contains(user2) && Assignments[@event].Count < EventCapacity[@event].Max)
                {
                    _queue.AddOrUpdate(newPriority.Utility, new UserEvent { User = user2, Event = @event });
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
            DisposeUserEvents = new Dictionary<string, UserEvent>();
            _userTempAssignments = new Dictionary<int, List<int>>();

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
            _queue = new FakeHeap();
            _phantomEvents = new List<int>();
            _allCandidates = new Dictionary<string, bool>();
            //_deficit = 0;
            _init = true;
            //_affectedUserEvents = new List<UserEvent>();

            for (var i = 0; i < _conf.NumberOfUsers; i++)
            {
                _users.Add(i);
                UserAssignments.Add(null);
                _numberOfUserAssignments.Add(0);
                _userTempAssignments.Add(i, new List<int>());
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

            foreach (var u in _users)
            {
                foreach (var e in _events)
                {
                    var ue = new UserEvent { Event = e, User = u, Utility = 0d };
                    _allCandidates.Add(ue.Key, false);
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
                        ue.Utility += _conf.Alpha * EventCapacity[e].Max * _users.Sum(x => SocAffinities[u, x] + (Conf.Asymmetric ? SocAffinities[x, u] : 0d)) / (_users.Count - denomDeduction);
                    }

                    if (ue.Utility != 0)
                    {
                        _queue.AddOrUpdate(ue.Utility, ue);
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
            _userTempAssignments = null;
            _allCandidates = null;
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
