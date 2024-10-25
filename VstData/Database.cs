using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace BsonData
{
    public class FileStorage
    {
        public DirectoryInfo Folder { get; private set; }
        public FileStorage(string path)
        {
            Folder = new DirectoryInfo(path);
            if (Folder.Exists == false)
            {
                Folder.Create();
            }
        }
        public FileStorage GetSubStorage(string name)
        {
            return new FileStorage(Folder.FullName + '/' + name);
        }
        public FileInfo GetFile(string id)
        {
            return new FileInfo(Folder.FullName + '/' + id);
        }

        public void Write(string id, object doc)
        {
            WriteDocument(GetFile(id), doc);
        }
        public void Delete(string id)
        {
            GetFile(id).Delete();
        }
        public Document Read(string id)
        {
            return ReadDocument(GetFile(id));
        }
        public List<Document> ReadAll()
        {
            return ReadFolderAsync(Folder);
        }
        public List<string> ReadKeys()
        {
            var lst = new List<string>();
            foreach (var fi in Folder.GetFiles())
            {
                lst.Add(fi.Name);
            }
            return lst;
        }

        public static List<Document> ReadFolderAsync(DirectoryInfo src)
        {
            var dst = new List<Document>();
            //var map = new Dictionary<FileInfo, Document>();
            foreach (var fi in src.GetFiles())
            {
                var doc = ReadDocument(fi);
                if (doc.ObjectId == null)
                {
                    doc.ObjectId = fi.Name;
                    //map.Add(fi, doc);
                }
                dst.Add(doc);
            }

            //foreach (var p in map)
            //{
            //    WriteDocument(p.Key, p.Value);
            //}
            return dst;
        }

        public static Document ReadDocument(FileInfo fi)
        {
            try
            {
                using (var br = new BsonDataReader(fi.OpenRead()))
                {
                    var serializer = new JsonSerializer();
                    var doc = serializer.Deserialize<Document>(br);

                    return doc;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new Document();
        }
        public static void WriteDocument(FileInfo fi, object doc)
        {
            try
            {
                using (var bw = new BsonDataWriter(fi.OpenWrite()))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(bw, doc);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    public class Record
    {
        public const int Deleted = -1;
        public const int Updated = 0;
        public const int Inserted = 1;
        public const int Changed = 2;

        public int State { get; set; }
        public Collection Collection { get; set; }
        public Document Document { get; set; }
    }

    public partial class Database : Dictionary<string, Record>
    {
        #region STORAGE
        public FileStorage Storage { get; private set; }
        FileStorage _documentStorage;
        public FileStorage DocumentStorage
        {
            get
            {
                if (_documentStorage == null) { _documentStorage = Storage.GetSubStorage("Documents"); }
                return _documentStorage;
            }
        }
        FileStorage _collectionStorage;
        public FileStorage CollectionStorage
        {
            get
            {
                if (_collectionStorage == null) { _collectionStorage = Storage.GetSubStorage("Collections"); }
                return _collectionStorage;
            }
        }

        public bool IsBusy
        {
            get
            {
                foreach (var p in _collections)
                {
                    if (p.Value.IsBusy) { return true; }
                }
                return false;
            }
        }
        Thread _storageThread;
        public virtual Database StartStorageThread(int interval)
        {
            _storageThread?.Abort();
            _storageThread = new Thread(() => {

                while (true)
                {
                    foreach (var p in _collections)
                    {
                        p.Value.BeginWrite();
                    }
                    Thread.Sleep(interval);
                }
            });
            _storageThread.Start();
            return this;
        }
        public Database StartStorageThread()
        {
            return this.StartStorageThread(1000);
        }

        #endregion

        public string ConnectionString { get; private set; }
        public string Name { get; private set; }
        public string PhysicalPath => ConnectionString + '/' + Name;
        public string DataPath(string name) => ConnectionString + '/' + name;
        public Database(string name)
        {
            Name = name;
        }

        public virtual Database Connect(string connectionString)
        {
            this.ConnectionString = connectionString;
            Storage = new FileStorage(PhysicalPath);

            return this;
        }
        public virtual void Disconnect()
        {
            Console.Write("Disconnecting ... ");

            _storageThread.Abort();

            while (IsBusy) { }
            Console.WriteLine("done");
        }

        #region COLLECTIONS
        BsonDataMap<Collection> _collections = new BsonDataMap<Collection>();
        public Collection GetCollection(string name)
        {
            Collection data = _collections[name];
            if (data == null)
            {
                _collections.Add(name, data = new Collection(name, this));
            }
            return data;
        }
        public Collection GetCollection<T>()
        {
            return GetCollection(typeof(T).Name);
        }
        #endregion

        #region MAPPING
        protected DatabaseMapping _mapping;
        public DatabaseMapping Mapping
        {
            get
            {
                if (_mapping == null)
                {
                    _mapping = new DatabaseMapping(this);
                }
                return _mapping;
            }
        }
        #endregion

        #region DOCUMENT
        public Record Find(string id)
        {
            Record r;
            if (TryGetValue(id, out r))
            {
                if (r.Document == null)
                {
                    r.Document = r.Collection.ReadOne(id);
                    r.Document.ObjectId = id;
                }
            }
            return r;
        }
        public Record Find(string id, Action<Record> callback)
        {
            var r = Find(id);
            if (r != null && callback != null)
            {
                callback(r);
            }
            return r;
        }
        public Record Find(string id, IEnumerable<string> tableNames)
        {
            var r = this.Find(id);
            if (r == null)
            {
                foreach (var name in tableNames)
                {
                    GetCollection(name).Wait();
                    r = this.Find(id);
                    if (r != null)
                    {
                        break;
                    }
                }
            }
            return r;
        }
        public Document DocumentAt(string id)
        {
            return base[id].Document;
        }
        public Document DocumentAt(string id, Document doc)
        {
            base[id].Document = doc;
            return doc;
        }
        #endregion

        new public void Clear()
        {
            base.Clear();
            new DirectoryInfo(this.PhysicalPath).Delete(true);
        }
    }

    public class DatabaseMapping : BsonDataMap<object>
    {
        string _fileName;
        public DatabaseMapping(string filename)
        {
            _fileName = filename;
            try
            {
                this.Read(_fileName);
            }
            catch
            {
            }
        }
        public DatabaseMapping(Database db)
            : this(db.PhysicalPath + "/mapping.json") {
        }
        public DatabaseMapping Save()
        {
            this.Write(_fileName);
            return this;
        }
        public DatabaseMapping Add(string context)
        {
            return this.Add(Document.Parse(context));
        }
        public DatabaseMapping Add(Document context)
        {
            foreach (var p in context)
            {
                this[p.Key] = p.Value;
            }
            return this;
        }

        protected virtual Document GetRootDocument(string name)
        {
            var doc = this[name];
            return doc == null ? null : Document.FromObject(doc);
        }
    }
}
