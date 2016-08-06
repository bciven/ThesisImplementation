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
        private readonly List<List<AlgorithmWelfare>> _welfares;

        public Charts()
        {
            InitializeComponent();
            listBox.DisplayMember = "Text";
            listBox.ValueMember = "Id";
            _welfares = new List<List<AlgorithmWelfare>>();
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
            listBox.Items.Add(new Item { Id = _index++, Text = textBoxFolder.Text });
        }

        private void WriteOutput(List<string> files)
        {
            buttonAdd.Enabled = false;
            foreach (var file in files)
            {
                var stats = ReadData.CalcAverageWelfares(file);
                _welfares.Add(stats);
            }
            MessageBox.Show("Job Completed!");
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            var item = (Item)listBox.SelectedItem;
            listBox.Items.Remove(item);

        }

        private void buttonOutput_Click(object sender, EventArgs e)
        {
            var files = listBox.Items.Cast<Item>().Select(x => x.Text).ToList();
            WriteOutput(files);
            WriteData.Write("", _welfares);
            buttonAdd.Enabled = true;
        }

        private void Charts_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}
