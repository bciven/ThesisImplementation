using System;
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

        protected List<int> AllEvents;
        protected List<int> AllUsers;
        protected List<Cardinality> EventCapacity;
        protected List<List<int>> Assignments;
        protected List<int?> UserAssignments;
        protected List<List<double>> InAffinities;
        protected double[,] SocAffinities;
        protected SgConf Conf;
        protected Stopwatch _watch;
        protected readonly int _index;

        protected Algorithm(int index)
        {
            _index = index;
            _watch = new Stopwatch();
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

        private void PrintToExcel(List<UserEvent> result, Welfare welfare, FileInfo output)
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

                    usereventsheet.Cells[user + 2, @event + 2].Value = InAffinities[user][@event];
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

                    socialaffinitiessheet.Cells[user2 + 2, user1 + 2].Value = SocAffinities[user1, user2];
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
            assignmentssheet.Cells[1, 4].Value = "Total Welfare";
            assignmentssheet.Cells[1, 5].Value = welfare.TotalWelfare;

            assignmentssheet.Cells[2, 4].Value = "Innate Welfare";
            assignmentssheet.Cells[2, 5].Value = welfare.InnateWelfare;

            assignmentssheet.Cells[3, 4].Value = "Social Welfare";
            assignmentssheet.Cells[3, 5].Value = welfare.SocialWelfare;
            assignmentssheet.Cells[assignmentssheet.Dimension.Address].AutoFitColumns();

            var regRatiosheet = excel.Workbook.Worksheets.Add("RegRatios");
            var ratios = CalcRegRatios(AllUsers);
            int index = 1;
            foreach (var ratio in ratios)
            {
                regRatiosheet.Cells[index, 1].Value = ratio.Key;
                regRatiosheet.Cells[index, 2].Value = ratio.Value;
                index++;
            }
            regRatiosheet.Cells[regRatiosheet.Dimension.Address].AutoFitColumns();

            Conf.PrintToExcel(excel, _watch);

            var eventAssignmentsSheet = excel.Workbook.Worksheets.Add("Event Assignments");
            eventAssignmentsSheet.Cells[1, 1].Value = "Event";
            eventAssignmentsSheet.Cells[1, 2].Value = "Min";
            eventAssignmentsSheet.Cells[1, 3].Value = "Count";
            eventAssignmentsSheet.Cells[1, 4].Value = "Max";
            eventAssignmentsSheet.Cells[1, 5].Value = "Users";

            var sum = 0;
            int e = 0;
            for (e = 0; e < Assignments.Count; e++)
            {
                var assignment = Assignments[e];
                eventAssignmentsSheet.Cells[e + 2, 1].Value = e + 1;
                eventAssignmentsSheet.Cells[e + 2, 2].Value = EventCapacity[e].Min;
                eventAssignmentsSheet.Cells[e + 2, 3].Value = assignment.Count;
                sum += assignment.Count;
                eventAssignmentsSheet.Cells[e + 2, 4].Value = EventCapacity[e].Max;
                for (int j = 0; j < assignment.Count; j++)
                {
                    eventAssignmentsSheet.Cells[e + 2, j + 5].Value = assignment[j] + 1;
                }
            }
            eventAssignmentsSheet.Cells[e + 2, 3].Value = sum;
            eventAssignmentsSheet.Cells[eventAssignmentsSheet.Dimension.Address].AutoFitColumns();

            var userGainsSheet = excel.Workbook.Worksheets.Add("User Gains");
            userGainsSheet.Cells[1, 1].Value = "User";
            userGainsSheet.Cells[1, 2].Value = "Event";
            userGainsSheet.Cells[1, 3].Value = "Innate Gain";
            userGainsSheet.Cells[1, 4].Value = "Social Gain";
            userGainsSheet.Cells[1, 5].Value = "Total Gain";

            int u = 0;
            foreach (var user in AllUsers)
            {
                var userWelfare = CalculateSocialWelfare(Assignments, user);
                userGainsSheet.Cells[u + 2, 1].Value = user + 1;
                userGainsSheet.Cells[u + 2, 2].Value = UserAssignments[user];
                userGainsSheet.Cells[u + 2, 3].Value = userWelfare.InnateWelfare;
                userGainsSheet.Cells[u + 2, 4].Value = userWelfare.SocialWelfare;
                userGainsSheet.Cells[u + 2, 5].Value = userWelfare.TotalWelfare;
                u++;
            }
            userGainsSheet.Cells[userGainsSheet.Dimension.Address].AutoFitColumns();

            excel.Save();
        }

        private void PrintToText(List<UserEvent> result, Welfare welfare, FileInfo output)
        {
            var dir = Directory.CreateDirectory(output.FullName);
            var usereventFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.InnateAffinity));
            foreach (var user in AllUsers)
            {
                foreach (var @event in AllEvents)
                {

                    usereventFile.WriteLine("{0},{1},{2}", user + 1, @event + 1, InAffinities[user][@event]);
                }
            }
            usereventFile.Close();

            var socialAffinityFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.SocialAffinity));
            foreach (var user1 in AllUsers)
            {
                foreach (var user2 in AllUsers)
                {
                    socialAffinityFile.WriteLine("{0},{1},{2}", user1 + 1, user2 + 1, SocAffinities[user1, user2]);
                }
            }
            socialAffinityFile.Close();

            var cardinalitiesFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.Cardinality));
            for (int i = 0; i < EventCapacity.Count; i++)
            {
                var cap = EventCapacity[i];
                cardinalitiesFile.WriteLine("{0},{1},{2}", i + 1, cap.Min, cap.Max);
            }
            cardinalitiesFile.Close();

            var assignmentsFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.UserAssignment));
            for (int i = 0; i < result.Count; i++)
            {
                var userEvent = result[i];
                if (userEvent.Event >= 0)
                {
                    assignmentsFile.WriteLine("{0},{1}", userEvent.User + 1, userEvent.Event + 1);
                }
            }
            assignmentsFile.Close();

            var welfareFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.Welfare));
            welfareFile.WriteLine("{0},{1}", "Total Welfare", welfare.TotalWelfare);
            welfareFile.WriteLine("{0},{1}", "Innate Welfare", welfare.InnateWelfare);
            welfareFile.WriteLine("{0},{1}", "Social Welfare", welfare.SocialWelfare);
            welfareFile.Close();


            var regRatiosFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.RegretRatio));
            var ratios = CalcRegRatios(AllUsers);
            foreach (var ratio in ratios)
            {
                regRatiosFile.WriteLine("{0},{1}", ratio.Key, ratio.Value);
            }

            regRatiosFile.Close();

            Conf.PrintToText(dir, _watch);

            var eventAssignmentsFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.EventAssignment));
            int e = 0;
            for (e = 0; e < Assignments.Count; e++)
            {
                var assignment = Assignments[e];
                eventAssignmentsFile.Write("{0},{1},{2},{3}", e + 1, EventCapacity[e].Min, assignment.Count, EventCapacity[e].Max);

                for (int j = 0; j < assignment.Count; j++)
                {
                    eventAssignmentsFile.Write(",{0}", assignment[j] + 1);
                }
                eventAssignmentsFile.Write(Environment.NewLine);
            }
            eventAssignmentsFile.Close();

            var userGainFile = new StreamWriter(Path.Combine(dir.FullName, OutputFiles.UserGain));
            foreach (var user in AllUsers)
            {
                var userWelfare = CalculateSocialWelfare(Assignments, user);
                userGainFile.WriteLine("{0},{1},{2},{3},{4}", user + 1, UserAssignments[user], userWelfare.InnateWelfare, userWelfare.SocialWelfare, userWelfare.TotalWelfare);
            }
            userGainFile.Close();
        }

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
                    PrintToExcel(result, Welfare, file);
                    break;
                case OutputTypeEnum.Text:
                    PrintToText(result, Welfare, file);
                    break;
                case OutputTypeEnum.None:
                    break;
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

        protected bool EventIsReal(int @event)
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
