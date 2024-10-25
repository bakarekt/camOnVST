using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace System
{

    public partial class Document
    {
        #region Request
        public string Url { get { return GetString("#url"); } set => Push("#url", value); }
        public string Token { get { return GetString("token"); } set => Push("token", value); }
        #endregion

        #region Response
        public object Value { get { return GetValueCore("value", false); } set => Push("value", value); }
        public int Code { get { return GetValue<int>("code"); } set => Push("code", value); }
        public string Message { get { return GetString("message"); } set => Push("message", value); }
        #endregion

        #region VALUES
        Document _valueContext;
        public Document ValueContext
        {
            get
            {
                if (_valueContext == null)
                {
                    object value = Value;
                    _valueContext = (value == null ? new Document() : FromObject(value));
                }
                return _valueContext;
            }
        }
        public T GetValue<T>() => (T)Convert.ChangeType(Value, typeof(T));
        #endregion

        #region CONVERT
        public string Join(string seperator, params string[] names)
        {
            string s = string.Empty;
            foreach (string name in names)
            {
                object v;
                if (this.TryGetValue(name, out v))
                {
                    if (s.Length > 0) { s += seperator; }
                    s += v.ToString();
                }
            }
            return s;
        }
        public static implicit operator Document(string s)
        {
            return Parse<Document>(s);
        }
        public static implicit operator Document(byte[] bytes)
        {
            return Parse<Document>(bytes.UTF8());
        }
        public static explicit operator byte[](Document context)
        {
            return context.ToString().UTF8();
        }
        #endregion
    }
}
