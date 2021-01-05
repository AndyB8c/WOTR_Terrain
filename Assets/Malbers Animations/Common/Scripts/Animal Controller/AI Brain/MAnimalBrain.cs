using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations.Events;
using UnityEngine.AI;
using MalbersAnimations.Scriptables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations.Controller.AI
{
    //[RequireComponent(typeof(MAnimalAIControl))]
    public class MAnimalBrain : MonoBehaviour, IAnimatorListener
    {
        /// <summary>Reference for the Ai Control Movement</summary>
        [RequiredField] public MAnimalAIControl AIMovement;
        /// <summary>Transform used to raycast Rays to interact with the world</summary>
        [RequiredField, Tooltip("Transform used to raycast Rays to interact with the world")]
        public Transform Eyes;
        /// <summary>Time needed to make a new transition. Necesary to avoid Changing to multiple States in the same frame</summary>
        [Tooltip("Time needed to make a new transition. Necesary to avoid Changing to multiple States in the same frame")]
        public FloatReference TransitionCoolDown = new FloatReference(0.2f);

        /// <summary>Reference AI State for the animal</summary>
        public MAIState currentState;

        ///// <summary>Reference of an Empty State</summary>
        public MAIState remainInState;

        [Space, Tooltip("Removes all AI Components when the Animal Dies. (Brain, AiControl, Agent)")]
        public bool RemoveAIOnDeath = true;
        public bool debug = true;


        public IntEvent OnTaskStarted = new IntEvent();
        public IntEvent OnDecisionSucceded = new IntEvent();
        public IntEvent OnAIStateChanged = new IntEvent();


        /// <summary>Last Time the Animal make a new transition</summary>
        private float TransitionLastTime;

        /// <summary>Last Time the Animal  started a transition</summary>
        public float StateLastTime { get; set; }

        /// <summary>Tasks Local Vars (1 Int,1 Bool,1 Float)</summary>
        internal BrainVars[] TasksVars;
        /// <summary>Saves on the a Task that it has finish is stuff</summary>
        internal bool[] TasksDone;
        /// <summary>Store if a Task has Started</summary>
        internal bool[] TasksStarted;
        /// <summary>Decision Local Vars to store values on Prepare Decision</summary>
        internal BrainVars[] DecisionsVars;

        #region Properties


        /// <summary>Reference for the Animal</summary>
        public MAnimal Animal => AIMovement.animal;

        /// <summary>Reference for the AnimalStats</summary>
        public Dictionary<int, Stat> AnimalStats { get; set; }

        #region Target References
        /// <summary>Reference for the Current Target the Animal is using</summary>
        public Transform Target { get; set; }
        //{ 
        //    get => target; 
        //    set 
        //    { target = value;
        //    }
        //}
        //private Transform target;

        /// <summary>Reference for the Target the Animal Component</summary>
        public MAnimal TargetAnimal { get; set; }

        public Vector3 AgentPosition => AIMovement.Agent.transform.position;
        public NavMeshAgent Agent => AIMovement.Agent;

        /// <summary>Has the Animal Arrived to the Target Position... [NOT THE DESTINATION POSITION] </summary>
        public bool ArrivedToTarget => Target ? (Vector3.Distance(AgentPosition, AIMovement.GetTargetPosition()) < AIMovement.GetTargetStoppingDistance()) : false;

        public float AgentHeight { get; private set; }

        /// <summary>True if the Current Target has Stats</summary>
        public bool TargetHasStats { get; private set; }

        /// <summary>Reference for the Target the Stats Component</summary>
        public Dictionary<int, Stat> TargetStats { get; set; }
        #endregion

        /// <summary>Reference for the Last WayPoint the Animal used</summary>
        public IWayPoint LastWayPoint { get; set; }

        /// <summary>Time Elapsed for the Tasks on an AI State</summary>
        [HideInInspector] public float[] TasksTime;// { get; set; }

        /// <summary>Time Elapsed for the State Decisions</summary>
        [HideInInspector] public float[] DecisionsTime;// { get; set; }

        #endregion


        void Awake()
        {
            if (AIMovement == null) AIMovement = this.FindComponent<MAnimalAIControl>();
            var AnimalStatscomponent = this.FindComponent<Stats>();
            if (AnimalStatscomponent) AnimalStats = AnimalStatscomponent.stats_D;

            AgentHeight = transform.lossyScale.y * AIMovement.Agent.height;
        }

        void Start()
        {
            Animal.isPlayer.Value = false; //If is using a brain... disable that he is the main player

            if (currentState)
            {
                StartNewState(currentState); 
            }
            else
            {
                enabled = false;
                return;
            }
            AIMovement.AutoNextTarget = false;

            LastWayPoint = null;

            if (AIMovement.Target)
                SetLastWayPoint(AIMovement.Target);
        }


        void Update() { currentState?.Update_State(this); }

        public virtual void TransitionToState(MAIState nextState, bool decisionValue, MAIDecision decision)
        {
            if (Time.time - TransitionLastTime >= TransitionCoolDown) //This avoid making transition on the same Frame ****IMPORTANT
            {
                if (nextState != null && nextState != remainInState)
                {
                    TransitionLastTime = Time.time;

                    if (debug)
                        Debug.Log($"Changed AI State from <B>[{currentState.name}]</B> to <B>[{nextState.name}]</B>. Decision: <b>[{decision.name}]</b> = <B>[{decisionValue}]</B>.");

                    InvokeDecisionEvent(decisionValue, decision);

                    StartNewState(nextState);
                }
            }
        }

        private void InvokeDecisionEvent(bool decisionValue, MAIDecision decision)
        {
            if (decision.send == MAIDecision.WSend.SendTrue && decisionValue)
            {
                OnDecisionSucceded.Invoke(decision.MessageID);
            }
            else if (decision.send == MAIDecision.WSend.SendFalse && !decisionValue)
            {
                OnDecisionSucceded.Invoke(decision.MessageID);
            }
        }

        public virtual void StartNewState(MAIState newState)
        {
            StateLastTime = Time.time;      //Store the last time the Animal made a transition


            if (currentState != null && currentState != newState)
            {
                currentState.Finish_Tasks(this);                 //Finish all the Task on the Current State
                currentState.Finish_Decisions(this);             //Finish all the Decisions on the Current State
            }

            currentState = newState;                            //Set a new State

            ResetVarsOnNewState();

            OnAIStateChanged.Invoke(currentState.ID);
            currentState.Start_AIState(this);                      //Start all Tasks on the new State
            currentState.Prepare_Decisions(this);               //Start all Tasks on the new State
        }


        /// <summary>Prepare all the local variables on the New State before starting new tasks</summary>
        private void ResetVarsOnNewState()
        {
            TasksVars = new BrainVars[currentState.tasks.Length];                //Local Variables you can use on your tasks
            TasksTime = new float[currentState.tasks.Length];                    //Reset all the Tasks    Time elapsed time

            TasksDone = new bool[currentState.tasks.Length];                     //Reset if they are Done
            TasksStarted = new bool[currentState.tasks.Length];                  //Reset if they tasks are started

            DecisionsVars = new BrainVars[currentState.transitions.Length];      //Local Variables you can use on your Decisions
            DecisionsTime = new float[currentState.transitions.Length];          //Reset all the Decisions Time elapsed time
        }


        public bool IsTaskDone(int TaskIndex) => TasksDone[TaskIndex];

        public void TaskDone(int TaskIndex, bool value = true)
        {
            TasksDone[TaskIndex] = value;

            if (TaskIndex + 1 < currentState.tasks.Length)
            {
                currentState.StartTaks(this, TaskIndex + 1);
            }
        }

        

        /// <summary> Check if the time elapsed of a task using a duration or CountDown time </summary>
        /// <param name="duration">Duration of the countDown|CoolDown</param>
        /// <param name="index">Index of the Task on the AI State Tasks list</param>
        public bool CheckIfDecisionsCountDownElapsed(float duration, int index)
        {
            DecisionsTime[index] += Time.deltaTime;
            return DecisionsTime[index] >= duration;
        }

        /// <summary>Reset the Time elapsed on a Task using its index from the Tasks list</summary>
        /// <param name="Index">Index of the Task on the AI state</param>
        public void ResetTaskTime(int Index) => TasksTime[Index] = 0;
        public void SetElapsedTaskTime(int Index) => TasksTime[Index] = Time.time;




        /// <summary>Reset the Time elapsed on a Decision using its index from the Transition List </summary>
        /// <param name="Index">Index of the Decision on the AI State Transition List</param>
        public void ResetDecisionTime(int Index) => DecisionsTime[Index] = 0;

        /// <summary>Removes the Target on the Animal</summary>
        public void RemoveTarget() => AIMovement.SetTarget(null, false);

        public virtual bool OnAnimatorBehaviourMessage(string message, object value) =>
            this.InvokeWithParams(message, value);

        #region Event Listeners
        void OnEnable()
        {
            AIMovement.OnTargetArrived.AddListener(OnTargetArrived);
            AIMovement.OnTargetPositionArrived.AddListener(OnPositionArrived);
            AIMovement.OnTargetSet.AddListener(OnTargetSet);

            Animal.OnStateChange.AddListener(OnAnimalStateChange);
            Animal.OnStanceChange.AddListener(OnAnimalStanceChange);
            Animal.OnModeStart.AddListener(OnAnimalModeStart);
            Animal.OnModeEnd.AddListener(OnAnimalModeEnd);
        }

        void OnDisable()
        {
            AIMovement.OnTargetArrived.RemoveListener(OnTargetArrived);
            AIMovement.OnTargetPositionArrived.RemoveListener(OnPositionArrived);

            Animal.OnStateChange.RemoveListener(OnAnimalStateChange);
            Animal.OnStanceChange.RemoveListener(OnAnimalStanceChange);
            Animal.OnModeStart.RemoveListener(OnAnimalModeStart);
            Animal.OnModeEnd.RemoveListener(OnAnimalModeEnd);
        }
        #endregion

        #region SelfAnimal Event Listeners
        void OnAnimalStateChange(int state)
        {
            currentState?.OnAnimalStateEnter(this, Animal.ActiveState);
            currentState?.OnAnimalStateExit(this, Animal.LastState);

            if (state == StateEnum.Death) //meaning this animal has died
            {
                for (int i = 0; i < currentState.tasks.Length; i++)         //Exit the Current Tasks
                    currentState.tasks[i].ExitAIState(this, i);

                enabled = false;

                if (RemoveAIOnDeath)
                {
                    Destroy(AIMovement.Agent.gameObject);
                    Destroy(AIMovement);
                    Destroy(this);
                }
            }
        }


        void OnAnimalStanceChange(int stance) => currentState.OnAnimalStanceChange(this, Animal.Stance);
        void OnAnimalModeStart(int mode, int ability) => currentState.OnAnimalModeStart(this, Animal.ActiveMode);
        void OnAnimalModeEnd(int mode, int ability) => currentState.OnAnimalModeEnd(this, Animal.ActiveMode);
        #endregion

        #region TargetAnimal Event Listeners
        void OnTargetAnimalStateChange(int state)
        {
            currentState.OnTargetAnimalStateEnter(this, Animal.ActiveState);
            currentState.OnTargetAnimalStateExit(this, Animal.LastState);
        }

        private void OnTargetArrived(Transform target) => currentState.OnTargetArrived(this, target);

        private void OnPositionArrived(Vector3 position) => currentState.OnPositionArrived(this, position);

        private void OnTargetSet(Transform target)
        {
            Target = target;

            if (target)
            {
                TargetAnimal = target.GetComponentInParent<MAnimal>();// ?? target.GetComponentInChildren<MAnimal>();

                TargetStats = null;
                var TargetStatsC = target.GetComponentInParent<Stats>();// ?? target.GetComponentInChildren<Stats>();

                TargetHasStats = TargetStatsC != null;
                if (TargetHasStats) TargetStats = TargetStatsC.stats_D;

                // SetLastWayPoint(target);
            }
        }

        public bool CheckForPreviusTaskDone(int index)
        {
            if (index == 0) return true;

            if (!TasksStarted[index] && IsTaskDone(index - 1))
                return true;

            return false;
        }

        public void SetLastWayPoint(Transform target)
        {
            LastWayPoint = target.FindComponent<IWayPoint>() ?? LastWayPoint; //If not is a waypoint save the last one
        }

        public void SetLastWayPoint()
        {
            if (Target)
                LastWayPoint = Target.GetComponentInParent<IWayPoint>() ?? LastWayPoint; //If not is a waypoint save the last one
        }

        #endregion


#if UNITY_EDITOR
        void Reset()
        {
            remainInState = MTools.GetInstance<MAIState>("Remain in State");
            AIMovement = this.FindComponent<MAnimalAIControl>();

            if (AIMovement)
            {
                AIMovement.AutoInteract = false;
                AIMovement.AutoNextTarget = false;
                AIMovement.UpdateTargetPosition = false;
                AIMovement.MoveAgentOnMovingTarget = false;
                AIMovement.LookAtTargetOnArrival = false;

                if (Animal) Animal.isPlayer.Value = false; //Make sure this animal is not the Main Player

            }
            else
            {
                Debug.LogWarning("There's AI Control in this GameObject");
            }
        }

        void OnDrawGizmos()
        {
            if (isActiveAndEnabled && currentState && Eyes)
            {
                Gizmos.color = currentState.GizmoStateColor;
                Gizmos.DrawWireSphere(Eyes.position, 0.2f);

                if (currentState != null  && currentState.tasks != null && debug)
                {
                    foreach (var task in currentState.tasks)
                        task?.DrawGizmos(this);

                    foreach (var tran in currentState.transitions)
                        tran?.decision?.DrawGizmos(this);
                }
            }
        }
#endif 
    }

    public enum Affected { Self, Target }
    public enum ExecuteTask { OnStart, OnUpdate, OnExit }

    public struct BrainVars
    {
        public int intValue;
        public float floatValue;
        public bool boolValue;
        public Vector3 V3Value;
        public MonoBehaviour MonoValue;
        public Component ComponentValue;
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(MAnimalBrain)), CanEditMultipleObjects]
    public class MAnimalBrainEditor : Editor
    {
        SerializedProperty AIMovement, Eyes, debug, TransitionCoolDown, RemoveAIOnDeath,
            currentState, remainInState, OnTaskStarted, OnDecisionSucceded, OnAIStateChanged;
        private void OnEnable()
        {
            AIMovement = serializedObject.FindProperty("AIMovement");
            Eyes = serializedObject.FindProperty("Eyes");
            TransitionCoolDown = serializedObject.FindProperty("TransitionCoolDown");
            RemoveAIOnDeath = serializedObject.FindProperty("RemoveAIOnDeath");
            currentState = serializedObject.FindProperty("currentState");
            remainInState = serializedObject.FindProperty("remainInState");

            OnTaskStarted = serializedObject.FindProperty("OnTaskStarted");
            OnDecisionSucceded = serializedObject.FindProperty("OnDecisionSucceded");
            OnAIStateChanged = serializedObject.FindProperty("OnAIStateChanged");
            debug = serializedObject.FindProperty("debug");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(AIMovement, new GUIContent("AI Control"));
            EditorGUILayout.PropertyField(Eyes);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("AI States", EditorStyles.boldLabel);
            MTools.DrawScriptableObject(currentState, false);
            EditorGUILayout.PropertyField(remainInState);
            EditorGUILayout.PropertyField(TransitionCoolDown);
            EditorGUILayout.PropertyField(RemoveAIOnDeath);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(OnAIStateChanged);
            EditorGUILayout.PropertyField(OnTaskStarted);
            EditorGUILayout.PropertyField(OnDecisionSucceded);
            EditorGUILayout.EndVertical();

            EditorGUILayout.PropertyField(debug);
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}