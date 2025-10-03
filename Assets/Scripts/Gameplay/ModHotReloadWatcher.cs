using UnityEngine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class ModHotReloadWatcher : MonoBehaviour
{
    FileSystemWatcher watcher; CancellationTokenSource debounceCts;

    void Start()
    {
        var modsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Mods");
        if (!Directory.Exists(modsPath)) return;

        watcher = new FileSystemWatcher(modsPath, "*.json");
        watcher.IncludeSubdirectories = true;
        watcher.Changed += (_, __) => DebounceReload();
        watcher.Created += (_, __) => DebounceReload();
        watcher.Deleted += (_, __) => DebounceReload();
        watcher.EnableRaisingEvents = true;
    }

    void OnDestroy() { watcher?.Dispose(); debounceCts?.Cancel(); }

    void DebounceReload()
    {
        debounceCts?.Cancel();
        debounceCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, debounceCts.Token);
                await ModRegistryLoader.LoadAllAsync(System.IO.Path.Combine(Application.streamingAssetsPath, "Mods"));
                Debug.Log("[Mods] Hot reloaded registries.");
            }
            catch (System.OperationCanceledException) { }
        });
    }
}
