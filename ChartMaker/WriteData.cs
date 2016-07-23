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

            var file = new FileInfo(Path.Combine(folder, DateTime.Now.ToFileTime() + ".xlsx"));
            var package = new ExcelPackage(file);
            var ws = package.Workbook.Worksheets.Add("Chart");
            var welfareChart = ws.Drawings.AddChart("chart1", eChartType.Line);
            var regretChart = ws.Drawings.AddChart("chart2", eChartType.Line);

            ws.Cells[1, 1].Value = "User Count";
            ws.Cells[1, 2].Value = "Event Count";
            ws.Cells[1, 3].Value = "Alpha";
            var rows = welfares.Count + 1;
            for (int i = 0; i < welfares.Count; i++)
            {
                ws.Cells[i + 2, 1].Value = welfares[i][0].UserCount;
                ws.Cells[i + 2, 2].Value = welfares[i][0].EventCount;
                ws.Cells[i + 2, 3].Value = welfares[i][0].Alpha;
                var col = 4;
                for (int j = 0; j < welfares[i].Count; j++, col++)
                {
                    ws.Cells[1, col].Value = welfares[i][j].Version;
                    ws.Cells[i + 2, col].Value = welfares[i][j].AvgWelfare;
                    if (i == 0)
                    {
                        welfareChart.Series.Add(ws.Cells[2, col, rows, col], ws.Cells[2, 2, rows, 2]);
                        welfareChart.Series[welfareChart.Series.Count - 1].HeaderAddress = ws.Cells[1, col];
                    }
                }

                for (int j = 0; j < welfares[i].Count; j++, col++)
                {
                    ws.Cells[1, col].Value = welfares[i][j].Version;
                    ws.Cells[i + 2, col].Value = welfares[i][j].AvgWelfare;
                    if (i == 0)
                    {
                        regretChart.Series.Add(ws.Cells[2, col, rows, col], ws.Cells[2, 2, rows, 2]);
                        regretChart.Series[regretChart.Series.Count - 1].HeaderAddress = ws.Cells[1, col];
                    }
                }

            }
            welfareChart.SetPosition(1, 0, ws.Dimension.End.Column + 1, 0);
            //welfareChart.SetSize(400,400);
            regretChart.SetPosition(ws.Dimension.End.Row + 1, 0, ws.Dimension.End.Column + 1, 0);
            //regretChart.SetSize(400,400);

            package.Save();
        }
    }
}
