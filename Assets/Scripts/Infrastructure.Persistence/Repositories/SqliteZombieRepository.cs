using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

namespace ZomboZ.Infrastructure.Persistence
{
    public class SqliteZombieRepository : IDisposable
    {
        readonly SQLiteConnection _db;

        public SqliteZombieRepository(string dbPath = null)
        {
            var dataFolder = Path.Combine(Application.persistentDataPath, "data");
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            var path = dbPath ?? Path.Combine(dataFolder, "zombies.db");
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _db = new SQLiteConnection(path);
            _db.CreateTable<ZombiePersistenceModel>();
            try
            {
                _db.Execute("CREATE INDEX IF NOT EXISTS idx_pos ON zombies(PosX, PosZ);");
            }
            catch { }
        }

        public void AddOrUpdate(ZombiePersistenceModel model)
        {
            if (model.Id == Guid.Empty)
                model.Id = Guid.NewGuid();

            _db.InsertOrReplace(model);
        }

        public void Remove(Guid id)
        {
            _db.Delete<ZombiePersistenceModel>(id);
        }

        public List<ZombiePersistenceModel> QueryAll()
        {
            return _db.Table<ZombiePersistenceModel>().ToList();
        }

        public void Dispose()
        {
            try { _db?.Close(); } catch { }
            _db?.Dispose();
        }
    }
}
