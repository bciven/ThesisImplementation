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
                Cadg p = ShowMenu();

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

        private static Cadg ShowMenu()
        {
            Console.ForegroundColor = ConsoleColor.White;
            CadgConf conf = new CadgConf();
            conf.NumberOfUsers = 500;
            conf.NumberOfEvents = 50;
            conf.InputFilePath = null;
            while (true)
            {
                Console.WriteLine(" ---Choose Algorithm Options--- ");
                Console.WriteLine("|1.Phantom Awareness           |");
                Console.WriteLine("|2.Post Initialization Insert  |");
                Console.WriteLine("|3.Immediate Reaction          |");
                Console.WriteLine("|4.Reassignment                |");
                Console.WriteLine("|5.Deficit Fix                 |");
                Console.WriteLine("|6.Lazy Adjustment             |");
                Console.WriteLine("|7.Print Stack                 |");
                Console.WriteLine("|8.Pure                        |");
                Console.WriteLine(" ------------------------------ ");
                Console.WriteLine();
                Console.Write("Type your choice: ");
                var algInt = 1;
                var input = Console.ReadLine();
                if (input != null && int.TryParse(input, out algInt) && algInt >= 1 && algInt <= 7654321)
                {
                    conf.PhantomAware = input.Contains("1");
                    conf.PostInitializationInsert = input.Contains("2");
                    conf.ImmediateReaction = input.Contains("3");
                    conf.Reassign = input.Contains("4");
                    conf.DeficitFix = input.Contains("5");
                    conf.LazyAdjustment = input.Contains("6");
                    conf.PrintOutEachStep = input.Contains("7");
                    break;
                }
                Console.WriteLine("Wrong Input, Try Again.");
            }
            Console.WriteLine();

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
                var inputInt = 1;
                if (int.TryParse(input, out inputInt) && inputInt >= 1 && inputInt <= 4)
                {
                    conf.FeedType = FeedTypeEnum.Random;
                    switch (inputInt)
                    {
                        case 1:
                            conf.FeedType = FeedTypeEnum.Random;
                            break;
                        case 2:
                            conf.FeedType = FeedTypeEnum.OriginalExperiment;
                            break;
                        case 3:
                            conf.FeedType = FeedTypeEnum.Example1;
                            break;
                        case 4:
                            conf.FeedType = FeedTypeEnum.XlsxFile;
                            Console.Write("Enter File Name:");
                            conf.InputFilePath = Console.ReadLine();
                            break;
                    }
                    break;
                }
                Console.WriteLine("Wrong Input, Try Again.");
            }
            Console.WriteLine();

            return new Cadg(conf);
        }

        private static void CalcPossibility(List<int> states, int numberOfEvents, Cadg p)
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
/*            var filtered = result.Where(x => x.Event >= 0).ToList();
            foreach (var userEvent in filtered)
            {
                Console.WriteLine("{0} ----> {1}", userEvent.User + 1, userEvent.Event + 1);
            }*/
            Console.WriteLine("Exection Time: {0}ms", watch.ElapsedMilliseconds);
            Console.WriteLine("Social Welfare: {0}", gain);
/*            Console.WriteLine();
            var count = filtered.GroupBy(x => x.Event).Select(x => new { e = x.Key, c = x.Count() }).ToList();
            foreach (var c in count)
            {
                Console.WriteLine("Count: Event {0} ----> {1} User(s)", c.e + 1, c.c);
            }
            Console.WriteLine("{0} User(s) are assigned", count.Sum(x => x.c));*/
            Console.WriteLine();
        }
    }
}
