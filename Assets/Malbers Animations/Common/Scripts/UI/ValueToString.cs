using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using MalbersAnimations.Events;

namespace MalbersAnimations.Utilities
{
    /// <summary>Converts a value to string </summary>
    public class ValueToString : MonoBehaviour
    {
        public int FloatDecimals = 2;
        public StringEvent toString = new StringEvent();


        public virtual void ToString(float value)
        {
            var F = "F" + FloatDecimals;

            toString.Invoke(value.ToString(F));
        }

        public virtual void ToString(int value)
        {
            toString.Invoke(value.ToString());
        }

        public virtual void ToString(bool value)
        {
            toString.Invoke(value.ToString());
        }

        public virtual void ToString(Transform value)
        {
            toString.Invoke(value.ToString());
        }

        public virtual void ToString(Vector3 value)
        {
            toString.Invoke(value.ToString());
        }
    }
}