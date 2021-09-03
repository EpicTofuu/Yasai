using System;
using Yasai.Resources;

namespace Yasai.Resources
{
    public interface ILoad : IDisposable
    {
        public bool Loaded { get; }
        public void Load(ContentStore cs);
    }
}