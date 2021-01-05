using UnityEngine;

namespace MalbersAnimations
{  
    public interface IMLayer
    {  
        /// <summary>Layers to Interact</summary>
        LayerMask Layer { get; set; }

        /// <summary>What to do with the Triggers ... Ignore them? Use them?</summary>
        QueryTriggerInteraction TriggerInteraction { get; set; }
    }
}