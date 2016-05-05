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
                int numberOfUsers = 3;
                int numberOfEvents = 2;
                Pcadg p = new Pcadg(numberOfUsers, numberOfEvents);
                /*p.Initialize();

                for (int i = 0; i < Math.Pow(numberOfEvents, numberOfUsers); i++)
                {
                    var binary = Convert.ToString(i, 2).PadLeft(3, '0');
                    var l = binary.Select(x => int.Parse(x.ToString())).ToList();
                    CalcPossibility(l, numberOfEvents, p);
                }*/

                var watch = new Stopwatch();

                p.CalculateAffectedEvents = true;
                p.Initialize();
                watch.Start();
                var result = p.Run();
                watch.Stop();
                Print(result, p.SocialWelfare, watch);
                watch.Reset();

                p.CalculateAffectedEvents = false;
                p.Initialize(false);
                watch.Start();
                result = p.Run();
                watch.Stop();
                Print(result, p.SocialWelfare, watch);
                watch.Reset();

                str = Console.ReadKey();
                Console.WriteLine();
            } while (str.Key == ConsoleKey.Enter);

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
