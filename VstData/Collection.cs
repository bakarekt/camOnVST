using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BsonData
{
    class UpdatingState : Dictionary<string, int>
    {
        public bool Busy { get; private set; }
        public void Set(string key, int val)
        {
            while (Busy) { }
            int v;
            if (this.TryGetValue(key, out v))
            {
                if (v != val)
                {
                    base[key] = val;
                }
            }
            else
            {
                base.Add(key, val);
            }
        }
        public int Get(string key)
        {
            while (Busy) { }
            
            int v = int.MaxValue;
            this.TryGetValue(key, out v);
            return v;
        }

        public void Clear(Action<string, int> action)
        {
            if (this.Count > 0)
            {
                Busy = true;
                var ts = new ThreadStart(() =>
                {
                    try
                    {
                        foreach (var p in this)
                        {
                            action(p.Key, p.Value);
                        }
                        base.Clear();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    Busy = false;
                });
                new Thread(ts).Start();
            }
        }
    }
    public class Collection
    {
        UpdatingState _updating = new UpdatingState();

        public Database Database { get; private set; }
        public string Name { get; private set; }
        public Collection(string name, Database db)
        {
            Database = db;
            Name = name;

            _storage = db.CollectionStorage.GetSubStorage(name);
            BeginRead();
        }

        #region LIST
        public bool IsBusy => (_storeThread != null && _storeThread.IsAlive);
        int _count;
        public int Count
        {
            get
            {
                Wait(null);
                return _count;
            }
        }

        class Node
        {
            public string Id { get; set; }
            public Node Next { get; set; }
            public Node Prev { get; set; }
        }
        Node _head, _tail;
        Record _add(string id)
        {
            var node = new Node { Id = id };
            if (_count++ == 0)
            {
                _head = node;
            }
            else
            {
                node.Prev = _tail;
                _tail.Next = node;
            }

            _tail = node;
            
            var r = new Record { 
                Collection = this,
            };
            Database.Add(id, r);

            return r;
        }
        void _remove(Node node)
        {
            var next = node.Next;
            var prev = node.Prev;

            if (next != null) next.Prev = prev;
            if (prev != null) prev.Next = next;

            if (node == _head) _head = next;
            if (node == _tail) _tail = prev;

            _count--;
        }
        void _load()
        {
            foreach (var e in _storage.ReadKeys())
            {
                _add(e);
            }
        }

        FileStorage _storage;
        Thread _storeThread;
        void _store()
        {
            if (_updating.Count == 0) return;

            _updating.Clear((k, v) =>
            {
                if (v == Record.Deleted)
                {
                    _storage.Delete(k);
                    return;
                }

                WriteOne(k, Database.DocumentAt(k));
            });
        }

        public void BeginRead()
        {
            try
            {
                _storeThread = new Thread(() => _load());
                _storeThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public void BeginReadContent(int blockSize, int delay)
        {
            Wait(() => {
                var node = _head;
                _storeThread = new Thread(() => {
                    while (node != null)
                    {
                        for (int i = 0; i < blockSize; i++)
                        {
                            Database.Find(node.Id);
                            node = node.Next;
                            if (node == null) return;
                        }
                        Thread.Sleep(delay);
                    }
                });
                _storeThread.Start();
            });
        }
        public void BeginWrite()
        {
            _store();
        }

        public Document ReadOne(string id)
        {
            return _storage.Read(id);
        }
        public void WriteOne(string id, Document doc)
        {
            var s = doc.Clone();
            s.ObjectId = null;

            _storage.Write(id, s);
        }

        #endregion

        #region FINDING
        public Document Find(string objectId, Action<Document> callback)
        {
            var doc = Find(objectId);
            if (doc != null && callback != null)
            {
                callback(doc);
            }
            return doc;
        }
        public Document Find(string objectId)
        {
            Wait();
            if (_count == 0 || string.IsNullOrEmpty(objectId))
            {
                return null;
            }

            return Database.Find(objectId)?.Document;
        }
        public T Find<T>(string objectId)
            where T : Document, new() {

            var doc = Find(objectId);
            if (doc == null) { return null; }

            if (doc.GetType() == typeof(T)) { return (T)doc; }

            var context = new T();
            context.Copy(doc);

            Database.DocumentAt(objectId, context);

            return context;
        }
        public void FindAndDelete(string objectId, Action<Document> before)
        {
            Find(objectId, doc =>
            {
                before?.Invoke(doc);
                Database.Remove(objectId);

                _updating.Set(objectId, Record.Deleted);
            });
        }
        public void FindAndUpdate(string objectId, Action<Document> before)
        {
            Find(objectId, doc =>
            {
                before?.Invoke(doc);
                _updating.Set(objectId, Record.Changed);
            });
        }
        #endregion

        #region DB
        public void Wait(Action callback)
        {
            if (_count == 0 && !IsBusy) { BeginRead(); }

            while (IsBusy) { }
            callback?.Invoke();
        }
        public Collection Wait()
        {
            Wait(null);
            return this;
        }
        public IEnumerable<Document> Select(Func<Document, bool> where)
        {
            var lst = Select();
            if (where != null)
            {
                lst = lst.Where(where);
            }
            return lst;
        }
        public IEnumerable<Document> Select()
        {
            var lst = new List<Document>();
            Wait(() =>
            {
                var node = _head;
                while (node != null)
                {
                    var next = node.Next;
                    var documentId = node.Id;

                    if (_updating.Get(documentId) == Record.Deleted)
                    {
                        _remove(node);
                    }
                    else
                    {
                        var doc = Database.Find(documentId)?.Document;
                        if (doc == null)
                        {
                            _remove(node);
                        }
                        else
                        {
                            lst.Add(doc);
                        }
                    }
                    node = next;
                }
            });
            return lst;
        }
        public bool Insert(string id, Document doc)
        {
            if (id == null) id = new ObjectId();

            doc.ObjectId = id;
            if (Database.ContainsKey(id))
            {
                return false;
            }

            _add(id).Document = doc;
            _updating.Set(id, Record.Inserted);
            return true;
        }
        public bool Insert(Document doc)
        {
            return Insert(doc.ObjectId, doc);
        }
        public bool Update(string id, Document doc)
        {
            var res = false;
            FindAndUpdate(id, current =>
            {
                res = true;
                if (doc != current)
                {
                    foreach (var p in doc)
                    {
                        current.Push(p.Key, p.Value);
                    }
                }
                _updating.Set(id, Record.Changed);
            });

            return res;

        }
        public bool Update(Document doc)
        {
            return Update(doc.ObjectId, doc);
        }
        public bool Delete(Document doc)
        {
            return Delete(doc.ObjectId);
        }
        public bool Delete(string id)
        {
            var res = false;
            FindAndDelete(id, exist => res = true);

            return res;
        }
        public void DeleteAll()
        {
            Wait();
            if (_count > 0)
            {
                _updating.Clear();

                var node = _head;
                while (node != null)
                {
                    _storage.Delete(node.Id);

                    Database.Remove(node.Id);
                    node = node.Next;
                }
                _count = 0;
                _head = null;
            }
        }
        public void InsertOrUpdate(Document doc)
        {
            InsertOrUpdate(doc.ObjectId, doc);
        }
        public void InsertOrUpdate(string id, Document doc)
        {
            if (Database.ContainsKey(id))
            {
                Update(id, doc);
                return;
            }
            Insert(id, doc);
        }
        #endregion

        public IEnumerable<string> GetPrimaryKeys()
        {
            var lst = new List<string>();
            Wait(() => {
                var node = _head;
                while (node != null)
                {
                    lst.Add(node.Id);
                    node = node.Next;
                }
            });
            return lst;
        }
        public Document[] DistinctRow(params string[] names)
        {
            var map = new DocumentMap();
            foreach (var doc in this.Select())
            {
                var key = doc.Join("_", names);
                if (map.ContainsKey(key) == false)
                {
                    map.Add(key, new Document().Copy(doc, names));
                }
            }
            return map.Values.ToArray();
        }
        public string[] DistinctColumn(string name)
        {
            var map = new DocumentMap();
            foreach (var doc in this.Select())
            {
                var key = doc.GetString(name);
                if (map.ContainsKey(key) == false)
                {
                    map.Add(key, doc);
                }
            }
            return map.Keys.ToArray();
        }
        public DocumentGroup[] GroupBy(params string[] names)
        {
            var map = new Dictionary<string, DocumentGroup>();
            foreach (var doc in this.Select())
            {
                DocumentGroup ext;
                var key = doc.Join("_", names);
                
                if (!map.TryGetValue(key, out ext))
                {
                    map.Add(key, ext = new DocumentGroup());
                    ext.Copy(doc, names);
                }
                ext.Items.Add(doc);
            }
            return map.Values.ToArray();
        }
    }

}
