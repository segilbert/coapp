using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Collections {
    using System.Threading.Tasks;

    public class ExpiringDictionary<key,val> : IDictionary<key,val> {

        public ExpiringDictionary(int timeoutMsec)
            : base() {
            
        }

        public void Add(key key, val value) {
            throw new NotImplementedException();
        }

        public bool ContainsKey(key key) {
            throw new NotImplementedException();
        }

        public ICollection<key> Keys {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(key key) {
            throw new NotImplementedException();
        }

        public bool TryGetValue(key key, out val value) {
            throw new NotImplementedException();
        }

        public ICollection<val> Values {
            get { throw new NotImplementedException(); }
        }

        public val this[key key] {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public void Add(KeyValuePair<key, val> item) {
            throw new NotImplementedException();
        }

        public void Clear() {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<key, val> item) {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<key, val>[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public int Count {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(KeyValuePair<key, val> item) {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<key, val>> GetEnumerator() {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
    }
}
