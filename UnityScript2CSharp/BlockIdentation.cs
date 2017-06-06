using System;

namespace UnityScript2CSharp
{
    internal class BlockIdentation : IDisposable
    {
        private readonly Writer _identationAware;

        public BlockIdentation(Writer identationAware)
        {
            _identationAware = identationAware;
            _identationAware.Identation++;
        }

        public void Dispose()
        {
            _identationAware.Identation--;
        }
    }
}
