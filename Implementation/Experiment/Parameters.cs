
using System.Collections.Generic;
using Implementation.Dataset_Reader;
using Implementation.Data_Structures;

namespace Implementation.Experiment
{
    public class Parameters
    {
        public int ExpCount { get; set; }
        public int UserCount { get; set; }
        public int EventCount { get; set; }
        public List<int?> TakeChanceLimits { get; set; }
        public double AlphaValue { get; set; }
        public double CapmeanValue { get; set; }
        public double CapVarValue { get; set; }
        public double SndensityValue { get; set; }
        public double EventInterestPerctValue { get; set; }
        public string ExperimentFile { get; set; }
        public List<AlgorithmEnum> ExpTypes { get; set; }
        public MinCardinalityOptions MinCardinalityOption { get; set; }
        public SocialNetworkModel SocialNetworkModel { get; set; }
    }
}