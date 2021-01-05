using UnityEngine;
using MalbersAnimations.Events;

namespace MalbersAnimations
{
    /// <summary>makes an UI Object to follow a World Object</summary>
    public class UIFollowTransform : MonoBehaviour
    {
        private Camera MainCamera;
        public Transform WorldTransform;
        [Tooltip("Reset the World Transform to Null when this component is Disable")]
        public bool ResetOnDisable = false;

        public Vector3 ScreenCenter { get; set; }
        public Vector3 DefaultScreenCenter { get; set; }
 

        void Awake()
        {
            MainCamera = MTools.FindMainCamera();
            ScreenCenter = transform.position;
            DefaultScreenCenter = transform.position;
        }

        private void OnEnable()
        {
            MainCamera = MTools.FindMainCamera();
            Align();
        }

        private void OnDisable()
        {
            if (ResetOnDisable)
            {
                WorldTransform = null;
                Align();
            }
        }

        public void SetTransform(Transform newTarget)
        {
            WorldTransform = newTarget;
            Align();
        }
        public void SetScreenCenter(Vector3 newScreenCenter)
        {
            ScreenCenter = newScreenCenter;
            Align();
        }



        void FixedUpdate()
        {
            Align();
        }

        public void Align()
        {
            if (MainCamera == null) return;
            transform.position = WorldTransform != null ? MainCamera.WorldToScreenPoint(WorldTransform.position) : ScreenCenter;
        }


#if UNITY_EDITOR

        void Reset()
        {
            MEventListener MeventL = GetComponent<MEventListener>();

            if (MeventL == null)
            {
                MeventL = gameObject.AddComponent<MEventListener>();
            }

            MeventL.Events = new System.Collections.Generic.List<MEventItemListener>(1) { new MEventItemListener() };

            var listener = MeventL.Events[0];

            listener.useTransform = true;
            listener.useVector3 = true;
            listener.useVoid = false;

            listener.Event = MTools.GetInstance<MEvent>("Follow UI Transform");

            if (listener.Event != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(listener.ResponseTransform, SetTransform);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(listener.ResponseVector3, SetScreenCenter);
            }

        }
#endif
    }
}