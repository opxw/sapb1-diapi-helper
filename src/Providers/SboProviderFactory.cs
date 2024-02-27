using System.Collections.Generic;
using System.Linq;

namespace SAPB1.DIAPI.Helper
{
    public class SboProviderFactory : ProviderBase, ISboProviderFactory
    {
        private Dictionary<string, SboProvider> _providers;

        public SboProviderFactory()
        {
            _providers = new Dictionary<string, SboProvider>();
        }

        public void AddProvider(string name, SboProvider provider, bool autoConnect = true)
        {
            _providers.Add(name, provider);
            
            if (autoConnect)
                _providers.ElementAt(_providers.Count - 1).Value.Connect();
        }

        public SboProvider GetProvider(string name)
        {
            return 
                _providers.Where(x => x.Key.Equals(name, System.StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault().Value;
        }

        public SboProvider GetProvider(int index)
        {
            return
                _providers.Values.ElementAt(index);
        }

        protected override void Dispose(bool disposing)
        {
            if (_providers.Count > 0)
            {
                foreach (var provider in _providers)
                    provider.Value.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}