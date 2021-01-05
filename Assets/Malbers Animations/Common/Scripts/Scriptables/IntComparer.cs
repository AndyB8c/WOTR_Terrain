using MalbersAnimations.Scriptables;
using MalbersAnimations.Events;
using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations
{
    public class IntComparer : MonoBehaviour
    {
        public IntReference value = new IntReference(0);
        public List<AdvancedIntegerEvent> compare = new List<AdvancedIntegerEvent>();

        public int Value { get => value.Value; set => this.value.Value = value; }
      
        /// <summary>Set the first value on the comparer </summary>
        public int SetFirstComparer { get => compare[0].Value.Value; set => compare[0].Value.Value = value; }

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
            foreach (var item in compare)
                item.ExecuteAdvanceIntegerEvent(value);
        }


        /// <summary>Compares an given int Value and if the condition is made then the event will be invoked</summary>
        public virtual void Compare(int value)
        {
            foreach (var item in compare)
                item.ExecuteAdvanceIntegerEvent(value);
        }

        /// <summary>Compares an given intVar Value and if the condition is made then the event will be invoked</summary>
        public virtual void Compare(IntVar value)
        {
            foreach (var item in compare)
                item.ExecuteAdvanceIntegerEvent(value.Value);
        }
    }
}