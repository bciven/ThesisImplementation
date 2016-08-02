using Implementation.Dataset_Reader;

namespace Implementation.Data_Structures
{
    public class DistDataParams
    {
        public int CapacityMean { get; set; }
        public int CapacityVariance { get; set; }
        public MinCardinalityOptions MinCardinalityOption { get; set; }
        public SocialNetworkModel SocialNetworkModel { get; set; }
        public double SocialNetworkDensity { get; set; }
    }
}