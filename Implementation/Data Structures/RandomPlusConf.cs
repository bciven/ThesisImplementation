using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Implementation.Experiment;
using OfficeOpenXml;

namespace Implementation.Data_Structures
{
    public class RandomPlusConf : RandomConf
    {
        public int TakeChanceLimit { get; set; }

        protected override void PrintConfigs(ExcelPackage excel, Stopwatch stopwatch)
        {
            var ws = PrintConfig(excel, stopwatch);
            var i = ws.Dimension.Rows + 1;
            ws.Cells[i, 1].Value = "TakeChanceLimit";
            ws.Cells[i, 2].Value = TakeChanceLimit;
        }
    }
}
