using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct BootstrapObserver : ISystem
{
    public void OnCreate(ref SystemState s)
    {
        var em = s.EntityManager;
        if (!SystemAPI.HasSingleton<ObserverSettings>())
        {
            var e = em.CreateEntity();
            em.AddComponentData(e, new ObserverSettings
            {
                SectorSize = 64f,
                LoadRadius = 2,
                HardUnloadRadius = 3
            });
        }
        s.Enabled = false;
    }
}
