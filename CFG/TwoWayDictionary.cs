using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CFG
{
    class TwoWayDictionary<T, U>
    {
        private Dictionary<T, U> forward = new Dictionary<T, U>();
        private Dictionary<U, T> backward = new Dictionary<U, T>();

        public void Add (T one, U two) {
            forward.Add(one, two);
            backward.Add(two, one);
        }

        public bool Remove (T key1, U key2) {
            bool success = false;
            success = success || backward.Remove(key2);
            return (success && forward.Remove(key1));
        }

        public bool ForwardSearch (T key, out U value) {
            return forward.TryGetValue(key, out value);
        }

        public bool BackwardSearch (U key, out T value) {
            return backward.TryGetValue(key, out value);
        }

        public T[] GetForwardKeys () {
            return forward.Keys.ToArray<T>();
        }
        public U[] GetForwardValues () {
            return forward.Values.ToArray<U>();
        }

        public bool ContainsForwardKey (T key) {
            return forward.ContainsKey(key);
        }

        public bool ContainsBackwardKey (U key) {
            return backward.ContainsKey(key);
        }

        public U[] GetBackwardKeys () {
            return backward.Keys.ToArray<U>();
        }
        public T[] GetBackwardValues () {
            return backward.Values.ToArray<T>();
        }

        public Dictionary<T, U> GetForwardDictionary () {
            return this.forward;
        }

        public Dictionary<U, T> GetBackwardDictionary () {
            return this.backward;
        }

    }
}
