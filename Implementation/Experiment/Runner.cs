using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                               let maxcard = exp.Element("maxcard")
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
                                   MaxCardinalityOption = maxcard != null ? (MaxCardinalityOptions)Convert.ToInt32(maxcard.Attribute("value").Value) : MaxCardinalityOptions.Random,
                                   SocialNetworkModel = (SocialNetworkModel)Convert.ToInt32(snmodel.Attribute("value").Value),
                                   Asymmetric = Convert.ToBoolean(snmodel.Attribute("asymmetric").Value),
                                   OutputType = (OutputTypeEnum)Convert.ToInt32(exp.Attribute("output").Value),
                                   ExpTypes = exptypes.Descendants("type").Select(x =>
                                   {
                                       var communityfix = x.Attribute("CommunityFix") != null ? (CommunityFixEnum)Convert.ToInt32(x.Attribute("CommunityFix").Value) : CommunityFixEnum.None;
                                       var reassignment = AlgorithmSpec.ReassignmentEnum.None;
                                       double preservePercentage = 50d;
                                       if (x.Attribute("Reassignment") != null)
                                       {
                                           reassignment = (AlgorithmSpec.ReassignmentEnum)Convert.ToInt32(x.Attribute("Reassignment").Value);
                                           if (reassignment == AlgorithmSpec.ReassignmentEnum.Reduction && x.Attribute("PreservePercentage") != null)
                                           {
                                               preservePercentage = Convert.ToDouble(x.Attribute("PreservePercentage").Value);
                                           }
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

                                       bool reuseDisposedPairs = false;
                                       if (x.Attribute("ReuseDisposedPairs") != null)
                                       {
                                           reuseDisposedPairs = Convert.ToBoolean(x.Attribute("ReuseDisposedPairs").Value);
                                       }

                                       bool lazyAdjustment = true;
                                       if (x.Attribute("LazyAdjustment") != null)
                                       {
                                           lazyAdjustment = Convert.ToBoolean(x.Attribute("LazyAdjustment").Value);
                                       }

                                       bool postPhantomRealization = true;
                                       if (x.Attribute("PostPhantomRealization") != null)
                                       {
                                           postPhantomRealization = Convert.ToBoolean(x.Attribute("PostPhantomRealization").Value);
                                       }

                                       bool swap = false;
                                       double swapThreshold = 0d;
                                       if (x.Attribute("Swap") != null)
                                       {
                                           swap = Convert.ToBoolean(x.Attribute("Swap").Value);
                                           if (swap && x.Attribute("SwapThreshold") != null)
                                           {
                                               swapThreshold = Convert.ToDouble(x.Attribute("SwapThreshold").Value);
                                           }
                                       }

                                       var initStrategy = InitStrategyEnum.RandomSort;
                                       if (x.Attribute("InitStrategy") != null)
                                       {
                                           initStrategy = (InitStrategyEnum) Convert.ToInt32(x.Attribute("InitStrategy").Value);
                                       }

                                       var algspec = new AlgorithmSpec
                                       {
                                           CommunityFix = communityfix,
                                           Reassignment = reassignment,
                                           TakeChanceLimit = takechancelimit,
                                           DeficitFix = deficitFix,
                                           ReuseDisposedPairs = reuseDisposedPairs,
                                           LazyAdjustment = lazyAdjustment,
                                           Swap = swap,
                                           SwapThreshold = swapThreshold,
                                           PreservePercentage = preservePercentage,
                                           PostPhantomRealization = postPhantomRealization,
                                           InitStrategy = initStrategy
                                       };

                                       switch (x.Value.ToUpper())
                                       {
                                           case "IR":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.IR;
                                               break;
                                           case "IRC":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.IRC;
                                               break;
                                           case "CPRDG":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.CPRDG;
                                               break;
                                           case "CPRPADG":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.CPRPADG;
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
                                           case "ECADG":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.ECADG;
                                               break;

                                           case "LA":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.LA;
                                               break;
                                           case "LAPLUS":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.LAPlus;
                                               break;
                                           case "PLA":
                                               algspec.Algorithm = AlgorithmSpec.AlgorithmEnum.PLA;
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
                EventInterestPerct = parameters.EventInterestPerctValue,
                MaxCardinalityOption = parameters.MaxCardinalityOption
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
                case FeedTypeEnum.TextFile:
                    dataFeeder = new TextFileFeed(inputFilePath);
                    break;
                case FeedTypeEnum.OriginalExperiment:
                    dataFeeder = new DistDataFeed(distDataParams);
                    break;
                case FeedTypeEnum.SerialExperiment:
                    if (string.IsNullOrEmpty(inputFilePath))
                    {
                        dataFeeder = new DistDataFeed(distDataParams);
                    }
                    else if (parameters.OutputType == OutputTypeEnum.Excel)
                    {
                        dataFeeder = new ExcelFileFeed(inputFilePath);
                    }
                    else
                    {
                        dataFeeder = new TextFileFeed(inputFilePath);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return dataFeeder;
        }

        public void RunExperiments()
        {
            bool useMenu = false; //ChooseByMenu();

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
                        var output = CreateOutputFileInfo(configs, numOfExp, i, round, dir);
                        var algorithm = CreateAlgorithm(configs, round, parameters, round);
                        Run(i, algorithm, output, parameters.ExpTypes[round].Algorithm);
                    }
                }
            }

            Exit();
        }

        private static FileInfo CreateOutputFileInfo(List<SGConf> configs, int numOfExp, int i, int round, DirectoryInfo dir)
        {
            var algDigits = Convert.ToInt32(Math.Floor(Math.Log10(configs.Count) + 1));
            var expDigits = Convert.ToInt32(Math.Floor(Math.Log10(numOfExp) + 1));
            var fileName = i.ToString().PadLeft(expDigits, '0') + "-" + round.ToString().PadLeft(algDigits, '0');
            if (configs[round].OutputType == OutputTypeEnum.Excel)
            {
                var output = new FileInfo(Path.Combine(dir.Name, fileName + ".xlsx"));
                return output;
            }

            if (configs[round].OutputType == OutputTypeEnum.Text)
            {
                var output = new FileInfo(Path.Combine(dir.Name, fileName));
                return output;
            }

            if (configs[round].OutputType == OutputTypeEnum.None)
            {
                return null;
            }

            throw new InvalidEnumArgumentException("Output type is invalid");
        }

        private static void SetInputFile(bool serial, int round, List<SGConf> configs)
        {
            if (serial)
            {
                //configs[round].InputFilePath = @"E:\Concordia\Thesis\Implementation\Implementation\bin\Debug\Batch - 1482217922.10948\2016-12-20 08-55-47-108\04-06";
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

        private Algorithm<List<UserEvent>> CreateAlgorithm(List<SGConf> configs, int j, Parameters parameters, int index)
        {
            if (configs[j] is CADGConf)
            {
                var cadgConf = (CADGConf)configs[j];
                var feed = CreateFeed(cadgConf.FeedType, cadgConf.InputFilePath, parameters);
                return new CADG(cadgConf, feed, index);
            }

            if (configs[j] is ECADGConf)
            {
                var conf = (ECADGConf)configs[j];
                var feed = CreateFeed(conf.FeedType, conf.InputFilePath, parameters);
                return new ECADG(conf, feed, index);
            }

            if (configs[j] is LAConf)
            {
                var ogConf = (LAConf)configs[j];
                var feed = CreateFeed(ogConf.FeedType, ogConf.InputFilePath, parameters);
                return new LA(ogConf, feed, index);
            }

            if (configs[j] is OGConf)
            {
                var ogConf = (OGConf)configs[j];
                var feed = CreateFeed(ogConf.FeedType, ogConf.InputFilePath, parameters);
                return new OG(ogConf, feed, index);
            }

            {
                var sgConf = (SGConf)configs[j];
                var feed = CreateFeed(sgConf.FeedType, sgConf.InputFilePath, parameters);
                return new SG(sgConf, feed, index);
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

        private List<SGConf> ToConfigs(Parameters parameters)
        {
            var configs = new List<SGConf>();
            for (int i = 0; i < parameters.ExpTypes.Count; i++)
            {
                var algorithmEnum = parameters.ExpTypes[i].Algorithm;
                var alg = (int)algorithmEnum;
                if (algorithmEnum == AlgorithmSpec.AlgorithmEnum.OG || algorithmEnum == AlgorithmSpec.AlgorithmEnum.COG)
                {
                    var conf = new OGConf
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
                        OutputType = parameters.OutputType,
                        Swap = parameters.ExpTypes[i].Swap,
                        SwapThreshold = parameters.ExpTypes[i].SwapThreshold,
                        PreservePercentage = parameters.ExpTypes[i].PreservePercentage,
                        Reassignment = parameters.ExpTypes[i].Reassignment,
                        Asymmetric = parameters.Asymmetric,
                        ReuseDisposedPairs = parameters.ExpTypes[i].ReuseDisposedPairs
                    };

                    configs.Add(conf);
                }
                else if (algorithmEnum == AlgorithmSpec.AlgorithmEnum.LAPlus)
                {
                    var conf = new LAPlusConf();
                    var tcl = parameters.ExpTypes[i].TakeChanceLimit ?? parameters.EventCount;

                    conf = new LAPlusConf
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
                        TakeChanceLimit = tcl,
                        OutputType = parameters.OutputType,
                        Swap = parameters.ExpTypes[i].Swap,
                        SwapThreshold = parameters.ExpTypes[i].SwapThreshold,
                        PreservePercentage = parameters.ExpTypes[i].PreservePercentage,
                        Asymmetric = parameters.Asymmetric,
                        Reassignment = parameters.ExpTypes[i].Reassignment,
                        InitStrategyEnum = parameters.ExpTypes[i].InitStrategy
                    };

                    configs.Add(conf);
                }
                else if (algorithmEnum == AlgorithmSpec.AlgorithmEnum.LA || algorithmEnum == AlgorithmSpec.AlgorithmEnum.PLA)
                {
                    var conf = new LAConf();
                    conf = new LAConf
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
                        OutputType = parameters.OutputType,
                        Swap = parameters.ExpTypes[i].Swap,
                        SwapThreshold = parameters.ExpTypes[i].SwapThreshold,
                        PreservePercentage = parameters.ExpTypes[i].PreservePercentage,
                        Asymmetric = parameters.Asymmetric,
                        Reassignment = parameters.ExpTypes[i].Reassignment,
                        ReuseDisposedPairs = parameters.ExpTypes[i].ReuseDisposedPairs,
                        PostPhantomRealization = parameters.ExpTypes[i].PostPhantomRealization
                    };

                    if (algorithmEnum == AlgorithmSpec.AlgorithmEnum.LA)
                    {
                        conf.InitStrategyEnum = InitStrategyEnum.RandomSort;
                    }
                    else if(algorithmEnum == AlgorithmSpec.AlgorithmEnum.PLA)
                    {
                        conf.InitStrategyEnum = parameters.ExpTypes[i].InitStrategy;
                    }

                    configs.Add(conf);
                }
                else if(algorithmEnum == AlgorithmSpec.AlgorithmEnum.ECADG)
                {
                    var conf = new ECADGConf();
                    var DG = alg == (int)AlgorithmSpec.AlgorithmEnum.DG;
                    var PCADG = alg == (int)AlgorithmSpec.AlgorithmEnum.PCADG;
                    var PADG = alg == (int)AlgorithmSpec.AlgorithmEnum.PADG;
                    var IR = alg == (int)AlgorithmSpec.AlgorithmEnum.IR;
                    var IRC = alg == (int)AlgorithmSpec.AlgorithmEnum.IRC;

                    conf = new ECADGConf
                    {
                        NumberOfUsers = parameters.UserCount,
                        NumberOfEvents = parameters.EventCount,
                        InputFilePath = null,
                        PhantomAware = !DG,
                        ImmediateReaction = IR || IRC,
                        Reassignment = parameters.ExpTypes[i].Reassignment,
                        PrintOutEachStep = false,
                        FeedType = FeedTypeEnum.SerialExperiment,
                        CommunityAware = IRC || PCADG,
                        Alpha = parameters.AlphaValue,
                        AlgorithmName = ConvertToString(algorithmEnum),
                        Parameters = parameters,
                        CommunityFix = parameters.ExpTypes[i].CommunityFix,
                        OutputType = parameters.OutputType,
                        Swap = parameters.ExpTypes[i].Swap,
                        SwapThreshold = parameters.ExpTypes[i].SwapThreshold,
                        PreservePercentage = parameters.ExpTypes[i].PreservePercentage,
                        Asymmetric = parameters.Asymmetric,
                        ReuseDisposedPairs = parameters.ExpTypes[i].ReuseDisposedPairs
                    };

                    configs.Add(conf);
                }
                else if(algorithmEnum == AlgorithmSpec.AlgorithmEnum.CPRDG || algorithmEnum == AlgorithmSpec.AlgorithmEnum.CPRPADG)
                {
                    var conf = new CADGConf();
                    var phantomAware = alg == (int)AlgorithmSpec.AlgorithmEnum.CPRPADG;

                    conf = new CADGConf
                    {
                        NumberOfUsers = parameters.UserCount,
                        NumberOfEvents = parameters.EventCount,
                        InputFilePath = null,
                        PhantomAware = phantomAware,
                        
                        PostInitializationInsert = true,
                        ImmediateReaction = phantomAware,
                        Reassignment = parameters.ExpTypes[i].Reassignment,
                        DeficitFix = true,
                        LazyAdjustment = parameters.ExpTypes[i].LazyAdjustment,
                        PostPhantomRealization = true,
                        PrintOutEachStep = false,
                        FeedType = FeedTypeEnum.SerialExperiment,
                        CommunityAware = true,
                        Alpha = parameters.AlphaValue,
                        AlgorithmName = ConvertToString(algorithmEnum),
                        Parameters = parameters,
                        CommunityFix = parameters.ExpTypes[i].CommunityFix,
                        OutputType = parameters.OutputType,
                        Swap = parameters.ExpTypes[i].Swap,
                        SwapThreshold = parameters.ExpTypes[i].SwapThreshold,
                        PreservePercentage = parameters.ExpTypes[i].PreservePercentage,
                        Asymmetric = parameters.Asymmetric,
                        ReuseDisposedPairs = parameters.ExpTypes[i].ReuseDisposedPairs
                    };

                    configs.Add(conf);
                }
                else
                {
                    var conf = new CADGConf();
                    var DG = alg == (int)AlgorithmSpec.AlgorithmEnum.DG;
                    var PCADG = alg == (int)AlgorithmSpec.AlgorithmEnum.PCADG;
                    var PADG = alg == (int)AlgorithmSpec.AlgorithmEnum.PADG;
                    var IR = alg == (int)AlgorithmSpec.AlgorithmEnum.IR;
                    var IRC = alg == (int)AlgorithmSpec.AlgorithmEnum.IRC;

                    conf = new CADGConf
                    {
                        NumberOfUsers = parameters.UserCount,
                        NumberOfEvents = parameters.EventCount,
                        InputFilePath = null,
                        PhantomAware = !DG,
                        PostInitializationInsert = true,
                        PostPhantomRealization = false,
                        ImmediateReaction = IR || IRC,
                        Reassignment = parameters.ExpTypes[i].Reassignment,
                        DeficitFix = parameters.ExpTypes[i].DeficitFix,
                        LazyAdjustment = parameters.ExpTypes[i].LazyAdjustment,
                        PrintOutEachStep = false,
                        FeedType = FeedTypeEnum.SerialExperiment,
                        CommunityAware = IRC || PCADG,
                        Alpha = parameters.AlphaValue,
                        AlgorithmName = ConvertToString(algorithmEnum),
                        Parameters = parameters,
                        CommunityFix = parameters.ExpTypes[i].CommunityFix,
                        OutputType = parameters.OutputType,
                        Swap = parameters.ExpTypes[i].Swap,
                        SwapThreshold = parameters.ExpTypes[i].SwapThreshold,
                        PreservePercentage = parameters.ExpTypes[i].PreservePercentage,
                        Asymmetric = parameters.Asymmetric,
                        ReuseDisposedPairs = parameters.ExpTypes[i].ReuseDisposedPairs
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

        private List<SGConf> ShowMenu(Parameters parameters)
        {
            Console.ForegroundColor = ConsoleColor.White;
            int algInt = 0;

            FeedTypeEnum feedType = FeedTypeEnum.Random;
            string inputFilePath = null;
            int numberOfExperimentTypes = 1;
            var configs = new List<SGConf>();
            while (true)
            {
                Console.WriteLine(" ---------Choose Input--------- ");
                Console.WriteLine("|1.RANDOM                      |");
                Console.WriteLine("|2.Original Experiment         |");
                Console.WriteLine("|3.Example1                    |");
                Console.WriteLine("|4.From Excel File             |");
                Console.WriteLine("|5.From Text File              |");
                Console.WriteLine("|6.Serial Experiments          |");
                Console.WriteLine(" ------------------------------ ");
                Console.WriteLine();
                Console.Write("Type your choice: ");
                var input = Console.ReadLine();
                var inputInt = 1;
                if (int.TryParse(input, out inputInt) && inputInt >= 1 && inputInt <= 6)
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
                            feedType = FeedTypeEnum.TextFile;
                            Console.Write("Enter File Name:");
                            inputFilePath = Console.ReadLine();
                            break;
                        case 6:
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
                    var conf = new LAConf()
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
                        Parameters = parameters,
                        OutputType = OutputTypeEnum.Text
                    };
                    configs.Add(conf);
                }
            }
            else if (algInt == 2)
            {
                for (int i = 0; i < numberOfExperimentTypes; i++)
                {
                    var conf = new SGConf
                    {
                        NumberOfUsers = 500,
                        NumberOfEvents = 50,
                        InputFilePath = inputFilePath,
                        FeedType = feedType,
                        Alpha = parameters.AlphaValue,
                        OutputType = OutputTypeEnum.Text
                    };
                    configs.Add(conf);
                }
            }
            else if (algInt == 1)
            {
                for (int i = 0; i < numberOfExperimentTypes;)
                {
                    CADGConf conf = new CADGConf();
                    Console.WriteLine(" ---Choose Algorithm Options--- ");
                    Console.WriteLine("|1.Phantom Awareness           |");
                    Console.WriteLine("|2.Post Initialization Insert  |");
                    Console.WriteLine("|3.Immediate Reaction          |");
                    Console.WriteLine("|4.Dynamic Reassignment        |");
                    Console.WriteLine("|5.Deficit Fix                 |");
                    Console.WriteLine("|6.Agile Adjustment            |");
                    Console.WriteLine("|7.Community Aware             |");
                    Console.WriteLine("|8.Community Fix               |");
                    Console.WriteLine("|9.Print Stack                 |");
                    Console.WriteLine("|10.Pure                       |");
                    Console.WriteLine(" ------------------------------ ");
                    Console.WriteLine();
                    Console.Write("Type your choice: ");
                    var options = 1;
                    var input = Console.ReadLine();
                    if (input != null && int.TryParse(input, out options) && options >= 1 && options <= 87654321)
                    {
                        conf = new CADGConf
                        {
                            NumberOfUsers = parameters.UserCount,
                            NumberOfEvents = parameters.EventCount,
                            InputFilePath = inputFilePath,
                            PhantomAware = input.Contains("1"),
                            PostInitializationInsert = input.Contains("2"),
                            ImmediateReaction = input.Contains("3"),
                            Reassignment = input.Contains("4") ? AlgorithmSpec.ReassignmentEnum.Default : AlgorithmSpec.ReassignmentEnum.Greedy,
                            DeficitFix = input.Contains("5"),
                            LazyAdjustment = !input.Contains("6"),
                            CommunityAware = input.Contains("7"),
                            CommunityFix = input.Contains("8") ? CommunityFixEnum.Version1 : CommunityFixEnum.None,
                            PrintOutEachStep = input.Contains("9"),
                            FeedType = feedType,
                            Alpha = parameters.AlphaValue,
                            AlgorithmName = "",
                            OutputType = OutputTypeEnum.Text
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

        private void Print(List<UserEvent> result, Welfare welfare, Stopwatch watch)
        {
            Console.WriteLine("Date: {0}", DateTime.Now.ToString("HH:mm:ss"));
            Console.WriteLine("Exection Time: {0}ms", watch.ElapsedMilliseconds);
            Console.WriteLine("Social Welfare: {0}", welfare.SocialWelfare);
            Console.WriteLine("Innate Welfare: {0}", welfare.InnateWelfare);
            Console.WriteLine("Total Welfare: {0}", welfare.TotalWelfare);
            Console.WriteLine();
        }

        private void Run(int round, Algorithm<List<UserEvent>> alg, FileInfo output, AlgorithmSpec.AlgorithmEnum algorithmEnum)
        {
            Console.WriteLine("....Round {0}-{1}....", round, ConvertToString(algorithmEnum));
            alg.Initialize();
            var watch = alg.Execute(output);
            var result = alg.CreateOutput(output);
            Print(result, alg.Welfare, watch);
        }

        public string ConvertToString(Enum eff)
        {
            return Enum.GetName(eff.GetType(), eff);
        }
    }
}
