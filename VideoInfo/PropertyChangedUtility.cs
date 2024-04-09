using System.Collections.Generic;
using System.ComponentModel;

namespace VideoInfo
{
    internal static class PropertyChangedUtility
    {
        internal static readonly PropertyChangedEventArgs NullPropertyChanged = new(null);
        internal static PropertyChangedEventArgs GetOrAdd(this Dictionary<string, PropertyChangedEventArgs> dict, string? key)
        {
            if (key is null) return NullPropertyChanged;
            if (dict.TryGetValue(key, out var value)) return value;
            PropertyChangedEventArgs pcea = new(key);
            dict.Add(key, pcea);
            return pcea;
        }
    }
}
