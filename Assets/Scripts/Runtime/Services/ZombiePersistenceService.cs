using System;
using System.Collections.Generic;
using System.Linq;
using ZomboZ.Infrastructure.Cache;
using ZomboZ.Infrastructure.Mappers;
using ZomboZ.Infrastructure.Persistence;

namespace ZomboZ.Runtime
{
    public static class ZombiePersistenceService
    {
        static SqliteZombieRepository _repo;

        static SqliteZombieRepository Repo
        {
            get
            {
                if (_repo == null)
                    _repo = new SqliteZombieRepository();
                return _repo;
            }
        }

        public static void AddOrUpdate(ZombieCacheModel record)
        {
            var model = ZombieMapper.ToPersistence(record);
            Repo.AddOrUpdate(model);
        }

        public static void Remove(Guid id)
        {
            Repo.Remove(id);
        }

        public static List<ZombieCacheModel> LoadAll()
        {
            var models = Repo.QueryAll();
            return models.Select(ZombieMapper.ToCache).ToList();
        }
    }
}
