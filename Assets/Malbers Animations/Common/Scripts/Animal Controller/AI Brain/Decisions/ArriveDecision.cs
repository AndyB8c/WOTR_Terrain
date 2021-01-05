using MalbersAnimations.Scriptables;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MalbersAnimations.Controller.AI
{
    [CreateAssetMenu(menuName = "Malbers Animations/Pluggable AI/Decision/Arrived to Target")]
    public class ArriveDecision : MAIDecision
    {
        [Space,Tooltip("(OPTIONAL)Use it if you want to know if we have arrived to a specific Target")]
        public string TargetName = string.Empty;
        public float MinTargetHeight = 1f;


        public override bool Decide(MAnimalBrain brain, int index)
        {
            if (string.IsNullOrEmpty(TargetName))
            {
                return brain.AIMovement.HasArrived && brain.AIMovement.TargetHeight <= MinTargetHeight;
            }
            else
            {
                return brain.AIMovement.HasArrived && brain.AIMovement.TargetHeight <= MinTargetHeight && brain.Target.root.name == TargetName; //If we are looking for an specific Target
            }
        }
    }
}