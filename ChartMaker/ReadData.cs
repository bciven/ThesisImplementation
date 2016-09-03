using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace ChartMaker
{
    public class ReadData
    {
        public List<AlgorithmWelfare> CalcAverageWelfares(string folder)
        {
            var directory = new DirectoryInfo(folder);
            var allFiles = directory.GetFiles();

            var files = allFiles.Where(x => Path.GetExtension(x.Name).ToLower() == ".xlsx" && !x.Attributes.HasFlag(FileAttributes.Hidden));
            var groups = files.GroupBy(x => x.Name.Split('-')[1], (key, g) => g.ToList()).ToList();
            var fResults = new List<AlgorithmWelfare>();
            foreach (var @group in groups)
            {
                var excelPackage = new ExcelPackage(group.ElementAt(0));
                var wb = excelPackage.Workbook;
                var config = ReadConfig(wb);
                excelPackage.Dispose();
                for (int i = 0; i < @group.Count; i++)
                {
                    var file = @group[i];
                    excelPackage = new ExcelPackage(file);
                    wb = excelPackage.Workbook;
                    config.AvgWelfare += ReadSocialWelfare(wb);
                    config.AvgRegRatio += ReadRegRatio(wb);
                    config.Count++;
                    excelPackage.Dispose();
                }
                fResults.Add(config);
            }

            for (int i = 0; i < fResults.Count; i++)
            {
                fResults[i].AvgWelfare = fResults[i].AvgWelfare / fResults[i].Count;
                fResults[i].AvgRegRatio = fResults[i].AvgRegRatio / fResults[i].Count;
            }
            //var maxWelfare = fResults.Max(x => x.AvgWelfare);
            //fResults.ForEach(x => x.AvgWelfare = x.AvgWelfare / maxWelfare);

            return fResults;
        }

        private double ReadSocialWelfare(ExcelWorkbook wb)
        {
            var ws = wb.Worksheets[4];
            var value = ws.Cells[1, 5].Value;
            return Convert.ToDouble(value);
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

        private AlgorithmWelfare ReadConfig(ExcelWorkbook wb)
        {
            var ws = wb.Worksheets[6];
            var welfare = new AlgorithmWelfare();
            var algorithmIndex = 1;
            for (; ; algorithmIndex++)
            {
                if (Convert.ToString(ws.Cells[algorithmIndex, 1].Value) == "Algorithm Name")
                {
                    break;
                }
            }

            welfare.Version = Convert.ToString(ws.Cells[algorithmIndex, 2].Value);
            welfare.Alpha = Convert.ToDouble(ws.Cells[11, 2].Value);
            welfare.UserCount = Convert.ToInt32(ws.Cells[2, 2].Value);
            welfare.EventCount = Convert.ToInt32(ws.Cells[3, 2].Value);
            return welfare;
        }
    }

    public class AlgorithmWelfare
    {
        public string Version { get; set; }
        public double AvgWelfare { get; set; }
        public double AvgRegRatio { get; set; }
        public int UserCount { get; set; }
        public int EventCount { get; set; }
        public double Alpha { get; set; }
        public int Count { get; set; }
    }
}
