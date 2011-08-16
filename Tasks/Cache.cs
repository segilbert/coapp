using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Tasks {

    public class SessionCacheMessages : MessageHandlers<SessionCacheMessages> {
        public Func<Type, Func<object>,object> GetInstance;
    }

    public class Cache<T> where T : class {
        public static Cache<T> Value;

        protected Dictionary<string, T> _cache = new Dictionary<string,T>();
        protected Dictionary<string, List<Func<string, T>>> _delegateCache = new Dictionary<string, List<Func<string, T>>>();

        protected T GetAndRememberDelegateValue(string index) {
            T result = null;
            lock (_delegateCache) {
                lock (_cache) {
                    if (_delegateCache.ContainsKey(index)) {
                        foreach (var dlg in _delegateCache[index]) {
                            result = dlg(index);
                            if (result != null) {
                                _cache.Add(index, result);
                                break;
                            }
                        }
                    }
                }
            }
            return result;
        }

        public virtual T this[string index] {
            get {
                return _cache.ContainsKey(index) ? _cache[index] : GetAndRememberDelegateValue(index);
            }
            set {
                lock (_cache) {
                    if (_cache.ContainsKey(index)) {
                        _cache[index] = value;
                    }
                    else {
                        _cache.Add(index, value);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a function delegate to the cache that can get the value requested
        /// This adds the delegate at the bottom of the list of possible functions that can get the value requested.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="delegte"></param>
        public virtual void Add( string index, Func<string, T> delegte ) {
            lock (_delegateCache) {
                if (!_delegateCache.ContainsKey(index)) {
                    _delegateCache.Add(index, new List<Func<string, T>>());
                }
                _delegateCache[index].Add(delegte);
            }
        }

        public virtual void Insert(string index, Func<string, T> delegte) {
            lock (_delegateCache) {
                if (!_delegateCache.ContainsKey(index)) {
                    _delegateCache.Add(index, new List<Func<string, T>>());
                }
                _delegateCache[index].Insert(0, delegte);
            }
        }

        public virtual void ReplaceOrAdd( string index, Func<string, T> delegte ) {
            lock (_delegateCache) {
                if (!_delegateCache.ContainsKey(index)) {
                    _delegateCache.Add(index, new List<Func<string, T>>());
                }
                _delegateCache[index].Clear();
                _delegateCache[index].Insert(0, delegte);
            }
        }

        public virtual void Clear() {
            lock (_cache) {
                _cache.Clear();
            }
        }

        public virtual void Clear(string index ) {
            lock (_cache) {
                if (_cache.ContainsKey(index)) {
                    _cache.Remove(index);
                }
            }
        }

        public virtual void Wipe() {
            _cache.Clear();
            lock (_delegateCache) {
                _delegateCache.Clear();
            }
        }

        public virtual void Wipe(string index) {
            Clear(index);
            lock (_delegateCache) {
                if (_delegateCache.ContainsKey(index)) {
                    _delegateCache.Remove(index);
                }
            }
        }

        public virtual IEnumerable<string> Keys { get { return _cache.Keys; } }
        public virtual IEnumerable<T> Values { get { return _cache.Values.AsEnumerable(); } }
    }

    public class SessionCache<T> : Cache<T> where T : class  {
        public new static Cache<T> Value { get {
            return (SessionCacheMessages.Invoke.GetInstance(typeof(T), () => new SessionCache<T>())) as SessionCache<T>;
        }}

        public override T this[string index] {
            get {
                // check current cache.
                return _cache.ContainsKey(index) ? _cache[index] : GetAndRememberDelegateValue(index) ??  Cache<T>.Value[index];
            }
            set {
                lock (_cache) {
                    if (_cache.ContainsKey(index)) {
                        _cache[index] = value;
                    }
                    else {
                        _cache.Add(index, value);
                    }
                }
            }
        }

        public override IEnumerable<string> Keys { get { return _cache.Keys.Union(Cache<T>.Value.Keys); } }
        public override IEnumerable<T> Values { get { return _cache.Values.AsEnumerable().Union(Cache<T>.Value.Values); } }

        public IEnumerable<string> SessionKeys { get { return _cache.Keys; } }
        public IEnumerable<T> SessionValues { get { return _cache.Values.AsEnumerable(); } }

    }

    /*
    public class test {
        public test() {

            // gets from session cache first
            // then checks system cache
            var x = Cache<string>.Value["name"];

            // gets only from session cache.
            var y = SessionCache<string>.Value["name"];

            SessionCache<string>.Value.Add("foo", (packageName) => { return "hello"; });

        }
    }*/

}
