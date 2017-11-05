using System;
using System.Collections.Generic;
using System.Text;

namespace CFG
{
    class MultiValueDictionary<K, V>
    {
        Dictionary<K, HashSet<V>> dictionary = new Dictionary<K, HashSet<V>>();

        public HashSet<V> this[K key] {
            get { return dictionary[key]; }
            set { dictionary[key] = value; }
        }

        public void Add (K key, V value) {
            HashSet<V> values = new HashSet<V>();
            if (dictionary.ContainsKey(key)) {
                values.UnionWith(dictionary[key]);
                dictionary.Remove(key);
            }
            values.Add(value);
            dictionary.Add(key, values);
        }

        public bool Remove (K key) {
            return dictionary.Remove(key);
        }

        public bool TryGetValue (K key, out IEnumerable<V> values) {
            bool  returnVal = dictionary.TryGetValue(key, out HashSet<V> valueSet);
            values = valueSet;
            return returnVal;
        }

        public bool ContainsKey (K key) {
            return dictionary.ContainsKey(key);
        }
    }
}
