using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;
using OfficeOpenXml;

namespace ChartMaker
{
    public class ReadData
    {
        public List<AlgorithmWelfare> CalcAverageWelfares(string folder)
        {
            var directory = new DirectoryInfo(folder);
            var allFiles = directory.GetFiles();
            if (allFiles.Any(x => x.Extension.Contains("xlsx")))
            {
                return Read(allFiles);
            }
            var allDirectories = directory.GetDirectories();
            return Read(allDirectories);
        }

        private List<AlgorithmWelfare> Read(FileInfo[] allFiles)
        {
            var files = allFiles.Where(x => Path.GetExtension(x.Name).ToLower() == ".xlsx" && !x.Attributes.HasFlag(FileAttributes.Hidden));
            var groups = files.GroupBy(x => x.Name.Split('-')[1], (key, g) => g.ToList()).ToList();
            var fResults = new List<AlgorithmWelfare>();
            foreach (var @group in groups)
            {
                var excelPackage = new ExcelPackage(@group.ElementAt(0));
                var wb = excelPackage.Workbook;
                var config = ReadConfig(wb);
                excelPackage.Dispose();
                for (int i = 0; i < @group.Count; i++)
                {
                    var file = @group[i];
                    excelPackage = new ExcelPackage(file);
                    wb = excelPackage.Workbook;
                    config.AvgTotalWelfare += ReadTotalWelfare(wb);
                    config.AvgSocialWelfare += ReadSocialWelfare(wb);
                    config.AvgInnatelWelfare += ReadInnateWelfare(wb);
                    config.AvgRegRatio += ReadRegRatio(wb);
                    config.Count++;
                    excelPackage.Dispose();
                }
                fResults.Add(config);
            }

            TakeAverage(fResults);

            return fResults;
        }

        private List<AlgorithmWelfare> Read(DirectoryInfo[] allDirectories)
        {
            var groups = allDirectories.GroupBy(x => x.Name.Split('-')[1], (key, g) => g.ToList()).ToList();
            var fResults = new List<AlgorithmWelfare>();
            foreach (var @group in groups)
            {
                var directory = new DirectoryInfo(@group.ElementAt(0).FullName);
                var config = ReadConfig(directory);
                for (int i = 0; i < @group.Count; i++)
                {
                    var file = @group[i];
                    directory = new DirectoryInfo(file.FullName);
                    var configLines = File.ReadAllLines(Path.Combine(directory.FullName, OutputFiles.Welfare));
                    config.AvgTotalWelfare += ReadTotalWelfare(configLines);
                    config.AvgSocialWelfare += ReadSocialWelfare(configLines);
                    config.AvgInnatelWelfare += ReadInnateWelfare(configLines);
                    config.AvgRegRatio += ReadRegRatio(directory);
                    config.Count++;
                }
                fResults.Add(config);
            }

            TakeAverage(fResults);

            return fResults;
        }

        private static void TakeAverage(List<AlgorithmWelfare> fResults)
        {
            foreach (AlgorithmWelfare welfare in fResults)
            {
                welfare.AvgTotalWelfare = welfare.AvgTotalWelfare / welfare.Count;
                welfare.AvgInnatelWelfare = welfare.AvgInnatelWelfare / welfare.Count;
                welfare.AvgSocialWelfare = welfare.AvgSocialWelfare / welfare.Count;
                welfare.AvgRegRatio = welfare.AvgRegRatio / welfare.Count;
                welfare.AvgExecTime = Math.Round(welfare.AvgExecTime / 1000, 2);
            }
        }

        private double ReadInnateWelfare(ExcelWorkbook wb)
        {
            var ws = wb.Worksheets[4];
            var value = ws.Cells[2, 5].Value;
            return Convert.ToDouble(value);
        }

        private double ReadInnateWelfare(string[] lines)
        {
            foreach (var line in lines)
            {
                if (line.Contains("Innate"))
                {
                    return CsvReader.ReadDoubleValue(line, 1);
                }
            }

            throw new ArgumentException("Argument is not available");
        }

        private double ReadSocialWelfare(ExcelWorkbook wb)
        {
            var ws = wb.Worksheets[4];
            var value = ws.Cells[3, 5].Value;
            return Convert.ToDouble(value);
        }

        private double ReadSocialWelfare(string[] lines)
        {
            foreach (var line in lines)
            {
                if (line.Contains("Social"))
                {
                    return CsvReader.ReadDoubleValue(line, 1);
                }
            }

            throw new ArgumentException("Argument is not available");
        }

        private double ReadTotalWelfare(ExcelWorkbook wb)
        {
            var ws = wb.Worksheets[4];
            var value = ws.Cells[1, 5].Value;
            return Convert.ToDouble(value);
        }

        private double ReadTotalWelfare(string[] lines)
        {
            foreach (var line in lines)
            {
                if (line.Contains("Total"))
                {
                    return CsvReader.ReadDoubleValue(line, 1);
                }
            }

            throw new ArgumentException("Argument is not available");
        }

        private double ReadRegRatio(ExcelWorkbook wb)
        {
            var ws = wb.Worksheets[5];
            var cells = ws.Cells[1, 2, ws.Dimension.End.Row, 2];
            var avg = 0d;
            var count = 0;
            foreach (var cell in cells)
            {
                avg += Convert.ToDouble(cell.Value);
                count++;
            }
            avg = avg / count;
            return avg;
        }

        private double ReadRegRatio(DirectoryInfo directory)
        {
            var lines = File.ReadAllLines(Path.Combine(directory.FullName, OutputFiles.RegretRatio));
            var avg = 0d;
            var count = 0;
            foreach (var line in lines)
            {
                avg += CsvReader.ReadDoubleValue(line, 1);
                count++;
            }
            avg = avg / count;
            return avg;
        }

        private AlgorithmWelfare ReadConfig(ExcelWorkbook wb)
        {
            var wsConfigs = wb.Worksheets[6];
            var welfare = new AlgorithmWelfare();
            var algorithmIndex = 1;
            for (; ; algorithmIndex++)
            {
                if (Convert.ToString(wsConfigs.Cells[algorithmIndex, 1].Value) == "Algorithm Name")
                {
                    break;
                }
            }

            welfare.Version = Convert.ToString(wsConfigs.Cells[algorithmIndex, 2].Value);
            welfare.Alpha = Convert.ToDouble(wsConfigs.Cells[11, 2].Value);
            welfare.UserCount = Convert.ToInt32(wsConfigs.Cells[2, 2].Value);
            welfare.EventCount = Convert.ToInt32(wsConfigs.Cells[3, 2].Value);

            var wsParameters = wb.Worksheets["Parameters"];
            if (wsParameters == null)
            {
                return welfare;
            }

            var snDensityIndex = 1;
            for (; ; snDensityIndex++)
            {
                if (Convert.ToString(wsParameters.Cells[snDensityIndex, 1].Value) == "SndensityValue")
                {
                    break;
                }
            }
            welfare.NetworkDensity = Convert.ToDouble(wsParameters.Cells[snDensityIndex, 2].Value);

            var minCardinalityOptionIndex = 1;
            for (; ; minCardinalityOptionIndex++)
            {
                if (Convert.ToString(wsParameters.Cells[minCardinalityOptionIndex, 1].Value) == "MinCardinalityOption")
                {
                    break;
                }
            }
            welfare.MinCardinalityOption = Convert.ToString(wsParameters.Cells[minCardinalityOptionIndex, 2].Value);

            var execTimeIndex = 1;
            for (; ; execTimeIndex++)
            {
                if (Convert.ToString(wsParameters.Cells[execTimeIndex, 1].Value) == "Execution Time")
                {
                    break;
                }
            }
            welfare.AvgExecTime = Convert.ToDouble(wsParameters.Cells[execTimeIndex, 2].Value);

            var socialNetworkModelIndex = 1;
            for (; ; socialNetworkModelIndex++)
            {
                if (Convert.ToString(wsParameters.Cells[socialNetworkModelIndex, 1].Value) == "SocialNetworkModel")
                {
                    break;
                }
            }
            welfare.SocialNetworkModel = Convert.ToString(wsParameters.Cells[socialNetworkModelIndex, 2].Value);

            return welfare;
        }


        private AlgorithmWelfare ReadConfig(DirectoryInfo directory)
        {
            var configLines = File.ReadAllLines(Path.Combine(directory.FullName, OutputFiles.Configs)).ToList();
            var welfare = new AlgorithmWelfare();

            welfare.Version = CsvReader.ReadStringValue(configLines.First(x => x.Contains("Algorithm Name")), 1);
            welfare.Alpha = CsvReader.ReadDoubleValue(configLines.First(x => x.Contains("Alpha")), 1);
            welfare.UserCount = CsvReader.ReadIntValue(configLines.First(x => x.Contains("Number Of Users")), 1);
            welfare.EventCount = CsvReader.ReadIntValue(configLines.First(x => x.Contains("Number Of Events")), 1);
            welfare.AvgExecTime = CsvReader.ReadDoubleValue(configLines.First(x => x.Contains("Execution Time")), 1);

            if (!File.Exists(Path.Combine(directory.FullName, OutputFiles.Parameters)))
            {
                return welfare;
            }
            var parameterLines = File.ReadAllLines(Path.Combine(directory.FullName, OutputFiles.Parameters)).ToList();

            welfare.NetworkDensity = CsvReader.ReadDoubleValue(parameterLines.First(x => x.Contains("SndensityValue")), 1);
            welfare.MinCardinalityOption = CsvReader.ReadStringValue(parameterLines.First(x => x.Contains("MinCardinalityOption")), 1);
            welfare.SocialNetworkModel = CsvReader.ReadStringValue(parameterLines.First(x => x.Contains("SocialNetworkModel")), 1);

            return welfare;
        }
    }

    public class AlgorithmWelfare
    {
        public string Version { get; set; }
        public double AvgTotalWelfare { get; set; }
        public double AvgSocialWelfare { get; set; }
        public double AvgInnatelWelfare { get; set; }
        public double AvgRegRatio { get; set; }
        public int UserCount { get; set; }
        public int EventCount { get; set; }
        public double NetworkDensity { get; set; }
        public double Alpha { get; set; }
        public int Count { get; set; }
        public string MinCardinalityOption { get; set; }
        public string SocialNetworkModel { get; set; }
        public double AvgExecTime { get; internal set; }
    }
}
