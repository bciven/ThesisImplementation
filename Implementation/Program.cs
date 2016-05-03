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
                Pcadg p = new Pcadg(20, 5);
                var watch = new Stopwatch();

                p.CalculateAffectedEvents = false;
                p.Initialize();
                watch.Start();
                var result = p.Run();
                watch.Stop();
                Print(result, p.SocialWelfare, watch);
                watch.Reset();

                p.CalculateAffectedEvents = true;
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
            Console.WriteLine("{0} User(s) are assigned", count.Sum(x=>x.c));
            Console.WriteLine();
        }
    }
}
