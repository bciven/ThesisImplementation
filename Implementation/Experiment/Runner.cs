using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;

namespace Implementation.Experiment
{
    public class Runner
    {
        const string experimentFile = "Experiment\\Experiments.xml";

        private List<Parameters> ReadExperiments()
        {
            var root = XDocument.Load(experimentFile);

            var experiments = (from exp in root.Descendants("Experiment")
                               let users = exp.Element("users")
                               let events = exp.Element("events")
                               let alpha = exp.Element("alpha")
                               let capmean = exp.Element("capmean")
                               let capstddev = exp.Element("capstddev")
                               let sndensity = exp.Element("sndensity")
                               let exptypes = exp.Element("exptypes")
                               let mincard = exp.Element("mincard")
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
                                   MinCardinalityOption = (MinCardinalityOptions)Convert.ToInt32(mincard.Attribute("value").Value),
                                   ExpTypes = exptypes.Descendants("type").Select(x =>
                                   {
                                       switch (x.Value.ToUpper())
                                       {
                                           case "IR":
                                               return AlgorithmEnum.IR;
                                           case "IRC":
                                               return AlgorithmEnum.IRC;
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

        private IDataFeed CreateFeed(FeedTypeEnum feedType, string inputFilePath, Parameters parameters)
        {
            IDataFeed dataFeeder;
            switch (feedType)
            {
                case FeedTypeEnum.Random:
                    dataFeeder = new RandomDataFeed();
                    break;
                case FeedTypeEnum.Example1:
                    dataFeeder = new Example1Feed();
                    break;
                case FeedTypeEnum.XlsxFile:
                    dataFeeder = new ExcelFileFeed(inputFilePath);
                    break;
                case FeedTypeEnum.OriginalExperiment:
                    dataFeeder = new DistDataFeed(Convert.ToInt32(parameters.CapmeanValue), Convert.ToInt32(parameters.CapstddevValue), parameters.MinCardinalityOption);
                    break;
                case FeedTypeEnum.SerialExperiment:
                    if (string.IsNullOrEmpty(inputFilePath))
                    {
                        dataFeeder = new DistDataFeed(Convert.ToInt32(parameters.CapmeanValue), Convert.ToInt32(parameters.CapstddevValue), parameters.MinCardinalityOption);
                    }
                    else
                    {
                        dataFeeder = new ExcelFileFeed(inputFilePath);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();

            }
            return dataFeeder;
        }

        public void RunExperiments()
        {
            var useMenu = ChooseByMenu();
            var experiments = ReadExperiments();
            CreateWorkingDirectory();

            foreach (var parameters in experiments)
            {
                var dir = CreateOutputDirectory();
                var numOfExp = experiments.Count();
                var configs = useMenu ? ShowMenu(parameters) : ToConfigs(parameters);
                var serial = configs.Any(x => x.FeedType == FeedTypeEnum.SerialExperiment);

                for (int i = 0; i < parameters.ExpCount; i++)
                {
                    for (int j = 0; j < configs.Count; j++)
                    {
                        Algorithm<List<UserEvent>> algorithm;
                        if (serial)
                        {
                            if (j == 0)
                            {
                                configs[j].InputFilePath = null;
                            }
                            else if (j > 0)
                            {
                                configs[j].InputFilePath = configs[j - 1].InputFilePath;
                            }
                        }
                        
                        var cadgConf = configs[j] as CadgConf;
                        if (cadgConf != null)
                        {
                            var feed = CreateFeed(cadgConf.FeedType, cadgConf.InputFilePath, parameters);
                            algorithm = new Cadg(cadgConf, feed);
                        }
                        else
                        {
                            var sgConf = (SgConf)configs[j];
                            var feed = CreateFeed(sgConf.FeedType, sgConf.InputFilePath, parameters);
                            algorithm = new Sg(sgConf, feed);
                        }

                        var algDigits = Convert.ToInt32(Math.Floor(Math.Log10(configs.Count) + 1));
                        var expDigits = Convert.ToInt32(Math.Floor(Math.Log10(numOfExp) + 1));
                        var fileName = i.ToString().PadLeft(expDigits, '0') + "-" + j.ToString().PadLeft(algDigits, '0');
                        var output = new FileInfo(Path.Combine(dir.Name, fileName + ".xlsx"));
                        Run(i, algorithm, output, parameters.ExpTypes[j]);
                    }
                }
            }

            Exit();
        }

        private static void CreateWorkingDirectory()
        {
            var folder = "Batch - " + (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Directory.CreateDirectory(folder);
            var fileInfo = new FileInfo(experimentFile);
            File.Copy(experimentFile, Path.Combine(folder, fileInfo.Name));
            Directory.SetCurrentDirectory(folder);
        }

        private bool ChooseByMenu()
        {
            Console.WriteLine("Do you want to choose experiment by menu?");
            var line = Console.ReadLine();
            return line == "y";
        }

        private List<SgConf> ToConfigs(Parameters parameters)
        {
            var configs = new List<SgConf>();
            foreach (var algorithmEnum in parameters.ExpTypes)
            {
                var alg = (int)algorithmEnum;
                var conf = new CadgConf();
                conf = new CadgConf
                {
                    NumberOfUsers = parameters.UserCount,
                    NumberOfEvents = parameters.EventCount,
                    InputFilePath = null,
                    PhantomAware = alg != (int)AlgorithmEnum.DG,
                    PostInitializationInsert = true,
                    ImmediateReaction = alg >= (int)AlgorithmEnum.IRC,
                    Reassign = alg >= (int)AlgorithmEnum.IRC,
                    DeficitFix = alg >= (int)AlgorithmEnum.IRC,
                    LazyAdjustment = false,
                    PrintOutEachStep = false,
                    FeedType = FeedTypeEnum.SerialExperiment,
                    CommunityAware = (alg == (int)AlgorithmEnum.PCADG || alg == (int)AlgorithmEnum.IRC),
                    Alpha = parameters.AlphaValue,
                    AlgorithmName = ConvertToString(algorithmEnum),
                    Parameters = parameters
                };

                configs.Add(conf);
            }

            return configs;
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

        private static DirectoryInfo CreateOutputDirectory()
        {
            var folder = DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ss-fff", CultureInfo.CurrentCulture);
            var dir = Directory.CreateDirectory(folder);
            return dir;
        }

        private List<SgConf> ShowMenu(Parameters parameters)
        {
            Console.ForegroundColor = ConsoleColor.White;
            int algInt = 0;

            FeedTypeEnum feedType = FeedTypeEnum.Random;
            string inputFilePath = null;
            int numberOfExperimentTypes = 1;
            var configs = new List<SgConf>();
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
                    var conf = new SgConf();
                    conf = new SgConf
                    {
                        NumberOfUsers = 500,
                        NumberOfEvents = 50,
                        InputFilePath = inputFilePath,
                        FeedType = feedType,
                        Alpha = parameters.AlphaValue
                    };
                    configs.Add(conf);
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
                            AlgorithmName = ""
                        };

                        Console.WriteLine();

                        configs.Add(conf);
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Wrong Input, Try Again.");
                    }
                }
            }

            return configs;
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
