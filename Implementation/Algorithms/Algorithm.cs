using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace Implementation.Algorithms
{
    public abstract class Algorithm<T>
    {
        public abstract void Run();
        public abstract void Initialize();
        //public abstract T CreateOutput(FileInfo file);
        //public abstract string GetInputFile();
        //public abstract FeedTypeEnum GetFeedType();
        public double SocialWelfare { get; set; }

        protected List<int> AllEvents;
        protected List<int> AllUsers;
        protected List<Cardinality> EventCapacity;
        protected List<List<int>> Assignments;
        protected List<int?> UserAssignments;
        protected List<List<double>> InAffinities;
        protected double[,] SocAffinities;
        protected SgConf Conf;

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
            assignmentssheet.Cells[1, 4].Value = "Social Welfare";
            assignmentssheet.Cells[1, 5].Value = welfare;
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

            Conf.Print(excel);

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
            excel.Save();
        }

        public List<UserEvent> CreateOutput(FileInfo file)
        {
            SetInputFile(file.FullName);
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
            SocialWelfare = CalculateSocialWelfare(Assignments);
            Print(result, SocialWelfare, file);
            return result;
        }

        protected void SetInputFile(string file)
        {
            Conf.InputFilePath = file;
        }

        public string GetInputFile()
        {
            return Conf.InputFilePath;
        }

        public FeedTypeEnum GetFeedType()
        {
            return Conf.FeedType;
        }

        public double CalculateSocialWelfare(List<List<int>> assignments)
        {
            double u = 0;

            for (int @event = 0; @event < assignments.Count; @event++)
            {
                double s1 = 0;
                double s2 = 0;
                if (!EventIsReal(@event))
                {
                    continue;
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
                s1 *= (1 - Conf.Alpha);
                s2 *= Conf.Alpha;
                u += s1 + s2;
            }

            return u;
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

        protected UserEvent Util(int @event, int user, bool communityAware, bool communityFix, List<int> users)
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

                if (!communityFix)
                {
                    s = Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                        users.Sum(u => SocAffinities[user, u]) / (double)Math.Max(users.Count - 1, 1);
                }
                else
                {
                    s = Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                        (users.Sum(u => SocAffinities[user, u]) / (double)Math.Max(users.Count - 1, 1));

                    //s += Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                        //(users.Sum(u => InAffinities[u][@event]) / (double)Math.Max(users.Count - 1, 1));
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
