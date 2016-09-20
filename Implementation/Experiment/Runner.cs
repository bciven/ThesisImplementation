using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Algorithms;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;
using Random = Implementation.Algorithms.Random;

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
                               let capvar = exp.Element("capvar")
                               let sndensity = exp.Element("sndensity")
                               let exptypes = exp.Element("exptypes")
                               let mincard = exp.Element("mincard")
                               let snmodel = exp.Element("snmodel")
                               let eventinterestperct = exp.Element("eventinterestperct")
                               where users != null && events != null && alpha != null &&
                                     capmean != null && capvar != null && sndensity != null
                               select new Parameters
                               {
                                   ExpCount = Convert.ToInt32(exp.Attribute("count").Value),
                                   UserCount = Convert.ToInt32(users.Attribute("count").Value),
                                   EventCount = Convert.ToInt32(events.Attribute("count").Value),
                                   AlphaValue = Convert.ToDouble(alpha.Attribute("value").Value),
                                   CapmeanValue = Convert.ToDouble(capmean.Attribute("value").Value),
                                   CapVarValue = Convert.ToDouble(capvar.Attribute("value").Value),
                                   EventInterestPerctValue = Convert.ToDouble(eventinterestperct.Attribute("value").Value),
                                   SndensityValue = Convert.ToDouble(sndensity.Attribute("value").Value),
                                   MinCardinalityOption = (MinCardinalityOptions)Convert.ToInt32(mincard.Attribute("value").Value),
                                   SocialNetworkModel = (SocialNetworkModel)Convert.ToInt32(snmodel.Attribute("value").Value),
                                   ExpTypes = exptypes.Descendants("type").Select(x =>
                                   {
                                       var communityfix = x.Attribute("CommunityFix") != null && Convert.ToBoolean(x.Attribute("CommunityFix").Value);
                                       var reassignment = AlgorithmSpec.ReassignmentEnum.None;
                                       if(x.Attribute("Reassignment") != null)
                                       {
                                           reassignment = x.Attribute("Reassignment").Value == "dynamic"
                                               ? AlgorithmSpec.ReassignmentEnum.Dynamic
                                               : AlgorithmSpec.ReassignmentEnum.Greedy;
                                       }
                                       int? takechancelimit = null;
                                       if (x.Attribute("TakeChanceLimit") != null)
                                       {
                                           takechancelimit = Convert.ToInt32(x.Attribute("TakeChanceLimit").Value);
                                       }

                                       bool deficitFix = false;
                                       if (x.Attribute("DeficitFix") != null)
                                       {
                                           deficitFix = Convert.ToBoolean(x.Attribute("DeficitFix").Value);
                                       }

                                       bool doublePriority = false;
                                       if (x.Attribute("DoublePriority") != null)
                                       {
                                           doublePriority = Convert.ToBoolean(x.Attribute("DoublePriority").Value);
                                       }

                                       var algspec = new AlgorithmSpec
                                       {
                                           CommunityFix = communityfix,
                                           Reassignment = reassignment,
                                           TakeChanceLimit = takechancelimit,
                                           DeficitFix = deficitFix,
                                           DoublePriority = doublePriority
                                       };

                                       switch (x.Value.ToUpper())
                                       {
                                           case "IR":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.IR;
                                               break;
                                           case "IRC":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.IRC;
                                               break;
                                           case "DG":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.DG;
                                               break;
                                           case "PADG":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.PADG;
                                               break;
                                           case "PCADG":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.PCADG;
                                               break;

                                           case "RANDOM":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.Random;
                                               break;
                                           case "RANDOMPLUS":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.RandomPlus;
                                               break;
                                           case "OG":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.OG;
                                               break;
                                           case "COG":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.COG;
                                               break;
                                           default:
                                               throw new Exception("Wrong Experiment Type");
                                       }
                                       return algspec;

                                   }).ToList(),
                                   ExperimentFile = experimentFile
                               }).ToList();
            return experiments;
        }

        private IDataFeed CreateFeed(FeedTypeEnum feedType, string inputFilePath, Parameters parameters)
        {
            IDataFeed dataFeeder;
            var distDataParams = new DistDataParams
            {
                MinCardinalityOption = parameters.MinCardinalityOption,
                CapacityMean = Convert.ToInt32(parameters.CapmeanValue),
                CapacityVariance = Convert.ToInt32(parameters.CapVarValue),
                SocialNetworkModel = parameters.SocialNetworkModel,
                SocialNetworkDensity = parameters.SndensityValue,
                EventInterestPerct = parameters.EventInterestPerctValue
            };

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
                    dataFeeder = new DistDataFeed(distDataParams);
                    break;
                case FeedTypeEnum.SerialExperiment:
                    if (string.IsNullOrEmpty(inputFilePath))
                    {
                        dataFeeder = new DistDataFeed(distDataParams);
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
                    for (int round = 0; round < configs.Count; round++)
                    {
                        SetInputFile(serial, round, configs);
                        var algorithm = CreateAlgorithm(configs, round, parameters);
                        var output = CreateOutputFileInfo(configs, numOfExp, i, round, dir);
                        Run(i, algorithm, output, parameters.ExpTypes[round].Algorithm);
                    }
                }
            }

            Exit();
        }

        private static FileInfo CreateOutputFileInfo(List<SgConf> configs, int numOfExp, int i, int round, DirectoryInfo dir)
        {
            var algDigits = Convert.ToInt32(Math.Floor(Math.Log10(configs.Count) + 1));
            var expDigits = Convert.ToInt32(Math.Floor(Math.Log10(numOfExp) + 1));
            var fileName = i.ToString().PadLeft(expDigits, '0') + "-" + round.ToString().PadLeft(algDigits, '0');
            var output = new FileInfo(Path.Combine(dir.Name, fileName + ".xlsx"));
            return output;
        }

        private static void SetInputFile(bool serial, int round, List<SgConf> configs)
        {
            if (serial)
            {
                if (round == 0)
                {
                    configs[round].InputFilePath = null;
                }
                else if (round > 0)
                {
                    configs[round].InputFilePath = configs[round - 1].InputFilePath;
                }
            }
        }

        private Algorithm<List<UserEvent>> CreateAlgorithm(List<SgConf> configs, int j, Parameters parameters)
        {
            if (configs[j] is CadgConf)
            {
                var cadgConf = (CadgConf)configs[j];
                var feed = CreateFeed(cadgConf.FeedType, cadgConf.InputFilePath, parameters);
                return new Cadg(cadgConf, feed);
            }

            if (configs[j] is RandomConf)
            {
                var ogConf = (RandomConf)configs[j];
                var feed = CreateFeed(ogConf.FeedType, ogConf.InputFilePath, parameters);
                return new Random(ogConf, feed);
            }

            if (configs[j] is OgConf)
            {
                var ogConf = (OgConf)configs[j];
                var feed = CreateFeed(ogConf.FeedType, ogConf.InputFilePath, parameters);
                return new Og(ogConf, feed);
            }

            {
                var sgConf = (SgConf)configs[j];
                var feed = CreateFeed(sgConf.FeedType, sgConf.InputFilePath, parameters);
                return new Sg(sgConf, feed);
            }
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
            for (int i = 0; i < parameters.ExpTypes.Count; i++)
            {
                var algorithmEnum = parameters.ExpTypes[i].Algorithm;
                var alg = (int)algorithmEnum;
                if (algorithmEnum == AlgorithmSpec.AlgorithmEnum.OG || algorithmEnum == AlgorithmSpec.AlgorithmEnum.COG)
                {
                    var conf = new OgConf
                    {
                        NumberOfUsers = parameters.UserCount,
                        NumberOfEvents = parameters.EventCount,
                        InputFilePath = null,
                        PrintOutEachStep = false,
                        FeedType = FeedTypeEnum.SerialExperiment,
                        Alpha = parameters.AlphaValue,
                        AlgorithmName = ConvertToString(algorithmEnum),
                        Parameters = parameters,
                        CommunityAware = algorithmEnum == AlgorithmSpec.AlgorithmEnum.COG,
                        DoublePriority = parameters.ExpTypes[i].DoublePriority
                    };

                    configs.Add(conf);
                }
                else if (algorithmEnum == AlgorithmSpec.AlgorithmEnum.RandomPlus)
                {
                    var conf = new RandomPlusConf();
                    var tcl = parameters.ExpTypes[i].TakeChanceLimit ?? parameters.EventCount;

                    conf = new RandomPlusConf
                    {
                        NumberOfUsers = parameters.UserCount,
                        NumberOfEvents = parameters.EventCount,
                        InputFilePath = null,
                        Reassign = true,
                        PrintOutEachStep = false,
                        FeedType = FeedTypeEnum.SerialExperiment,
                        Alpha = parameters.AlphaValue,
                        AlgorithmName = ConvertToString(algorithmEnum),
                        Parameters = parameters,
                        TakeChanceLimit = tcl
                    };

                    configs.Add(conf);
                }
                else if (algorithmEnum == AlgorithmSpec.AlgorithmEnum.Random)
                {
                    var conf = new RandomConf();
                    conf = new RandomConf
                    {
                        NumberOfUsers = parameters.UserCount,
                        NumberOfEvents = parameters.EventCount,
                        InputFilePath = null,
                        Reassign = true,
                        PrintOutEachStep = false,
                        FeedType = FeedTypeEnum.SerialExperiment,
                        Alpha = parameters.AlphaValue,
                        AlgorithmName = ConvertToString(algorithmEnum),
                        Parameters = parameters
                    };

                    configs.Add(conf);
                }
                else
                {
                    var conf = new CadgConf();
                    var DG = alg == (int)AlgorithmSpec.AlgorithmEnum.DG;
                    var PCADG = alg == (int)AlgorithmSpec.AlgorithmEnum.PCADG;
                    var PADG = alg == (int)AlgorithmSpec.AlgorithmEnum.PADG;
                    var IR = alg == (int)AlgorithmSpec.AlgorithmEnum.IR;
                    var IRC = alg == (int)AlgorithmSpec.AlgorithmEnum.IRC;

                    conf = new CadgConf
                    {
                        NumberOfUsers = parameters.UserCount,
                        NumberOfEvents = parameters.EventCount,
                        InputFilePath = null,
                        PhantomAware = !DG,
                        PostInitializationInsert = true,
                        ImmediateReaction = IR || IRC,
                        Reassignment = parameters.ExpTypes[i].Reassignment,
                        DeficitFix = parameters.ExpTypes[i].DeficitFix,
                        LazyAdjustment = false,
                        PrintOutEachStep = false,
                        FeedType = FeedTypeEnum.SerialExperiment,
                        CommunityAware = IRC || PCADG,
                        Alpha = parameters.AlphaValue,
                        AlgorithmName = ConvertToString(algorithmEnum),
                        Parameters = parameters,
                        CommunityFix = parameters.ExpTypes[i].CommunityFix,
                        DoublePriority = parameters.ExpTypes[i].DoublePriority
                    };

                    configs.Add(conf);
                }
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
                Console.WriteLine("|3.OG                     |");
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

            if (algInt == 3)
            {
                for (int i = 0; i < numberOfExperimentTypes; i++)
                {
                    var conf = new RandomConf()
                    {
                        NumberOfUsers = 500,
                        NumberOfEvents = 50,
                        InputFilePath = inputFilePath,
                        FeedType = feedType,
                        Alpha = parameters.AlphaValue,
                        AlgorithmName = "OG",
                        Percision = 7,
                        NumberOfExperimentTypes = 1,
                        Reassign = true,
                        PrintOutEachStep = false,
                        Parameters = parameters
                    };
                    configs.Add(conf);
                }
            }
            else if (algInt == 2)
            {
                for (int i = 0; i < numberOfExperimentTypes; i++)
                {
                    var conf = new SgConf
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
                    Console.WriteLine("|4.Dynamic Reassignment        |");
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
                            Reassignment = input.Contains("4") ? AlgorithmSpec.ReassignmentEnum.Dynamic : AlgorithmSpec.ReassignmentEnum.Greedy,
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

        private void Run(int round, Algorithm<List<UserEvent>> alg, FileInfo output, AlgorithmSpec.AlgorithmEnum algorithmEnum)
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
