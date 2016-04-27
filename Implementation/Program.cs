using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleKeyInfo str;
            do
            {
                Pcadg p = new Pcadg();
                var result = p.Run();
                foreach (var userEvent in result)
                {
                    Console.WriteLine("{0} ----> {1}", userEvent.User, userEvent.Event);
                }
                Console.WriteLine();
                str = Console.ReadKey();
                p = null;
            } while (str.Key == ConsoleKey.Enter);

        }
    }
}
