using MalbersAnimations.Scriptables;
using MalbersAnimations.Events;
using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations
{
    public class FloatComparer : MonoBehaviour
    {
        public FloatReference value = new FloatReference(0);
        public List<AdvancedFloatEvent> compare = new List<AdvancedFloatEvent>();

        public float Value { get => value.Value; set => this.value.Value = value; }

        //Set the first value on the comparer
        public float SetFirstComparer { get => compare[0].Value.Value; set => compare[0].Value.Value = value; }

        void OnEnable()
        {
            if (value.Variable) value.Variable.OnValueChanged += Compare;
        }

        void OnDisable()
        {
            if (value.Variable) value.Variable.OnValueChanged -= Compare;
        }

        /// <summary>Compares the Int parameter on this Component and if the condition is made then the event will be invoked</summary>
        public virtual void Compare()
        {
            if (enabled)
            foreach (var item in compare)
                item.ExecuteAdvanceFloatEvent(value);
        }


        /// <summary>Compares an given int Value and if the condition is made then the event will be invoked</summary>
        public virtual void Compare(float value)
        {
            if (enabled)
                foreach (var item in compare)
                item.ExecuteAdvanceFloatEvent(value);
        }

        /// <summary>Compares an given intVar Value and if the condition is made then the event will be invoked</summary>
        public virtual void Compare(FloatVar value)
        {
            if (enabled)
                foreach (var item in compare)
                item.ExecuteAdvanceFloatEvent(value.Value);
        }
    }
}