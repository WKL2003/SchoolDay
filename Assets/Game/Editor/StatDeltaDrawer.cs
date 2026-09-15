using UnityEditor;
using UnityEngine;

namespace SchoolDay.Editor
{
    [CustomPropertyDrawer(typeof(StatDelta))]
    public sealed class StatDeltaDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight * 7 + 8f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, "Meters (Budget / Energy / Reputation / keep $2)", true);
            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;
            row.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("PocketCost"), new GUIContent("Budget", "Money this choice spends. Runtime uses ChoiceData.Cost."));
            row.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("FuelChange"), new GUIContent("Energy", "Did you eat?"));
            row.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("FaceChange"), new GUIContent("Reputation", "Standing. Debt pulls this down."));
            row.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("BufferChange"), new GUIContent("Keep $2", "Cash that must still be there after a surprise bill."));
            row.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("LateRisk"), new GUIContent("Late risk"));
            row.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("MarksLeak"), new GUIContent("Marks leak"));
            EditorGUI.indentLevel--;
        }
    }
}
