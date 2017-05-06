using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Implementation.Data_Structures;
using LouvainCommunityPL;
using OfficeOpenXml;
using System.Threading.Tasks;

namespace Implementation.Algorithms
{
    public abstract class Algorithm<T>
    {
        public abstract void Run(FileInfo output);
        public abstract void Initialize();
        public Welfare Welfare { get; set; }
        public List<int> AllEvents;
        public List<int> AllUsers;
        public List<Cardinality> EventCapacity;
        public List<List<int>> Assignments;
        public List<int?> UserAssignments;
        public List<List<double>> InAffinities;
        public double[,] SocAffinities;
        public SGConf Conf;
        public Stopwatch _watch;
        protected readonly int _index;
        private readonly ReassignmentStrategy<T> _reassignmentStrategy;
        private readonly PrintOutput<T> _printOutput;
        protected Dictionary<string, UserEvent> DisposeUserEvents;
        protected double MaxInterest;
        protected Dictionary<string, UserEvent> UserEventsInit;
        protected List<UserEvent> ExcludingUserEvents;

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

        protected void UserAssignmentFault(List<List<int>> assignments)
        {
            var userAssignments = assignments.Where(x => x.Distinct().Count() != x.Count).ToList();
            if (userAssignments.Count > 0)
            {
                throw new Exception("User assigned to more than one event");
            }
        }

        protected void RemovePhantomEvents()
        {
            Assignments = AllEvents.Select(x => new List<int>()).ToList();
            for (int user = 0; user < UserAssignments.Count; user++)
            {
                var userAssignment = UserAssignments[user];
                if (userAssignment.HasValue && !Assignments[userAssignment.Value].Contains(user))
                {
                    Assignments[userAssignment.Value].Add(user);
                }
            }
        }

        protected void UserMultiAssignmentFault(List<List<int>> assignments)
        {
            foreach (var user in AllUsers)
            {
                var userEvents = new List<UserEvent>();
                for (int i = 0; i < assignments.Count; i++)
                {
                    if (EventIsReal(i, assignments[i]) && assignments[i].Contains(user))
                    {
                        userEvents.Add(new UserEvent(user, i));
                    }
                }

                if (userEvents.Count > 1)
                {
                    throw new Exception("User assigned to more than one event");
                }
            }
        }

        protected void UserAssignmentFault(int @event, List<List<int>> assignments)
        {
            var equal = assignments[@event].Distinct().Count() == assignments[@event].Count;
            if (!equal)
            {
                throw new Exception("User assigned to more than one event");
            }
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

        protected void DefaultReassign()
        {
            if (Conf.Reassignment != AlgorithmSpec.ReassignmentEnum.Default)
                return;

            for (int i = 0; i < UserAssignments.Count; i++)
            {
                if (UserAssignments[i] != null && !EventIsReal(UserAssignments[i].Value, Assignments[UserAssignments[i].Value]))
                {
                    UserAssignments[i] = null;
                }
            }

            if (UserAssignments.Any(x => !x.HasValue))
            {
                List<int> availableUsers;
                List<int> realOpenEvents;
                PrepareReassignment(out availableUsers, out realOpenEvents);
                RefillQueue(realOpenEvents, availableUsers);
            }
        }

        protected void Reassign()
        {
            if (Conf.Reassignment == AlgorithmSpec.ReassignmentEnum.None
                || Conf.Reassignment == AlgorithmSpec.ReassignmentEnum.Default
                || Conf.Reassignment == AlgorithmSpec.ReassignmentEnum.Greedy)
                return;

            for (int i = 0; i < UserAssignments.Count; i++)
            {
                if (UserAssignments[i] != null && !EventIsReal(UserAssignments[i].Value, Assignments[UserAssignments[i].Value]))
                {
                    UserAssignments[i] = null;
                }
            }

            if (UserAssignments.All(x => x.HasValue))
            {
                return;
            }

            List<int> realOpenEvents;
            List<int> availableUsers;
            PrepareReassignment(out availableUsers, out realOpenEvents);
            KeepPhantomEvents(availableUsers, realOpenEvents, Conf.Reassignment);
            RefillQueue(realOpenEvents, availableUsers);
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

        protected abstract void RefillQueue(List<int> realOpenEvents, List<int> availableUsers);

        protected void PrepareReassignment(out List<int> availableUsers, out List<int> realOpenEvents)
        {
            var phantomEvents = GetPhantomEvents();
            realOpenEvents =
                AllEvents.Where(
                    x => EventCapacity[x].Min <= Assignments[x].Count && Assignments[x].Count < EventCapacity[x].Max)
                    .ToList();
            availableUsers = GetAvailableUsers();

            foreach (var phantomEvent in phantomEvents)
            {
                if (Assignments[phantomEvent].Count > 0)
                {
                    availableUsers.AddRange(Assignments[phantomEvent]);
                    Assignments[phantomEvent].RemoveAll(x => true);
                }
            }
            availableUsers = availableUsers.Distinct().OrderBy(x => x).ToList();


            PhantomAware(availableUsers, phantomEvents);
        }

        protected List<int> GetPhantomEvents()
        {
            var phantomEvents = AllEvents.Where(x => Assignments[x].Count < EventCapacity[x].Min).ToList();
            return phantomEvents;
        }

        protected List<int> GetRealEvents()
        {
            var realEvents = AllEvents.Where(x => Assignments[x].Count >= EventCapacity[x].Min).ToList();
            return realEvents;
        }

        protected List<int> GetAvailableUsers()
        {
            var availableUsers = new List<int>();
            for (int i = 0; i < UserAssignments.Count; i++)
            {
                if (UserAssignments[i] == null)
                {
                    availableUsers.Add(i);
                }
            }

            return availableUsers;
        }

        protected abstract void PhantomAware(List<int> availableUsers, List<int> phantomEvents);

        protected List<List<int>> Swap(List<List<int>> assignments)
        {
            switch (Conf.Swap)
            {
                case SwapEnum.None:
                    return assignments;
                    break;
                case SwapEnum.Linear:
                    return LinearSwap(assignments);
                    break;
                case SwapEnum.Parallel:
                    return ParallelSwap(assignments);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected List<List<int>> LinearSwap(List<List<int>> assignments)
        {
            var rnd = new Random();
            var users = new List<int>();
            for (int i = 0; i < UserAssignments.Count; i++)
            {
                var userAssignment = UserAssignments[i];
                if (userAssignment.HasValue)
                {
                    users.Add(i);
                }
            }

            var oldSocialWelfare = new Welfare();
            var newSocialWelfare = new Welfare();
            do
            {
                Conf.EvenSwitchRoundCount++;
                oldSocialWelfare = CalculateSocialWelfare(assignments);
                var rndUsers1 = users.OrderBy(item => rnd.Next()).ToList();
                var rndUsers2 = users.OrderBy(item => rnd.Next()).ToList();

                for (int i = 0; i < rndUsers1.Count; i++)
                {
                    var user1 = rndUsers1[i];

                    for (int j = i + 1; j < rndUsers2.Count; j++)
                    {
                        var user2 = rndUsers2[j];
                        TryExchange(assignments, user1, user2);
                    }
                }
                newSocialWelfare = CalculateSocialWelfare(assignments);

            } while (1 - oldSocialWelfare.TotalWelfare / newSocialWelfare.TotalWelfare > Conf.SwapThreshold);

            return assignments;
        }

        //private void TryExchange(List<List<int>> assignments, int user1, int user2)
        //{
        //    if (user1 != user2 && UserAssignments[user1] != null && UserAssignments[user2] != null
        //                                && UserAssignments[user1] != UserAssignments[user2]
        //                                && !assignments[UserAssignments[user1].Value].Contains(user2)
        //                                && !assignments[UserAssignments[user2].Value].Contains(user1)
        //                                && EventIsReal(UserAssignments[user1].Value, assignments[UserAssignments[user1].Value])
        //                                && EventIsReal(UserAssignments[user2].Value, assignments[UserAssignments[user2].Value]))
        //    {
        //        var e1 = UserAssignments[user1].Value;
        //        var e2 = UserAssignments[user2].Value;
        //        var oldWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
        //        CalculateEventWelfare(assignments, e1, oldWelfare);
        //        CalculateEventWelfare(assignments, e2, oldWelfare);

        //        assignments[e1].Remove(user1);
        //        assignments[e1].Add(user2);

        //        assignments[e2].Remove(user2);
        //        assignments[e2].Add(user1);
        //        UserAssignments[user1] = e2;
        //        UserAssignments[user2] = e1;

        //        var newWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
        //        CalculateEventWelfare(assignments, e1, newWelfare);
        //        CalculateEventWelfare(assignments, e2, newWelfare);

        //        if (newWelfare.TotalWelfare <= oldWelfare.TotalWelfare)
        //        {
        //            //undo
        //            assignments[e1].Remove(user2);
        //            assignments[e1].Add(user1);

        //            assignments[e2].Remove(user1);
        //            assignments[e2].Add(user2);

        //            UserAssignments[user1] = e1;
        //            UserAssignments[user2] = e2;
        //        }
        //    }
        //}

        private void TryExchange(List<List<int>> assignments, int user1, int user2)
        {
            if (user1 != user2 && UserAssignments[user1] != null && UserAssignments[user2] != null
                                                && UserAssignments[user1] != UserAssignments[user2]
                                                && !assignments[UserAssignments[user1].Value].Contains(user2)
                                                && !assignments[UserAssignments[user2].Value].Contains(user1)
                                                && assignments[UserAssignments[user1].Value].Count > 0
                                                && assignments[UserAssignments[user2].Value].Count > 0
                                                && EventIsReal(UserAssignments[user1].Value, assignments[UserAssignments[user1].Value])
                                                && EventIsReal(UserAssignments[user2].Value, assignments[UserAssignments[user2].Value]))
            {
                var e1 = UserAssignments[user1].Value;
                var e2 = UserAssignments[user2].Value;
                //var oldWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                //CalculateEventWelfare(assignments, e1, oldWelfare);
                //CalculateEventWelfare(assignments, e2, oldWelfare);

                //assignments[e1].Remove(user1);
                //assignments[e1].Add(user2);

                //assignments[e2].Remove(user2);
                //assignments[e2].Add(user1);
                //UserAssignments[user1] = e2;
                //UserAssignments[user2] = e1;

                //var newWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                //CalculateEventWelfare(assignments, e1, newWelfare);
                //CalculateEventWelfare(assignments, e2, newWelfare);


                //if (newWelfare.TotalWelfare <= oldWelfare.TotalWelfare)
                //{
                //    //undo
                //    assignments[e1].Remove(user2);
                //    assignments[e1].Add(user1);

                //    assignments[e2].Remove(user1);
                //    assignments[e2].Add(user2);

                //    UserAssignments[user1] = e1;
                //    UserAssignments[user2] = e2;
                //}

                var different = (1 - Conf.Alpha) * ((InAffinities[user1][e2] + InAffinities[user2][e1]) - (InAffinities[user1][e1] + InAffinities[user2][e2]));
                double p1 = Sum(assignments, e2, user2, user1);
                double p2 = Sum(assignments, e1, user1, user2);

                double p3 = Sum(assignments, e1, user1, user1);
                double p4 = Sum(assignments, e2, user2, user2);

                different += Conf.Alpha * ((p1 + p2) - (p3 + p4));

                if (different > 0)
                {
                    assignments[e1].Remove(user1);
                    assignments[e1].Add(user2);

                    assignments[e2].Remove(user2);
                    assignments[e2].Add(user1);
                    UserAssignments[user1] = e2;
                    UserAssignments[user2] = e1;
                }
            }
        }

        private double Sum(List<List<int>> assignments, int @event, int excludingUser, int includingUser)
        {
            var assignment = assignments[@event].Where(x => x != excludingUser).ToList();//.Sum(x => SocAffinities[includingUser, x] + SocAffinities[x, includingUser]);
            var sum = 0d;
            for (int i = 0; i < assignment.Count; i++)
            {
                var user = assignment[i];
                sum += SocAffinities[includingUser, user] + SocAffinities[user, includingUser];
            }
            return sum;
        }

        protected struct EventPair
        {
            public int event1;
            public int event2;
            public bool used;
        }

        protected List<List<int>> ParallelSwap(List<List<int>> assignments)
        {
            var users = new List<int>();
            for (int i = 0; i < UserAssignments.Count; i++)
            {
                var userAssignment = UserAssignments[i];
                if (userAssignment.HasValue)
                {
                    users.Add(i);
                }
            }

            var realEvents = new List<int>();
            var usedEvents = new Dictionary<int, bool>();

            for (int i = 0; i < AllEvents.Count; i++)
            {
                if (EventIsReal(i, assignments[i]))
                {
                    realEvents.Add(i);
                    usedEvents.Add(i, false);
                }
            }

            var allPossibleEventPairs = new List<EventPair>();
            for (int i = 0; i < realEvents.Count; i++)
            {
                var e1 = realEvents[i];
                for (int j = i + 1; j < realEvents.Count; j++)
                {
                    var e2 = realEvents[j];
                    allPossibleEventPairs.Add(new EventPair { event1 = e1, event2 = e2 });
                }
            }

            var eventPairBatches = new List<List<EventPair>>();
            var eventPairBatcheContains = new List<List<int>>();
            eventPairBatches.Add(new List<EventPair>());
            eventPairBatcheContains.Add(new List<int>());

            foreach (var eventPair in allPossibleEventPairs)
            {
                for (int batchIndex = 0; batchIndex < eventPairBatches.Count; batchIndex++)
                {
                    if (!eventPairBatcheContains[batchIndex].Exists(x => x == eventPair.event1 || x == eventPair.event2))
                    {
                        eventPairBatches[batchIndex].Add(eventPair);
                        eventPairBatcheContains[batchIndex].Add(eventPair.event1);
                        eventPairBatcheContains[batchIndex].Add(eventPair.event2);
                        break;
                    }
                    else if (batchIndex == eventPairBatches.Count - 1)
                    {
                        eventPairBatches.Add(new List<EventPair>());
                        eventPairBatcheContains.Add(new List<int>());
                    }
                }
            }

            var different = allPossibleEventPairs.RemoveAll(x => eventPairBatches.Any(y => y.Any(z =>
                 (x.event1 == z.event1 && x.event2 == z.event2)
              || (x.event1 == z.event2 && x.event2 == z.event1))));

            var oldSocialWelfare = new Welfare();
            var newSocialWelfare = new Welfare();
            do
            {
                Conf.EvenSwitchRoundCount++;
                oldSocialWelfare = CalculateSocialWelfare(assignments);
                //for (int i = 0; i < users.Count; i++)

                assignments = ExchangeEvents(assignments, eventPairBatches);

                newSocialWelfare = CalculateSocialWelfare(assignments);

            } while (1 - oldSocialWelfare.TotalWelfare / newSocialWelfare.TotalWelfare > Conf.SwapThreshold);

            return assignments;
        }

        private List<List<int>> ExchangeEvents(List<List<int>> assignments, List<List<EventPair>> eventPairsBatches)
        {
            foreach (List<EventPair> eventPairs in eventPairsBatches)
            {

                var batches = new List<List<EventPair>>();
                var pairIndex = 0;
                while (pairIndex < eventPairs.Count)
                {
                    for (int i = 0; i < 8 && pairIndex < eventPairs.Count; i++)
                    {
                        if (batches.ElementAtOrDefault(i) == null)
                        {
                            batches.Add(new List<EventPair>());
                        }
                        batches[i].Add(eventPairs[pairIndex]);
                        pairIndex++;
                    }
                }

                Task[] taskArray = new Task[batches.Count];

                for (int taskIndex = 0; taskIndex < taskArray.Length; taskIndex++)
                {
                    taskArray[taskIndex] = Task.Factory.StartNew((Action<object>)((object obj) =>
                    {
                        var index = Convert.ToInt32(obj);
                        foreach (var eventPair in batches[index])
                        {
                            var users = new List<int>();
                            users.AddRange(assignments[eventPair.event1]);
                            users.AddRange(assignments[eventPair.event2]);

                            foreach (var user1 in users)
                            {
                                foreach (var user2 in users)
                                {
                                    this.TryExchange((List<List<int>>)assignments, (int)user1, (int)user2);
                                }
                            }
                        }
                    }), taskIndex);
                }
                Task.WaitAll(taskArray);
            }
            return assignments;
        }

        protected List<List<int>> Sweep(List<List<int>> assignments)
        {
            //if (!Conf.Sweep)
            //{
            return assignments;
            //}

            var users = new List<int>();
            for (int i = 0; i < UserAssignments.Count; i++)
            {
                var userAssignment = UserAssignments[i];
                if (userAssignment.HasValue)
                {
                    users.Add(i);
                }
            }

            var oldSocialWelfare = new Welfare();
            var newSocialWelfare = new Welfare();
            do
            {
                oldSocialWelfare = CalculateSocialWelfare(assignments);
                for (int i = 0; i < users.Count; i++)
                {
                    var user = users[i];
                    if (UserAssignments[user] == null && EventCapacity[UserAssignments[user].Value].Min < assignments[UserAssignments[user].Value].Count)
                    {
                        continue;
                    }

                    var originEvent = UserAssignments[user].Value;
                    var oldUserSocialWelfare = CalculateSocialWelfare(assignments);
                    assignments[originEvent].Remove(user);
                    var bestChoice = new UserEvent(user, originEvent, oldUserSocialWelfare.TotalWelfare);

                    foreach (var @event in AllEvents)
                    {
                        if (originEvent != @event
                            && EventIsReal(@event, assignments[@event])
                            && assignments[@event].Count < EventCapacity[@event].Max)
                        {
                            assignments[@event].Add(user);
                            UserAssignments[user] = @event;
                            var newUserSocialWelfare = CalculateSocialWelfare(assignments);

                            if (bestChoice.Utility < newUserSocialWelfare.TotalWelfare)
                            {
                                bestChoice.Event = @event;
                                bestChoice.Utility = newUserSocialWelfare.TotalWelfare;
                            }
                            assignments[@event].Remove(user);
                            UserAssignments[user] = @event;
                        }
                    }

                    UserAssignments[user] = bestChoice.Event;
                    if (!assignments[bestChoice.Event].Contains(user))
                    {
                        assignments[bestChoice.Event].Add(user);
                    }
                }
                newSocialWelfare = CalculateSocialWelfare(assignments);

            } while (1 - oldSocialWelfare.TotalWelfare / newSocialWelfare.TotalWelfare > Conf.SwapThreshold);

            return assignments;
        }

        private static void Swap(ref char a, ref char b)
        {
            if (a == b) return;

            a ^= b;
            b ^= a;
            a ^= b;
        }

        public static void GetPer(char[] list)
        {
            int x = list.Length - 1;
            GetPer(list, 0, x);
        }

        private static void GetPer(char[] list, int k, int m)
        {
            if (k == m)
            {
                Console.Write(list);
            }
            else
            {
                for (int i = k; i <= m; i++)
                {
                    Swap(ref list[k], ref list[i]);
                    GetPer(list, k + 1, m);
                    Swap(ref list[k], ref list[i]);
                }
            }
        }

        protected List<List<int>> Permutations(List<List<int>> assignments)
        {
            //makePermutations(permutation) {
            //  if (length permutation < required length) {
            //    for (i = min digit to max digit) {
            //      if (i not in permutation) {
            //        makePermutations(permutation+i)
            //      }
            //    }
            //  }
            //  else {
            //    add permutation to list
            //  }
            //}

            if (Conf.Swap == SwapEnum.None)
            {
                return assignments;
            }

            var users = new List<int>();
            for (int i = 0; i < UserAssignments.Count; i++)
            {
                var userAssignment = UserAssignments[i];
                if (userAssignment.HasValue)
                {
                    users.Add(i);
                }
            }

            var oldSocialWelfare = new Welfare();
            var newSocialWelfare = new Welfare();
            do
            {
                oldSocialWelfare = CalculateSocialWelfare(assignments);
                for (int i = 0; i < users.Count; i++)
                {
                    var user1 = users[i];

                    for (int j = i + 1; j < users.Count; j++)
                    {
                        var user2 = users[j];
                        if (user1 != user2 && UserAssignments[user1] != null && UserAssignments[user2] != null
                            && UserAssignments[user1] != UserAssignments[user2]
                            && !assignments[UserAssignments[user1].Value].Contains(user2)
                            && !assignments[UserAssignments[user2].Value].Contains(user1)
                            && EventIsReal(UserAssignments[user1].Value, assignments[UserAssignments[user1].Value])
                            && EventIsReal(UserAssignments[user2].Value, assignments[UserAssignments[user2].Value]))
                        {
                            var e1 = UserAssignments[user1].Value;
                            var e2 = UserAssignments[user2].Value;
                            var oldWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                            CalculateEventWelfare(assignments, e1, oldWelfare);
                            CalculateEventWelfare(assignments, e2, oldWelfare);

                            assignments[e1].Remove(user1);
                            assignments[e1].Add(user2);

                            assignments[e2].Remove(user2);
                            assignments[e2].Add(user1);
                            UserAssignments[user1] = e2;
                            UserAssignments[user2] = e1;

                            var newWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                            CalculateEventWelfare(assignments, e1, newWelfare);
                            CalculateEventWelfare(assignments, e2, newWelfare);

                            if (newWelfare.TotalWelfare <= oldWelfare.TotalWelfare)
                            {
                                //undo
                                assignments[e1].Remove(user2);
                                assignments[e1].Add(user1);

                                assignments[e2].Remove(user1);
                                assignments[e2].Add(user2);

                                UserAssignments[user1] = e1;
                                UserAssignments[user2] = e2;
                            }
                        }
                    }
                }
                newSocialWelfare = CalculateSocialWelfare(assignments);

            } while (1 - oldSocialWelfare.TotalWelfare / newSocialWelfare.TotalWelfare > Conf.SwapThreshold);

            return assignments;
        }

        protected List<List<int>> ReuseDisposedPairs(List<List<int>> assignments)
        {
            if (!Conf.ReuseDisposedPairs)
            {
                return assignments;
            }

            foreach (var disposeUserEvent in DisposeUserEvents)
            {
                var @event = disposeUserEvent.Value.Event;
                var leftoutUser = disposeUserEvent.Value.User;
                var assignment = assignments[@event];
                if (UserAssignments[leftoutUser] != null || !EventIsReal(@event, assignments[@event]) || assignments[@event].Contains(leftoutUser))
                {
                    continue;
                }
                List<UserEvent> gains = new List<UserEvent>();
                var oldWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                CalculateEventWelfare(assignments, @event, oldWelfare);

                for (int i = 0; i < assignment.Count; i++)
                {
                    var user = assignment[i];
                    ReplaceUser(assignments, @event, leftoutUser, user);
                    var newWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                    CalculateEventWelfare(assignments, @event, newWelfare);
                    gains.Add(new UserEvent(user, @event, newWelfare.TotalWelfare));
                    //undo
                    ReplaceUser(assignments, @event, user, leftoutUser);
                }
                var bestChoice = gains.Aggregate((current, next) => next.Utility > current.Utility ? next : current);
                if (bestChoice.Utility > oldWelfare.TotalWelfare)
                {
                    ReplaceUser(assignments, @event, leftoutUser, bestChoice.User);
                }
            }
            DisposeUserEvents.Clear();

            assignments = Swap(assignments);
            assignments = Sweep(assignments);

            return assignments;
        }

        private void ReplaceUser(List<List<int>> assignments, int @event, int newUser, int oldUser)
        {
            assignments[@event].Remove(oldUser);
            assignments[@event].Add(newUser);

            UserAssignments[newUser] = @event;
            UserAssignments[oldUser] = null;
        }

        protected void KeepPhantomEvents(List<int> availableUsers, List<int> realOpenEvents,
            AlgorithmSpec.ReassignmentEnum reassignment)
        {
            _reassignmentStrategy.KeepPhantomEvents(availableUsers, realOpenEvents, reassignment,
                Conf.PreservePercentage);
        }

        protected void KeepPotentialPhantomEvents(List<int> availableUsers, List<int> realOpenEvents)
        {
            var phantomEvents =
                AllEvents.Where(x => !EventIsReal(x, Assignments[x])).Select(x => new UserEvent { Event = x, Utility = 0d }).ToList();
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

        protected List<List<int>> RealizePhantomEvents(List<List<int>> assignments, List<int> numberOfUserAssignments)
        {
            if (!Conf.PostPhantomRealization)
            {
                return assignments;
            }

            var phantomEvents = AllEvents.Where(x => assignments[x].Count < EventCapacity[x].Min).ToList();
            var realEvents = AllEvents.Where(x => assignments[x].Count >= EventCapacity[x].Min).ToList();
            var phantomEventsInterests = phantomEvents.Select((x, y) => new KeyValuePair<int, int>(x, EventCapacity[x].Min - assignments[x].Count)).OrderBy(x => x.Value);
            var candidateUsers = realEvents.SelectMany(x => assignments[x]).ToList();

            foreach (var phantomEvent in phantomEventsInterests)
            {
                var transferedUserEvents = new List<UserEvent>();
                var destinationEvent = phantomEvent.Key;
                var oldSocialWelfare = CalculateSocialWelfare(assignments);
                var finalizedUsers = new List<int>();
                var realUsersWelfare = candidateUsers.ToDictionary(x => x, x => InAffinities[x][destinationEvent])
                .OrderByDescending(x => x.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

                bool dealmade = false;

                foreach (var userWelfare in realUsersWelfare)
                {
                    var canditateUser = userWelfare.Key;
                    var sourceEvent = UserAssignments[canditateUser];

                    if (sourceEvent != null && assignments[sourceEvent.Value].Count > EventCapacity[sourceEvent.Value].Min)
                    {
                        //var oldWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                        //CalculateEventWelfare(assignments, sourceEvent.Value, oldWelfare, false);
                        //CalculateEventWelfare(assignments, destinationEvent, oldWelfare, false);

                        if (InAffinities[canditateUser][sourceEvent.Value] < InAffinities[canditateUser][destinationEvent])
                        {
                            assignments[sourceEvent.Value].Remove(canditateUser);
                            assignments[destinationEvent].Add(canditateUser);
                            transferedUserEvents.Add(new UserEvent(canditateUser, sourceEvent.Value));
                        }

                        //var newWelfare = new Welfare { InnateWelfare = 0, SocialWelfare = 0, TotalWelfare = 0 };
                        //CalculateEventWelfare(assignments, sourceEvent.Value, newWelfare, false);
                        //CalculateEventWelfare(assignments, destinationEvent, newWelfare, false);

                        //if (newWelfare.TotalWelfare <= oldWelfare.TotalWelfare)
                        //{
                        //    //undo
                        //    assignments[destinationEvent].Remove(canditateUser);
                        //    assignments[sourceEvent.Value].Add(canditateUser);
                        //}
                        //else
                        //{
                        //    transferedUserEvents.Add(new UserEvent(canditateUser, sourceEvent.Value));
                        //}

                        if (EventIsReal(destinationEvent, assignments[destinationEvent]))
                        {
                            var newSocialWelfare = CalculateSocialWelfare(assignments);
                            if (newSocialWelfare.TotalWelfare > oldSocialWelfare.TotalWelfare)
                            {
                                RealizePhantomEvent(assignments, destinationEvent, new List<int>());
                                foreach (var finializedUser in assignments[destinationEvent])
                                {
                                    finalizedUsers.Add(finializedUser);
                                }
                                dealmade = true;
                                break;
                            }
                            else
                            {
                                foreach (var userEvent in transferedUserEvents)
                                {
                                    assignments[destinationEvent].Remove(userEvent.User);
                                    assignments[userEvent.Event].Add(userEvent.User);
                                }
                                transferedUserEvents.RemoveAll(x => true);
                            }
                        }
                    }
                }

                if (!dealmade)
                {
                    foreach (var userEvent in transferedUserEvents)
                    {
                        assignments[destinationEvent].Remove(userEvent.User);
                        assignments[userEvent.Event].Add(userEvent.User);
                    }
                    transferedUserEvents.RemoveAll(x => true);
                }

                candidateUsers.RemoveAll(x => finalizedUsers.Contains(x));
            }

            return assignments;
        }

        protected abstract void RealizePhantomEvent(List<List<int>> assignments, int @event, List<int> affectedEvents);

        protected void CalculateEventWelfare(List<List<int>> assignments, int @event, Welfare welfare, bool checkEventReality = true)
        {
            if (checkEventReality && !EventIsReal(@event, assignments[@event]))
            {
                return;
            }

            var w = CalculateEventWelfare(assignments, @event);
            welfare.InnateWelfare += w.InnateWelfare;
            welfare.SocialWelfare += w.SocialWelfare;
            welfare.TotalWelfare += w.InnateWelfare + w.SocialWelfare;
        }

        private static Random random = new Random();

        public static string GetVoucherNumber(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var result = new string(
                Enumerable.Repeat(chars, length)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());

            return result;
        }

        protected Welfare CalculateEventWelfare(List<List<int>> assignments, int @event)
        {
            var result = new Welfare
            {
                InnateWelfare = 0d,
                SocialWelfare = 0d,
                TotalWelfare = 0d
            };
            //var rand = GetVoucherNumber(5);

            var assignment = assignments[@event];
            //var file = new StreamWriter(string.Format("{0}-{1}.txt", @event, rand));
            //assignment.ForEach(x => file.WriteLine(x));

            foreach (var user1 in assignment)
            {
                result.InnateWelfare += InAffinities[user1][@event];

                foreach (var user2 in assignment)
                {
                    if (user1 != user2)
                    {
                        //file.WriteLine(string.Format("{0},{1}", user1, user2));
                        result.SocialWelfare += SocAffinities[user1, user2];
                    }
                }
            }
            //file.Close();

            result.InnateWelfare = (1 - Conf.Alpha) * result.InnateWelfare;
            result.SocialWelfare = Conf.Alpha * result.SocialWelfare;
            result.TotalWelfare += result.InnateWelfare + result.SocialWelfare;

            return result;
        }

        public Welfare CalculateSocialWelfare(List<List<int>> assignments, int user, bool onlyRealEvents = true)
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
            if (onlyRealEvents && !EventIsReal(@event, assignments[@event]))
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

        public Welfare CalculateSocialWelfare(List<int> assignment, int user, int @event)
        {
            var welfare = new Welfare
            {
                TotalWelfare = 0d,
                InnateWelfare = 0d,
                SocialWelfare = 0d
            };

            if (!assignment.Contains(user))
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
            if (!UserAssignments[user].HasValue || !EventIsReal(UserAssignments[user].Value, Assignments[UserAssignments[user].Value]))
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

                    if (Conf.Asymmetric && SocAffinities[i, user] > 0)
                    {
                        friendAffinities.Add(SocAffinities[i, user]);
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
            var socialAffinity = users.Sum(x => SocAffinities[user, x] + (Conf.Asymmetric ? SocAffinities[x, user] : 0d));
            var numerator = (1 - Conf.Alpha) * InAffinities[user][assignedEvent] + Conf.Alpha * socialAffinity;

            if (finalDenom == 0)
            {
                return 1;
            }
            var phi = 1 - (numerator / finalDenom);
            return phi;
        }

        public bool EventIsReal(int @event, List<int> assignment)
        {
            var usersCount = assignment.Count;
            var min = EventCapacity[@event].Min;
            var max = EventCapacity[@event].Max;
            return usersCount >= min && usersCount <= max;
        }

        protected Dictionary<int, List<int>> DetectCommunities()
        {
            var graph = new Graph();
            int edgecounter = 0;
            for (int i = 0; i < SocAffinities.GetLength(0); i++)
            {
                for (int j = 0; j < SocAffinities.GetLength(1); j++)
                {
                    if (SocAffinities[i, j] != 0)
                    {
                        graph.AddEdge(i, j, SocAffinities[i, j]);
                        edgecounter++;
                    }
                }
            }
            Console.WriteLine("{0} edges added", edgecounter);

            Dictionary<int, int> partition = Community.BestPartition(graph);
            var communities = new Dictionary<int, List<int>>();
            foreach (var kvp in partition)
            {
                List<int> nodeset;
                if (!communities.TryGetValue(kvp.Value, out nodeset))
                {
                    nodeset = communities[kvp.Value] = new List<int>();
                }
                nodeset.Add(kvp.Key);
            }
            Console.WriteLine("{0} communities found", communities.Count);
            return communities;
        }

        protected UserEvent Util(int @event, int user, bool communityAware, CommunityFixEnum communityFix,
            List<int> users)
        {
            //if (communityAware && communityFix.HasFlag(CommunityFixEnum.Predictive))
            //{
            //    var ue = new UserEvent(user, @event);
            //    ue.Utility += (1 - Conf.Alpha) * InAffinities[user][@event];
            //    var probableParticipants = 0;
            //    //probability of landing on this event
            //    var potentialSocialGain = AllUsers.Sum(x =>
            //    {
            //        var probabilityOfLanding = userEvents1[UserEvent.GetKey(x, e)].Utility / maxInterest * 100;
            //        var thereOrNotThere = rnd.Next(0, 100) < probabilityOfLanding;
            //        if (thereOrNotThere)
            //        {
            //            probableParticipants++;
            //            return SocAffinities[u, x] + (Conf.Asymmetric ? SocAffinities[x, u] : 0d);
            //        }
            //        return 0;
            //    });
            //    ue.Utility += Conf.Alpha * EventCapacity[e].Max * (potentialSocialGain / probableParticipants);
            //    userEvents2.Add(ue);
            //    return ue;
            //}

            var userevent = new UserEvent
            {
                Event = @event,
                User = user
            };

            var g = (1 - Conf.Alpha) * InAffinities[user][@event];

            var s = Conf.Alpha * Assignments[@event].Sum(u => SocAffinities[user, u] + (Conf.Asymmetric ? SocAffinities[u, user] : 0d));

            g = g + s;

            if (communityAware)
            {
                //var assignedUsers = Assignments.SelectMany(x => x).ToList();
                //var users = AllUsers.Where(x => !UserAssignments[x].HasValue && !assignedUsers.Contains(x)).ToList();

                var denumDeduction = 1;
                if (communityFix.HasFlag(CommunityFixEnum.DenomFix))
                {
                    denumDeduction = 0;
                }

                if (communityFix.HasFlag(CommunityFixEnum.None))
                {
                    s = Conf.Alpha * ((EventCapacity[@event].Max - Assignments[@event].Count) *
                        users.Sum(u => SocAffinities[user, u] + (Conf.Asymmetric ? SocAffinities[u, user] : 0d)) /
                        (double)Math.Max(users.Count - denumDeduction, 1));
                }
                else if (communityFix.HasFlag(CommunityFixEnum.Version1))
                {
                    var lowInterestedUsers =
                        users.OrderBy(x => SocAffinities[user, x]).Take(EventCapacity[@event].Max).ToList();
                    s = Conf.Alpha * ((EventCapacity[@event].Max - Assignments[@event].Count) *
                        (lowInterestedUsers.Sum(
                            u => SocAffinities[user, u] + (Conf.Asymmetric ? SocAffinities[u, user] : 0d)) /
                         (double)Math.Max(lowInterestedUsers.Count - denumDeduction, 1)));

                    //s += Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) *
                    //(users.Sum(u => InAffinities[u][@event]) / (double)Math.Max(users.Count - 1, 1));
                }
                else if (communityFix.HasFlag(CommunityFixEnum.Version2))
                {
                    var lowInterestedUsers =
                        users.OrderBy(x => SocAffinities[user, x])
                            .Take(EventCapacity[@event].Max - Assignments[@event].Count)
                            .ToList();
                    s = Conf.Alpha * ((EventCapacity[@event].Max - Assignments[@event].Count) *
                        (lowInterestedUsers.Sum(
                            u => SocAffinities[user, u] + (Conf.Asymmetric ? SocAffinities[u, user] : 0d)) /
                         (double)Math.Max(lowInterestedUsers.Count - denumDeduction, 1)));
                }
                else if (communityFix.HasFlag(CommunityFixEnum.Version3))
                {
                    var lowInterestedUsers =
                        users.OrderBy(x => SocAffinities[user, x] + InAffinities[x][@event])
                            .Take(EventCapacity[@event].Max - Assignments[@event].Count)
                            .ToList();
                    s = Conf.Alpha * ((EventCapacity[@event].Max - Assignments[@event].Count) *
                        (lowInterestedUsers.Sum(
                            u => SocAffinities[user, u] + (Conf.Asymmetric ? SocAffinities[u, user] : 0d)) /
                         (double)Math.Max(lowInterestedUsers.Count - denumDeduction, 1)));
                }
                else if (communityFix.HasFlag(CommunityFixEnum.Version4))
                {
                    var lowInterestedUsers = users.Take(EventCapacity[@event].Max - Assignments[@event].Count).ToList();
                    s = Conf.Alpha * ((EventCapacity[@event].Max - Assignments[@event].Count) *
                        (lowInterestedUsers.Sum(
                            u => SocAffinities[user, u] + (Conf.Asymmetric ? SocAffinities[u, user] : 0d)) /
                         (double)Math.Max(lowInterestedUsers.Count - denumDeduction, 1)));
                }
                else if (communityFix.HasFlag(CommunityFixEnum.Predictive))
                {
                    /*int probableParticipants = 0;
                    var rnd = new Random();
                    var potentialSocialGain = users.Sum(x =>
                    {
                        var probabilityOfLanding = UserEventsInit[UserEvent.GetKey(x, @event)].Utility / MaxInterest * 100;
                        var thereOrNotThere = rnd.Next(0, 100) < probabilityOfLanding;
                        if (thereOrNotThere)
                        {
                            probableParticipants++;
                            return SocAffinities[user, x] + (Conf.Asymmetric ? SocAffinities[x, user] : 0d);
                        }
                        return 0;
                    });
                    s = Conf.Alpha * (EventCapacity[@event].Max - Assignments[@event].Count) * (potentialSocialGain / Math.Max(probableParticipants, 1));*/
                    s = Conf.Alpha * ((EventCapacity[@event].Max - Assignments[@event].Count) *
                        users.Sum(u => SocAffinities[user, u] + (Conf.Asymmetric ? SocAffinities[u, user] : 0d)) /
                        (double)Math.Max(users.Count - denumDeduction, 1));
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

            return userevent; //Math.Round(g, Conf.Percision);
        }

        protected List<UserEvent> ExtraversionIndexInitialization(List<int> users, List<int> events)
        {
            UserEventsInit = new Dictionary<string, UserEvent>();
            List<UserEvent> userEvents = new List<UserEvent>();

            //find maximum values
            var maxInnateInterest = double.MinValue;
            foreach (var inAffinity in InAffinities)
            {
                maxInnateInterest = Math.Max(inAffinity.Max(), maxInnateInterest);
            }

            var maxSocialInterest = SocAffinities.Cast<double>().Max();

            //normalize values
            var innateAffinities = InAffinities.Select(x => x.Select(y => y / maxInnateInterest).ToList()).ToList();
            var socialAffinities = new List<List<double>>();
            for (int i = 0; i < SocAffinities.GetLength(0); i++)
            {
                socialAffinities.Add(new List<double>());
                for (int j = 0; j < SocAffinities.GetLength(1); j++)
                {
                    socialAffinities[i].Add(SocAffinities[i, j] / maxSocialInterest);
                }
            }

            //find extraversion index
            var extraIndeces = new Dictionary<int, double>();
            foreach (var user in users)
            {
                var avgSoc = socialAffinities[user].Sum(x => x) / socialAffinities[user].Count;
                var avgIn = innateAffinities[user].Sum(x => x) / innateAffinities[user].Count;
                //greater values indicate more extraversion
                var index = avgIn / avgSoc;
                extraIndeces.Add(user, index != 0 ? index : 0.5);
            }

            var keys = extraIndeces.OrderByDescending(x => x.Value).ToList();

            foreach (var u in users)
            {
                foreach (var e in events)
                {
                    var ue = new UserEvent { Event = e, User = u, Utility = 0d };

                    ue.Utility += (1 - extraIndeces[u]) * InAffinities[u][e];
                    ue.Utility += extraIndeces[u] * (EventCapacity[e].Max * users.Sum(x => SocAffinities[u, x] + (Conf.Asymmetric ? SocAffinities[x, u] : 0d)) / (users.Count - 1));
                    UserEventsInit.Add(ue.Key, ue);

                    userEvents.Add(ue);
                }
            }

            return userEvents;
        }

        protected List<UserEvent> EventRankingInitialization(List<int> users, List<int> events)
        {
            UserEventsInit = new Dictionary<string, UserEvent>();
            List<UserEvent> userEvents = new List<UserEvent>();

            foreach (var u in users)
            {
                foreach (var e in events)
                {
                    var ue = new UserEvent { Event = e, User = u, Utility = 1d };

                    ue.Utility += (1 - Conf.Alpha) * InAffinities[u][e];
                    var socinnate = 0d;
                    var count = 0;

                    foreach (var user in users)
                    {
                        if (InAffinities[user][e] > 0)
                        {
                            socinnate += SocAffinities[u, user] + (Conf.Asymmetric ? SocAffinities[user, u] : 0d);
                            count++;
                        }
                    }
                    ue.Utility += Conf.Alpha * (EventCapacity[e].Max * socinnate / (count - 1));

                    UserEventsInit.Add(ue.Key, ue);
                    userEvents.Add(ue);
                }
            }

            return userEvents;
        }

        protected List<UserEvent> PredictiveInitialization(InitStrategyEnum initStrategy, List<int> users, List<int> events)
        {
            UserEventsInit = new Dictionary<string, UserEvent>();
            List<UserEvent> userEvents = new List<UserEvent>();
            foreach (var u in users)
            {
                foreach (var e in events)
                {
                    if (ExcludingUserEvents != null && ExcludingUserEvents.Any(x => x.Event == e && x.User == u))
                    {
                        continue;
                    }

                    var ue = new UserEvent { Event = e, User = u, Utility = 0d };

                    ue.Utility += (1 - Conf.Alpha) * InAffinities[u][e];
                    ue.Utility += Conf.Alpha * (EventCapacity[e].Max *
                                  users.Sum(
                                      x => SocAffinities[u, x] + (Conf.Asymmetric ? SocAffinities[x, u] : 0d)) /
                                  (users.Count - 1));
                    UserEventsInit.Add(ue.Key, ue);

                    if (initStrategy == InitStrategyEnum.CommunityAwareSort)
                    {
                        userEvents.Add(ue);
                    }
                }
            }


            MaxInterest = UserEventsInit.Count > 0 ? UserEventsInit.Max(x => x.Value.Utility) : 0d;
            if (initStrategy == InitStrategyEnum.CommunityAwareSort)
            {
                return userEvents;
            }

            //foreach (var ue1 in userEvents1)
            //{
            //    var ue = new UserEvent { Event = ue1.Value.Event, User = ue1.Value.User, Utility = ue1.Value.Utility };
            //    userEvents2.Add(ue);
            //}
            /*var rnd = new Random();
            foreach (var u in AllUsers)
            {
                foreach (var e in events)
                {
                    var ue = new UserEvent { Event = e, User = u, Utility = 0d };

                    ue.Utility += (1 - Conf.Alpha) * InAffinities[u][e];
                    var probableParticipants = 0;
                    probability of landing on this event
                    var potentialSocialGain = users.Sum(x =>
                    {
                        var probabilityOfLanding = UserEventsInit[UserEvent.GetKey(x, e)].Utility / MaxInterest * 100;
                        var thereOrNotThere = rnd.Next(0, 100) < probabilityOfLanding;
                        if (thereOrNotThere)
                        {
                            probableParticipants++;
                            return SocAffinities[u, x] + (Conf.Asymmetric ? SocAffinities[x, u] : 0d);
                        }
                        return 0;
                    });
                    ue.Utility += Conf.Alpha * EventCapacity[e].Max * (potentialSocialGain / Math.Max(probableParticipants, 1));
                    ue.Priority = ue.Utility;
                    userEvents.Add(ue);
                }
            }*/

            if (initStrategy == InitStrategyEnum.ProbabilisticSort)
            {
                foreach (var u in users)
                {
                    foreach (var e in events)
                    {
                        var ue = new UserEvent { Event = e, User = u, Utility = 0d };

                        ue.Utility += (1 - Conf.Alpha) * InAffinities[u][e];
                        //probability of landing on this event
                        var gains = users.Select(x => new UserEvent(x, e, UserEventsInit[UserEvent.GetKey(x, e)].Utility));
                        var gain = gains.OrderByDescending(x => x.Utility).Take(EventCapacity[e].Max - 1).Sum(x => x.Utility);

                        ue.Utility += Conf.Alpha * gain;
                        ue.Priority = ue.Utility;
                        userEvents.Add(ue);
                    }
                }

                if (initStrategy != InitStrategyEnum.ProbabilisticSort)
                {
                    return userEvents;
                }
            }

            return userEvents;
        }

        protected double Util(int @event, int mainUser)
        {
            return (1 - Conf.Alpha) * InAffinities[mainUser][@event];
        }

        protected double Util(int @event, int mainUser, int friendUser, UserFriends friends)
        {
            var g = (1 - Conf.Alpha) * InAffinities[friendUser][@event];

            var s = Conf.Alpha * friends.Sum(u => SocAffinities[friendUser, u.User] + (Conf.Asymmetric ? SocAffinities[u.User, friendUser] : 0d));

            return g + s;
        }
    }
}
