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
            var welfareEventChart = ws.Drawings.AddChart("chart1", eChartType.Line);
            var regretEventChart = ws.Drawings.AddChart("chart2", eChartType.Line);

            ws.Cells[1, 1].Value = "User Count";
            ws.Cells[1, 2].Value = "Event Count";
            ws.Cells[1, 3].Value = "Alpha";
            var rows = welfares.Count + 1;
            for (int i = 0; i < welfares.Count; i++)
            {
                var horizontalFactor = 1;
                if (i == 0 && welfares[i].Count > 1)
                {
                    if (welfares[i][0].UserCount != welfares[i][1].UserCount)
                    {
                        horizontalFactor = 1;
                        welfareEventChart.Title.Text = "Number of Users/Welfare Ratio";
                        regretEventChart.Title.Text = "Number of Users/Regret Ratio";
                    }
                    else if (welfares[i][0].EventCount != welfares[i][1].EventCount)
                    {
                        horizontalFactor = 2;
                        welfareEventChart.Title.Text = "Number of Events/Welfare Ratio";
                        regretEventChart.Title.Text = "Number of Events/Regret Ratio";
                    }
                    else if (welfares[i][0].Alpha != welfares[i][1].Alpha)
                    {
                        horizontalFactor = 3;
                        welfareEventChart.Title.Text = "Alpha/Welfare Ratio";
                        regretEventChart.Title.Text = "Alpha/Regret Ratio";
                    }
                }
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
                        welfareEventChart.Series.Add(ws.Cells[2, col, rows, col], ws.Cells[2, horizontalFactor, rows, 2]);
                        welfareEventChart.Series[welfareEventChart.Series.Count - 1].HeaderAddress = ws.Cells[1, col];
                    }
                }

                for (int j = 0; j < welfares[i].Count; j++, col++)
                {
                    ws.Cells[1, col].Value = welfares[i][j].Version;
                    ws.Cells[i + 2, col].Value = welfares[i][j].AvgRegRatio;
                    if (i == 0)
                    {
                        regretEventChart.Series.Add(ws.Cells[2, col, rows, col], ws.Cells[2, horizontalFactor, rows, 2]);
                        regretEventChart.Series[regretEventChart.Series.Count - 1].HeaderAddress = ws.Cells[1, col];
                    }
                }

            }
            welfareEventChart.SetPosition(1, 0, ws.Dimension.End.Column + 1, 0);
            regretEventChart.SetPosition(12, 0, ws.Dimension.End.Column + 1, 0);

            package.Save();
        }
    }
}
