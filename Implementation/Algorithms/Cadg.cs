using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public Cadg(CadgConf conf, IDataFeed dataFeed, int index) : base(index)
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
                hitcount++;
                //WriteQueue(hitcount, output);
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

            _permanentAssignments = Swap(_permanentAssignments);
            Assignments = _permanentAssignments;
        }

        private List<List<int>> Swap(List<List<int>> assignments)
        {
            if (!_conf.Swap)
            {
                return assignments;
            }

            var users = new List<int>();
            for (int i = 0; i < UserAssignments.Count; i++)
            {
                var userAssignment = UserAssignments[i];
                if (userAssignment.HasValue)
                {
                    users.Add(i);
                }
            }

            for (int i = 0; i < users.Count; i++)
            {
                var user1 = users[i];

                for (int j = i + 1; j < users.Count; j++)
                {
                    var user2 = users[j];
                    if (user1 != user2 && UserAssignments[user1] != null && UserAssignments[user2] != null)
                    {
                        var e1 = UserAssignments[user1].Value;
                        var e2 = UserAssignments[user2].Value;
                        var oldWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                        CalculateEventWelfare(assignments, e1, oldWelfare);
                        CalculateEventWelfare(assignments, e2, oldWelfare);

                        assignments[e1].Remove(user1);
                        assignments[e1].Add(user2);

                        assignments[e2].Remove(user2);
                        assignments[e2].Add(user1);
                        UserAssignments[user1] = e2;
                        UserAssignments[user2] = e1;

                        var newWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                        CalculateEventWelfare(assignments, e1, newWelfare);
                        CalculateEventWelfare(assignments, e2, newWelfare);

                        if (newWelfare.TotalWelfare <= oldWelfare.TotalWelfare)
                        {
                            //undo
                            assignments[e1].Remove(user2);
                            assignments[e1].Add(user1);

                            assignments[e2].Remove(user1);
                            assignments[e2].Add(user2);

                            UserAssignments[user1] = e1;
                            UserAssignments[user2] = e2;
                        }
                    }
                }
            }
            return assignments;
        }

        private void GreedyAssign()
        {
            if (_conf.Reassignment != AlgorithmSpec.ReassignmentEnum.Greedy)
                return;

            if (UserAssignments.Any(x => !x.HasValue))
            {
                List<int> availableUsers;
                List<int> realOpenEvents;
                PrepareReassignment(out availableUsers, out realOpenEvents);

                var queue = new FakeHeap(_conf.DoublePriority);
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

        private void DynamicReassign()
        {
            if (_conf.Reassignment != AlgorithmSpec.ReassignmentEnum.Dynamic)
                return;

            for (int i = 0; i < UserAssignments.Count; i++)
            {
                if (UserAssignments[i] != null && !EventIsReal(UserAssignments[i].Value))
                {
                    UserAssignments[i] = null;
                }
            }

            if (UserAssignments.Any(x => !x.HasValue))
            {
                List<int> availableUsers;
                List<int> realOpenEvents;
                PrepareReassignment(out availableUsers, out realOpenEvents);

                foreach (var @event in realOpenEvents)
                {
                    foreach (var availableUser in availableUsers)
                    {
                        var q = Util(@event, availableUser, _conf.CommunityAware, _conf.CommunityFix, _users);
                        _queue.AddOrUpdate(q.Utility, new UserEvent { User = availableUser, Event = @event });
                    }
                }
            }
        }

        private void PrepareReassignment(out List<int> availableUsers, out List<int> realOpenEvents)
        {
            var phantomEvents = AllEvents.Where(x => Assignments[x].Count < EventCapacity[x].Min).ToList();
            realOpenEvents =
                AllEvents.Where(
                    x => EventCapacity[x].Min <= Assignments[x].Count && Assignments[x].Count < EventCapacity[x].Max)
                    .ToList();
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
                    if (_conf.PhantomAware)
                    {
                        _eventDeficitContribution[phantomEvent] = 0;
                    }
                }
            }
            availableUsers = availableUsers.Distinct().OrderBy(x => x).ToList();
            _users.AddRange(availableUsers);
            availableUsers.ForEach(x => _numberOfUserAssignments[x] = 0);
            if (_conf.PhantomAware)
            {
                _deficit = 0;
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
                var newPriority = Util(@event, user2, _conf.CommunityAware, _conf.CommunityFix, _users);
                if (!_conf.LazyAdjustment)
                {
                    if (!Assignments[@event].Contains(user2) && Assignments[@event].Count < EventCapacity[@event].Max)
                    {
                        _queue.AddOrUpdate(newPriority.Utility, new UserEvent { User = user2, Event = @event });
                    }
                }
                else
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
            Welfare = new Welfare();
            _queue = new FakeHeap/*<double, UserEvent>*/(_conf.DoublePriority);
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
                    var ue = new UserEvent { Event = e, User = u, Utility = 0d };
                    if (_conf.DoublePriority)
                    {
                        var friends = _users.Where(x => SocAffinities[u, x] > 0 || SocAffinities[x, u] > 0 || InAffinities[x][e] > 0);
                        var bestFriends = friends.OrderByDescending(x => SocAffinities[u, x] + SocAffinities[x, u] + InAffinities[x][e]).Take(EventCapacity[e].Max - 1).ToList();
                        var worstFriends = friends.OrderBy(x => SocAffinities[u, x] + SocAffinities[x, u] + InAffinities[x][e]).Take(EventCapacity[e].Max - 1).ToList();
                        var bestGain = bestFriends.Sum(x => SocAffinities[u, x]);
                        var worstGain = worstFriends.Sum(x => SocAffinities[u, x]);
                        ue.Priority = (bestGain + worstGain) / 2;
                    }
                    if (InAffinities[u][e] != 0)
                    {
                        ue.Utility += (1 - _conf.Alpha) * InAffinities[u][e];
                        //gain = Math.Round(gain, _conf.Percision);
                    }
                    _queue.AddOrUpdate(ue.Utility, ue);
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
