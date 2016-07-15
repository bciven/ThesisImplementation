using System.IO;

namespace Implementation
{
    public abstract class Algorithm<T>
    {
        public abstract void Run();
        public abstract void Initialize();
        public abstract T CreateOutput(FileInfo file);
        public abstract void SetInputFile(string file);
        public abstract string GetInputFile();
        public double SocialWelfare { get; set; }
    }
}
