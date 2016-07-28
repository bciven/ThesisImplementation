using System.IO;
using Implementation.Data_Structures;

namespace Implementation
{
    public abstract class Algorithm<T>
    {
        public abstract void Run();
        public abstract void Initialize();
        public abstract T CreateOutput(FileInfo file);
        public abstract string GetInputFile();
        public abstract FeedTypeEnum GetFeedType();
        public double SocialWelfare { get; set; }
    }
}
