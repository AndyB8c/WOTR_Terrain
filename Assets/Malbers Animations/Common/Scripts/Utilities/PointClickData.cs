using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MalbersAnimations.Controller
{
    public class PointClickData : ScriptableObject
    {
        [HideInInspector] public BaseEventDataEvent baseDataEvent = new BaseEventDataEvent();
        public void Invoke(BaseEventData data) => baseDataEvent.Invoke(data);
    }

    [System.Serializable]  public class BaseEventDataEvent : UnityEvent<BaseEventData> { }
}