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
    public class SgConf
    {
        public FeedTypeEnum FeedType { get; set; }
        public int NumberOfUsers { get; set; }
        public int NumberOfEvents { get; set; }
        public bool PrintOutEachStep { get; set; }
        public string InputFilePath { get; set; }
        public double Alpha;
        public int Percision { get; set; }
        public int NumberOfExperimentTypes { get; set; }
        public string AlgorithmName { get; set; }
        public Parameters Parameters { get; set; }
        public OutputTypeEnum OutputType { get; set; }

        public SgConf()
        {
            NumberOfUsers = 10;
            NumberOfEvents = 4;
            PrintOutEachStep = false;
            InputFilePath = null;
            Alpha = 0.5;
            Percision = 10;
            NumberOfExperimentTypes = 1;
            AlgorithmName = null;
            Parameters = null;
            OutputType = OutputTypeEnum.Excel;
        }

        public void PrintToExcel(ExcelPackage excel, Stopwatch stopwatch)
        {
            PrintConfigs(excel, stopwatch);
            PrintParameters(excel);
        }

        public void PrintToText(DirectoryInfo directory, Stopwatch stopwatch)
        {
            PrintConfigs(directory, stopwatch);
            PrintParameters(directory);
        }

        protected virtual void PrintConfigs(ExcelPackage excel, Stopwatch stopwatch)
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
            i++;

            PrintAdditionals(ws, i);

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
        }

        protected virtual void PrintConfigs(DirectoryInfo directoryInfo, Stopwatch stopwatch)
        {
            var configsFile = new StreamWriter(Path.Combine(directoryInfo.FullName, OutputFiles.Configs), true);

            configsFile.WriteLine("{0},{1}", "FeedType", FeedType);

            configsFile.WriteLine("{0},{1}", "Number Of Users", NumberOfUsers);

            configsFile.WriteLine("{0},{1}", "Number Of Events", NumberOfEvents);

            configsFile.WriteLine("{0},{1}", "Print Each Step", PrintOutEachStep);

            configsFile.WriteLine("{0},{1}", "Input File Path", InputFilePath);

            configsFile.WriteLine("{0},{1}", "Alpha", Alpha);

            configsFile.WriteLine("{0},{1}", "Percision", Percision);

            configsFile.WriteLine("{0},{1}", "Algorithm Name", AlgorithmName);

            configsFile.WriteLine("{0},{1}", "Execution Time", stopwatch.ElapsedMilliseconds);

            PrintAdditionals(configsFile);

            configsFile.Close();
        }

        protected virtual void PrintAdditionals(ExcelWorksheet ws, int i)
        {
        }

        protected virtual void PrintAdditionals(StreamWriter streamWriter)
        {
        }

        protected void PrintParameters(ExcelPackage excel)
        {
            if (Parameters == null)
            {
                return;
            }

            var ws = excel.Workbook.Worksheets.Add("Parameters");
            int i = 1;
            ws.Cells[i, 1].Value = "SndensityValue";
            ws.Cells[i, 2].Value = Parameters.SndensityValue;
            i++;

            ws.Cells[i, 1].Value = "CapVarValue";
            ws.Cells[i, 2].Value = Parameters.CapVarValue;
            i++;

            ws.Cells[i, 1].Value = "CapmeanValue";
            ws.Cells[i, 2].Value = Parameters.CapmeanValue;
            i++;

            ws.Cells[i, 1].Value = "EventInterestPerctValue";
            ws.Cells[i, 2].Value = Parameters.EventInterestPerctValue;
            i++;

            ws.Cells[i, 1].Value = "SocialNetworkModel";
            ws.Cells[i, 2].Value = Parameters.SocialNetworkModel;
            i++;

            ws.Cells[i, 1].Value = "MinCardinalityOption";
            ws.Cells[i, 2].Value = Parameters.MinCardinalityOption;
        }

        protected void PrintParameters(DirectoryInfo directoryInfo)
        {
            if (Parameters == null)
            {
                return;
            }

            var parametersFile = new StreamWriter(Path.Combine(directoryInfo.FullName, OutputFiles.Parameters), true);

            parametersFile.WriteLine("{0},{1}", "SndensityValue", Parameters.SndensityValue);

            parametersFile.WriteLine("{0},{1}", "CapVarValue", Parameters.CapVarValue);

            parametersFile.WriteLine("{0},{1}", "CapmeanValue", Parameters.CapmeanValue);

            parametersFile.WriteLine("{0},{1}", "EventInterestPerctValue", Parameters.EventInterestPerctValue);

            parametersFile.WriteLine("{0},{1}", "SocialNetworkModel", Parameters.SocialNetworkModel);

            parametersFile.WriteLine("{0},{1}", "MinCardinalityOption", Parameters.MinCardinalityOption);

            parametersFile.Close();
        }
    }
}
