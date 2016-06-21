using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace Implementation.Data_Structures
{
    public class SgConf
    {
        public FeedTypeEnum FeedType { get; set; }
        public int NumberOfUsers { get; set; }
        public int NumberOfEvents { get; set; }
        public bool PrintOutEachStep { get; set; }
        public string InputFilePath { get; set; }
        public double Alpha;
        public int Percision { get; set; }

        public SgConf()
        {
            NumberOfUsers = 10;
            NumberOfEvents = 4;
            PrintOutEachStep = false;
            InputFilePath = null;
            Alpha = 0.5;
            Percision = 7;
        }

        public virtual void Print(ExcelPackage excel)
        {
            var ws = excel.Workbook.Worksheets.Add("Configs");
            int i = 1;
            ws.Cells[i, 1].Value = "FeedType";
            ws.Cells[i, 2].Value = FeedType;
            i++;
            ws.Cells[i, 1].Value = "Number Of Users";
            ws.Cells[i, 2].Value = NumberOfUsers;
            i++;

            ws.Cells[i, 1].Value = "Number Of Events";
            ws.Cells[i, 2].Value = NumberOfEvents;
            i++;

            ws.Cells[i, 1].Value = "Print Each Step";
            ws.Cells[i, 2].Value = PrintOutEachStep;
            i++;

            ws.Cells[i, 1].Value = "Input File Path";
            ws.Cells[i, 2].Value = InputFilePath;
            i++;


            ws.Cells[i, 1].Value = "Alpha";
            ws.Cells[i, 2].Value = Alpha;
            i++;

            ws.Cells[i, 1].Value = "Percision";
            ws.Cells[i, 2].Value = Percision;
            i++;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

        }
    }
}
