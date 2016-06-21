using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;

namespace Implementation
{
    class Program
    {
        static void Main(string[] args)
        {
            //MeetupReader meetupReader = new MeetupReader();
            //meetupReader.CalculateSocialAffinity();

            ConsoleKeyInfo str;
            do
            {
                Algorithm<List<UserEvent>> p = ShowMenu();

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

        private static Algorithm<List<UserEvent>> ShowMenu()
        {
            Console.ForegroundColor = ConsoleColor.White;
            int algInt = 0;

            FeedTypeEnum feedType = FeedTypeEnum.Random;
            string inputFilePath = null;
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
                            Console.Write("Enter File Name:");
                            inputFilePath = Console.ReadLine();
                            break;
                    }
                    break;
                }
                Console.WriteLine("Wrong Input, Try Again.");
            }

            Console.WriteLine();

            while (true)
            {
                Console.WriteLine(" ------Choose Algorithm--- ");
                Console.WriteLine("|1.CADG                   |");
                Console.WriteLine("|2.SG                     |");
                Console.WriteLine(" ------------------------- ");
                Console.WriteLine();
                Console.Write("Type your choice: ");
                var input = Console.ReadLine();
                if (input != null && int.TryParse(input, out algInt) && algInt >= 1 && algInt <= 2)
                {
                    break;
                }
                Console.WriteLine("Wrong Input, Try Again.");
            }

            Console.WriteLine();

            while (algInt == 2)
            {
                SgConf conf = new SgConf();
                conf = new SgConf
                {
                    NumberOfUsers = 500,
                    NumberOfEvents = 50,
                    InputFilePath = inputFilePath,
                    FeedType = feedType
                };

                return new Sg(conf);
            }

            while (algInt == 1)
            {
                CadgConf conf = new CadgConf();
                Console.WriteLine(" ---Choose Algorithm Options--- ");
                Console.WriteLine("|1.Phantom Awareness           |");
                Console.WriteLine("|2.Post Initialization Insert  |");
                Console.WriteLine("|3.Immediate Reaction          |");
                Console.WriteLine("|4.Reassignment                |");
                Console.WriteLine("|5.Deficit Fix                 |");
                Console.WriteLine("|6.Agile Adjustment            |");
                Console.WriteLine("|7.Print Stack                 |");
                Console.WriteLine("|8.Pure                        |");
                Console.WriteLine(" ------------------------------ ");
                Console.WriteLine();
                Console.Write("Type your choice: ");
                var options = 1;
                var input = Console.ReadLine();
                if (input != null && int.TryParse(input, out options) && options >= 1 && options <= 7654321)
                {
                    conf = new CadgConf
                    {
                        NumberOfUsers = 500,
                        NumberOfEvents = 50,
                        InputFilePath = inputFilePath,
                        PhantomAware = input.Contains("1"),
                        PostInitializationInsert = input.Contains("2"),
                        ImmediateReaction = input.Contains("3"),
                        Reassign = input.Contains("4"),
                        DeficitFix = input.Contains("5"),
                        LazyAdjustment = !input.Contains("6"),
                        PrintOutEachStep = input.Contains("7"),
                        FeedType = feedType
                    };

                    Console.WriteLine();

                    return new Cadg(conf);
                }
                Console.WriteLine("Wrong Input, Try Again.");
            }

            throw new Exception("Wrong Input!");
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
