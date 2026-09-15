using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SchoolDay.Editor
{
    public static class SchoolDayMetricsWire
    {
        const string ConfigPath = "Assets/Game/Data/MetricsConfig.asset";

        [MenuItem("School Day/Attach Metrics Client")]
        public static void AttachMetricsClient()
        {
            DayDirector director = Object.FindFirstObjectByType<DayDirector>();
            if (director == null)
            {
                Debug.LogWarning("Open Assets/Scenes/SchoolDay.unity first.");
                return;
            }

            MetricsConfig config = AssetDatabase.LoadAssetAtPath<MetricsConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogWarning("Missing " + ConfigPath + ". Run School Day / Seed Content If Missing.");
                return;
            }

            MetricsClient client = director.GetComponent<MetricsClient>();
            if (client == null)
                client = Undo.AddComponent<MetricsClient>(director.gameObject);

            var so = new SerializedObject(client);
            so.FindProperty("director").objectReferenceValue = director;
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director.gameObject);
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        }
    }
}
