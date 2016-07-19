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
                               let exptypes = exp.Element("exptypes")
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
                                   ExpTypes = exptypes.Descendants("type").Select(x =>
                                   {
                                       switch (x.Value.ToUpper())
                                       {
                                           case "IR":
                                               return AlgorithmEnum.IR;
                                           case "DG":
                                               return AlgorithmEnum.DG;
                                           case "PADG":
                                               return AlgorithmEnum.PADG;
                                           case "PCADG":
                                               return AlgorithmEnum.PCADG;
                                       }
                                       throw new Exception("Wrong Experiment Type");
                                   }).ToList(),
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
                //var algorithms = ShowMenu(experiment);
                var algorithms = ToAlgorithm(experiment);
                var serial = algorithms.Any(x => x.GetFeedType() == FeedTypeEnum.SerialExperiment);
                for (int i = 0; i < experiment.ExpCount; i++)
                {
                    for (int j = 0; j < algorithms.Count; j++)
                    {
                        var algorithm = algorithms[j];

                        if (serial)
                        {
                            if (j == 0)
                            {
                                algorithm.SetInputFile(null);
                            }
                            else if (j > 0)
                            {
                                algorithm.SetInputFile(algorithms[j - 1].GetInputFile());
                            }
                        }
                        var algDigits = Convert.ToInt32(Math.Floor(Math.Log10(algorithms.Count) + 1));
                        var expDigits = Convert.ToInt32(Math.Floor(Math.Log10(numOfExp) + 1));
                        var fileName = i.ToString().PadLeft(expDigits, '0') + "-" + j.ToString().PadLeft(algDigits, '0');
                        var output = new FileInfo(Path.Combine(dir.Name, fileName + ".xlsx"));
                        Run(i, algorithm, output, experiment.ExpTypes[j]);
                    }
                }
            }

            Exit();
        }

        private List<Algorithm<List<UserEvent>>> ToAlgorithm(Parameters experiment)
        {
            var algorithms = new List<Algorithm<List<UserEvent>>>();
            foreach (var algorithmEnum in experiment.ExpTypes)
            {
                var alg = (int)algorithmEnum;
                CadgConf conf = new CadgConf();
                conf = new CadgConf
                {
                    NumberOfUsers = experiment.UserCount,
                    NumberOfEvents = experiment.EventCount,
                    InputFilePath = null,
                    PhantomAware = alg != (int)AlgorithmEnum.DG,
                    PostInitializationInsert = true,
                    ImmediateReaction = alg == (int)AlgorithmEnum.IR,
                    Reassign = alg == (int)AlgorithmEnum.IR,
                    DeficitFix = alg == (int)AlgorithmEnum.IR,
                    LazyAdjustment = false,
                    PrintOutEachStep = false,
                    FeedType = FeedTypeEnum.SerialExperiment,
                    CommunityAware = (alg != (int)AlgorithmEnum.DG && alg != (int)AlgorithmEnum.PADG),
                    Alpha = experiment.AlphaValue
                };

                algorithms.Add(new Cadg(conf));
            }

            return algorithms;
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

        private List<Algorithm<List<UserEvent>>> ShowMenu(Parameters parameters)
        {
            Console.ForegroundColor = ConsoleColor.White;
            int algInt = 0;

            FeedTypeEnum feedType = FeedTypeEnum.Random;
            string inputFilePath = null;
            int numberOfExperimentTypes = 1;
            var algorithms = new List<Algorithm<List<UserEvent>>>();
            while (true)
            {
                Console.WriteLine(" ---------Choose Input--------- ");
                Console.WriteLine("|1.RANDOM                      |");
                Console.WriteLine("|2.Original Experiment         |");
                Console.WriteLine("|3.Example1                    |");
                Console.WriteLine("|4.From Excel File             |");
                Console.WriteLine("|5.Serial Experiments          |");
                Console.WriteLine(" ------------------------------ ");
                Console.WriteLine();
                Console.Write("Type your choice: ");
                var input = Console.ReadLine();
                var inputInt = 1;
                if (int.TryParse(input, out inputInt) && inputInt >= 1 && inputInt <= 5)
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
                        case 5:
                            feedType = FeedTypeEnum.SerialExperiment;
                            Console.Write("Enter Number Of Experiment Types:");
                            numberOfExperimentTypes = Convert.ToInt32(Console.ReadLine());
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

            if (algInt == 2)
            {
                for (int i = 0; i < numberOfExperimentTypes; i++)
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
                    algorithms.Add(new Sg(conf));
                }
            }
            else if (algInt == 1)
            {
                for (int i = 0; i < numberOfExperimentTypes;)
                {
                    CadgConf conf = new CadgConf();
                    Console.WriteLine(" ---Choose Algorithm Options--- ");
                    Console.WriteLine("|1.Phantom Awareness           |");
                    Console.WriteLine("|2.Post Initialization Insert  |");
                    Console.WriteLine("|3.Immediate Reaction          |");
                    Console.WriteLine("|4.Reassignment                |");
                    Console.WriteLine("|5.Deficit Fix                 |");
                    Console.WriteLine("|6.Agile Adjustment            |");
                    Console.WriteLine("|7.Community Aware             |");
                    Console.WriteLine("|8.Print Stack                 |");
                    Console.WriteLine("|9.Pure                        |");
                    Console.WriteLine(" ------------------------------ ");
                    Console.WriteLine();
                    Console.Write("Type your choice: ");
                    var options = 1;
                    var input = Console.ReadLine();
                    if (input != null && int.TryParse(input, out options) && options >= 1 && options <= 87654321)
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
                            CommunityAware = !input.Contains("7"),
                            PrintOutEachStep = input.Contains("8"),
                            FeedType = feedType,
                            Alpha = parameters.AlphaValue,

                        };

                        Console.WriteLine();

                        algorithms.Add(new Cadg(conf));
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Wrong Input, Try Again.");
                    }
                }
            }

            return algorithms;
        }

        private void Print(List<UserEvent> result, double gain, Stopwatch watch)
        {
            Console.WriteLine("Exection Time: {0}ms", watch.ElapsedMilliseconds);
            Console.WriteLine("Social Welfare: {0}", gain);
            Console.WriteLine();
        }

        private void Run(int round, Algorithm<List<UserEvent>> alg, FileInfo output, AlgorithmEnum algorithmEnum)
        {
            Console.WriteLine("....Round {0}-{1}....", round, ConvertToString(algorithmEnum));
            alg.Initialize();
            var watch = new Stopwatch();
            watch.Start();
            alg.Run();
            var result = alg.CreateOutput(output);
            watch.Stop();
            Print(result, alg.SocialWelfare, watch);
            watch.Reset();
        }

        public string ConvertToString(Enum eff)
        {
            return Enum.GetName(eff.GetType(), eff);
        }
    }
}
