using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation
{
    public class Pcadg : IAlgorithm<List<UserEvent>>
    {
        public bool CalculateAffectedEvents { get; set; }
        public double SocialWelfare { get; set; }

        private readonly FeedTypeEnum _feedType;
        private int _numberOfUsers;
        private int _numberOfEvents;
        private readonly bool _reassign;
        private readonly string _inputFilePath;
        private double _alpha = 0.5;
        private List<List<double>> _inAffinities;
        private double[,] _socAffinities;
        private List<int> _events;
        private List<int> _users;
        private readonly List<int> _eventsReadonly;
        private readonly List<int> _usersReadonly;
        private List<List<int>> _assignments;
        private List<int?> _userAssignments;
        private int _deficit = 0;
        private List<Cardinality> _eventCapacity;
        //private Heap<double, UserEvent> _queue;
        private FakeHeap/*<double, UserEvent>*/ _queue;
        private List<int> _phantomEvents;
        private List<int> _affectedEvents;
        //private List<UserEvent> _affectedUserEvents;
        private bool _init;
        private Dictionary<string, double> _priorities;
        private int _percision = 7;
        private IDataFeed _dataFeeder;
        private bool _printOutEachStep;

        public Pcadg(FeedTypeEnum feedType, int numberOfUsers = 10, int numberOfEvents = 4, bool calculateAffectedEvents = false, bool reassign = false, bool _printOutEachStep = false, string inputFilePath = null)
        {
            _reassign = reassign;
            _inputFilePath = inputFilePath;
            _feedType = feedType;
            InitializeFeed();

            CalculateAffectedEvents = calculateAffectedEvents;
            _usersReadonly = new List<int>();
            _eventsReadonly = new List<int>();
            _numberOfUsers = numberOfUsers;
            _numberOfEvents = numberOfEvents;
            _init = false;

            if (_feedType == FeedTypeEnum.Example1 || _feedType == FeedTypeEnum.XlsxFile)
            {
                _dataFeeder.GetNumberOfUsersAndEvents(out _numberOfUsers, out _numberOfEvents);
            }

            for (var i = 0; i < _numberOfUsers; i++)
            {
                _usersReadonly.Add(i);
            }

            for (var i = 0; i < _numberOfEvents; i++)
            {
                _eventsReadonly.Add(i);
            }
        }

        private void InitializeFeed()
        {
            if (_feedType == FeedTypeEnum.Random)
            {
                _dataFeeder = new RandomDataFeed();
            }
            else if (_feedType == FeedTypeEnum.Example1)
            {
                _dataFeeder = new Example1Feed();
            }
            else if (_feedType == FeedTypeEnum.XlsxFile)
            {
                _dataFeeder = new ExcelFileFeed(_inputFilePath);
            }
        }

        public List<UserEvent> Run()
        {
            if (!_init)
                throw new Exception("Not Initialized");

            while (!_queue.IsEmpty())
            {
                PrintQueue();
                var min = _queue.RemoveMax();
                var user = min.Value.User;
                var @event = min.Value.Event;
                var minCapacity = _eventCapacity[@event].Min;
                var maxCapacity = _eventCapacity[@event].Max;
                _affectedEvents.RemoveAll(x => true);
                bool assignmentMade = false;

                if (_userAssignments[user] == null && _assignments[@event].Count < maxCapacity)
                {
                    if (_assignments[@event].Count == 0)
                    {
                        if (_deficit + minCapacity <= _users.Count)
                        {
                            _deficit = _deficit + minCapacity - 1;
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
                            _deficit--;
                        }
                    }

                    _assignments[@event].Add(user);
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
                            if (_assignments[e].Contains(user))
                            {
                                _assignments[e].Remove(user);
                                if (CalculateAffectedEvents)
                                {
                                    ReaddAffectedUserEvents(e);
                                }
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
                                _assignments[e].Remove(u);
                                //affected_evts.append(e)  # this line is not in ref paper
                                if (CalculateAffectedEvents)
                                {
                                    ReaddAffectedUserEvents(e);
                                }
                            }
                        }
                    }
                }

                var temp = _affectedEvents.Concat(new List<int> { @event });
                foreach (var e in temp)
                {
                    foreach (var u in _users)
                    {
                        Update(user, u, e);
                    }
                }
                PrintAssignments(assignmentMade);
            }

            return CreateOutput();
        }

        private void ReaddAffectedUserEvents(int @event)
        {
            var affectedUserEvents = new List<UserEvent>();
            for (int i = 0; i < _assignments[@event].Count; i++)
            {
                var userOfOtherEvent = _assignments[@event][i];
                _assignments[@event].Remove(userOfOtherEvent);
                var ue = new UserEvent { Event = @event, User = userOfOtherEvent };
                affectedUserEvents.Add(ue);
            }

            foreach (var userEvent in affectedUserEvents)
            {
                var newPriority = Util(userEvent.Event, userEvent.User);
                _queue.Add(newPriority, userEvent);
                foreach (var user in _users)
                {
                    Update(userEvent.User, user, userEvent.Event);
                }
            }
            _affectedEvents.Add(@event);
        }

        private void PrintQueue()
        {
            if (!_printOutEachStep)
            {
                return;
            }
            
            var max = _queue.Max;
            Console.WriteLine("User {0}, Event {1}, Value {2}", (char) (max.Value.User + 97),
                (char) (max.Value.Event + 88), max.Key);
        }

        private void PrintAssignments(bool assignmentMade)
        {
            if (!_printOutEachStep)
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

        private void Update(int user1, int user2, int @event)
        {
            if (_socAffinities[user2, user1] > 0 && _userAssignments[user2] == null) /* or a in affected_evts)*/
            {
                var key = @event + "-" + user2;
                var newPriority = Util(@event, user2);
                var oldPriority = _priorities[key];
                if (newPriority == oldPriority)
                {
                    return;
                }
                _queue.UpdateKey(oldPriority, new UserEvent { User = user2, Event = @event }, newPriority);
                //Console.WriteLine("Old:{0}, New:{1}", oldPriority, newPriority);
                _priorities[key] = newPriority;
            }
        }

        private void Print(List<UserEvent> result, double welfare)
        {
            var name = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);
            if (CalculateAffectedEvents)
            {
                name += "-AffectedEvents";
            }
            FileInfo fileInfo = new FileInfo(name + ".xlsx");
            ExcelPackage excel = new ExcelPackage(fileInfo);
            var usereventsheet = excel.Workbook.Worksheets.Add("Innate Affinities");
            usereventsheet.Cells[1, 1].Value = @"User\Event";
            foreach (var @event in _eventsReadonly)
            {
                usereventsheet.Cells[1, @event + 2].Value = @event + 1;
                foreach (var user in _usersReadonly)
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
            foreach (var user1 in _usersReadonly)
            {
                socialaffinitiessheet.Cells[1, user1 + 2].Value = user1 + 1;
                foreach (var user2 in _usersReadonly)
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
            var g = (1 - _alpha) * _inAffinities[user][@event];

            var s = 0d;
            foreach (var u in _assignments[@event])
            {
                s += _socAffinities[user, u];
            }

            s *= _alpha;
            g += s;
            s = 0d;

            foreach (var u in _users)
            {
                s += _socAffinities[user, u];
            }
            g += (s * _alpha * (_eventCapacity[@event].Min - _assignments[@event].Count)) / Math.Max(_users.Count - 1, 1);
            return Math.Round(g, _percision);
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
                s1 *= (1 - _alpha);
                s2 *= _alpha;
                u += s1 + s2;
            }
            return u;
        }

        public void Initialize()
        {
            _users = new List<int>();
            _events = new List<int>();
            _assignments = new List<List<int>>();
            _userAssignments = new List<int?>();
            _priorities = new Dictionary<string, double>();
            SocialWelfare = 0;
            _queue = new FakeHeap/*<double, UserEvent>*/();
            _phantomEvents = new List<int>();
            _affectedEvents = new List<int>();
            _deficit = 0;
            _init = true;
            //_affectedUserEvents = new List<UserEvent>();

            for (var i = 0; i < _numberOfUsers; i++)
            {
                _users.Add(i);
                _userAssignments.Add(null);
            }

            for (var i = 0; i < _numberOfEvents; i++)
            {
                _events.Add(i);
                _assignments.Add(new List<int>());
            }

            _eventCapacity = _dataFeeder.GenerateCapacity(_users, _events);
            _inAffinities = _dataFeeder.GenerateInnateAffinities(_users, _events);
            _socAffinities = _dataFeeder.GenerateSocialAffinities(_users);

            foreach (var u in _users)
            {
                foreach (var evt in _events)
                {
                    var gain = 0d;
                    if (_inAffinities[u][evt] != 0)
                    {
                        gain = (1 - _alpha) * _inAffinities[u][evt];
                        gain = Math.Round(gain, _percision);
                        var ue = new UserEvent { Event = evt, User = u };
                        _queue.Add(gain, ue);
                    }
                    _priorities.Add(evt + "-" + u, gain);
                }
            }
        }
    }
}
