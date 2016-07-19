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
            var groups = files.GroupBy(x => x.Name.Split('-')[0]).ToList();
            var fResults = new List<AlgorithmWelfare>();
            foreach (var @group in groups)
            {
                var welfares = @group.Select(ReadSocialWelfare).ToList();
                var regRatios = @group.Select(ReadRegRatio).ToList();
                if (fResults.Count == 0)
                {
                    for (int i = 0; i < @group.Count(); i++)
                    {
                        var version = ReadVersion(@group.ElementAt(i));
                        fResults.Add(new AlgorithmWelfare() { Version = version, AvgWelfare = welfares[i], AvgRegRatio = regRatios[i] });
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
            return fResults;
        }

        private static double ReadSocialWelfare(FileInfo file)
        {
            ExcelPackage package = new ExcelPackage(file);
            var ws = package.Workbook.Worksheets[4];
            var value = ws.Cells[ws.Dimension.End.Row, 2].Value;
            package.Dispose();
            return Convert.ToDouble(value);
        }

        private static double ReadRegRatio(FileInfo file)
        {
            ExcelPackage package = new ExcelPackage(file);
            var ws = package.Workbook.Worksheets[5];
            var cells = ws.Cells[1, 2, ws.Dimension.End.Row, 2];
            var avg = 0d;
            foreach (var cell in cells)
            {
                avg += Convert.ToDouble(cell.Value);
            }
            avg = avg/cells.Count();
            package.Dispose();
            return avg;
        }

        private static int ReadEventCount(FileInfo file)
        {
            ExcelPackage package = new ExcelPackage(file);
            var ws = package.Workbook.Worksheets[6];
            var value = ws.Cells[3, 2].Value;
            package.Dispose();
            return Convert.ToInt32(value);
        }

        private static string ReadVersion(FileInfo file)
        {
            ExcelPackage package = new ExcelPackage(file);
            var ws = package.Workbook.Worksheets[6];
            var values = ws.Cells[4, 2, 10, 2];
            var numberTrue = 0;
            foreach (var value in values)
            {
                if (value.Text == "1")
                {
                    numberTrue++;
                }
            }
            package.Dispose();
            return numberTrue > 2 ? "Imediate Reaction" : "Original";
        }
    }

    public class AlgorithmWelfare
    {
        public string Version { get; set; }
        public double AvgWelfare { get; set; }
        public double AvgRegRatio { get; set; }
    }
}
