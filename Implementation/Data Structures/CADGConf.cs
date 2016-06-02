using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace Implementation.Data_Structures
{
    public class CadgConf
    {
        public FeedTypeEnum FeedType { get; set; }
        public int NumberOfUsers { get; set; }
        public int NumberOfEvents { get; set; }
        public bool ImmediateReaction { get; set; }
        public bool Reassign { get; set; }
        public bool PrintOutEachStep { get; set; }
        public string InputFilePath { get; set; }
        public bool PhantomAware { get; set; }
        public double Alpha;
        public int Percision { get; set; }
        public bool DeficitFix { get; set; }
        public int NumberOfPhantomEvents{ get; set; }

        public CadgConf()
        {
            NumberOfUsers = 10;
            NumberOfEvents = 4;
            ImmediateReaction = false;
            Reassign = false;
            PrintOutEachStep = false;
            InputFilePath = null;
            PhantomAware = false;
            DeficitFix = false;
            Alpha = 0.5;
            Percision = 7;
            NumberOfPhantomEvents = 0;
        }

        public void Print(ExcelPackage excel)
        {
            var ws = excel.Workbook.Worksheets.Add("Configs");
            int i = 1;
            ws.Cells[i, 1].Value = "FeedType";
            ws.Cells[i, 2].Value = FeedType;
            i++;
            ws.Cells[i, 1].Value = "NumberOfUsers";
            ws.Cells[i, 2].Value = NumberOfUsers;
            i++;

            ws.Cells[i, 1].Value = "NumberOfEvents";
            ws.Cells[i, 2].Value = NumberOfEvents;
            i++;

            ws.Cells[i, 1].Value = "CalculateAffectedEvents";
            ws.Cells[i, 2].Value = ImmediateReaction;
            i++;

            ws.Cells[i, 1].Value = "Reassign";
            ws.Cells[i, 2].Value = Reassign;
            i++;

            ws.Cells[i, 1].Value = "PrintOutEachStep";
            ws.Cells[i, 2].Value = PrintOutEachStep;
            i++;

            ws.Cells[i, 1].Value = "InputFilePath";
            ws.Cells[i, 2].Value = InputFilePath;
            i++;

            ws.Cells[i, 1].Value = "PhantomAware";
            ws.Cells[i, 2].Value = PhantomAware;
            i++;

            ws.Cells[i, 1].Value = "DeficitFix";
            ws.Cells[i, 2].Value = DeficitFix;
            i++;


            ws.Cells[i, 1].Value = "Alpha";
            ws.Cells[i, 2].Value = Alpha;
            i++;

            ws.Cells[i, 1].Value = "Percision";
            ws.Cells[i, 2].Value = Percision;
            i++;

            ws.Cells[i, 1].Value = "NumberOfPhantomEvents";
            ws.Cells[i, 2].Value = NumberOfPhantomEvents;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

        }
    }
}
