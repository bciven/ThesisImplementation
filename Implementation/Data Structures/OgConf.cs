using OfficeOpenXml;

namespace Implementation.Data_Structures
{
    public class OgConf : SgConf
    {
        public bool CommunityAware { get; set; }
        public bool DoublePriority { get; set; }
        public bool ProbabilisticApproach { get; set; }
        public AlgorithmSpec.ReassignmentEnum Reassignment { get; set; }

        public OgConf()
        {
            CommunityAware = false;
            DoublePriority = false;
            ProbabilisticApproach = false;
            Reassignment = AlgorithmSpec.ReassignmentEnum.None;
        }

        protected override void PrintAdditionals(ExcelWorksheet ws, int i)
        {
            ws.Cells[i, 1].Value = "Community Aware";
            ws.Cells[i, 2].Value = CommunityAware;
            i++;

            ws.Cells[i, 1].Value = "Double Priority";
            ws.Cells[i, 2].Value = DoublePriority;
            i++;


            ws.Cells[i, 1].Value = "Reassignment";
            ws.Cells[i, 2].Value = Reassignment;
            i++;

            ws.Cells[i, 1].Value = "Probabilistic Approach";
            ws.Cells[i, 2].Value = ProbabilisticApproach;
        }

        public enum PotentialSocialGainEnum
        {
            None = 0,
        }
    }
}