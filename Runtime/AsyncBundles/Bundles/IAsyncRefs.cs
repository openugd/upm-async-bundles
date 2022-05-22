using System.Collections.Generic;

namespace OpenUGD.AsyncBundles.Bundles
{
    public interface IAsyncRefs
    {
        IEnumerable<object> Refs { get; }
        string Name { get; }
    }
}
