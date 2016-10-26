using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Experiment;
using OfficeOpenXml;

namespace Implementation.Data_Structures
{
    public class CadgConf : SgConf
    {
        public bool ImmediateReaction { get; set; }
        public AlgorithmSpec.ReassignmentEnum Reassignment { get; set; }
        public bool PhantomAware { get; set; }
        public bool DeficitFix { get; set; }
        public bool PostInitializationInsert { get; set; }
        public bool LazyAdjustment { get; set; }
        public int NumberOfPhantomEvents { get; set; }
        public bool CommunityAware { get; set; }
        public CommunityFixEnum CommunityFix { get; set; }
        public bool DoublePriority { get; set; }

        public CadgConf()
        {
            NumberOfUsers = 10;
            NumberOfEvents = 4;
            ImmediateReaction = false;
            Reassignment = AlgorithmSpec.ReassignmentEnum.None;
            PrintOutEachStep = false;
            InputFilePath = null;
            PhantomAware = false;
            DeficitFix = false;
            PostInitializationInsert = false;
            Alpha = 0.5;
            Percision = 7;
            NumberOfPhantomEvents = 0;
            LazyAdjustment = false;
            CommunityAware = false;
            AlgorithmName = null;
            CommunityFix = CommunityFixEnum.None;
            DoublePriority = false;
        }

        protected override void PrintConfigs(ExcelPackage excel, Stopwatch stopwatch)
        {
            var ws = excel.Workbook.Worksheets.Add("Configs");
            int i = 1;
            ws.Cells[i, 1].Value = "Feed Type";
            ws.Cells[i, 2].Value = FeedType;
            i++;
            ws.Cells[i, 1].Value = "Number Of Users";
            ws.Cells[i, 2].Value = NumberOfUsers;
            i++;

            ws.Cells[i, 1].Value = "Number Of Events";
            ws.Cells[i, 2].Value = NumberOfEvents;
            i++;

            ws.Cells[i, 1].Value = "Immediate Reaction";
            ws.Cells[i, 2].Value = ImmediateReaction;
            i++;

            ws.Cells[i, 1].Value = "Reassignment Type";
            ws.Cells[i, 2].Value = Reassignment;
            i++;

            ws.Cells[i, 1].Value = "Print Each Step";
            ws.Cells[i, 2].Value = PrintOutEachStep;
            i++;

            ws.Cells[i, 1].Value = "Input File Path";
            ws.Cells[i, 2].Value = InputFilePath;
            i++;

            ws.Cells[i, 1].Value = "Phantom Aware";
            ws.Cells[i, 2].Value = PhantomAware;
            i++;

            ws.Cells[i, 1].Value = "Deficit Fix";
            ws.Cells[i, 2].Value = DeficitFix;
            i++;

            ws.Cells[i, 1].Value = "Post Initialization Insert";
            ws.Cells[i, 2].Value = PostInitializationInsert;
            i++;

            ws.Cells[i, 1].Value = "Alpha";
            ws.Cells[i, 2].Value = Alpha;
            i++;

            ws.Cells[i, 1].Value = "Percision";
            ws.Cells[i, 2].Value = Percision;
            i++;

            ws.Cells[i, 1].Value = "Lazy Adjustment";
            ws.Cells[i, 2].Value = LazyAdjustment;
            i++;

            ws.Cells[i, 1].Value = "Community Aware";
            ws.Cells[i, 2].Value = CommunityAware;
            i++;

            ws.Cells[i, 1].Value = "Number Of Phantom Events";
            ws.Cells[i, 2].Value = NumberOfPhantomEvents;
            i++;

            ws.Cells[i, 1].Value = "Algorithm Name";
            ws.Cells[i, 2].Value = AlgorithmName;
            i++;

            ws.Cells[i, 1].Value = "Execution Time";
            ws.Cells[i, 2].Value = stopwatch.ElapsedMilliseconds;
            i++;

            ws.Cells[i, 1].Value = "Community Fix";
            ws.Cells[i, 2].Value = CommunityFix;
            i++;

            ws.Cells[i, 1].Value = "Double Priority";
            ws.Cells[i, 2].Value = DoublePriority;
            i++;

            ws.Cells[i, 1].Value = "Swap";
            ws.Cells[i, 2].Value = Swap;
            i++;

            ws.Cells[i, 1].Value = "Swap Threshold";
            ws.Cells[i, 2].Value = SwapThreshold;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
        }

        protected override void PrintConfigs(DirectoryInfo directoryInfo, Stopwatch stopwatch)
        {
            var configsFile = new StreamWriter(Path.Combine(directoryInfo.FullName, OutputFiles.Configs), true);

            configsFile.WriteLine("{0},{1}", "FeedType", FeedType);

            configsFile.WriteLine("{0},{1}", "Number Of Users", NumberOfUsers);

            configsFile.WriteLine("{0},{1}", "Number Of Events", NumberOfEvents);

            configsFile.WriteLine("{0},{1}", "Immediate Reaction", ImmediateReaction);

            configsFile.WriteLine("{0},{1}", "Reassignment Type", Reassignment);

            configsFile.WriteLine("{0},{1}", "Print Each Step", PrintOutEachStep);

            configsFile.WriteLine("{0},{1}", "Input File Path", InputFilePath);

            configsFile.WriteLine("{0},{1}", "Phantom Aware", PhantomAware);

            configsFile.WriteLine("{0},{1}", "Deficit Fix", DeficitFix);

            configsFile.WriteLine("{0},{1}", "Post Initialization Insert", PostInitializationInsert);

            configsFile.WriteLine("{0},{1}", "Alpha", Alpha);

            configsFile.WriteLine("{0},{1}", "Percision", Percision);

            configsFile.WriteLine("{0},{1}", "Lazy Adjustment", LazyAdjustment);

            configsFile.WriteLine("{0},{1}", "Community Aware", CommunityAware);

            configsFile.WriteLine("{0},{1}", "Number Of Phantom Events", NumberOfPhantomEvents);

            configsFile.WriteLine("{0},{1}", "Algorithm Name", AlgorithmName);

            configsFile.WriteLine("{0},{1}", "Execution Time", stopwatch.ElapsedMilliseconds);

            configsFile.WriteLine("{0},{1}", "Community Fix", CommunityFix);

            configsFile.WriteLine("{0},{1}", "Double Priority", DoublePriority);

            configsFile.WriteLine("{0},{1}", "Swap", Swap);

            configsFile.WriteLine("{0},{1}", "Swap Threshold", SwapThreshold);

            PrintAdditionals(configsFile);

            configsFile.Close();
        }
    }
}
