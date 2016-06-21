using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation
{
    public abstract class Algorithm<T>
    {
        public abstract T Run();
        public abstract void Initialize();
        public double SocialWelfare { get; set; }
    }
}
