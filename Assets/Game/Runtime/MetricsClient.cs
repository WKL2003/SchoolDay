using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace SchoolDay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DayDirector))]
    public sealed class MetricsClient : MonoBehaviour
    {
        [SerializeField] DayDirector director;
        [SerializeField] MetricsConfig config;
        [SerializeField] Text statusLabel;
        [SerializeField] string liveLabel = "LIVE";
        [SerializeField] string sentLabel = "SENT";
        [SerializeField] string offlineLabel = "OFFLINE";
        [SerializeField] float sentHoldSeconds = 0.4f;

        public const string PlayerIdPrefsKey = "SchoolDay.PlayerId";

        readonly Queue<string> pending = new Queue<string>();

        string sessionId;
        string playerId;
        DaySession startedSession;
        float dayStartedAt;
        Coroutine pump;

        void Awake()
        {
            if (director == null)
                director = GetComponent<DayDirector>();
        }

        void OnEnable()
        {
            if (director != null)
            {
                director.DayStarted += HandleDayStarted;
                director.LookChanged += HandleLookChanged;
                director.ChoiceCommitted += HandleChoice;
                director.DayEnded += HandleDayEnded;
            }

            PaintStatus(HasEndpoint() ? liveLabel : offlineLabel);
        }

        void OnDisable()
        {
            if (director != null)
            {
                director.DayStarted -= HandleDayStarted;
                director.LookChanged -= HandleLookChanged;
                director.ChoiceCommitted -= HandleChoice;
                director.DayEnded -= HandleDayEnded;
            }

            if (pump != null)
            {
                StopCoroutine(pump);
                pump = null;
            }

            pending.Clear();
        }

        void HandleDayStarted(DaySession session, CharacterLook look)
        {
            startedSession = session;
            sessionId = director != null && !string.IsNullOrEmpty(director.ActiveSessionId)
                ? director.ActiveSessionId
                : Telemetry.NewSessionId();
            dayStartedAt = Time.unscaledTime - (director != null ? director.SavedElapsed : 0f);
            if (director != null && director.SuppressStartTelemetry)
                return;

            string lookId = look != null ? look.DisplayName : "";
            Enqueue(Telemetry.Started(sessionId, session, lookId, EnsurePlayerId()));
        }

        void HandleLookChanged(DaySession session, CharacterLook look)
        {
            if (string.IsNullOrEmpty(sessionId) || session == null || session != startedSession)
                return;

            string lookId = look != null ? look.DisplayName : "";
            Enqueue(Telemetry.Started(sessionId, session, lookId, EnsurePlayerId()));
        }

        void HandleChoice(ChoiceResult result)
        {
            if (string.IsNullOrEmpty(sessionId))
                return;

            DayConfig day = director != null && director.Session != null
                ? director.Session.Config
                : null;
            TelemetryEvent ev = Telemetry.FromChoice(sessionId, result, day, EnsurePlayerId());
            Telemetry.WriteReputation(ev, director != null ? director.Session : null);
            Enqueue(ev);
        }

        void HandleDayEnded(DaySession session)
        {
            if (string.IsNullOrEmpty(sessionId))
                return;

            float duration = Time.unscaledTime - dayStartedAt;
            string achievementId = director != null ? director.LastAchievementId : "";
            Enqueue(Telemetry.Ended(sessionId, session, duration, achievementId, EnsurePlayerId()));
        }

        string EnsurePlayerId()
        {
            if (!string.IsNullOrEmpty(playerId))
                return playerId;

            playerId = PlayerPrefs.GetString(PlayerIdPrefsKey, "");
            if (string.IsNullOrEmpty(playerId))
            {
                playerId = Telemetry.NewPlayerId();
                PlayerPrefs.SetString(PlayerIdPrefsKey, playerId);
                PlayerPrefs.Save();
            }

            return playerId;
        }

        void Enqueue(TelemetryEvent payload)
        {
            if (payload == null)
                return;

            if (!HasEndpoint())
            {
                PaintStatus(offlineLabel);
                return;
            }

            pending.Enqueue(JsonUtility.ToJson(payload));
            if (pump == null)
                pump = StartCoroutine(Pump());
        }

        IEnumerator Pump()
        {
            while (pending.Count > 0)
                yield return Send(pending.Dequeue());

            pump = null;
        }

        IEnumerator Send(string json)
        {
            string url = config.EventsUrl();
            if (string.IsNullOrEmpty(url))
            {
                PaintStatus(offlineLabel);
                yield break;
            }

            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = Mathf.Max(1, Mathf.RoundToInt(config.TimeoutSeconds));
                yield return request.SendWebRequest();

                bool ok = request.result == UnityWebRequest.Result.Success
                    && request.responseCode >= 200
                    && request.responseCode < 300;

                if (!ok)
                {
                    PaintStatus(offlineLabel);
                    Debug.LogWarning(
                        "SchoolDay metrics POST failed (" + request.responseCode + "): " + request.error);
                    yield break;
                }
            }

            PaintStatus(sentLabel);
            if (sentHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(sentHoldSeconds);
            if (HasEndpoint())
                PaintStatus(liveLabel);
        }

        bool HasEndpoint()
        {
            return config != null && !string.IsNullOrEmpty(config.EventsUrl());
        }

        void PaintStatus(string label)
        {
            if (statusLabel == null)
                return;

            statusLabel.text = label ?? "";
        }
    }
}
