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
            var welfareEventChart = ws.Drawings.AddChart("chart1", eChartType.ColumnClustered);
            var innateWelfareEventChart = ws.Drawings.AddChart("chart2", eChartType.ColumnClustered);
            var socialWelfareEventChart = ws.Drawings.AddChart("chart3", eChartType.ColumnClustered);
            var regretEventChart = ws.Drawings.AddChart("chart4", eChartType.ColumnClustered);
            var execTimeChart = ws.Drawings.AddChart("chart5", eChartType.ColumnClustered);

            var col = 1;
            ws.Cells[1, col].Value = "User Count";
            col++;
            ws.Cells[1, col].Value = "Event Count";
            col++;
            ws.Cells[1, col].Value = "SocialNetworkModel";
            col++;
            ws.Cells[1, col].Value = "NetworkDensity";
            col++;
            ws.Cells[1, col].Value = "MinCardinalityOption";

            var rows = welfares.Count + 1;
            for (int i = 0; i < welfares.Count; i++)
            {
                var horizontalFactor = 1;
                /*if (i == 0 && welfares[i].Count > 1)
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
                }*/

                col = 1;
                ws.Cells[i + 2, col].Value = welfares[i][0].UserCount;
                col++;
                ws.Cells[i + 2, col].Value = welfares[i][0].EventCount;
                col++;
                ws.Cells[i + 2, col].Value = welfares[i][0].SocialNetworkModel;
                col++;
                ws.Cells[i + 2, col].Value = welfares[i][0].NetworkDensity;
                col++;
                ws.Cells[i + 2, col].Value = welfares[i][0].MinCardinalityOption;
                col++;

                for (int j = 0; j < welfares[i].Count; j++, col++)
                {
                    ws.Cells[1, col].Value = welfares[i][j].Version;
                    ws.Cells[i + 2, col].Value = welfares[i][j].AvgTotalWelfare;
                    if (i == 0)
                    {
                        welfareEventChart.Series.Add(ws.Cells[2, col, rows, col], ws.Cells[2, horizontalFactor, rows, 2]);
                        welfareEventChart.Series[welfareEventChart.Series.Count - 1].HeaderAddress = ws.Cells[1, col];
                    }
                }

                for (int j = 0; j < welfares[i].Count; j++, col++)
                {
                    ws.Cells[1, col].Value = welfares[i][j].Version;
                    ws.Cells[i + 2, col].Value = welfares[i][j].AvgInnatelWelfare;
                    if (i == 0)
                    {
                        innateWelfareEventChart.Series.Add(ws.Cells[2, col, rows, col], ws.Cells[2, horizontalFactor, rows, 2]);
                        innateWelfareEventChart.Series[innateWelfareEventChart.Series.Count - 1].HeaderAddress = ws.Cells[1, col];
                    }
                }

                for (int j = 0; j < welfares[i].Count; j++, col++)
                {
                    ws.Cells[1, col].Value = welfares[i][j].Version;
                    ws.Cells[i + 2, col].Value = welfares[i][j].AvgSocialWelfare;
                    if (i == 0)
                    {
                        socialWelfareEventChart.Series.Add(ws.Cells[2, col, rows, col], ws.Cells[2, horizontalFactor, rows, 2]);
                        socialWelfareEventChart.Series[socialWelfareEventChart.Series.Count - 1].HeaderAddress = ws.Cells[1, col];
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

                for (int j = 0; j < welfares[i].Count; j++, col++)
                {
                    ws.Cells[1, col].Value = welfares[i][j].Version;
                    ws.Cells[i + 2, col].Value = welfares[i][j].AvgExecTime;
                    if (i == 0)
                    {
                        execTimeChart.Series.Add(ws.Cells[2, col, rows, col], ws.Cells[2, horizontalFactor, rows, 2]);
                        execTimeChart.Series[execTimeChart.Series.Count - 1].HeaderAddress = ws.Cells[1, col];
                    }
                }

            }

            welfareEventChart.SetPosition(1, 0, 1, 0);
            innateWelfareEventChart.SetPosition(12, 0, 1, 0);
            socialWelfareEventChart.SetPosition(24, 0, 1, 0);
            regretEventChart.SetPosition(36, 0, 1, 0);
            execTimeChart.SetPosition(48, 0, 1, 0);

            package.Save();
        }
    }
}
