using System.Linq;
using UnityEngine;

namespace MalbersAnimations.Controller.AI
{
    [CreateAssetMenu(menuName = "Malbers Animations/Pluggable AI/Decision/Check Var Listener", order = 4)]
    public class CheckVarListener : MAIDecision
    {
        public enum Affected { Self, Target,Tag }

        [Space]
        /// <summary>Range for Looking forward and Finding something</summary>
        [Space, Tooltip("Check the Decision on the Animal(Self) or the Target(Target), or on an object with a tag")]
        public Affected checkOn = Affected.Self;
        [Hide("showTag", true, false)]
        public Tag tag;
      
        
        [Space, Tooltip("Check on the Target or Self if it has a Listener Variable Component <Int><Bool><Float> and compares it with the local variable)")]
        public VarType varType = VarType.Bool;



        [Hide("showBoolValue", true,true)]
        public ComparerInt comparer;

        [Hide("showBoolValue", true)]
        public bool boolValue = true;
        [Hide("showIntValue", true)]
        public int intValue = 0;
        [Hide("showFloatValue", true)]
        public float floatValue = 0f;
        
        [Tooltip("Check the Variable Listener ID Value, when this value is Zero, the ID is ignored")]
        public int ListenerID = 0;

        [HideInInspector] public bool showFloatValue;
        [HideInInspector] public bool showBoolValue = true;
        [HideInInspector] public bool showIntValue;
        [HideInInspector] public bool showTag;


        private void OnValidate()
        {
            showTag = checkOn == Affected.Tag;

            switch (varType)
            {
                case VarType.Bool:
                    showFloatValue = false;
                    showBoolValue = true;
                    showIntValue = false;
                    break;
                case VarType.Int:
                    showFloatValue = false;
                    showBoolValue = false;
                    showIntValue = true;
                    break;
                case VarType.Float:
                    showFloatValue = true;
                    showBoolValue = false;
                    showIntValue = false;
                    break;
                default:
                    break;
            }
        }



        public override void PrepareDecision(MAnimalBrain brain, int Index)
        {
            brain.DecisionsVars[Index].MonoValue = null;

            var objective = (checkOn == Affected.Self) ? brain.transform : brain.Target;

            if (checkOn == Affected.Tag)
            {
                var tagH = Tags.TagsHolders.Find(X => X.HasTag(tag));
                if (tagH != null) objective = tagH.transform;
            }
            if (objective)
            {
                if (ListenerID == 0)
                {
                    switch (varType)
                    {
                        case VarType.Bool:
                            brain.DecisionsVars[Index].MonoValue = objective.GetComponentInParent<BoolVarListener>();
                            break;
                        case VarType.Int:
                            brain.DecisionsVars[Index].MonoValue = objective.GetComponentInParent<IntVarListener>();
                            break;
                        case VarType.Float:
                            brain.DecisionsVars[Index].MonoValue = objective.GetComponentInParent<FloatVarListener>();
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (varType)
                    {
                        case VarType.Bool:
                            brain.DecisionsVars[Index].MonoValue = objective.GetComponentsInParent<BoolVarListener>().ToList().Find(x => x.ID == ListenerID);
                            break;
                        case VarType.Int:
                            brain.DecisionsVars[Index].MonoValue = objective.GetComponentsInParent<IntVarListener>().ToList().Find(x => x.ID == ListenerID);
                            break;
                        case VarType.Float:
                            brain.DecisionsVars[Index].MonoValue = objective.GetComponentsInParent<FloatVarListener>().ToList().Find(x => x.ID == ListenerID);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override bool Decide(MAnimalBrain brain, int Index)
        {
            var listener = brain.DecisionsVars[Index].MonoValue;
            if (listener == null) return false;

            switch (varType)
            {
                case VarType.Bool:
                    return (listener as BoolVarListener).Value == boolValue;
                case VarType.Int:
                    return CompareInteger((listener as IntVarListener).Value);
                case VarType.Float:
                    return CompareFloat((listener as FloatVarListener).Value);
                default:
                    return false;
            }
        }

        public override void FinishDecision(MAnimalBrain brain, int Index)
        {
            brain.DecisionsVars[Index].MonoValue = null;
        }

        public enum VarType { Bool, Int, Float }
        public enum BoolType { True, False }


        public bool CompareInteger(int IntValue)
        {
            switch (comparer)
            {
                case ComparerInt.Equal:
                    return (IntValue == intValue);
                case ComparerInt.Greater:
                    return (IntValue > intValue);
                case ComparerInt.Less:
                    return (IntValue < intValue);
                case ComparerInt.NotEqual:
                    return (IntValue != intValue);
                default:
                    return false;
            }
        }

        public bool CompareFloat(float IntValue)
        {
            switch (comparer)
            {
                case ComparerInt.Equal:
                    return (IntValue == floatValue);
                case ComparerInt.Greater:
                    return (IntValue > floatValue);
                case ComparerInt.Less:
                    return (IntValue < floatValue);
                case ComparerInt.NotEqual:
                    return (IntValue != floatValue);
                default:
                    return false;
            }
        }
    }
}
