using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    static class Extensions
    {
        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="map">The dictionary from which to remove a value.</param>
        /// <param name="key">The key of the item to remove.</param>
        /// <returns><c>true</c> if the item was removed; otherwise, <c>false</c>.</returns>
        public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> map, TKey key)
        {
            TValue value;
            return map.TryRemove(key, out value);
        }
    }
}
