using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;
using Implementation.Experiment;
using MathNet.Numerics.Distributions;

namespace Implementation
{
    class Program
    {
        static void Main(string[] args)
        {
            //var rand = new Random();
            //var max = double.MinValue;
            //for (int i = 0; i < 300000; i++)
            //{
            //    double r = 1.0 / Math.Pow(1 - rand.NextDouble(), 1.5);
            //    max = Math.Max(r, max);
            //}
            //Console.WriteLine(max);
            //Console.ReadLine();
            //MeetupReader meetupReader = new MeetupReader();
            //meetupReader.CalculateSocialAffinity();

            Runner runner = new Runner();
            runner.RunExperiments();
        }
    }
}
