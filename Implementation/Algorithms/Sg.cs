using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation.Algorithms
{
    public class SG : Algorithm<List<UserEvent>>
    {
        private List<List<double>> _inAffinities;
        private double[,] _socAffinities;
        private List<int> _events;
        private List<int> _users;
        private List<UserPairEvent> _queue;
        private bool _init;
        private readonly IDataFeed _dataFeeder;
        private SGConf _conf => (SGConf)Conf;

        public SG(SGConf conf, IDataFeed dataFeeder, int index): base(index)
        {
            _dataFeeder = dataFeeder;
            Conf = conf;
        }

        public override void Run(FileInfo output)
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
                var maxCapacity = EventCapacity[@event].Max;

                if (UserAssignments[user1] == null && UserAssignments[user2] == null && Assignments[@event].Count + 1 < maxCapacity

                    && !Assignments[@event].Contains(user1) && !Assignments[@event].Contains(user2))
                {
                    Assignments[@event].Add(user1);
                    Assignments[@event].Add(user2);
                    UserAssignments[user1] = @event;
                    UserAssignments[user2] = @event;
                }
                _queue.RemoveAt(0);

                if (_queue.Count == 0)
                {
                    var phantomEvents = new List<int>();
                    for (int e = 0; e < Assignments.Count; e++)
                    {
                        var assignment = Assignments[e];
                        if (assignment.Count < EventCapacity[e].Min)
                        {
                            phantomEvents.Add(e);
                        }
                    }

                    var openEvents = _events.Where(x => Assignments[x].Count >= EventCapacity[x].Min && Assignments[x].Count < EventCapacity[x].Max);

                    foreach (var phantomEvent in phantomEvents)
                    {
                        List<int> users = Assignments[phantomEvent];

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

        private void Print(List<UserEvent> result, double welfare, FileInfo output)
        {
            ExcelPackage excel = new ExcelPackage(output);
            var usereventsheet = excel.Workbook.Worksheets.Add("Innate Affinities");
            usereventsheet.Cells[1, 1].Value = @"User\Event";
            foreach (var @event in AllEvents)
            {
                usereventsheet.Cells[1, @event + 2].Value = @event + 1;
                foreach (var user in AllUsers)
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
            foreach (var user1 in AllUsers)
            {
                socialaffinitiessheet.Cells[1, user1 + 2].Value = user1 + 1;
                foreach (var user2 in AllUsers)
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
            for (int i = 0; i < EventCapacity.Count; i++)
            {
                var cap = EventCapacity[i];
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
            _conf.PrintToExcel(excel, _watch);
            excel.Save();
        }

        private double Util(int @event, int user)
        {
            var g = (1 - _conf.Alpha) * _inAffinities[user][@event];

            var s = 0d;
            foreach (var u in Assignments[@event])
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
            g += (s * _conf.Alpha * (EventCapacity[@event].Min - Assignments[@event].Count)) / Math.Max(_users.Count - 1, 1);
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
