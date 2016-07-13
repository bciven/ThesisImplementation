using System.IO;

namespace Implementation
{
    public abstract class Algorithm<T>
    {
        public abstract void Run();
        public abstract void Initialize();
        public abstract T CreateOutput(FileInfo file);
        public double SocialWelfare { get; set; }
    }
}
