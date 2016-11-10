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
        private List<int> _eventDeficitContribution;
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
            int hitcount = 0;

            while (_queue.Count > 0)
            {
                hitcount++;
                PrintQueue();
                var element = _queue.Dequeue();
                var user = element.User;
                var @event = element.Event;
                var minCapacity = EventCapacity[@event].Min;
                var maxCapacity = EventCapacity[@event].Max;
                bool assignmentMade = false;
                List<int> affectedEvents = new List<int>();

                if (UserAssignments[user] == null && Assignments[@event].Count < maxCapacity)
                {
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

                        var excludedEvents = _events.Where(x => x != @event && Assignments[x].Contains(user)).ToList();
                        foreach (var e in excludedEvents)
                        {
                            Assignments[e].Remove(user);
                            _numberOfUserAssignments[user]--;
                        }
                    }

                    if (Assignments[@event].Count == minCapacity)
                    {
                        foreach (var u in Assignments[@event])
                        {
                            //permanently assign all users to real events
                            UserAssignments[u] = @event;

                            //unassign these users from all other events
                            var excludedEvents = _events.Where(x => x != @event && Assignments[x].Contains(u));
                            foreach (var e in excludedEvents)
                            {
                                Assignments[e].Remove(u);
                                _numberOfUserAssignments[u]--;
                            }
                        }
                    }

                    //AdjustList(affectedEvents, user, @event, assignmentMade);
                }
            }

            Assignments = AllEvents.Select(x => new List<int>()).ToList();
            for (int user = 0; user < UserAssignments.Count; user++)
            {
                var userAssignment = UserAssignments[user];
                if (userAssignment.HasValue && !Assignments[userAssignment.Value].Contains(user))
                {
                    Assignments[userAssignment.Value].Add(user);
                }
            }
            Assignments = Swap(Assignments);
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
            _init = false;

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
                _eventDeficitContribution.Add(0);
                Assignments.Add(new List<int>());
            }

            EventCapacity = _dataFeeder.GenerateCapacity(_users, _events);
            InAffinities = _dataFeeder.GenerateInnateAffinities(_users, _events);
            SocAffinities = _dataFeeder.GenerateSocialAffinities(_users);

            InitializationStrategy();
        }

        private void InitializationStrategy()
        {
            switch (_conf.InitStrategyEnum)
            {
                case InitStrategyEnum.RandomSort:
                    RandomInitialization();
                    break;
                case InitStrategyEnum.PredictiveSort:
                    PredictiveInitialization();
                    break;
            }
        }

        private void RandomInitialization()
        {
            var randomQueue = new List<UserEvent>();
            var rnd = new System.Random();
            _users = _users.OrderBy(item => rnd.Next()).ToList();
            _events = _events.OrderBy(item => rnd.Next()).ToList();

            foreach (var u in _users)
            {
                foreach (var e in _events)
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

        private void PredictiveInitialization()
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
            _eventDeficitContribution = null;
            Assignments = null;
            UserAssignments = null;
            EventCapacity = null;
            _queue = null;
            _init = false;
        }

    }
}
