using SQLite;
using System;


namespace ZomboZ.Infrastructure.Persistence
{
    [Table("zombies")]
    public class ZombiePersistenceModel
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotationY { get; set; }
        public int Health { get; set; }
        public long LastSeenTicks { get; set; }
    }
}
