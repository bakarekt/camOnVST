using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System
{
    public class DocumentMap<T> : Dictionary<string, T>
        where T: Document
    {
        new public T this[string objectId]
        {
            get
            {
                if (string.IsNullOrEmpty(objectId))
                {
                    return default(T);
                }

                T value;
                TryGetValue(objectId, out value);

                return value;
            }
            set
            {
                if (base.ContainsKey(objectId))
                {
                    base[objectId] = value;
                }
                else
                {
                    base.Add(objectId, value);
                }
            }
        }
        public T DocumentAt(string objectId)
        {
            return base[objectId];
        }
        public void DocumentAt(string objectId, T doc)
        {
            base[objectId] = doc;
        }
        public void Add(T doc)
        {
            this[doc.ObjectId] = doc;
        }
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var doc in items)
            {
                this.Add(doc);
            }
        }
        new public DocumentMap<T> Clear()
        {
            base.Clear();
            return this;
        }
    }
    public class DocumentList : List<Document>
    {
        public DocumentList() { }
        public DocumentList(IEnumerable<Document> items)
        {
            this.AddRange(items);
        }
        public Document Push(object value)
        {
            
            Document doc = value as Document;
            if (doc == null)
            {
                doc = Document.FromObject(value);
            }
            base.Add(doc);

            return doc;
        }

        public DocumentList Sort<TKey>(Func<Document, TKey> predicate)
        {
            return new DocumentList(this.OrderBy(predicate));
        }
    }
    public class DocumentMap : DocumentMap<System.Document> { }
    public class DocumentGroup : Document
    {
        DocumentList _items;
        public int RowsCount => _items == null ? 0 : _items.Count;
        public DocumentList Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new DocumentList();
                }
                return _items;
            }
        }
    }
}
