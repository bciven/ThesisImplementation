using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace ChartMaker
{
    public static class ReadData
    {
        public static List<AlgorithmWelfare> CalcAverageWelfares(string folder)
        {
            var directory = new DirectoryInfo(folder);
            var allFiles = directory.GetFiles();

            var files = allFiles.Where(x => Path.GetExtension(x.Name).ToLower() == ".xlsx" && !x.Attributes.HasFlag(FileAttributes.Hidden));
            var groups = files.GroupBy(x => x.Name.Split('-')[0], (key, g) => g.Select(x => new ExcelPackage(x)).ToList()).ToList();
            var fResults = new List<AlgorithmWelfare>();
            foreach (var @group in groups)
            {
                var welfares = @group.Select(ReadSocialWelfare).ToList();
                var regRatios = @group.Select(ReadRegRatio).ToList();
                if (fResults.Count == 0)
                {
                    for (int i = 0; i < @group.Count(); i++)
                    {
                        var config = ReadConfig(@group.ElementAt(i));
                        config.AvgWelfare = welfares[i];
                        config.AvgRegRatio = regRatios[i];
                        fResults.Add(config);
                    }
                    continue;
                }

                for (int i = 0; i < welfares.Count; i++)
                {
                    fResults[i].AvgWelfare += welfares[i];
                    fResults[i].AvgRegRatio += regRatios[i];
                }
            }

            for (int i = 0; i < fResults.Count; i++)
            {
                fResults[i].AvgWelfare = fResults[i].AvgWelfare / groups.Count();
                fResults[i].AvgRegRatio = fResults[i].AvgRegRatio / groups.Count();
            }
            var maxWelfare = fResults.Max(x => x.AvgWelfare);
            fResults.ForEach(x => x.AvgWelfare = x.AvgWelfare / maxWelfare);
            return fResults;
        }

        private static double ReadSocialWelfare(ExcelPackage package)
        {
            var ws = package.Workbook.Worksheets[4];
            var value = ws.Cells[1, 5].Value;
            return Convert.ToDouble(value);
        }

        private static double ReadRegRatio(ExcelPackage package)
        {
            var ws = package.Workbook.Worksheets[5];
            var cells = ws.Cells[1, 2, ws.Dimension.End.Row, 2];
            var avg = 0d;
            foreach (var cell in cells)
            {
                avg += Convert.ToDouble(cell.Value);
            }
            avg = avg / cells.Count();
            return avg;
        }

        private static AlgorithmWelfare ReadConfig(ExcelPackage package)
        {
            var ws = package.Workbook.Worksheets[6];
            var welfare = new AlgorithmWelfare();
            welfare.Version = Convert.ToString(ws.Cells[16, 2].Value);
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
    }
}
