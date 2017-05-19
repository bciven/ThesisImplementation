using Implementation.Dataset_Reader;

namespace Implementation.Data_Structures
{
    public class DistDataParams
    {
        public int CapacityMean { get; set; }
        public int CapacityVariance { get; set; }
        public MinCardinalityOptions MinCardinalityOption { get; set; }
        public MaxCardinalityOptions MaxCardinalityOption { get; set; }
        public SocialNetworkModel SocialNetworkModel { get; set; }
        public double SocialNetworkDensity { get; set; }
        public double EventInterestPerct { get; set; }
        public double Exponent { get; set; }
        public int MinDegree { get; internal set; }
        public int SocialNetworkGraphMaxDegree { get; set; }
        public ExtrovertIndexEnum ExtrovertIndexModel { get; set; }
    }
}