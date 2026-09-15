using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Metrics Config", fileName = "MetricsConfig")]
    public sealed class MetricsConfig : ScriptableObject
    {
        [Tooltip("Empty = offline. The day still plays. Example: http://127.0.0.1:8787")]
        public string BaseUrl;

        [Tooltip("Appended to BaseUrl. Leading slash optional.")]
        public string EventsPath = "/events";

        public float TimeoutSeconds = 3f;

        public string EventsUrl()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
                return null;

            string root = BaseUrl.Trim().TrimEnd('/');
            string path = string.IsNullOrWhiteSpace(EventsPath) ? "/events" : EventsPath.Trim();
            if (path.Length == 0 || path[0] != '/')
                path = "/" + path;

            return root + path;
        }
    }
}
