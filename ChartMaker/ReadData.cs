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
        public static List<double> CalcAverageWelfares(string folder)
        {
            var files = Directory.GetFiles(folder);
            var groups = files.GroupBy(x => x.Split('-')[0]);
            var averages = new List<double>();
            foreach (var @group in groups)
            {
                var welfares = @group.Select(ReadSocialWelfare).ToList();
                averages.Add(welfares.Average(x => x));
            }
            return averages;
        }

        private static double ReadSocialWelfare(string file)
        {
            ExcelPackage package = new ExcelPackage(new FileInfo(file));
            var ws = package.Workbook.Worksheets["Assignments"];
            var address = ws.Cells.First(x => x.Text == "Social Welfare").Start;
            var value = ws.Cells[address.Row, 2].Value;
            return Convert.ToDouble(value);
        }
    }
}
