using UnityEngine;

namespace MalbersAnimations
{
    public class CreateScriptableAssetAttribute : PropertyAttribute { }
   

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(CreateScriptableAssetAttribute), true)]
    public class CreateAssetDrawer : UnityEditor.PropertyDrawer
    {
       // private Color color = new Color(1,0.4f,0.4f,1);

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            var element = property.objectReferenceValue;

            if (element == null)
            {
                position.width -= 22;

                //var oldColor = GUI.color;
                //GUI.color = color;
                UnityEditor.EditorGUI.PropertyField(position, property);
               // GUI.color = oldColor;
                var AddButtonRect = new Rect(position) { x = position.width + position.x + 2, width = 20 };

                if (GUI.Button(AddButtonRect, "+"))
                {
                    MTools.CreateScriptableAsset(property, MTools.Get_Type(property), MTools.GetSelectedPathOrFallback());
                }
            }
            else
            {
                UnityEditor.EditorGUI.PropertyField(position, property);
            }
        }
    }
#endif
}