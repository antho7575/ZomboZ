using UnityEngine;
using Unity.Entities;

public class AppStartup : MonoBehaviour
{
    async void Awake()
    {
        // Load mod registry (JSON -> registries -> BlobAssets)
        await ModRegistryLoader.LoadAllAsync(Application.streamingAssetsPath + "/Mods");

        // Init save backend (switch driver implementation here if desired)
        SaveBackend.Init();
        SaveBackend.Driver.Open(System.IO.Path.Combine(Application.persistentDataPath, "save.db"));

        // Move to world scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("World");
    }

    void OnApplicationQuit()
    {
        SaveBackend.Driver?.Close();
    }
}
