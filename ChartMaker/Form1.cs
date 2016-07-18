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

namespace ChartMaker
{
    public partial class Charts : Form
    {
        public Charts()
        {
            InitializeComponent();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var browser = new FolderBrowserDialog {SelectedPath = Directory.GetCurrentDirectory()};
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
            var welfares = ReadData.CalcAverageWelfares(textBoxFolder.Text);

            chart.DataSource = welfares;
        }
    }
}
