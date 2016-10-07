﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation.Algorithms
{
    public abstract class Algorithm<T>
    {
        public abstract void Run(FileInfo output);
        public abstract void Initialize();
        //public abstract T CreateOutput(FileInfo file);
        //public abstract string GetInputFile();
        //public abstract FeedTypeEnum GetFeedType();
        public Welfare Welfare { get; set; }

        public List<int> AllEvents;
        public List<int> AllUsers;
        public List<Cardinality> EventCapacity;
        public List<List<int>> Assignments;
        public List<int?> UserAssignments;
        public List<List<double>> InAffinities;
        public double[,] SocAffinities;
        public SgConf Conf;
        public Stopwatch _watch;
        protected readonly int _index;
        private readonly ReassignmentStrategy<T> _reassignmentStrategy;
        private readonly PrintOutput<T> _printOutput;

        protected Algorithm(int index)
        {
            _index = index;
            _watch = new Stopwatch();
            _reassignmentStrategy = new ReassignmentStrategy<T>(this);
            _printOutput = new PrintOutput<T>(this);
        }

        public Stopwatch Execute(FileInfo output)
        {
            _watch.Reset();
            _watch.Start();
            Run(output);
            _watch.Stop();
            return _watch;
        }

        protected void PrintAssignments(bool assignmentMade)
        {
            if (!Conf.PrintOutEachStep)
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
            PrintQueue();
            Console.WriteLine("{0}{0}*****************************", Environment.NewLine);
            Console.ReadLine();
        }

        protected abstract void PrintQueue();

        public List<UserEvent> CreateOutput(FileInfo file)
        {
            SetInputFile(file);
            var result = new List<UserEvent>();
            for (int i = 0; i < UserAssignments.Count; i++)
            {
                var userAssignment = UserAssignments[i];
                result.Add(new UserEvent
                {
                    Event = userAssignment ?? -1,
                    User = i
                });
            }
            Welfare = CalculateSocialWelfare(Assignments);
            Print(result, Welfare, file);
            return result;
        }

        private void Print(List<UserEvent> result, Welfare welfare, FileInfo file)
        {
            switch (Conf.OutputType)
            {
                case OutputTypeEnum.Excel:
                    _printOutput.PrintToExcel(result, Welfare, file);
                    break;
                case OutputTypeEnum.Text:
                    _printOutput.PrintToText(result, Welfare, file);
                    break;
                case OutputTypeEnum.None:
                    break;
            }
        }

        protected void KeepPhantomEvents(List<int> availableUsers, List<int> realOpenEvents, AlgorithmSpec.ReassignmentEnum reassignment)
        {
            _reassignmentStrategy.KeepPhantomEvents(availableUsers, realOpenEvents, reassignment);
        }

        protected void KeepPotentialPhantomEvents(List<int> availableUsers, List<int> realOpenEvents)
        {
            var phantomEvents = AllEvents.Where(x => !EventIsReal(x)).Select(x => new UserEvent { Event = x, Utility = 0d }).ToList();
            foreach (var @event in phantomEvents)
            {
                @event.Utility = availableUsers.Sum(user => InAffinities[user][@event.Event]);
            }
            phantomEvents = phantomEvents.OrderByDescending(x => x.Utility).ToList();
            var numberOfAvailableUsers = availableUsers.Count;
            foreach (var phantomEvent in phantomEvents)
            {
                if (EventCapacity[phantomEvent.Event].Min <= numberOfAvailableUsers)
                {
                    realOpenEvents.Add(phantomEvent.Event);
                    numberOfAvailableUsers -= EventCapacity[phantomEvent.Event].Min;
                    if (numberOfAvailableUsers == 0)
                    {
                        break;
                    }
                }
            }
        }

        protected void SetInputFile(FileInfo file)
        {
            if (file != null)
            {
                Conf.InputFilePath = file.FullName;
            }
        }

        public string GetInputFile()
        {
            return Conf.InputFilePath;
        }

        public FeedTypeEnum GetFeedType()
        {
            return Conf.FeedType;
        }

        public Welfare CalculateSocialWelfare(List<List<int>> assignments)
        {
            var welfare = new Welfare
            {
                TotalWelfare = 0d,
                InnateWelfare = 0d,
                SocialWelfare = 0d
            };

            for (int @event = 0; @event < assignments.Count; @event++)
            {
                CalculateEventWelfare(assignments, @event, welfare);
            }

            return welfare;
        }

        protected void CalculateEventWelfare(List<List<int>> assignments, int @event, Welfare welfare)
        {
            double s1 = 0;
            double s2 = 0;
            if (!EventIsReal(@event))
            {
                return;
            }

            var assignment = assignments[@event];

            foreach (var user1 in assignment)
            {
                s1 += InAffinities[user1][@event];
                foreach (var user2 in assignment)
                {
                    if (user1 != user2)
                    {
                        s2 += SocAffinities[user1, user2];
                    }
                }
            }
            s1 = (1 - Conf.Alpha)*s1;
            s2 = Conf.Alpha*s2;
            welfare.InnateWelfare += s1;
            welfare.SocialWelfare += s2;
            welfare.TotalWelfare += s1 + s2;
        }

        public Welfare CalculateSocialWelfare(List<List<int>> assignments, int user)
        {
            var welfare = new Welfare
            {
                TotalWelfare = 0d,
                InnateWelfare = 0d,
                SocialWelfare = 0d
            };

            var userAssignments = assignments.Where(x => x.Contains(user)).ToList();
            if (userAssignments.Count > 1)
            {
                throw new Exception("User assigned to more than one event");
            }

            if (userAssignments.Count == 0)
            {
                return welfare;
            }
            var assignment = userAssignments.First();
            var @event = assignments.IndexOf(assignment);
            if (!EventIsReal(@event))
            {
                return welfare;
            }

            double s1 = InAffinities[user][@event];
            double s2 = 0;

            foreach (var user2 in assignment)
            {
                if (user != user2)
                {
                    s2 += SocAffinities[user, user2];
                }
            }
            s1 = (1 - Conf.Alpha) * s1;
            s2 = Conf.Alpha * s2;
            welfare.InnateWelfare += s1;
            welfare.SocialWelfare += s2;
            welfare.TotalWelfare += s1 + s2;

            return welfare;
        }

        public Dictionary<int, double> CalcRegRatios(List<int> allUsers)
        {
            var ratios = new Dictionary<int, double>();
            foreach (var user in allUsers)
            {
                var ratio = CalculateRegRatio(user);
                ratios.Add(user, ratio);
            }
            return ratios;
        }

        public double CalculateRegRatio(int user)
        {
            if (!UserAssignments[user].HasValue || !EventIsReal(UserAssignments[user].Value))
            {
                return 1;
            }

            var finalDenom = double.MinValue;
            foreach (var @event in AllEvents)
            {
                var friendAffinities = new List<double>();
                for (int i = 0; i < Conf.NumberOfUsers; i++)
                {
                    if (SocAffinities[user, i] > 0)
                    {
                        friendAffinities.Add(SocAffinities[user, i]);
                    }
                }
                friendAffinities = friendAffinities.OrderByDescending(x => x).ToList();
                var k = Math.Min(EventCapacity[@event].Max - 1, friendAffinities.Count);
                var localSocialAffinity = friendAffinities.Take(k).Sum(x => x);
                var denom = (1 - Conf.Alpha) * InAffinities[user][@event] + Conf.Alpha * localSocialAffinity;
                finalDenom = Math.Max(finalDenom, denom);
            }

            var assignedEvent = UserAssignments[user].Value;
            var users = Assignments[assignedEvent];
            var socialAffinity = users.Sum(x => SocAffinities[user, x]);
            var numerator = (1 - Conf.Alpha) * InAffinities[user][assignedEvent] + Conf.Alpha * socialAffinity;

            if (finalDenom == 0)
            {
                return 1;
            }
            var phi = 1 - (numerator / finalDenom);
            return phi;
        }

        public bool EventIsReal(int @event)
        {
            var usersCount = Assignments[@event].Count;
            var min = EventCapacity[@event].Min;
            var max = EventCapacity[@event].Max;
            return usersCount >= min && usersCount <= max;
        }

        protected UserEvent Util(int @event, int user, bool communityAware, CommunityFixEnum communityFix, List<int> users)
        {
            var userevent = new UserEvent
            {
                Event = @event,
                User = user
            };
            var g = (1 - Conf.Alpha) * InAffinities[user][@event];

            var s = Conf.Alpha * Assignments[@event].Sum(u => SocAffinities[user, u]);

            g = g + s;

            if (communityAware)
            {
                //var assignedUsers = Assignments.SelectMany(x => x).ToList();
                //var users = AllUsers.Where(x => !UserAssignments[x].HasValue && !assignedUsers.Contains(x)).ToList();

                if (communityFix == CommunityFixEnum.None)
                {
                    s = Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                        users.Sum(u => SocAffinities[user, u]) / (double)Math.Max(users.Count - 1, 1);
                }
                else if (communityFix == CommunityFixEnum.Version1)
                {
                    var lowInterestedUsers = users.OrderBy(x => SocAffinities[user, x]).Take(EventCapacity[@event].Max).ToList();
                    s = Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                        (lowInterestedUsers.Sum(u => SocAffinities[user, u]) / (double)Math.Max(lowInterestedUsers.Count - 1, 1));

                    //s += Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                    //(users.Sum(u => InAffinities[u][@event]) / (double)Math.Max(users.Count - 1, 1));
                }
                else if (communityFix == CommunityFixEnum.Version2)
                {
                    var lowInterestedUsers = users.OrderBy(x => SocAffinities[user, x]).Take(EventCapacity[@event].Max - Assignments[@event].Count).ToList();
                    s = Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                        (lowInterestedUsers.Sum(u => SocAffinities[user, u]) / (double)Math.Max(lowInterestedUsers.Count - 1, 1));
                }
                else if (communityFix == CommunityFixEnum.Version3)
                {
                    var lowInterestedUsers = users.OrderBy(x => SocAffinities[user, x] + InAffinities[x][@event]).Take(EventCapacity[@event].Max - Assignments[@event].Count).ToList();
                    s = Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                        (lowInterestedUsers.Sum(u => SocAffinities[user, u]) / (double)Math.Max(lowInterestedUsers.Count - 1, 1));
                }
                else if (communityFix == CommunityFixEnum.Version4)
                {
                    var lowInterestedUsers = users.Take(EventCapacity[@event].Max - Assignments[@event].Count).ToList();
                    s = Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                        (lowInterestedUsers.Sum(u => SocAffinities[user, u]) / (double)Math.Max(lowInterestedUsers.Count - 1, 1));
                }

                g = s + g;
                /*var firstNotSecond = usersints.Except(users).ToList();
                var secondNotFirst = users.Except(usersints).ToList();
                if (firstNotSecond.Count != 0 || secondNotFirst.Count != 0)
                {
                    Console.WriteLine("|_user| bigger than |users| is {0}.", firstNotSecond.Count > secondNotFirst.Count);
                }*/
            }
            userevent.Utility = g;

            return userevent;//Math.Round(g, Conf.Percision);
        }
    }
}
