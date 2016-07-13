using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Data_Structures;

namespace Implementation.Experiment
{
    public class Runner
    {
        private List<Parameters> ReadExperiments()
        {
            const string experimentFile = "Experiment\\Experiments.xml";
            var root = XDocument.Load(experimentFile);

            var experiments = (from exp in root.Descendants("Experiment")
                               let users = exp.Element("users")
                               let events = exp.Element("events")
                               let alpha = exp.Element("alpha")
                               let capmean = exp.Element("capmean")
                               let capstddev = exp.Element("capstddev")
                               let sndensity = exp.Element("sndensity")
                               where users != null && events != null && alpha != null &&
                                     capmean != null && capstddev != null && sndensity != null
                               select new Parameters
                               {
                                   ExpCount = Convert.ToInt32(exp.Attribute("count").Value),
                                   UserCount = Convert.ToInt32(users.Attribute("count").Value),
                                   EventCount = Convert.ToInt32(events.Attribute("count").Value),
                                   AlphaValue = Convert.ToDouble(alpha.Attribute("value").Value),
                                   CapmeanValue = Convert.ToDouble(capmean.Attribute("value").Value),
                                   CapstddevValue = Convert.ToDouble(capstddev.Attribute("value").Value),
                                   SndensityValue = Convert.ToDouble(sndensity.Attribute("value").Value),
                                   ExperimentFile = experimentFile
                               }).ToList();
            return experiments;
        }

        public void RunExperiments()
        {
            var experiments = ReadExperiments();
            foreach (var experiment in experiments)
            {
                var dir = CopyOutputFiles(experiment);
                var numOfExp = experiments.Count().ToString().Length;
                var alg = ShowMenu(experiment);

                for (int i = 0; i < experiment.ExpCount; i++)
                {
                    var fileName = i.ToString().PadLeft(numOfExp, '0');
                    var output = new FileInfo(Path.Combine(dir.Name, fileName + ".xlsx"));
                    Run(i, alg, output);
                }
            }

            Exit();
        }

        private void Exit()
        {
            ConsoleKeyInfo str;
            do
            {
                str = Console.ReadKey();
                Console.WriteLine();
            } while (str.Key != ConsoleKey.Enter);
        }

        private static DirectoryInfo CopyOutputFiles(Parameters experiment)
        {
            var folder = DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ss-fff", CultureInfo.CurrentCulture);
            var dir = Directory.CreateDirectory(folder);
            var fileInfo = new FileInfo(experiment.ExperimentFile);
            File.Copy(experiment.ExperimentFile, Path.Combine(folder, fileInfo.Name));
            return dir;
        }

        private Algorithm<List<UserEvent>> ShowMenu(Parameters parameters)
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
                    FeedType = feedType,
                    Alpha = parameters.AlphaValue
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
                        NumberOfUsers = parameters.UserCount,
                        NumberOfEvents = parameters.EventCount,
                        InputFilePath = inputFilePath,
                        PhantomAware = input.Contains("1"),
                        PostInitializationInsert = input.Contains("2"),
                        ImmediateReaction = input.Contains("3"),
                        Reassign = input.Contains("4"),
                        DeficitFix = input.Contains("5"),
                        LazyAdjustment = !input.Contains("6"),
                        PrintOutEachStep = input.Contains("7"),
                        FeedType = feedType,
                        Alpha = parameters.AlphaValue
                    };

                    Console.WriteLine();

                    return new Cadg(conf);
                }
                Console.WriteLine("Wrong Input, Try Again.");
            }

            throw new Exception("Wrong Input!");
        }

        private void Print(List<UserEvent> result, double gain, Stopwatch watch)
        {
            Console.WriteLine("Exection Time: {0}ms", watch.ElapsedMilliseconds);
            Console.WriteLine("Social Welfare: {0}", gain);
            Console.WriteLine();
        }

        private void Run(int round, Algorithm<List<UserEvent>> alg, FileInfo output)
        {
            Console.WriteLine("....Round {0}....", round);
            alg.Initialize();
            var watch = new Stopwatch();
            watch.Start();
            alg.Run();
            var result = alg.CreateOutput(output);
            watch.Stop();
            Print(result, alg.SocialWelfare, watch);
            watch.Reset();
        }
    }
}
