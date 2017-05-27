using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation.Algorithms
{
    public class POSPG : Algorithm<List<UserEvent>>
    {
        private List<int> _events;
        private List<int> _users;
        private List<UserPairEvent> _queue;
        private bool _init;
        private double _Q;
        private readonly IDataFeed _dataFeeder;
        private SGConf _conf => (SGConf)Conf;

        public POSPG(SGConf conf, IDataFeed dataFeeder, int index) : base(index)
        {
            _dataFeeder = dataFeeder;
            Conf = conf;
        }

        public override void Run(FileInfo output)
        {
            if (!_init)
                throw new Exception("Not Initialized");
            Conf.PopOperationCount = 0;
            Conf.EvenSwitchRoundCount = 0;
            _watches._assignmentWatch.Start();

            while (_queue.Any())
            {
                PrintQueue();
                Conf.PopOperationCount++;
                var bestPair = _queue.First();
                _queue.RemoveAt(0);
                int user1 = bestPair.User1;
                int? user2 = bestPair.User2;
                var @event = bestPair.Event;
                var maxCapacity = EventCapacity[@event].Max;

                if (bestPair.User2.HasValue)
                {
                    if (UserAssignments[user1] == null && UserAssignments[user2.Value] == null && Assignments[@event].Count + 1 < maxCapacity
                        && !Assignments[@event].Contains(user1) && !Assignments[@event].Contains(user2.Value))
                    {
                        Assignments[@event].Add(user1);
                        Assignments[@event].Add(user2.Value);

                        UserAssignments[user1] = @event;
                        UserAssignments[user2.Value] = @event;
                    }
                }
                else if (UserAssignments[user1] == null && Assignments[@event].Count < maxCapacity && !Assignments[@event].Contains(user1))
                {
                    Assignments[@event].Add(user1);
                    UserAssignments[user1] = @event;
                }

                if (_queue.Count == 0)
                {
                    Reinitialize();
                }
            }
            _watches._assignmentWatch.Stop();
        }

        private void Reinitialize()
        {
            var availableUsers = new List<int>();
            for (int e = 0; e < Assignments.Count; e++)
            {
                var assignment = Assignments[e];
                if (assignment.Count < EventCapacity[e].Min)
                {
                    availableUsers.AddRange(Assignments[e]);
                    Assignments[e].RemoveAll(x => true);
                }
            }

            var openEvents = _events.Where(x => Assignments[x].Count >= EventCapacity[x].Min && Assignments[x].Count < EventCapacity[x].Max);
            availableUsers = availableUsers.Distinct().ToList();

            foreach (var u1 in availableUsers)
            {
                foreach (var u2 in availableUsers)
                {
                    foreach (var e in openEvents)
                    {
                        if (u1 != u2)
                        {
                            if (Assignments[e].Count + 1 < EventCapacity[e].Max)
                            {
                                var ue = new UserPairEvent();
                                ue.User1 = u1;
                                ue.User2 = u2;
                                ue.Utility = CalculatePOUserEventPairGain(u1, u2, e);
                                _queue.Add(ue);
                            }
                        }
                    }
                }
            }

            foreach (var u in availableUsers)
            {
                foreach (var e in openEvents)
                {
                    if (Assignments[e].Count + 1 == EventCapacity[e].Max)
                    {
                        var ue = new UserPairEvent();
                        ue.User1 = u;
                        ue.User2 = null;
                        ue.Utility = CalculateUserEventPair(u, e);
                        _queue.Add(ue);
                    }
                }
            }
        }

        protected override void PrintQueue()
        {
            if (!_conf.PrintOutEachStep)
            {
                return;
            }

            var max = _queue.First();
            Console.WriteLine("User1 {0}, User2 {1}, Event {2}, Value {3}", (char)(max.User1 + 97), (char)(max.User2 + 97),
                (char)(max.Event + 88), max.Utility);
        }

        protected override void RefillQueue(List<int> realOpenEvents, List<int> availableUsers)
        {
            throw new NotImplementedException();
        }

        protected override void PhantomAware(List<int> availableUsers, List<int> phantomEvents)
        {
            throw new NotImplementedException();
        }

        private void PrintAssignments(bool assignmentMade)
        {
            if (!_conf.PrintOutEachStep)
            {
                return;
            }

            if (!assignmentMade)
            {
                Console.WriteLine("No assignment made.");
            }
            for (int i = 0; i < Assignments.Count; i++)
            {
                Console.WriteLine();
                Console.Write("Event {0}", (char)(i + 88));
                var assignment = Assignments[i];
                if (assignment.Count == 0)
                {
                    Console.Write(" is empty.");
                    continue;
                }
                Console.Write(" contains ");
                foreach (var user in assignment)
                {
                    Console.Write("{0}  ", (char)(user + 97));
                }
            }
            PrintList();
            Console.WriteLine("{0}{0}*****************************", Environment.NewLine);
            Console.ReadLine();
        }

        private void PrintList()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("User1|User2|Event|Value");
            Console.WriteLine("-----------------------");
            foreach (var value in _queue.OrderByDescending(x => x.Utility))
            {
                Console.WriteLine("{0,-4}|{1,-4}|{2,-5}|{3,-5}", (char)(value.User1 + 97), (char)(value.User2 + 97), (char)(value.Event + 88), value.Utility);
            }
        }


        protected override void RealizePhantomEvent(List<List<int>> assignments, int @event, List<int> affectedEvents)
        {
            throw new NotImplementedException();
        }

        public override void Initialize()
        {
            AllUsers = new List<int>();
            AllEvents = new List<int>();
            _init = false;
            Conf.EvenSwitchRoundCount = 0;
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
            ExtrovertIndeces = new List<double>();
            Welfare = new Welfare();
            _queue = new List<UserPairEvent>(_conf.NumberOfUsers * _conf.NumberOfUsers * _conf.NumberOfEvents);
            _init = true;

            for (var i = 0; i < _conf.NumberOfUsers; i++)
            {
                _users.Add(i);
                UserAssignments.Add(null);
            }

            for (var i = 0; i < _conf.NumberOfEvents; i++)
            {
                _events.Add(i);
                Assignments.Add(new List<int>());
            }

            EventCapacity = _dataFeeder.GenerateCapacity(_users, _events);
            InAffinities = _dataFeeder.GenerateInnateAffinities(_users, _events);
            SocAffinities = _dataFeeder.GenerateSocialAffinities(_users);
            ExtrovertIndeces = _dataFeeder.GenerateExtrovertIndeces(_users, SocAffinities);

            foreach (var u1 in _users)
            {
                foreach (var u2 in _users)
                {
                    if (u1 == u2)
                    {
                        continue;
                    }
                    foreach (var e in _events)
                    {
                        var gain = CalculatePOUserEventPairGain(u1, u2, e);
                        var ue = new UserPairEvent { Event = e, User1 = u1, User2 = u2, Utility = gain };
                        if (ue.Utility > 0)
                        {
                            _queue.Add(ue);
                        }
                    }
                }
            }
            _queue = _queue.OrderByDescending(x => x.Utility).ToList();
        }

        private double CalculateUserEventPair(int user, int e)
        {
            var assignments = new List<int>();
            assignments.AddRange(Assignments[e]);
            assignments.Add(user);
            return POGainFunciton(assignments, e);
        }


    }
}
