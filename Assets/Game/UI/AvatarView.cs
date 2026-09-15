using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class AvatarView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] ArtSet art;
        [SerializeField] CharacterLook look;
        [SerializeField] Image portrait;
        [SerializeField] Button swapLook;
        [SerializeField] RectTransform titlePlace;
        [SerializeField] RectTransform playPlace;
        [SerializeField] float waveInterval = 0.20f;
        [SerializeField] float standingLoopInterval = 0.35f;

        Action swapRequested;
        Coroutine waveRoutine;
        Coroutine standingRoutine;
        MoneyStats currentStats;
        float currentStanding;
        bool holding;

        void Awake()
        {
            if (swapLook != null)
                swapLook.onClick.AddListener(HandleSwap);
            SetSwapVisible(false);
        }

        void OnEnable()
        {
            if (portrait != null)
                portrait.raycastTarget = true;
            ShowMood();
        }

        void OnDisable()
        {
            StopWaveImmediate();
            StopStandingLoop();
        }

        public void Wire(Action onSwapRequested)
        {
            swapRequested = onSwapRequested;
        }

        public void UseLook(CharacterLook selected)
        {
            look = selected;
            if (waveRoutine != null)
                StopWaveImmediate();
            else
                ShowMood();
        }

        void HandleSwap()
        {
            swapRequested?.Invoke();
        }

        public void ShowTitlePlace()
        {
            SnapTo(titlePlace);
        }

        public void ShowPlayPlace()
        {
            SnapTo(playPlace);
            SetSwapVisible(false);
        }

        public void SetSwapVisible(bool visible)
        {
            if (swapLook != null)
                swapLook.gameObject.SetActive(visible);
        }

        void SnapTo(RectTransform place)
        {
            RectTransform self = transform as RectTransform;
            if (self == null || place == null)
                return;

            self.anchorMin = place.anchorMin;
            self.anchorMax = place.anchorMax;
            self.pivot = place.pivot;
            self.anchoredPosition = place.anchoredPosition;
            self.sizeDelta = place.sizeDelta;
        }

        public void Bind(MoneyStats stats)
        {
            Bind(stats, stats != null ? stats.Face : 0f);
        }

        public void Bind(MoneyStats stats, float standing)
        {
            currentStats = stats;
            currentStanding = standing;
            if (waveRoutine == null)
                ShowMood();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            holding = true;
            if (waveRoutine == null && gameObject.activeInHierarchy)
            {
                StopStandingLoop();
                waveRoutine = StartCoroutine(WaveWhileHeld());
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            holding = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            holding = false;
        }

        void StopWaveImmediate()
        {
            if (waveRoutine != null)
            {
                StopCoroutine(waveRoutine);
                waveRoutine = null;
            }
            holding = false;
            ShowMood();
        }

        void StopStandingLoop()
        {
            if (standingRoutine != null)
            {
                StopCoroutine(standingRoutine);
                standingRoutine = null;
            }
        }

        IEnumerator WaveWhileHeld()
        {
            Sprite[] frames = look != null ? look.WaveFrames : null;
            if (frames == null || frames.Length < 2)
            {
                waveRoutine = null;
                ShowMood();
                yield break;
            }

            do
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    SetPortrait(frames[i]);
                    yield return new WaitForSeconds(waveInterval);
                }
                for (int i = frames.Length - 2; i >= 0; i--)
                {
                    SetPortrait(frames[i]);
                    yield return new WaitForSeconds(waveInterval);
                }
            }
            while (holding);

            waveRoutine = null;
            ShowMood();
        }

        void ShowMood()
        {
            StopStandingLoop();
            if (gameObject.activeInHierarchy)
                standingRoutine = StartCoroutine(PlayStandingLoop());
            else
                SetPortrait(CurrentStill());
        }

        Sprite CurrentStill()
        {
            return art != null
                ? art.SpriteFor(currentStats, look, currentStanding)
                : look != null ? look.Idle : null;
        }

        Sprite[] CurrentMoodFrames()
        {
            if (look == null)
                return null;

            if (art != null && currentStats != null)
            {
                if (currentStats.Fuel <= art.LowEnergyMax)
                    return look.LowEnergyFrames != null && look.LowEnergyFrames.Length >= 2 ? look.LowEnergyFrames : null;
                if (currentStanding <= art.LowReputationMax)
                    return look.LowReputationFrames != null && look.LowReputationFrames.Length >= 2 ? look.LowReputationFrames : null;
                if (currentStanding >= art.HighReputationMin && currentStats.Fuel >= art.HighEnergyMin)
                    return look.HighEnergyReputationFrames != null && look.HighEnergyReputationFrames.Length >= 2 ? look.HighEnergyReputationFrames : null;
            }

            return look.IdleFrames != null && look.IdleFrames.Length >= 2 ? look.IdleFrames : null;
        }

        IEnumerator PlayStandingLoop()
        {
            while (true)
            {
                Sprite[] frames = CurrentMoodFrames();
                if (frames == null || frames.Length < 2)
                {
                    SetPortrait(CurrentStill());
                    yield return new WaitForSeconds(standingLoopInterval);
                    continue;
                }

                // Ping-pong: 0 -> 1 -> 2 -> 1 -> 0
                for (int i = 0; i < frames.Length; i++)
                {
                    SetPortrait(frames[i]);
                    yield return new WaitForSeconds(standingLoopInterval);
                }
                for (int i = frames.Length - 2; i >= 1; i--)
                {
                    SetPortrait(frames[i]);
                    yield return new WaitForSeconds(standingLoopInterval);
                }
            }
        }

        void SetPortrait(Sprite sprite)
        {
            if (portrait != null && sprite != null)
                portrait.sprite = sprite;
        }
    }
}
