using OfficeOpenXml;

namespace Implementation.Data_Structures
{
    public class OgConf : SgConf
    {
        public bool CommunityAware { get; set; }
        public bool CommunityFix { get; set; }

        public OgConf()
        {
            CommunityAware = false;
            CommunityFix = false;
        }

        protected override void PrintAdditionals(ExcelWorksheet ws, int i)
        {
            ws.Cells[i, 1].Value = "Community Aware";
            ws.Cells[i, 2].Value = CommunityAware;
            i++;

            ws.Cells[i, 1].Value = "Community Fix";
            ws.Cells[i, 2].Value = CommunityFix;
        }
    }
}