using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SchoolDay.Editor
{
    [CustomEditor(typeof(WeekBoard))]
    public sealed class WeekBoardEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WeekBoard board = (WeekBoard)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Demo", EditorStyles.boldLabel);

            if (GUILayout.Button("Reshuffle now"))
            {
                Undo.RecordObject(board, "Reshuffle week mix");
                board.ReshuffleNonce++;
                EditorUtility.SetDirty(board);
            }

            if (GUILayout.Button("Log mix for preview weekday"))
                LogMix(board);
        }

        static void LogMix(WeekBoard board)
        {
            if (board == null || board.Day == null)
            {
                Debug.LogWarning("WeekBoard needs a DayConfig.");
                return;
            }

            SchoolWeekday weekday = WeekCalendar.Resolve(board, DateTime.Now);
            DayMix mix = DayMixer.Mix(board, weekday, DateTime.Now.Date, 0f);
            var text = new StringBuilder();
            text.Append(mix.WeekdayName).Append(" seed ").Append(mix.Seed);
            text.Append(" pocket ").Append(PocketFormat.Cash(mix.Opening.Pocket));
            text.AppendLine();
            text.Append("promos:");
            if (mix.ActivePromos == null || mix.ActivePromos.Length == 0)
                text.Append(" none");
            else
            {
                for (int p = 0; p < mix.ActivePromos.Length; p++)
                {
                    PromoData promo = mix.ActivePromos[p];
                    text.Append(' ').Append(promo != null ? promo.Id : "null");
                }
            }

            text.AppendLine();
            text.AppendLine(mix.TitleBody);

            BeatData[] beats = board.Day.Beats;
            for (int i = 0; i < beats.Length; i++)
            {
                BeatData beat = beats[i];
                MixOffer offer;
                if (!mix.TryGet(beat, out offer) || offer == null || offer.Choices == null)
                    continue;

                text.Append(beat.Id).Append(':');
                for (int c = 0; c < offer.Choices.Length; c++)
                {
                    ChoiceData choice = offer.Choices[c];
                    text.Append(' ').Append(choice != null ? choice.Id : "null");
                    AppliedPromo overlay = offer.PromoFor(choice);
                    if (overlay.Active)
                        text.Append('[').Append(overlay.PromoId).Append(']');
                }

                text.AppendLine();
            }

            Debug.Log(text.ToString());
        }
    }
}
