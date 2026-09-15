using System;
using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class CharacterSelectView : MonoBehaviour
    {
        [SerializeField] CharacterOption[] options;
        [SerializeField] int defaultIndex;
        [SerializeField] Image room;

        Action<CharacterLook> confirmed;
        CharacterLook selected;

        public void Wire(Action<CharacterLook> onConfirmed)
        {
            confirmed = onConfirmed;
            if (options == null)
                return;

            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null)
                    options[i].Wire(HandlePicked, HandleChosen);
            }
        }

        public void BindDeskClock(string clockDigits)
        {
            Image host = room != null ? room : GetComponent<Image>();
            DeskClockView clock = DeskClockView.On(host);
            if (clock != null)
                clock.Bind(clockDigits);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            CharacterOption pick = FindOption(selected) ?? OptionAt(defaultIndex);
            if (pick != null)
                HandlePicked(pick);
            else
                selected = null;

            if (pick != null)
                pick.Focus();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public CharacterLook Current
        {
            get { return selected ?? OptionAt(defaultIndex)?.Look; }
        }

        public void Remember(CharacterLook look)
        {
            selected = look;
        }

        public CharacterLook FindLook(string lookId)
        {
            if (string.IsNullOrEmpty(lookId) || options == null)
                return null;

            for (int i = 0; i < options.Length; i++)
            {
                CharacterLook look = options[i] != null ? options[i].Look : null;
                if (look != null && look.DisplayName == lookId)
                    return look;
            }

            return null;
        }

        public CharacterLook NextAfter(CharacterLook current)
        {
            if (options == null || options.Length == 0)
                return current;

            int start = IndexOf(current);
            for (int step = 1; step <= options.Length; step++)
            {
                CharacterOption option = options[(start + step) % options.Length];
                if (option != null && option.Look != null)
                    return option.Look;
            }

            return current;
        }

        int IndexOf(CharacterLook look)
        {
            if (look == null || options == null)
                return -1;

            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null && options[i].Look == look)
                    return i;
            }

            return -1;
        }

        void HandlePicked(CharacterOption option)
        {
            selected = option != null ? option.Look : null;
            if (options == null)
                return;

            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null)
                    options[i].SetSelected(options[i] == option);
            }
        }

        void HandleChosen(CharacterOption option)
        {
            HandlePicked(option);
            if (option != null && option.Look != null)
                confirmed?.Invoke(option.Look);
        }

        CharacterOption FindOption(CharacterLook look)
        {
            if (look == null || options == null)
                return null;

            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null && options[i].Look == look)
                    return options[i];
            }

            return null;
        }

        CharacterOption OptionAt(int index)
        {
            if (options == null || options.Length == 0)
                return null;

            int clamped = Mathf.Clamp(index, 0, options.Length - 1);
            return options[clamped];
        }
    }
}
