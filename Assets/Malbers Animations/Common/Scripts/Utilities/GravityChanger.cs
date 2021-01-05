using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations
{
    public class GravityChanger : MonoBehaviour
    {
        IGravity animal;
        protected Collider Other;
        void OnTriggerEnter(Collider other)
        {
            Other = other;
            animal = other.GetComponentInParent<IGravity>();
        }

        void Update()
        {
            if (animal != null)
            {
                animal.Gravity = (transform.position - Other.transform.position).normalized;
            }
        }

        //void OnTriggerStay(Collider other)
        //{
        //    if (animal != null)
        //    {
        //        animal.GravityDirection = (transform.position - other.transform.position).normalized;
        //    }
        //}

        void OnTriggerExit(Collider other)
        {
            ResetAnimal();
        }

        public virtual void ResetAnimal()
        {
            if (animal != null)    animal.Gravity = Vector3.down;

            animal = null;
            Other = null;
        }
    }
}