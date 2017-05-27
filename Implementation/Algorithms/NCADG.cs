using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;

namespace Implementation.Algorithms
{
    public class NCADG : Algorithm<List<UserEvent>>
    {
        private List<int> _events;
        private List<int> _users;
        private List<int> _numberOfUserAssignments;
        private List<int> _eventDeficitContribution;
        private FakeHeap _queue;
        private List<int> _phantomEvents;
        private bool _init;
        private readonly IDataFeed _dataFeeder;
        private ECADGConf _conf => (ECADGConf)Conf;

        public NCADG(ECADGConf conf, IDataFeed dataFeed, int index) : base(index)
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
            _watches._assignmentWatch.Start();

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

                if (UserAssignments[user] == null && Assignments[@event].Count < maxCapacity && !Assignments[@event].Contains(user))
                {
                    /*if (_conf.PhantomAware)
                    {
                        if (Assignments[@event].Count == 0)
                        {
                            if (_eventDeficitContribution.Sum(x => x) + minCapacity <= _users.Count)
                            {
                                _eventDeficitContribution[@event] = minCapacity - 1;
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
                                _eventDeficitContribution[@event]--;
                            }
                        }
                    }*/

                    Assignments[@event].Add(user);
                    _numberOfUserAssignments[user]++;

                    /*if (Assignments[@event].Count > minCapacity)
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
                    }*/

                    /*if (Assignments[@event].Count == minCapacity)
                    {
                        _phantomEvents.Remove(@event);
                        foreach (var u in Assignments[@event])
                        {
                            //permanently assign all users to real events
                            //UserAssignments[u] = @event;
                            //_permanentAssignments[@event].Add(u);

                            //unassign these users from all other events
                            //var excludedEvents = _events.Where(x => x != @event && Assignments[x].Contains(u));
                            //foreach (var e in excludedEvents)
                            //{
                            //    if (_conf.PhantomAware && !_phantomEvents.Contains(e))
                            //    {
                            //        Console.WriteLine("This event should be phantom!");
                            //    }
                            //    Assignments[e].Remove(u);
                            //    _numberOfUserAssignments[u]--;

                            //    //affected_evts.append(e)  # this line is not in ref paper
                            //    if (_conf.ImmediateReaction)
                            //    {
                            //        affectedEvents.Add(e);
                            //    }
                            //}
                        }
                    }*/

                    AdjustList(affectedEvents, user, @event);
                }
                else if (_conf.ReuseDisposedPairs && !DisposeUserEvents.ContainsKey(userEvent.Key))
                {
                    DisposeUserEvents.Add(userEvent.Key, userEvent);
                }
                //else if(exhaustive && UserAssignments[user] == null && Assignments[@event].Count < maxCapacity && !Assignments[@event].Contains(user))
                //{

                //}

                if (_queue.IsEmpty())
                {
                    Assignments = RegulateAssignments(Assignments);
                    DefaultReassign();
                    Reassign();
                }
            }
            _watches._assignmentWatch.Stop();

            GreedyAssign();
            Assignments = Swap(Assignments);
            Assignments = Sweep(Assignments);
            Assignments = UserSubstitution(Assignments);
        }

        private List<List<int>> RegulateAssignments(List<List<int>> assignments)
        {
            var newAssignments = new List<List<int>>();
            for (var i = 0; i < _conf.NumberOfEvents; i++)
            {
                newAssignments.Add(new List<int>());
            }

            foreach (var user in AllUsers)
            {
                if (UserAssignments[user].HasValue)
                {
                    newAssignments[UserAssignments[user].Value].Add(user);
                    continue;
                }

                var eventAssignments = new Dictionary<int, List<int>>();
                for (int i = 0; i < assignments.Count; i++)
                {
                    if (assignments[i].Contains(user))
                    {
                        eventAssignments.Add(i, assignments[i]);
                    }
                }

                if (eventAssignments.Count == 0)
                {
                    continue;
                }

                var pairs = eventAssignments.Select(x => new UserEvent(user, x.Key, GetUserWelfare(new UserEvent(user, x.Key), x.Value).TotalWelfare)).ToList();
                pairs = pairs.OrderByDescending(x => x.Utility).ToList();
                foreach (var userEvent in pairs)
                {
                    if (newAssignments[userEvent.Event].Count < EventCapacity[userEvent.Event].Max)
                    {
                        newAssignments[userEvent.Event].Add(user);
                        UserAssignments[user] = userEvent.Event;
                        break;
                    }
                }
            }
            return newAssignments;
        }

        private List<UserEvent> userEvents1 = new List<UserEvent>();
        private List<UserEvent> userEvents2 = new List<UserEvent>();

        protected Welfare GetUserWelfare(UserEvent userEvent, List<int> assignment)
        {
            var welfare = new Welfare();
            welfare.InnateWelfare += InAffinities[userEvent.User][userEvent.Event];
            foreach (var user2 in assignment)
            {
                if (userEvent.User != user2)
                {
                    welfare.SocialWelfare += SocAffinities[userEvent.User, user2];
                }
            }
            welfare.InnateWelfare = (1 - Conf.Alpha) * welfare.InnateWelfare;
            welfare.SocialWelfare = Conf.Alpha * welfare.SocialWelfare;
            welfare.TotalWelfare += welfare.InnateWelfare + welfare.SocialWelfare;
            return welfare;
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
                    if(ExcludingUserEvents != null && ExcludingUserEvents.Any(x=> x.Event == @event && x.User == availableUser))
                    {
                        continue;
                    }
                    AddToQueue(@event, availableUser);
                }
            }
        }

        private void AddToQueue(int @event, int user)
        {
            var q = Util(@event, user, false, CommunityFixEnum.None, _users);
            _queue.AddOrUpdate(q.Utility, new UserEvent { User = user, Event = @event });
        }

        protected override void PhantomAware(List<int> availableUsers, List<int> phantomEvents)
        {

        }

        protected override void RealizePhantomEvent(List<List<int>> assignments, int @event, List<int> affectedEvents)
        {
            throw new NotImplementedException();
        }

        private void AdjustList(List<int> affectedEvents, int user, int @event)
        {
            foreach (var u in AllUsers)
            {
                Update(user, u, @event);
            }
        }

        private void Update(int user1, int user2, int @event)
        {
            if (SocAffinities[user1, user2] > 0 && UserAssignments[user2] == null) /* or a in affected_evts)*/
            {
                var newPriority = Util(@event, user2, false, CommunityFixEnum.None, _users);
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
            DisposeUserEvents = new Dictionary<string, UserEvent>();
            _init = false;
            _conf.NumberOfPhantomEvents = 0;
            _watches._assignmentWatch.Reset();
            _watches._eventSwitchWatch.Reset();
            _watches._userSubstitueWatch.Reset();

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
            }

            EventCapacity = _dataFeeder.GenerateCapacity(_users, _events);
            InAffinities = _dataFeeder.GenerateInnateAffinities(_users, _events);
            SocAffinities = _dataFeeder.GenerateSocialAffinities(_users);

            foreach (var u in _users)
            {
                foreach (var e in _events)
                {
                    if (InAffinities[u][e] != 0)
                    {
                        var ue = new UserEvent { Event = e, User = u, Utility = 0d };
                        ue.Utility += (1 - _conf.Alpha) * InAffinities[u][e];
                        ue.Utility += _conf.Alpha * EventCapacity[e].Max * _users.Sum(x => SocAffinities[u, x] + (Conf.Asymmetric ? SocAffinities[x, u] : 0d)) / (_users.Count - 1);
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
