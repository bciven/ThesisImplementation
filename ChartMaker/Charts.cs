using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
            textBoxFolder.Text = "";
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
            listBox.Items.Clear();
        }

        private void buttonOutput_Click(object sender, EventArgs e)
        {
            var folders = listBox.Items.Cast<Item>().Select(x => x.Text).ToList();
            WriteOutput(folders);
            var firstFolder = folders.First();
            var parent = new DirectoryInfo(firstFolder).Parent;
            WriteData.Write(parent.FullName, _welfares);
            buttonAdd.Enabled = true;
        }

        private void Charts_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void locationbtn_Click(object sender, EventArgs e)
        {
            var reader = new StreamReader(File.OpenRead(@"final.csv"));
            var fileVan = new System.IO.StreamWriter("van.csv");
            var fileChi = new System.IO.StreamWriter("chi.csv");

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line.Contains("van"))
                {
                    fileVan.WriteLine(line);
                }
                else if (line.Contains("chi"))
                {
                    fileChi.WriteLine(line);
                }
            }
            reader.Close();
            fileVan.Close();
            fileChi.Close();
            //TagLocation();
        }

        private static void TagLocation()
        {
            var reader = new StreamReader(File.OpenRead(@"D:\Dataset\Plancast_geo\van_chi_id_geo.csv"));
            var list = new List<Tuple<int, double, double>>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                list.Add(new Tuple<int, double, double>(int.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2])));
            }
            reader.Close();
            var file = new System.IO.StreamWriter("final.csv");
            int i = 0;
            foreach (var value in list)
            {
                i++;
                //var url = string.Format("https://maps.googleapis.com/maps/api/geocode/json?latlng={0},{1}&key={2}", value.Item3, value.Item2, "AIzaSyCAlADj7yFYncBdeDsQKy4vKzmoALFwe24");
                var url =
                    string.Format(
                        "http://dev.virtualearth.net/REST/v1/Locations/{0},{1}?includeEntityTypes=PopulatedPlace&o=xml&includeNeighborhood=1&key={2}&incl=ciso2",
                        value.Item3, value.Item2, "At-B7ze33uS9pAQ0hkZQ-YI4FDtvKJVTo5HshrGncPX6JXT6fQ-ltmT2UErJ_VX5");
                var request = WebRequest.Create(url);
                var response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                responseFromServer = responseFromServer.ToLower();
                var van = responseFromServer.Contains("vancouver");
                var chi = responseFromServer.Contains("chicago");
                if (van || chi)
                {
                    file.WriteLine(value.Item1 + (van ? ",van" : ",chi") + "," + value.Item3 + "," + value.Item2);
                }
                reader.Close();
                response.Close();
            }
            file.Close();
        }
    }
}
