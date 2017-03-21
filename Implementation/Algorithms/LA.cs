using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;
using LouvainCommunityPL;
using Graph = LouvainCommunityPL.Graph;

namespace Implementation.Algorithms
{
    public class LA : Algorithm<List<UserEvent>>
    {
        private List<int> _events;
        private List<int> _users;
        private List<int> _numberOfUserAssignments;
        private bool _init;
        private readonly IDataFeed _dataFeeder;
        private LAConf _conf => (LAConf)Conf;
        private Queue<UserEvent> _queue;

        public LA(LAConf conf, IDataFeed dataFeed, int index) : base(index)
        {
            _dataFeeder = dataFeed;
            Conf = conf;
        }

        public override void Run(FileInfo output)
        {
            if (!_init)
                throw new Exception("Not Initialized");
            if (Conf.Sweep)
            {
                ExcludingUserEvents = new List<UserEvent>();
                var queueCount = 0;
                while (_queue.Count > 0 && queueCount != _queue.Count)
                {
                    queueCount = _queue.Count;
                    RunAlgorithm();
                    for (int i = 0; i < UserAssignments.Count; i++)
                    {
                        if (!UserAssignments[i].HasValue)
                        {
                            continue;
                        }
                        var @event = UserAssignments[i].Value;
                        if (!ExcludingUserEvents.Any(x => x.Event == @event && x.User == i))
                        {
                            var wf = CalculateSocialWelfare(Assignments[@event], i, @event);
                            ExcludingUserEvents.Add(new UserEvent(i, UserAssignments[i].Value, wf.TotalWelfare));
                        }
                    }

                    Initialize();
                }
                _queue.Clear();
                ExcludingUserEvents = ExcludingUserEvents.OrderByDescending(x => x.Utility).ToList();
                foreach (var userEvent in ExcludingUserEvents)
                {
                    _queue.Enqueue(userEvent);
                }
                RunAlgorithm();
            }
            else
            {
                RunAlgorithm();
            }
        }

        private void RunAlgorithm()
        {
            int hitcount = 0;

            while (_queue.Count > 0)
            {
                hitcount++;
                PrintQueue();
                var userEvent = _queue.Dequeue();
                var user = userEvent.User;
                var @event = userEvent.Event;
                var minCapacity = EventCapacity[@event].Min;
                var maxCapacity = EventCapacity[@event].Max;

                if (UserAssignments[user] == null && Assignments[@event].Count < maxCapacity)
                {
                    Assignments[@event].Add(user);
                    _numberOfUserAssignments[user]++;
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
                        }
                    }

                    if (Assignments[@event].Count == minCapacity)
                    {
                        RealizePhantomEvent(Assignments, @event, null);
                    }

                    //AdjustList(affectedEvents, user, @event, assignmentMade);
                }
                else if (_conf.ReuseDisposedPairs && !DisposeUserEvents.ContainsKey(userEvent.Key))
                {
                    DisposeUserEvents.Add(userEvent.Key, userEvent);
                }

                if (_queue.Count == 0)
                {
                    DefaultReassign();
                    Reassign();
                }
            }

            RemovePhantomEvents();

            Assignments = Swap(Assignments);
            Assignments = Sweep(Assignments);
            Assignments = ReuseDisposedPairs(Assignments);
            Assignments = RealizePhantomEvents(Assignments, _numberOfUserAssignments);
        }

        protected override void RealizePhantomEvent(List<List<int>> assignments, int @event, List<int> affectedEvents)
        {
            foreach (var u in assignments[@event])
            {
                //permanently assign all users to real events
                UserAssignments[u] = @event;

                //unassign these users from all other events
                var excludedEvents = _events.Where(x => x != @event && assignments[x].Contains(u));
                foreach (var e in excludedEvents)
                {
                    assignments[e].Remove(u);
                    _numberOfUserAssignments[u]--;
                    affectedEvents?.Add(e);
                }
            }
        }

        protected override void RefillQueue(List<int> realOpenEvents, List<int> availableUsers)
        {
            InitializationStrategy(availableUsers, realOpenEvents);
        }

        protected override void PhantomAware(List<int> availableUsers, List<int> phantomEvents)
        {
            _users.AddRange(availableUsers);
            availableUsers.ForEach(x => _numberOfUserAssignments[x] = 0);
        }

        private double Util(int @event, int user)
        {
            var g = (1 - _conf.Alpha) * InAffinities[user][@event];

            var s = _conf.Alpha * Assignments[@event].Sum(u => SocAffinities[user, u]);

            g = g + s;

            s = _users.Sum(u => SocAffinities[user, u]) / (double)Math.Max(_users.Count - 1, 1);

            g += s * _conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count);

            return Math.Round(g, _conf.Percision);
        }

        private void AdjustList(List<int> affectedEvents, int user, int @event, bool assignmentMade)
        {
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

        private void Update(int user1, int user2, int @event)
        {
            if (SocAffinities[user2, user1] > 0 && UserAssignments[user2] == null) /* or a in affected_evts)*/
            {
                //What if this friend is already in that event, should it be aware that his friend is now assigned to this event?
                var newPriority = Util(@event, user2);
                _queue.Enqueue(new UserEvent { User = user2, Event = @event, Utility = newPriority });
                //_randomQueue.Update(newPriority, new UserEvent { User = user2, Event = @event });
            }
        }

        protected override void PrintQueue()
        {
            if (!_conf.PrintOutEachStep)
            {
                return;
            }

            var element = _queue.Peek();
            Console.WriteLine("User {0}, Event {1}, Value {2}", (char)(element.User + 97),
                (char)(element.Event + 88), element.Utility);
        }

        public override void Initialize()
        {
            SetNullMembers();

            AllUsers = new List<int>();
            AllEvents = new List<int>();
            DisposeUserEvents = new Dictionary<string, UserEvent>();
            _init = false;

            if ((_conf.FeedType == FeedTypeEnum.Example1 || _conf.FeedType == FeedTypeEnum.XlsxFile) && _conf.NumberOfUsers == 0)
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
            Welfare = new Welfare();
            _queue = new Queue<UserEvent>();
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
                Assignments.Add(new List<int>());
            }

            if (EventCapacity == null || EventCapacity.Count == 0)
            {
                EventCapacity = _dataFeeder.GenerateCapacity(_users, _events);
                InAffinities = _dataFeeder.GenerateInnateAffinities(_users, _events);
                SocAffinities = _dataFeeder.GenerateSocialAffinities(_users);
            }
            
            InitializationStrategy(AllUsers, AllEvents);
        }

        private void InitializationStrategy(List<int> users, List<int> events)
        {
            switch (_conf.InitStrategyEnum)
            {
                case InitStrategyEnum.RandomSort:
                    RandomInitialization(users, events);
                    break;
                case InitStrategyEnum.ExtraversionIndex:
                    {
                        var userEvents = ExtraversionIndexInitialization(users, events);
                        AddtoQueue(userEvents);
                    }
                    break;
                case InitStrategyEnum.EventRanking:
                    {
                        var userEvents = EventRankingInitialization(users, events);
                        AddtoQueue(userEvents);
                    }
                    break;
                default:
                    {
                        var userEvents = PredictiveInitialization(_conf.InitStrategyEnum, users, events);
                        AddtoQueue(userEvents);
                    }
                    break;
            }
        }

        private void RandomInitialization(List<int> users, List<int> events)
        {
            var randomQueue = new List<UserEvent>();
            var rnd = new System.Random();
            _users = users.OrderBy(item => rnd.Next()).ToList();
            _events = events.OrderBy(item => rnd.Next()).ToList();

            foreach (var u in users)
            {
                foreach (var e in events)
                {
                    var ue = new UserEvent { Event = e, User = u, Utility = 0 };
                    randomQueue.Add(ue);
                }
            }

            foreach (var userEvent in randomQueue.OrderBy(item => rnd.Next()))
            {
                _queue.Enqueue(userEvent);
            }
        }

        private void AddtoQueue(List<UserEvent> userEvents2)
        {
            foreach (var userEvent in userEvents2.OrderByDescending(x => x.Utility))
            {
                _queue.Enqueue(userEvent);
            }
        }

        private void CommunityInitialization()
        {
            var communities = DetectCommunities();
            var dictionary = new Dictionary<string, int>(AllUsers.Count * AllEvents.Count);
            foreach (var community in communities)
            {
                var userPreferedEvent = new List<UserEvent>();
                foreach (var user in community.Value)
                {
                    var maxIndex = -1;
                    var max = double.MinValue;
                    foreach (var @event in _events)
                    {
                        if (InAffinities[user][@event] > max)
                        {
                            max = InAffinities[user][@event];
                            maxIndex = @event;
                        }
                    }
                    userPreferedEvent.Add(new UserEvent(user, maxIndex));
                }
                var votes = userPreferedEvent.GroupBy(x => x.Event);
                votes = votes.OrderByDescending(x => x.Count()).ToList();
                foreach (var vote in votes)
                {
                    foreach (var userEvent in vote)
                    {
                        var key = userEvent.User + "-" + userEvent.Event;
                        if (!dictionary.ContainsKey(key))
                        {
                            _queue.Enqueue(new UserEvent(userEvent.User, userEvent.Event));
                            dictionary.Add(key, 1);
                        }
                    }
                }
            }

            foreach (var u in _users)
            {
                foreach (var e in _events)
                {
                    var key = u + "-" + e;
                    if (!dictionary.ContainsKey(key))
                    {
                        _queue.Enqueue(new UserEvent(u, e));
                        dictionary.Add(key, 1);
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
            Assignments = null;
            UserAssignments = null;
            EventCapacity = null;
            _queue = null;
            _init = false;
        }

    }
}
