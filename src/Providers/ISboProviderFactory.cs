using System;

namespace SAPB1.DIAPI.Helper
{
    public interface ISboProviderFactory : IDisposable
    {
        void AddProvider(string name, SboProvider provider, bool autoConnect = true);
        SboProvider GetProvider(string name);
    }
}