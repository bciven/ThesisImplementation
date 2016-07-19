using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;
using Implementation.Experiment;
using OfficeOpenXml;

namespace Implementation
{
    public class Cadg : Algorithm<List<UserEvent>>
    {
        private CadgConf _conf;
        private List<List<double>> _inAffinities;
        private double[,] _socAffinities;
        private List<int> _events;
        private List<int> _users;
        private List<int> _allEvents;
        private List<int> _allUsers;
        private List<int> _numberOfUserAssignments;
        private List<int> _eventDeficitContribution;
        private List<List<int>> _assignments;
        private List<int?> _userAssignments;
        private int _deficit = 0;
        private List<Cardinality> _eventCapacity;
        private FakeHeap _queue;
        private List<int> _phantomEvents;
        private bool _init;
        private IDataFeed _dataFeeder;

        public Cadg(CadgConf conf)
        {
            _conf = conf;
        }

        private void InitializeFeed()
        {
            switch (_conf.FeedType)
            {
                case FeedTypeEnum.Random:
                    _dataFeeder = new RandomDataFeed();
                    break;
                case FeedTypeEnum.Example1:
                    _dataFeeder = new Example1Feed();
                    break;
                case FeedTypeEnum.XlsxFile:
                    _dataFeeder = new ExcelFileFeed(_conf.InputFilePath);
                    break;
                case FeedTypeEnum.OriginalExperiment:
                    _dataFeeder = new DistDataFeed();
                    break;
                case FeedTypeEnum.SerialExperiment:
                    if (string.IsNullOrEmpty(_conf.InputFilePath))
                    {
                        _dataFeeder = new DistDataFeed();
                    }
                    else
                    {
                        _dataFeeder = new ExcelFileFeed(_conf.InputFilePath);
                    }
                    break;
            }
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
                var minCapacity = _eventCapacity[@event].Min;
                var maxCapacity = _eventCapacity[@event].Max;
                bool assignmentMade = false;
                List<int> affectedEvents = new List<int>();

                if (_userAssignments[user] == null && _assignments[@event].Count < maxCapacity)
                {
                    if (_conf.PhantomAware)
                    {
                        if (_assignments[@event].Count == 0)
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

                    _assignments[@event].Add(user);
                    _numberOfUserAssignments[user]++;
                    assignmentMade = true;
                    if (_users.Contains(user))
                    {
                        _users.Remove(user);
                    }

                    if (_assignments[@event].Count > minCapacity)
                    {
                        _userAssignments[user] = @event;

                        var excludedEvents = _events.Where(x => x != @event && _assignments[x].Contains(user)).ToList();
                        foreach (var e in excludedEvents)
                        {
                            _assignments[e].Remove(user);
                            _numberOfUserAssignments[user]--;
                            if (_conf.ImmediateReaction)
                            {
                                affectedEvents.Add(e);
                            }
                        }
                    }

                    if (_assignments[@event].Count == minCapacity)
                    {
                        _phantomEvents.Remove(@event);
                        foreach (var u in _assignments[@event])
                        {
                            //permanently assign all users to real events
                            _userAssignments[u] = @event;

                            //unassign these users from all other events
                            var excludedEvents = _events.Where(x => x != @event && _assignments[x].Contains(u));
                            foreach (var e in excludedEvents)
                            {
                                if (_conf.PhantomAware && !_phantomEvents.Contains(e))
                                {
                                    Console.WriteLine("This event should be phantom!");
                                }
                                _assignments[e].Remove(u);
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
            }

            if (_conf.Reassign && _phantomEvents.Any() && _userAssignments.Any(x => !x.HasValue))
            {
                var realOpenEvents = _allEvents.Where(x => !_phantomEvents.Contains(x) && _assignments[x].Count < _eventCapacity[x].Max).ToList();
                List<int> availableUsers = new List<int>();
                for (int i = 0; i < _userAssignments.Count; i++)
                {
                    if (_userAssignments[i] == null)
                    {
                        availableUsers.Add(i);
                    }
                }

                foreach (var phantomEvent in _phantomEvents)
                {
                    if (_assignments[phantomEvent].Count > 0)
                    {
                        availableUsers.AddRange(_assignments[phantomEvent]);
                        _assignments[phantomEvent].RemoveAll(x => true);
                    }
                }

                foreach (var @event in realOpenEvents)
                {
                    foreach (var availableUser in availableUsers)
                    {
                        var q = Util(@event, availableUser);
                        _queue.AddOrUpdate(q, new UserEvent { User = availableUser, Event = @event });
                    }
                }
            }
            _conf.NumberOfPhantomEvents = _phantomEvents.Count;
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

            foreach (var u in _allUsers)
            {
                Update(user, u, @event);
            }

            PrintAssignments(assignmentMade);
            CheckValidity();
        }

        private void CheckValidity()
        {
            foreach (var assignment in _assignments)
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
            var numberOfUsers = _assignments[@event].Count;
            for (int i = 0; i < numberOfUsers; i++)
            {
                var userOfOtherEvent = _assignments[@event][i];
                _numberOfUserAssignments[userOfOtherEvent]--;
                if (_numberOfUserAssignments[userOfOtherEvent] == 0)
                {
                    _users.Add(userOfOtherEvent);
                }
                if (_userAssignments[userOfOtherEvent] != null)
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
            _assignments[@event].Clear();

            foreach (var ue in userEvents)
            {
                var newPriority = Util(ue.Event, ue.User);
                _queue.AddOrUpdate(newPriority, ue);
                foreach (var user in _allUsers)
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
                else if (_eventCapacity[@event].Min >= numberOfUsers)
                {
                    _deficit = _deficit - (_eventCapacity[@event].Min - numberOfUsers);
                }
            }
        }

        private void Update(int user1, int user2, int @event)
        {
            if (_socAffinities[user2, user1] > 0 && _userAssignments[user2] == null) /* or a in affected_evts)*/
            {
                //What if this friend is already in that event, should it be aware that his friend is now assigned to this event?
                var newPriority = Util(@event, user2);
                if (_conf.PostInitializationInsert)
                {
                    if (!_assignments[@event].Contains(user2) && _assignments[@event].Count < _eventCapacity[@event].Max)
                    {
                        _queue.AddOrUpdate(newPriority, new UserEvent { User = user2, Event = @event });
                    }
                }
                else
                {
                    _queue.Update(newPriority, new UserEvent { User = user2, Event = @event });
                }
            }
        }

        private void PrintQueue()
        {
            if (!_conf.PrintOutEachStep)
            {
                return;
            }

            var max = _queue.Max;
            Console.WriteLine("User {0}, Event {1}, Value {2}", (char)(max.User + 97),
                (char)(max.Event + 88), max.Utility);
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
            for (int i = 0; i < _assignments.Count; i++)
            {
                Console.WriteLine();
                Console.Write("Event {0}", (char)(i + 88));
                var assignment = _assignments[i];
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
            _queue.Print();
            Console.WriteLine("{0}{0}*****************************", Environment.NewLine);
            Console.ReadLine();
        }

        private void Print(List<UserEvent> result, double welfare, FileInfo output)
        {
            ExcelPackage excel = new ExcelPackage(output);
            var usereventsheet = excel.Workbook.Worksheets.Add("Innate Affinities");
            usereventsheet.Cells[1, 1].Value = @"User\Event";
            foreach (var @event in _allEvents)
            {
                usereventsheet.Cells[1, @event + 2].Value = @event + 1;
                foreach (var user in _allUsers)
                {
                    if (@event == 0)
                    {
                        usereventsheet.Cells[user + 2, 1].Value = user + 1;
                    }

                    usereventsheet.Cells[user + 2, @event + 2].Value = _inAffinities[user][@event];
                }
            }
            usereventsheet.Cells[usereventsheet.Dimension.Address].AutoFitColumns();

            var socialaffinitiessheet = excel.Workbook.Worksheets.Add("Social Affinities");
            socialaffinitiessheet.Cells[1, 1].Value = @"User\User";
            foreach (var user1 in _allUsers)
            {
                socialaffinitiessheet.Cells[1, user1 + 2].Value = user1 + 1;
                foreach (var user2 in _allUsers)
                {
                    if (user1 == 0)
                    {
                        socialaffinitiessheet.Cells[user2 + 2, 1].Value = user2 + 1;
                    }

                    socialaffinitiessheet.Cells[user2 + 2, user1 + 2].Value = _socAffinities[user1, user2];
                }
            }
            socialaffinitiessheet.Cells[socialaffinitiessheet.Dimension.Address].AutoFitColumns();

            var cardinalitiessheet = excel.Workbook.Worksheets.Add("Cardinalities");
            cardinalitiessheet.Cells[1, 1].Value = "Event";
            cardinalitiessheet.Cells[1, 2].Value = "Min";
            cardinalitiessheet.Cells[1, 3].Value = "Max";
            for (int i = 0; i < _eventCapacity.Count; i++)
            {
                var cap = _eventCapacity[i];
                cardinalitiessheet.Cells[i + 2, 1].Value = i + 1;
                cardinalitiessheet.Cells[i + 2, 2].Value = cap.Min;
                cardinalitiessheet.Cells[i + 2, 3].Value = cap.Max;
            }
            cardinalitiessheet.Cells[cardinalitiessheet.Dimension.Address].AutoFitColumns();

            var assignmentssheet = excel.Workbook.Worksheets.Add("Assignments");
            assignmentssheet.Cells[1, 1].Value = "User";
            assignmentssheet.Cells[1, 2].Value = "Event";
            for (int i = 0; i < result.Count; i++)
            {
                var userEvent = result[i];
                assignmentssheet.Cells[i + 2, 1].Value = userEvent.User + 1;
                if (userEvent.Event >= 0)
                {
                    assignmentssheet.Cells[i + 2, 2].Value = userEvent.Event + 1;
                }
            }
            assignmentssheet.Cells[1, 4].Value = "Social Welfare";
            assignmentssheet.Cells[1, 5].Value = welfare;

            assignmentssheet.Cells[assignmentssheet.Dimension.Address].AutoFitColumns();

            var regRatiosheet = excel.Workbook.Worksheets.Add("RegRatios");
            var ratios = CalcRegRatios();
            int index = 1;
            foreach (var ratio in ratios)
            {
                regRatiosheet.Cells[index, 1].Value = ratio.Key;
                regRatiosheet.Cells[index, 2].Value = ratio.Value;
                index++;
            }
            regRatiosheet.Cells[assignmentssheet.Dimension.Address].AutoFitColumns();

            _conf.Print(excel);
            excel.Save();
        }

        public override List<UserEvent> CreateOutput(FileInfo file)
        {
            SetInputFile(file.FullName);
            var result = new List<UserEvent>();
            for (int i = 0; i < _userAssignments.Count; i++)
            {
                var userAssignment = _userAssignments[i];
                result.Add(new UserEvent
                {
                    Event = userAssignment ?? -1,
                    User = i
                });
            }
            SocialWelfare = CalculateSocialWelfare(_assignments);
            Print(result, SocialWelfare, file);
            return result;
        }

        public override void SetInputFile(string file)
        {
            _conf.InputFilePath = file;
        }

        public override string GetInputFile()
        {
            return _conf.InputFilePath;
        }

        public override FeedTypeEnum GetFeedType()
        {
            return _conf.FeedType;
        }

        private double Util(int @event, int user)
        {
            var g = (1 - _conf.Alpha) * _inAffinities[user][@event];

            var s = _assignments[@event].Sum(u => _socAffinities[user, u]);

            s *= _conf.Alpha;
            g += s;

            if (_conf.CommunityAware)
            {
                s = _users.Sum(u => _socAffinities[user, u]);

                g += (s*_conf.Alpha*(_eventCapacity[@event].Min - _assignments[@event].Count))/
                     Math.Max(_users.Count - 1, 1);
            }

            return Math.Round(g, _conf.Percision);
        }

        public double CalculateSocialWelfare(List<List<int>> assignments)
        {
            double u = 0;
            for (int @event = 0; @event < assignments.Count; @event++)
            {
                var assignment = assignments[@event];

                double s1 = 0;
                double s2 = 0;
                foreach (var user1 in assignment)
                {
                    s1 += _inAffinities[user1][@event];
                    foreach (var user2 in assignment)
                    {
                        if (user1 != user2)
                        {
                            s2 += _socAffinities[user1, user2];
                        }
                    }
                }
                s1 *= (1 - _conf.Alpha);
                s2 *= _conf.Alpha;
                u += s1 + s2;
            }
            return u;
        }

        public override void Initialize()
        {
            InitializeFeed();

            _allUsers = new List<int>();
            _allEvents = new List<int>();
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
                _allUsers.Add(i);
            }

            for (var i = 0; i < _conf.NumberOfEvents; i++)
            {
                _allEvents.Add(i);
            }

            _users = new List<int>();
            _events = new List<int>();
            _assignments = new List<List<int>>();
            _userAssignments = new List<int?>();
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
                _userAssignments.Add(null);
                _numberOfUserAssignments.Add(0);
            }

            for (var i = 0; i < _conf.NumberOfEvents; i++)
            {
                _events.Add(i);
                _eventDeficitContribution.Add(0);
                _assignments.Add(new List<int>());
            }

            _eventCapacity = _dataFeeder.GenerateCapacity(_users, _events);
            _inAffinities = _dataFeeder.GenerateInnateAffinities(_users, _events);
            _socAffinities = _dataFeeder.GenerateSocialAffinities(_users);

            foreach (var u in _users)
            {
                foreach (var e in _events)
                {
                    var gain = 0d;
                    if (_inAffinities[u][e] != 0)
                    {
                        gain = (1 - _conf.Alpha) * _inAffinities[u][e];
                        gain = Math.Round(gain, _conf.Percision);
                        var ue = new UserEvent { Event = e, User = u, Utility = gain };
                        _queue.AddOrUpdate(gain, ue);
                    }
                }
            }
        }

        public Dictionary<int, double> CalcRegRatios()
        {
            var ratios = new Dictionary<int, double>();
            foreach (var user in _allUsers)
            {
                var ratio = CalculateRegRatio(user);
                ratios.Add(user, ratio);
            }
            return ratios;
        }

        public double CalculateRegRatio(int user)
        {
            if (!_userAssignments[user].HasValue)
            {
                return 1;
            }

            var finalDenom = double.MinValue;
            foreach (var @event in _allEvents)
            {
                var friendAffinities = new List<double>();
                for (int i = 0; i < _conf.NumberOfUsers; i++)
                {
                    if (_socAffinities[user, i] > 0)
                    {
                        friendAffinities.Add(_socAffinities[user, i]);
                    }
                }
                friendAffinities = friendAffinities.OrderByDescending(x => x).ToList();
                var k = Math.Min(_eventCapacity[@event].Max - 1, friendAffinities.Count);
                var localSocialAffinity = friendAffinities.Take(k).Sum(x => x);
                var denom = (1 - _conf.Alpha) * _inAffinities[user][@event] + _conf.Alpha * localSocialAffinity;
                finalDenom = Math.Max(finalDenom, denom);
            }

            var assignedEvent = _userAssignments[user].Value;
            var users = _assignments[assignedEvent];
            var socialAffinity = users.Sum(x => _socAffinities[user, x]);
            var numerator = (1 - _conf.Alpha) * _inAffinities[user][assignedEvent] + _conf.Alpha * socialAffinity;

            var phi = 1 - (numerator / finalDenom);
            return phi;
        }
    }
}
