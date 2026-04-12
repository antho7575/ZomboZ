using System.IO;
using UnityEngine;

namespace ZomboZ.Infrastructure.Persistence
{
    public static class AppPaths
    {
        public static string DataFolder
        {
            get
            {
                var path = Path.Combine(Application.persistentDataPath, "Data");
                try { if (!Directory.Exists(path)) Directory.CreateDirectory(path); } catch { }
                return path;
            }   
        }
    }
}
