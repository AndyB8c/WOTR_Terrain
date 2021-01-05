using MalbersAnimations.Scriptables;
using MalbersAnimations.Events;
using UnityEngine;

namespace MalbersAnimations
{
    public class FloatVarListener : VarListener
    {
        public FloatReference value;
        public FloatEvent Raise = new FloatEvent();
        
        public float Value { get => value; set => this.value.Value = value; }

        void OnEnable()
        {
            if (value.Variable != null) value.Variable.OnValueChanged += InvokeFloat;
            Raise.Invoke(value);
        }

        void OnDisable()
        {
            if (value.Variable != null) value.Variable.OnValueChanged -= InvokeFloat;
        }

        public virtual void InvokeFloat(float value)   
        {
            if (Enable)  Raise.Invoke(value);
        }
    }



    //INSPECTOR
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(FloatVarListener)), UnityEditor.CanEditMultipleObjects]
    public class FloatVarListenerEditor : VarListenerEditor
    {
        private UnityEditor.SerializedProperty  Raise;

        private void OnEnable()
        {
            base.SetEnable();
            Raise = serializedObject.FindProperty("Raise");
        }

        protected override void DrawEvents()
        {
            UnityEditor.EditorGUILayout.PropertyField(Raise);

        }
    }
#endif
}