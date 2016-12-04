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
        private List<int> _phantomEvents;
        private bool _init;
        private readonly IDataFeed _dataFeeder;
        private ECADGConf _conf => (ECADGConf)Conf;
        private int _maxCapacityEvent;
        private Cardinality _largestCapacity;
        private Queue<UserEvent> _queue;

        public ECADG(ECADGConf conf, IDataFeed dataFeed, int index) : base(index)
        {
            _dataFeeder = dataFeed;
            Conf = conf;
        }

        public override void Run(FileInfo output)
        {
            if (!_init)
                throw new Exception("Not Initialized");

            _maxCapacityEvent = MaxCapacity();
            _largestCapacity = EventCapacity[_maxCapacityEvent];
            FillQueue(_users, _events);

            while (_queue.Count > 0)
            {
                var userEvent = _queue.Dequeue();
                var user = userEvent.User;
                var @event = userEvent.Event;
                var minCapacity = EventCapacity[@event].Min;
                var maxCapacity = EventCapacity[@event].Max;

                if (UserAssignments[user] == null && Assignments[@event].Count < maxCapacity &&
                    !Assignments[@event].Contains(user))
                {
                    Assignments[@event].Add(user);
                    UserAssignments[user] = @event;
                    _numberOfUserAssignments[user]++;
                }

                if (_queue.Count == 0)
                {
                    DefaultReassign();
                    Reassign();
                }
            }

            GreedyAssign();
            Assignments = Swap(Assignments);
            Assignments = ReuseDisposedPairs(Assignments);
        }

        private void FillQueue(List<int> users, List<int> events)
        {
            var userBestFriends = new Dictionary<int, UserFriends>();

            foreach (var user1 in users)
            {
                var userSortedFriends = new SortedDictionary<double, int>();
                foreach (var user2 in users)
                {
                    if (user1 != user2 && SocAffinities[user1, user2] > 0)
                    {
                        userSortedFriends.Add(SocAffinities[user1, user2], user2);
                    }
                }

                var numberOfFriends = userSortedFriends.Count;
                var userEvents = new UserFriends();
                for (int i = 0; i < Math.Min(_largestCapacity.Max - 1, numberOfFriends); i++)
                {
                    var friendKey = userSortedFriends.Keys.Max();
                    var friend = userSortedFriends[friendKey];
                    userEvents.Add(new UserEvent(friend, -1));
                    userEvents.Gain += friendKey;
                    userSortedFriends.Remove(friendKey);
                }
                userEvents.Gain /= userEvents.Count;
                userBestFriends.Add(user1, userEvents);
            }

            //people who are more into their friends has to be put first so that there is a higher chance to end up with their friends

            var tempUserEvents = new List<UserEvent>();
            foreach (var orderedUserFriend in userBestFriends)
            {
                foreach (var @event in events)
                {
                    var mainUser = orderedUserFriend.Key;
                    tempUserEvents.Add(new UserEvent(orderedUserFriend.Key, @event, Util(@event, mainUser)));
                    foreach (var userFriend in orderedUserFriend.Value)
                    {
                        var util = Util(@event, mainUser, userFriend.User, orderedUserFriend.Value);
                        tempUserEvents.Add(new UserEvent(orderedUserFriend.Key, @event, util));
                    }
                }
            }

            var orderedUserFriends = OrderUserFriends(tempUserEvents);
            foreach (var userFriend in orderedUserFriends)
            {
                _queue.Enqueue(userFriend);
            }
        }

        private IOrderedEnumerable<UserEvent> OrderUserFriends(List<UserEvent> userBestFriends)
        {
            return userBestFriends.OrderByDescending(x => x.Utility);
        }

        private int MaxCapacity()
        {
            var max = double.MinValue;
            var maxIndex = 0;

            for (int i = 0; i < EventCapacity.Count; i++)
            {
                if (max < EventCapacity[i].Max)
                {
                    max = EventCapacity[i].Max;
                    maxIndex = i;
                }
            }
            return maxIndex;
        }

        protected override void PrintQueue()
        {
            throw new NotImplementedException();
        }

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

        protected override void RefillQueue(List<int> realOpenEvents, List<int> availableUsers)
        {
            FillQueue(availableUsers, realOpenEvents);
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

        protected override void PhantomAware(List<int> availableUsers, List<int> phantomEvents)
        {

        }

        protected override void RealizePhantomEvent(List<List<int>> assignments, int @event, List<int> affectedEvents)
        {
            throw new NotImplementedException();
        }

        public override void Initialize()
        {
            SetNullMembers();

            AllUsers = new List<int>();
            AllEvents = new List<int>();
            DisposeUserEvents = new Dictionary<string, UserEvent>();
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
            UserAssignments = new List<int?>();
            _numberOfUserAssignments = new List<int>();
            _eventDeficitContribution = new List<int>();
            Welfare = new Welfare();
            _queue = new Queue<UserEvent>();
            _phantomEvents = new List<int>();
            _init = true;

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
            _phantomEvents = null;
            _init = false;
        }
    }
}
