
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
        public bool Asymmetric { get; set; }
        public double AlphaValue { get; set; }
        public double CapmeanValue { get; set; }
        public double CapVarValue { get; set; }
        public double SndensityValue { get; set; }
        public double EventInterestPerctValue { get; set; }
        public string ExperimentFile { get; set; }
        public List<AlgorithmSpec> ExpTypes { get; set; }
        public MinCardinalityOptions MinCardinalityOption { get; set; }
        public SocialNetworkModel SocialNetworkModel { get; set; }
        public MaxCardinalityOptions MaxCardinalityOption { get; set; }
        public OutputTypeEnum OutputType { get; set; }
        public string ExperimentInputFile { get; internal set; }
        public string Title { get; internal set; }
        public double Exponent { get; internal set; }
        public int MinDegree { get; internal set; }
        public ExtrovertIndexEnum ExtrovertIndexModel { get; internal set; }
    }
}