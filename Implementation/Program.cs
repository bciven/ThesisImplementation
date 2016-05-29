using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Data_Structures;

namespace Implementation
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleKeyInfo str;
            do
            {
                Pcadg p = ShowMenu();

                var watch = new Stopwatch();

                p.Initialize();
                watch.Start();
                var result = p.Run();
                watch.Stop();
                Print(result, p.SocialWelfare, watch);
                watch.Reset();

                str = Console.ReadKey();
                Console.WriteLine();
            } while (str.Key == ConsoleKey.Enter);

        }

        private static Pcadg ShowMenu()
        {
            Console.ForegroundColor = ConsoleColor.White;
            int numberOfUsers = 500;
            int numberOfEvents = 50;
            int algInt = 1;
            string filePath = null;
            while (true)
            {
                Console.WriteLine(" -------Choose Algorithm------- ");
                Console.WriteLine("|1.DG                          |");
                Console.WriteLine("|2.DG + PER                    |");
                Console.WriteLine("|3.DG + IR                     |");
                Console.WriteLine(" ------------------------------ ");
                Console.WriteLine();
                Console.Write("Type your choice: ");
                var input = Console.ReadLine();
                if (int.TryParse(input, out algInt) && algInt >= 1 && algInt <= 3)
                {
                    break;
                }
                Console.WriteLine("Wrong Input, Try Again.");
            }
            Console.WriteLine();
            int inputInt = 1;
            while (true)
            {
                Console.WriteLine(" ---------Choose Input--------- ");
                Console.WriteLine("|1.RANDOM                      |");
                Console.WriteLine("|2.OriginalExperiment          |");
                Console.WriteLine("|3.Example1                    |");
                Console.WriteLine("|4.From Excel File             |");
                Console.WriteLine(" ------------------------------ ");
                Console.WriteLine();
                Console.Write("Type your choice: ");
                var input = Console.ReadLine();
                if (int.TryParse(input, out inputInt) && inputInt >= 1 && inputInt <= 4)
                {
                    break;
                }
                Console.WriteLine("Wrong Input, Try Again.");
            }
            Console.WriteLine();
            FeedTypeEnum feedType = FeedTypeEnum.Random;
            bool calculateAffectedEvents = false;
            bool reassign = false;
            switch (algInt)
            {
                case 1:
                    break;
                case 2:
                    reassign = true;
                    break;
                case 3:
                    calculateAffectedEvents = true;
                    break;
            }
            switch (inputInt)
            {
                case 1:
                    feedType = FeedTypeEnum.Random;
                    break;
                case 2:
                    feedType = FeedTypeEnum.OriginalExperiment;
                    break;
                case 3:
                    feedType = FeedTypeEnum.Example1;
                    break;
                case 4:
                    feedType = FeedTypeEnum.XlsxFile;
                    filePath = Console.ReadLine();
                    break;
            }
            return new Pcadg(feedType, numberOfUsers, numberOfEvents, calculateAffectedEvents, reassign, false, filePath);
        }

        private static void CalcPossibility(List<int> states, int numberOfEvents, Pcadg p)
        {
            var q = new List<List<int>>();

            for (int i = 0; i < numberOfEvents; i++)
            {
                var l = new List<int>();
                for (int index = 0; index < states.Count; index++)
                {
                    var state = states[index];
                    if (state == i)
                        l.Add(index);
                }
                q.Add(l);
            }

            Console.WriteLine(p.CalculateSocialWelfare(q));
        }

        private static void Print(List<UserEvent> result, double gain, Stopwatch watch)
        {
            var filtered = result.Where(x => x.Event >= 0).ToList();
            foreach (var userEvent in filtered)
            {
                Console.WriteLine("{0} ----> {1}", userEvent.User + 1, userEvent.Event + 1);
            }
            Console.WriteLine("Exection Time: {0}ms", watch.ElapsedMilliseconds);
            Console.WriteLine("Social Welfare: {0}", gain);
            Console.WriteLine();
            var count = filtered.GroupBy(x => x.Event).Select(x => new { e = x.Key, c = x.Count() }).ToList();
            foreach (var c in count)
            {
                Console.WriteLine("Count: Event {0} ----> {1} User(s)", c.e + 1, c.c);
            }
            Console.WriteLine("{0} User(s) are assigned", count.Sum(x => x.c));
            Console.WriteLine();
        }
    }
}
