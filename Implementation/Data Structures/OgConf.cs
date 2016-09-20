using OfficeOpenXml;

namespace Implementation.Data_Structures
{
    public class OgConf : SgConf
    {
        public bool CommunityAware { get; set; }
        public bool DoublePriority { get; set; }

        public OgConf()
        {
            CommunityAware = false;
            DoublePriority = false;
        }

        protected override void PrintAdditionals(ExcelWorksheet ws, int i)
        {
            ws.Cells[i, 1].Value = "Community Aware";
            ws.Cells[i, 2].Value = CommunityAware;
            i++;

            ws.Cells[i, 1].Value = "Double Priority";
            ws.Cells[i, 2].Value = DoublePriority;
        }

        public enum PotentialSocialGainEnum
        {
            None = 0,
        }
    }
}