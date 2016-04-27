using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation
{
    public class Pcadg : IAlgorithm<List<UserEvent>>
    {
        private readonly int _numberOfUsers;
        private readonly int _numberOfEvents;
        private double _alpha = 0.2;
        private List<List<double>> _inAffinities;
        private List<List<double>> _socAffinities;
        private List<int> _events;
        private List<int> _users;
        private readonly List<int> _eventsReadonly;
        private readonly List<int> _usersReadonly;
        private List<List<int>> _assignments;
        private Dictionary<string, double> _priorities;
        private List<int?> _userAssignments;
        private int _deficit = 0;
        private List<Cardinality> _eventCapacity;
        private Heap<double, UserEvent> _queue;
        private List<int> _phantomEvents;
        private List<int> _affectedEvents;
        private readonly Random _rand;

        public Pcadg(int numberOfUsers = 10, int numberOfEvents = 4)
        {
            _rand = new Random();
            _usersReadonly = new List<int>();
            _eventsReadonly = new List<int>();
            _numberOfUsers = numberOfUsers;
            _numberOfEvents = numberOfEvents;

            for (var i = 0; i < _numberOfUsers; i++)
            {
                _usersReadonly.Add(i);
            }

            for (var i = 0; i < _numberOfEvents; i++)
            {
                _eventsReadonly.Add(i);
            }
        }

        public List<UserEvent> Run()
        {
            Initialize();
            while (!_queue.IsEmpty())
            {
                var min = _queue.RemoveMin();
                var user = min.Value.User;
                var @event = min.Value.Event;
                var minCapacity = _eventCapacity[@event].Min;
                var maxCapacity = _eventCapacity[@event].Max;

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
                    if (_users.Contains(user))
                    {
                        _users.Remove(user);
                    }

                    if (_assignments[@event].Count > minCapacity)
                    {
                        _userAssignments[user] = @event;
                        var excludedEvents = _events.Where(x => x != @event && _assignments[x].Contains(user));
                        foreach (var e in excludedEvents)
                        {
                            if (_assignments[e].Contains(user))
                            {
                                _assignments[e].Remove(user);
                            }
                        }
                    }

                    if (_assignments[@event].Count == minCapacity)
                    {
                        _phantomEvents.Remove(@event);
                        foreach (var u in _assignments[@event])
                        {
                            _userAssignments[u] = @event;
                            var excludedEvents = _events.Where(x => x != @event && _assignments[x].Contains(u));

                            foreach (var e in excludedEvents)
                            {
                                _assignments[e].Remove(u);
                                //affected_evts.append(e)  # this line is not in ref paper
                            }
                        }
                    }
                }

                foreach (var u in _users)
                {
                    if (_socAffinities[user][u] > 0 && _userAssignments[u] == null) /* or a in affected_evts)*/
                    {
                        var key = @event + "-" + u;
                        var newPriority = Util(@event, u);
                        var oldPriority = _priorities[key];
                        var ue = new UserEvent {Event = @event, User = u};
                        _queue.UpdateKey(oldPriority, ue, newPriority);
                        _priorities[key] = newPriority;
                    }
                }
            }

            return CreateOutput();
        }

        private void Print(List<UserEvent> result)
        {
            FileInfo fileInfo = new FileInfo(Path.GetRandomFileName() + ".xlsx");
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

                    socialaffinitiessheet.Cells[user2 + 2, user1 + 2].Value = _socAffinities[user1][user2];
                }
            }
            socialaffinitiessheet.Cells[socialaffinitiessheet.Dimension.Address].AutoFitColumns();

            var assignmentssheet = excel.Workbook.Worksheets.Add("Assignments");
            assignmentssheet.Cells[1, 1].Value = "User";
            assignmentssheet.Cells[1, 2].Value = "Event";
            for (int i = 0; i < result.Count; i++)
            {
                var userEvent = result[i];
                assignmentssheet.Cells[i + 2, 1].Value = userEvent.User + 1;
                assignmentssheet.Cells[i + 2, 2].Value = userEvent.Event + 1;
            }
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
                    Event = userAssignment ?? 0,
                    User = i
                });
            }
            Print(result);
            return result;
        }

        private double Util(int @event, int user)
        {
            var g = (1 - _alpha) * _inAffinities[user][@event];
            var s = 0d;
            foreach (var u in _assignments[@event])
            {
                s += _socAffinities[u][user];
            }

            g *= _alpha;
            g += s;
            foreach (var u in _users)
            {
                s += _socAffinities[u][user];
            }
            g += s * _alpha * (_eventCapacity[@event].Min - _assignments[@event].Count) / Math.Max(_users.Count - 1, 1);
            return s * -1;
        }

        public void Initialize()
        {
            _users = new List<int>();
            _events = new List<int>();
            _assignments = new List<List<int>>();
            _userAssignments = new List<int?>();
            _priorities = new Dictionary<string, double>();

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

            _eventCapacity = GenerateCapacity();
            //_queue = new FibonacciHeap<UserEvent>();
            _queue = new Heap<double, UserEvent>();
            _phantomEvents = new List<int>();
            _affectedEvents = new List<int>();
            _inAffinities = GenerateInnateAffinities();
            _socAffinities = GenerateSocialAffinities();

            foreach (var u in _users)
            {
                foreach (var evt in _events)
                {
                    if (_inAffinities[u][evt] > 0)
                    {
                        var gain = -1 * (1 - _alpha) * _inAffinities[u][evt];
                        var ue = new UserEvent { Event = evt, User = u};
                        _queue.Add(gain, ue);
                        _priorities.Add(evt + "-" + u, gain);
                    }
                }
            }
        }

        private List<Cardinality> GenerateCapacity()
        {
            var result = _events.Select(x =>
            {
                var s = GenerateRandom(1, 20);
                var l = GenerateRandom(1, 20);
                var c = new Cardinality
                {
                    Min = s,
                    Max = s + l
                };
                return c;
            }).ToList();

            return result;
        }

        private List<List<double>> GenerateInnateAffinities()
        {
            var usersInterests = new List<List<double>>();
            foreach (var user in _users)
            {
                var userInterests = new List<double>();
                foreach (var @event in _events)
                {
                    var r = GenerateRandom(0d, 1d);
                    r = Math.Round(r, 2);
                    userInterests.Add(r);
                }
                usersInterests.Add(userInterests);
            }
            return usersInterests;
        }

        private List<List<double>> GenerateSocialAffinities()
        {
            var usersInterests = new List<List<double>>();
            foreach (var user1 in _users)
            {
                var userInterests = new List<double>();
                foreach (var user2 in _users)
                {
                    if (user1 != user2)
                    {
                        var r = GenerateRandom(0d, 1d);
                        r = Math.Round(r, 2);
                        userInterests.Add(r);
                    }
                    else
                    {
                        userInterests.Add(0);
                    }
                }
                usersInterests.Add(userInterests);
            }
            return usersInterests;
        }

        private double GenerateRandom(double minimum, double maximum)
        {
            return _rand.NextDouble() * (maximum - minimum) + minimum;
        }


        private int GenerateRandom(int minimum, int maximum)
        {
            return _rand.Next(minimum, maximum + 1);
        }
    }
}
