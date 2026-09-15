using System;
using System.IO;
using UnityEngine;

namespace SchoolDay
{
    public static class InstallStamp
    {
        const string FileName = "school_day.install";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureFreshPrefs()
        {
#if UNITY_EDITOR
            return;
#else
            string path = StampPath();
            if (string.IsNullOrEmpty(path) || File.Exists(path))
                return;

            if (!TryWrite(path))
                return;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
#endif
        }

        static string StampPath()
        {
            string data = Application.dataPath;
            if (string.IsNullOrEmpty(data))
                return "";

            return Path.Combine(data, FileName);
        }

        static bool TryWrite(string path)
        {
            try
            {
                string folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                File.WriteAllText(path, Application.buildGUID ?? "1");
                return File.Exists(path);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
