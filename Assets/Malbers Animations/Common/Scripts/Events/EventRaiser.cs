using UnityEngine; 
namespace MalbersAnimations.Events
{  
    public class EventRaiser : MonoBehaviour
    {
        public UnityEngine.Events.UnityEvent onEnable;
        public void OnEnable() => onEnable.Invoke();
    }
}