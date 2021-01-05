using UnityEngine;
using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using System.Collections.Generic; 

namespace MalbersAnimations.Utilities
{
    /// <summary>
    /// This is used when the collider is in a different gameObject and you need to check the Collider Events
    /// Create this component at runtime and subscribe to the UnityEvents
    /// </summary>
    public class TriggerProxy : MonoBehaviour, IMLayer
    {
        //[SerializeField] private BoolReference active = new BoolReference(true);
        [Tooltip("Hit Layer for the Trigger Proxy")]
        [SerializeField] private LayerReference hitLayer = new LayerReference(-1);
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;


        public ColliderEvent OnTrigger_Enter = new ColliderEvent();
        public ColliderEvent OnTrigger_Exit = new ColliderEvent();

        protected List<Collider> m_colliders = new List<Collider>();

        public bool Active { get =>enabled; set => enabled = value; }
        public LayerMask Layer { get => hitLayer.Value; set => hitLayer.Value = value; }
        public QueryTriggerInteraction TriggerInteraction { get => triggerInteraction; set => triggerInteraction = value; }
       // public Collider OwnCollider { get; private set; }

        private Transform CurrentRoot;

        public bool TrueConditions(Collider other)
        {
            if (!Active) return false;
            if (triggerInteraction == QueryTriggerInteraction.Ignore && other.isTrigger) return false;
            if (!MTools.Layer_in_LayerMask(other.gameObject.layer, Layer)) return false;
            if (transform.IsChildOf(other.transform)) return false; // you are 

            return true;
        }


        //private void Start()
        //{
        //    OwnCollider = GetComponent<Collider>();
        //}

        private void OnDisable()
        {
            ResetTrigger();
        }

        void OnTriggerEnter(Collider other)
        {
           // Debug.Log("OnTriggerEnter" + other.transform.name);

            if (TrueConditions(other))
            {
                Transform newRoot = other.transform.root;                              //Get the animal on the entering collider


                if (m_colliders.Find(coll => coll == other) == null)                               //if the entering collider is not already on the list add it
                    m_colliders.Add(other);

                if (CurrentRoot == newRoot)
                {
                    return;
                }
                else
                {
                    if (CurrentRoot) m_colliders = new List<Collider>();                            //Clean the colliders
                    CurrentRoot = newRoot;
                    OnTrigger_Enter.Invoke(other);
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (TrueConditions(other))
            {
                if (m_colliders.Find(item => item == other))                       //Remove the collider from the list that is exiting the zone.
                    m_colliders.Remove(other);


                if (m_colliders.Count == 0)                                        //When all the collides are removed from the list..
                {
                    OnTrigger_Exit.Invoke(other);
                    ResetTrigger();
                }
            }
        }

        public void ResetTrigger()
        {
            m_colliders = new List<Collider>();
            CurrentRoot = null;
        }

        private void Reset()
        {
            var collider = GetComponent<Collider>();
            Active = true;

            if (collider)
                collider.isTrigger = true;
            else
                Debug.LogWarning("This Script requires a Collider, please add any type of collider");
        }

        public void SetLayer(LayerMask mask, QueryTriggerInteraction triggerInteraction)
        {
            TriggerInteraction = triggerInteraction;
            Layer = mask;
        }
    } 
}