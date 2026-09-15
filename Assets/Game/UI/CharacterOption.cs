using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class CharacterOption : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        [SerializeField] CharacterLook look;
        [SerializeField] Image portrait;
        [SerializeField] Text label;
        [SerializeField] Image frame;
        [SerializeField] Button button;
        [SerializeField] Color idleFrame = new Color(0.24f, 0.26f, 0.32f, 1f);
        [SerializeField] Color selectedFrame = new Color(0.36f, 0.32f, 0.22f, 1f);

        Action<CharacterOption> picked;
        Action<CharacterOption> chosen;

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        public CharacterLook Look => look;

        public void Wire(Action<CharacterOption> onPicked, Action<CharacterOption> onChosen)
        {
            picked = onPicked;
            chosen = onChosen;
            Paint();
        }

        public void SetSelected(bool selected)
        {
            if (frame == null)
                return;
            frame.color = selected ? selectedFrame : idleFrame;
        }

        public void Focus()
        {
            if (button == null || EventSystem.current == null)
                return;
            if (EventSystem.current.currentSelectedGameObject == button.gameObject)
                return;
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (look != null)
                picked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Focus();
        }

        void Paint()
        {
            if (look == null)
                return;

            if (label != null)
                label.text = look.DisplayName;

            if (portrait != null && look.Idle != null)
            {
                portrait.enabled = true;
                portrait.sprite = look.Idle;
            }
        }

        void HandleClick()
        {
            if (look == null)
                return;
            picked?.Invoke(this);
            Focus();
            chosen?.Invoke(this);
        }
    }
}
