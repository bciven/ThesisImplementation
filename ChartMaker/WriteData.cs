using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace ChartMaker
{
    public static class WriteData
    {
        public static void Write(string folder, List<List<AlgorithmWelfare>> welfares)
        {
            var file = new FileInfo(Path.Combine(folder, "chart.xlsx"));
            var package = new ExcelPackage(file);
            var ws = package.Workbook.Worksheets.Add("Chart");
            
            for (int i = 0; i < welfares.Count; i++)
            {
                ws.Cells[1, (2 * i + 1)].Value = welfares[i][0].UserCount;
                ws.Cells[1, (2 * i + 2)].Value = welfares[i][0].EventCount;
                ws.Cells[2, (2 * i + 1)].Value = welfares[i][0].Alpha;

                for (int j = 0; j < welfares[i].Count; j++)
                {
                    ws.Cells[j + 3, (2 * i + 1)].Value = welfares[i][j].Version;
                    ws.Cells[j + 3, (2 * i + 2)].Value = welfares[i][j].AvgWelfare;
                    ws.Cells[j + 3, (2 * i + 3)].Value = welfares[i][j].AvgRegRatio;
                }
                var chart = ws.Drawings.AddChart("chart", eChartType.Line);
                chart.Series.Add(ws.Cells[3, (2*i + 1), 3 + welfares[i].Count, (2*i + 2)], ws.Cells[1, (2*i + 2)]);
            }
            package.Save();
        }
    }
}
