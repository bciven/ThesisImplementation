using OfficeOpenXml;

namespace Implementation.Data_Structures
{
    public class OgConf : SgConf
    {
        public bool CommunityAware { get; set; }

        public OgConf()
        {
            CommunityAware = false;
        }

        protected override void PrintAdditionals(ExcelWorksheet ws, int i)
        {
            ws.Cells[i, 1].Value = "Community Aware";
            ws.Cells[i, 2].Value = CommunityAware;
        }
    }
}