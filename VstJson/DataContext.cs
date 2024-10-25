using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public partial class Document : Dictionary<string, object>
    {
        #region CLONE
        public Document Copy(Document src, params string[] names)
        {
            if (names.Length == 0)
            {
                names = src.Keys.ToArray();
            }
            foreach (var name in names)
            {
                if (this.ContainsKey(name) == false)
                {
                    object v = null;
                    src.TryGetValue(name, out v);
                    if (v != null)
                    {
                        base.Add(name, v);
                    }
                }
            }
            return this;
        }
        public Document Move(Document dst, params string[] names)
        {
            if (names.Length == 0)
            {
                names = Keys.ToArray();
            }
            foreach (var name in names)
            {
                var v = this.GetValueCore(name, true);
                dst.Push(name, v);
            }
            return dst;
        }
        public Document Clone()
        {
            return new Document().Copy(this);
        }
        public T ChangeType<T>() where T : Document, new()
        {
            var dst = new T();
            dst.Copy(this);

            return dst;
        }
        public T ToObject<T>()
        {
            return JObject.FromObject(this).ToObject<T>();
        }
        public static T Parse<T>(string text) where T : Document
        {
            return JObject.Parse(text).ToObject<T>();
        }
        public static Document Parse(string text)
        {
            return JObject.Parse(text).ToObject<Document>();
        }
        public static T FromObject<T>(object src) where T : Document
        {
            return JObject.FromObject(src).ToObject<T>();
        }
        public static Document FromObject(object src)
        {
            return JObject.FromObject(src).ToObject<Document>();
        }
        #endregion

        protected virtual object GetValueCore(string name, bool remove)
        {
            object value;
            if (base.TryGetValue(name, out value))
            {
                if (remove)
                {
                    base.Remove(name);
                }
            }
            return value;
        }
        public void Push(string name, object value)
        {
            if (value == null)
            {
                Remove(name);
                return;
            }

            if (base.ContainsKey(name))
            {
                base[name] = value;
            }
            else
            {
                base.Add(name, value);
            }
        }
        public object Pop(string name)
        {
            return GetValueCore(name, true);
        }
        public T Pop<T>(string name)
        {
            return (T)(GetValueCore(name, true) ?? default(T));
        }
        public T SelectContext<T>(string name, Action<T> callback) where T : new()
        {
            var v = GetValueCore(name, false);
            if (v == null)
            {
                v = new T();
                Push(name, v);
            }
            else if (v.GetType() != typeof(T))
            {
                var obj = v is string ? JObject.Parse((string)v) : JObject.FromObject(v);
                v = obj.ToObject<T>();
                Push(name, v);
            }

            var context = (T)v;
            if (callback != null)
            {
                callback.Invoke(context);
            }
            return context;
        }
        public Document SelectContext(string name, Action<Document> callback)
        {
            var v = GetValueCore(name, false);
            if (v == null)
            {
                v = new Document();
                Push(name, v);
            }
            else if (!(v is Document))
            {
                var obj = v is string ? JObject.Parse((string)v) : JObject.FromObject(v);
                v = obj.ToObject<Document>();
                Push(name, v);
            }

            var context = (Document)v;
            if (callback != null)
            {
                callback.Invoke(context);
            }
            return context;
        }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }

        #region GET ITEMS VALUES
        public T GetObject<T>(string name)
        {
            object v;
            if (!TryGetValue(name, out v))
            {
                return default(T);
            }
            if (typeof(T) == v.GetType())
            {
                return (T)v;
            }

            var obj = (v is string ? JObject.Parse((string)v) : JObject.FromObject(v)).ToObject<T>();
            return obj;
        }
        public T GetDocument<T>(string name) where T : Document, new()
        {
            var doc = GetObject<T>(name);
            if (doc == null)
            {
                doc = new T();
            }
            doc.ObjectId = name;
            return doc;
        }
        public Document GetDocument(string name)
        {
            return GetDocument<Document>(name);
        }
        public T GetArray<T>(string name)
        {
            object v;
            if (!TryGetValue(name, out v))
            {
                return default(T);
            }

            if (v.GetType() == typeof(T)) { return (T)v; }

            var obj = v is string ? JArray.Parse((string)v) : JArray.FromObject(v);
            return obj.ToObject<T>();
        }
        public T GetValue<T>(string name)
        {
            object v;
            if (TryGetValue(name, out v) && v != null)
            {
                return (T)Convert.ChangeType(v, typeof(T));
            }
            return default(T);
        }
        public DateTime? GetDateTime(string name)
        {
            var v = GetValueCore(name, false);
            if (v == null)
            {
                return null;
            }
            if (v is string)
            {
                return DateTime.Parse((string)v);
            }
            return (DateTime)v;
        }
        public virtual string GetString(string name) => GetValue<string>(name);

        #endregion

        #region SET ITEMS VALUES
        public void SetObject(string name, object value)
        {
            Push(name, JObject.FromObject(value).ToString());
        }
        public void SetDocument(Document doc)
        {
            string name = (string)doc.Pop("_id");
            Push(name, doc);
        }
        public void SetArray(string name, object value)
        {
            Push(name, JArray.FromObject(value).ToString());
        }
        public virtual void SetString(string name, string value) => Push(name, value);
        #endregion

        #region ObjectId
        public string ObjectId { get => GetString("_id"); set => Push("_id", value); }
        #endregion
    }
}
