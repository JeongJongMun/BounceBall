using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    // [Label("한글명")]이 붙은 필드의 인스펙터 라벨을 바꿔 그린다.
    // Tooltip은 그대로 유지된다.
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var labelAttribute = (LabelAttribute)attribute;
            var content = new GUIContent(labelAttribute.Text, label.tooltip);
            EditorGUI.PropertyField(position, property, content, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
