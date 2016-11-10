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
    public class LAConf : SGConf
    {
        public bool Reassign { get; set; }

        public InitStrategyEnum InitStrategyEnum { get; set; }
        
        public LAConf()
        {
            NumberOfUsers = 10;
            NumberOfEvents = 4;
            Reassign = false;
            PrintOutEachStep = false;
            InputFilePath = null;
            Alpha = 0.5;
            Percision = 7;
            AlgorithmName = null;
            InitStrategyEnum = InitStrategyEnum.RandomSort;
        }

        protected override void PrintConfigs(ExcelPackage excel, Stopwatch stopwatch)
        {
            PrintConfig(excel, stopwatch);
        }

        protected override void PrintConfigs(DirectoryInfo directoryInfo, Stopwatch stopwatch)
        {
            PrintConfig(directoryInfo, stopwatch);
        }

        protected StreamWriter PrintConfig(DirectoryInfo directoryInfo, Stopwatch stopwatch)
        {
            var configsFile = new StreamWriter(Path.Combine(directoryInfo.FullName, OutputFiles.Configs), true);

            configsFile.WriteLine("{0},{1}", "FeedType", FeedType);

            configsFile.WriteLine("{0},{1}", "Number Of Users", NumberOfUsers);

            configsFile.WriteLine("{0},{1}", "Number Of Events", NumberOfEvents);

            configsFile.WriteLine("{0},{1}", "Reassign", Reassign);

            configsFile.WriteLine("{0},{1}", "Print Each Step", PrintOutEachStep);

            configsFile.WriteLine("{0},{1}", "Input File Path", InputFilePath);

            configsFile.WriteLine("{0},{1}", "Alpha", Alpha);

            configsFile.WriteLine("{0},{1}", "Percision", Percision);

            configsFile.WriteLine("{0},{1}", "Algorithm Name", AlgorithmName);

            configsFile.WriteLine("{0},{1}", "Execution Time", stopwatch.ElapsedMilliseconds);

            PrintAdditionals(configsFile);

            configsFile.Close();

            return configsFile;
        }

        protected ExcelWorksheet PrintConfig(ExcelPackage excel, Stopwatch stopwatch)
        {
            var ws = excel.Workbook.Worksheets.Add("Configs");
            int i = 1;
            ws.Cells[i, 1].Value = "FeedType";
            ws.Cells[i, 2].Value = FeedType;
            i++;
            ws.Cells[i, 1].Value = "Number Of Users";
            ws.Cells[i, 2].Value = NumberOfUsers;
            i++;

            ws.Cells[i, 1].Value = "Number Of Events";
            ws.Cells[i, 2].Value = NumberOfEvents;
            i++;

            ws.Cells[i, 1].Value = "Reassign";
            ws.Cells[i, 2].Value = Reassign;
            i++;

            ws.Cells[i, 1].Value = "Print Each Step";
            ws.Cells[i, 2].Value = PrintOutEachStep;
            i++;

            ws.Cells[i, 1].Value = "Input File Path";
            ws.Cells[i, 2].Value = InputFilePath;
            i++;

            ws.Cells[i, 1].Value = "Alpha";
            ws.Cells[i, 2].Value = Alpha;
            i++;

            ws.Cells[i, 1].Value = "Percision";
            ws.Cells[i, 2].Value = Percision;
            i++;

            ws.Cells[i, 1].Value = "Algorithm Name";
            ws.Cells[i, 2].Value = AlgorithmName;
            i++;

            ws.Cells[i, 1].Value = "Execution Time";
            ws.Cells[i, 2].Value = stopwatch.ElapsedMilliseconds;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return ws;
        }
    }
}
