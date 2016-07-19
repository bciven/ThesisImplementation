using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ChartMaker
{
    public partial class Charts : Form
    {
        private int _index = 0;

        public Charts()
        {
            InitializeComponent();
            listBox.DisplayMember = "Text";
            listBox.ValueMember = "Id";
            regChart.Titles.Add("Regret Ratio");
            welfareChart.Titles.Add("Welfare Ratio");
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var browser = new FolderBrowserDialog { SelectedPath = Directory.GetCurrentDirectory() };
            var dlgResult = browser.ShowDialog();
            if (dlgResult == DialogResult.OK)
            {
                textBoxFolder.Text = browser.SelectedPath;
            }
        }

        private void buttonDraw_Click(object sender, EventArgs e)
        {
            if (textBoxFolder.Text == "")
            {
                return;
            }
            var stats = ReadData.CalcAverageWelfares(textBoxFolder.Text);
            listBox.Items.Add(new Item { Id = _index, Text = textBoxFolder.Text, Count = stats.Count });
            foreach (var welfare in stats)
            {
                var seriesWelfare = new Series
                {
                    Name = _index.ToString(),
                    Color = Color.Blue,
                    IsVisibleInLegend = false,
                    IsXValueIndexed = false,
                    ChartType = SeriesChartType.Line,
                    Label = welfare.Version
                };
                welfareChart.Series.Add(seriesWelfare);
                seriesWelfare.Points.AddXY(_index, welfare.AvgWelfare);

                var seriesReg = new Series
                {
                    Name = _index.ToString(),
                    Color = Color.Green,
                    IsVisibleInLegend = false,
                    IsXValueIndexed = false,
                    ChartType = SeriesChartType.Line,
                    Label = welfare.Version
                };
                regChart.Series.Add(seriesReg);
                seriesReg.Points.AddXY(_index, welfare.AvgRegRatio);
                _index++;
            }
            welfareChart.Invalidate();
            regChart.Invalidate();
            textBoxFolder.Text = "";
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            var item = (Item)listBox.SelectedItem;
            for (int i = 0; i < item.Count; i++)
            {
                welfareChart.Series.Remove(welfareChart.Series[(item.Id + i).ToString()]);
                regChart.Series.Remove(regChart.Series[(item.Id + i).ToString()]);
            }
            listBox.Items.Remove(item);
            welfareChart.Invalidate();
            regChart.Invalidate();
        }
    }
}
