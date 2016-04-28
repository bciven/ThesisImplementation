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
                Pcadg p = new Pcadg(100,40);
                p.Initialize();

                var watch = System.Diagnostics.Stopwatch.StartNew();
                p.CalculateAffectedEvents = false;
                var result = p.Run();
                watch.Stop();
                Print(result, watch);
                watch.Reset();

                watch.Start();
                p.CalculateAffectedEvents = true;
                result = p.Run();
                watch.Stop();
                Print(result, watch);
                watch.Reset();

                str = Console.ReadKey();
                Console.WriteLine();
            } while (str.Key == ConsoleKey.Enter);

        }

        private static void Print(List<UserEvent> result, Stopwatch watch)
        {
            foreach (var userEvent in result.Where(x=>x.Event > 0))
            {
                Console.WriteLine("{0} ----> {1}", userEvent.User, userEvent.Event);
            }
            Console.WriteLine("Exection Time: {0}ms", watch.ElapsedMilliseconds);

        }
    }
}
