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

namespace Implementation
{
    class Program
    {
        static void Main(string[] args)
        {
            //MeetupReader meetupReader = new MeetupReader();
            //meetupReader.CalculateSocialAffinity();
            
            Runner runner = new Runner();
            runner.RunExperiments();
        }
    }
}
