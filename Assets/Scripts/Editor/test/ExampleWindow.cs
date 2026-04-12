#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ZomboZ.Infrastructure.Cache;
using ZomboZ.Runtime;

namespace ZomboZ.Editor
{
    public class ExampleWindow : EditorWindow
    {
        [MenuItem("ZomboZ/Example Window")]
        public static void ShowWindow()
        {
            GetWindow<ExampleWindow>("ZomboZ");
        }

        void OnGUI()
        {
            GUILayout.Label("Service checks", EditorStyles.boldLabel);

            if (GUILayout.Button("Check ICache<string, object> registered"))
            {
                if (ServiceLocator.TryResolve<ICache<string, object>>(out var cache))
                {
                    EditorUtility.DisplayDialog("ZomboZ", "ICache<string, object> is registered", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("ZomboZ", "ICache<string, object> is NOT registered. Add Bootstrapper to a scene.", "OK");
                }
            }
        }
    }
}
#endif
