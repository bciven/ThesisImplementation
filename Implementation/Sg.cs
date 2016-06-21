using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation
{
    public class Sg : Algorithm<List<UserEvent>>
    {
        private readonly SgConf _conf;
        private List<List<double>> _inAffinities;
        private double[,] _socAffinities;
        private List<int> _events;
        private List<int> _users;
        private readonly List<int> _allEvents;
        private readonly List<int> _allUsers;
        private List<List<int>> _assignments;
        private List<int?> _userAssignments;
        private List<Cardinality> _eventCapacity;
        private List<UserPairEvent> _queue;
        private bool _init;
        private IDataFeed _dataFeeder;

        public Sg(SgConf conf)
        {
            _conf = conf;
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
            }
        }

        public override List<UserEvent> Run()
        {
            if (!_init)
                throw new Exception("Not Initialized");

            while (_queue.Any())
            {
                PrintQueue();
                var min = _queue.First();
                var user1 = min.User1;
                var user2 = min.User2;
                var @event = min.Event;
                var maxCapacity = _eventCapacity[@event].Max;

                if (_userAssignments[user1] == null && _userAssignments[user2] == null && _assignments[@event].Count + 1 < maxCapacity

                    && !_assignments[@event].Contains(user1) && !_assignments[@event].Contains(user2))
                {
                    _assignments[@event].Add(user1);
                    _assignments[@event].Add(user2);
                    _userAssignments[user1] = @event;
                    _userAssignments[user2] = @event;
                }
                _queue.RemoveAt(0);

                if (_queue.Count == 0)
                {
                    var phantomEvents = new List<int>();
                    for (int e = 0; e < _assignments.Count; e++)
                    {
                        var assignment = _assignments[e];
                        if (assignment.Count < _eventCapacity[e].Min)
                        {
                            phantomEvents.Add(e);
                        }
                    }

                    var openEvents = _events.Where(x => _assignments[x].Count >= _eventCapacity[x].Min && _assignments[x].Count < _eventCapacity[x].Max);

                    foreach (var phantomEvent in phantomEvents)
                    {
                        List<int> users = _assignments[phantomEvent];

                        foreach (var u1 in users)
                        {
                            foreach (var u2 in users)
                            {
                                foreach (var e in openEvents)
                                {
                                    var gain = 0d;
                                    gain = ((1 - _conf.Alpha) * (_inAffinities[u1][e] + _inAffinities[u2][e])) + (2 * _conf.Alpha * _socAffinities[u1, u2]);
                                    gain = Math.Round(gain, _conf.Percision);
                                    var ue = new UserPairEvent { Event = e, User1 = u1, User2 = u2, Utility = gain };
                                    _queue.Add(ue);
                                }
                            }
                        }
                    }
                }
            }

            return CreateOutput();
        }

        private void PrintQueue()
        {
            if (!_conf.PrintOutEachStep)
            {
                return;
            }

            var max = _queue.First();
            Console.WriteLine("User1 {0}, User2 {1}, Event {2}, Value {3}", (char)(max.User1 + 97), (char)(max.User2 + 97),
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

        private void Print(List<UserEvent> result, double welfare)
        {
            var name = DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ss-fff", CultureInfo.CurrentCulture);
            FileInfo fileInfo = new FileInfo(name + ".xlsx");
            ExcelPackage excel = new ExcelPackage(fileInfo);
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
            assignmentssheet.Cells[result.Count + 3, 1].Value = "Social Welfare";
            assignmentssheet.Cells[result.Count + 3, 2].Value = welfare;

            assignmentssheet.Cells[assignmentssheet.Dimension.Address].AutoFitColumns();
            _conf.Print(excel);
            excel.Save();
        }

        private List<UserEvent> CreateOutput()
        {
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
            Print(result, SocialWelfare);
            return result;
        }

        private double Util(int @event, int user)
        {
            var g = (1 - _conf.Alpha) * _inAffinities[user][@event];

            var s = 0d;
            foreach (var u in _assignments[@event])
            {
                s += _socAffinities[user, u];
            }

            s *= _conf.Alpha;
            g += s;
            s = 0d;

            foreach (var u in _users)
            {
                s += _socAffinities[user, u];
            }
            g += (s * _conf.Alpha * (_eventCapacity[@event].Min - _assignments[@event].Count)) / Math.Max(_users.Count - 1, 1);
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
            _users = new List<int>();
            _events = new List<int>();
            _assignments = new List<List<int>>();
            _userAssignments = new List<int?>();
            SocialWelfare = 0;
            _queue = new List<UserPairEvent>(_conf.NumberOfUsers * _conf.NumberOfUsers * _conf.NumberOfEvents);
            _init = true;

            for (var i = 0; i < _conf.NumberOfUsers; i++)
            {
                _users.Add(i);
                _userAssignments.Add(null);
            }

            for (var i = 0; i < _conf.NumberOfEvents; i++)
            {
                _events.Add(i);
                _assignments.Add(new List<int>());
            }

            _eventCapacity = _dataFeeder.GenerateCapacity(_users, _events);
            _inAffinities = _dataFeeder.GenerateInnateAffinities(_users, _events);
            _socAffinities = _dataFeeder.GenerateSocialAffinities(_users);

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
                        var gain = 0d;
                        gain = ((1 - _conf.Alpha) * (_inAffinities[u1][e] + _inAffinities[u2][e])) + (2 * _conf.Alpha * _socAffinities[u1, u2]);
                        gain = Math.Round(gain, _conf.Percision);
                        var ue = new UserPairEvent { Event = e, User1 = u1, User2 = u2, Utility = gain };
                        _queue.Add(ue);
                    }
                }
            }
            _queue = _queue.OrderByDescending(x => x.Utility).ToList();
        }
    }
}
