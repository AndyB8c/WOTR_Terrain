using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MalbersAnimations.Utilities;
using UnityEngine.Events;
using MalbersAnimations.Events;
 
using UnityEngine.AI;
using MalbersAnimations.Scriptables;
 

namespace MalbersAnimations.Controller
{
    public class MAnimalAIControl : MonoBehaviour, IAIControl, IAITarget
    {
        [HideInInspector] public int Editor_Tabs1;

        #region Components References
#pragma warning disable CS0649 // Mark all non-serializable fields CS0649
        [SerializeField, RequiredField] private NavMeshAgent agent;                 //The NavMeshAgent
#pragma warning restore CS0649 

        [RequiredField] public MAnimal animal;                    //The Animal Script
        #endregion

        #region Internal Variables
        /// <summary>Target Last Position (Useful to know if the Target is moving)</summary>
        protected Vector3 TargetLastPosition = MTools.NullVector;

        /// <summary>Stores the Remainin distance to the Target's Position</summary>
        public virtual float RemainingDistance { get; private set; }

        /// <summary>Stores the Remainin distance to the Target's Position</summary>
        protected float DefaultStopDistance;

        /// <summary>When Assigning a Target it will automatically start moving</summary>       
        public virtual bool MoveAgent  { get; set; }
        //{
        //    get => m_moveAgent;
        //    set
        //    {
        //        Debug.Log(m_moveAgent);
        //        m_moveAgent = value;
        //    }
        //}
        //bool m_moveAgent;

        private IEnumerator I_WaitToNextTarget;
        private IEnumerator IFlyOffMesh;
        private IEnumerator IClimbOffMesh;
        #endregion

        #region Public Variables
        [SerializeField] protected float stoppingDistance = 0.6f;

        /// <summary>The animal will change automatically to Walk if the distance to the target is this value</summary>
        [SerializeField] protected float walkDistance = 1f;

        internal void ResetStoppingDistance() { StoppingDistance = DefaultStopDistance; }

        [SerializeField] private Transform target;

        /// <summary>Means the Animal will go to a next Target when it reaches the current target automatically</summary>
        public bool AutoNextTarget = true;
        /// <summary>Means the Animal will interact to any Waypoint automatically when he arrived to it</summary>
        public bool AutoInteract = true;

        /// <summary>If the Target Moves the Agent will move too ... to the given destination</summary>
        public bool MoveAgentOnMovingTarget = true;

        public void SetMoveAgentOnMovingTarget(bool value) => MoveAgentOnMovingTarget = value;

        /// <summary>The Animal will Rotate/Look at the Target when he arrives to it</summary>
        public bool LookAtTargetOnArrival = false;

        /// <summary>Check if the Animal Moved every x Seconds</summary>
        public float MovingTargetInterval = 0.2f;

        /// <summary>If the Target moves then it will foollow it</summary>
        [SerializeField] private bool updateTargetPosition = true;

        public bool debug = false;
        public bool debugGizmos = true;
        #endregion

        #region Properties 
        /// <summary>is the Animal, Flying, swimming, On Free Mode?</summary>
        public  bool FreeMove { get; private set; }

        /// <summary>Is the Animal Playing a mode</summary>
        public bool IsOnMode { get; private set; }

         
        /// <summary>Has the animal Arrived to their current destination</summary>
        public bool HasArrived { get; internal set; }
        //{
        //    get => hasArriv;
        //    internal set
        //    {
        //        hasArriv = value;
        //        Debug.Log(hasArriv);
        //    }
        //}
        //private bool hasArriv;


        public virtual bool IsGrounded { get; private set; }

        public virtual bool IsMovingOffMesh { get; private set; }

        /// <summary>Is the Target a WayPoint?</summary>
        public IWayPoint IsWayPoint { get; private set; }

        /// <summary>Destination Point == NullVector which means that the Point is Empty </summary>
      //  public bool NullDestination => DestinationPosition == NullVector;


        /// <summary>Is the Target an AITarget</summary>
        public IAITarget IsAITarget { get; private set; }
        #endregion 

        #region Events
        [Space]
        public Vector3Event OnTargetPositionArrived = new Vector3Event();
        public TransformEvent OnTargetArrived = new TransformEvent();
        public TransformEvent OnTargetSet = new TransformEvent();
        #endregion

        #region Properties

        public WayPointType pointType = WayPointType.Ground;

        public virtual WayPointType TargetType => pointType;


        /// <summary>Reference of the Nav Mesh Agent</summary>
        public virtual NavMeshAgent Agent => agent;
       

        /// <summary>Current Stopping Distance for the Next Waypoint</summary>
        public virtual float StoppingDistance
        {
            get => stoppingDistance;
            set => Agent.stoppingDistance = stoppingDistance = value;
        }

        /// <summary>Get the Position of this AI target</summary>
        public virtual Vector3 GetPosition() => Agent.transform.position;


        /// <summary>is the Target transform moving??</summary>
        public virtual bool TargetIsMoving { get; private set; }


        /// <summary> Is the Animal waiting x time to go to the Next waypoint</summary>
        public virtual bool IsWaiting { get; private set; }

        /// <summary>Destination Position to use on Agent.SetDestination()</summary>
        public virtual Vector3 DestinationPosition { get; private set; }

        public virtual Transform NextTarget { get; set; }
        public virtual Transform Target => target;

        /// Height Diference from the Target and the Agent
        public virtual float TargetHeight => Mathf.Abs(Target.position.y - Agent.transform.position.y);

        /// <summary>Update The Target Position from the Target. This should be false if the Position you want to go is different than the Target's position</summary>
        public virtual bool UpdateTargetPosition { get => updateTargetPosition; set => updateTargetPosition = value; }

        #endregion

        public virtual void SetActive(bool value) => enabled = value;


        #region Unity Functions
        protected virtual void OnEnable()
        {
            if (animal == null) animal = this.FindComponent<MAnimal>();

            animal.OnStateChange.AddListener(OnState);
            animal.OnModeStart.AddListener(OnModeStart);
            animal.OnModeEnd.AddListener(OnModeEnd);
            animal.OnGrounded.AddListener(OnGrounded);
            StartAgent();
        }

        protected virtual void OnDisable()
        {
            animal.OnStateChange.RemoveListener(OnState);           //Listen when the Animations changes..
            animal.OnModeStart.RemoveListener(OnModeStart);           //Listen when the Animations changes..
            animal.OnModeEnd.RemoveListener(OnModeEnd);           //Listen when the Animations changes..
            animal.OnGrounded.RemoveListener(OnGrounded);           //Listen when the Animations changes..

            Stop();
            StopAllCoroutines();
        }

        protected virtual void Update() { Updating(); }
        #endregion

        public virtual void StartAgent()
        {
            Agent.updateRotation = false;                                       //The Animal will control the rotation . NOT THE AGENT
            Agent.updatePosition = false;                                       //The Animal will control the  postion . NOT THE AGENT
            DefaultStopDistance = StoppingDistance;                             //Store the Started Stopping Distance
            Agent.stoppingDistance = StoppingDistance;
            OnGrounded(animal.Grounded);

            HasArrived = false;
            TargetIsMoving = false;
            IsWaiting = false;
            var targ = target;
            target = null;
            SetTarget(targ);                                                  //Set the first Target (IMPORTANT)  it also set the next future targets

            InvokeRepeating("CheckMovingTarget", 0f, MovingTargetInterval);
        }

        public virtual void Updating()
        {
            if (IsMovingOffMesh) return;                                    //Do nothing while is moving ofmesh (THE Coroutine is in charge of the movement)

            Agent.nextPosition = agent.transform.position;                  //Update the Agent Position to the Transform position   IMPORTANT!!!!

            if (MovingTargetInterval == 0) CheckMovingTarget();

            if (FreeMove)
            {
                //Debug.Log("FreeMovement");
                FreeMovement();
            }
            else if (IsGrounded)                                               //if we are on a NAV MESH onGround
            {
                if (IsWaiting || IsOnMode)
                {
                    return;    //If the Animal is Waiting do nothing . .... he is doing something else... wait until he's finish
                }
                else
                    UpdateAgent();
            }
        }

        /// <summary> Check if the Target is moving </summary>
        public virtual void CheckMovingTarget()
        {
            if (target)
            {
                TargetIsMoving = (target.position - TargetLastPosition).magnitude > 0.005f;
                TargetLastPosition = target.position;

                if (TargetIsMoving)
                    Update_TargetPos();
            }
        }
        
        /// <summary> Updates the Agents using he animation root motion </summary>
        public virtual void UpdateAgent()
        {
            if (Agent.pathPending || !Agent.isOnNavMesh)
                return;    //Means is still calculating the path to go

            if (Agent.isStopped) return; // The Agent is not Moving

            RemainingDistance = Agent.remainingDistance;                //Store the remaining distance -- but if navMeshAgent is still looking for a path Keep Moving

            if (RemainingDistance <= StoppingDistance)                   //if We Arrive to the Destination
            {
                if (!HasArrived)
                {
                    Arrive_Destination();
                }
                else        //HasArrived == true
                {
                    if (LookAtTargetOnArrival)
                    {
                        var LookAtDir = (target != null ? target.position : DestinationPosition) - transform.position;
                        animal.RotateAtDirection(LookAtDir);
                    }
                }
            }
            else
            {
                HasArrived = false;
                animal.Move(Agent.desiredVelocity);                     //Move the Animal using the Agent Direction

                CheckWalkDistance();
                CheckOffMeshLinks();
            }
        }

        #region Set Assing Target and Next Targets
        public virtual void SetTarget(GameObject target) => SetTarget(target.transform, true);

        public virtual void SetTarget(Transform target) => SetTarget(target, true);

        /// <summary>Assign a new Target but it does not move it to it</summary>
        public virtual void SetTargetOnly(Transform target) => SetTarget(target, false);


        /// <summary>Moves to the Assigned Target</summary>
        public virtual void MoveToDestination()
        {
            CheckAirTarget();
            Debuging("is travelling to : <B>" + target.name + "</B>");
            MoveAgent = true;
            ResumeAgent();
        }

        /// <summary>Set the next Target</summary>   
        public virtual void SetTarget(Transform target, bool Move)
        {
            IsWaiting = false;
          
            if (animal.IsPlayingMode)
                animal.Mode_Interrupt();             //In Case it was making any Mode;

            this.target = target;

            IsAITarget = null; //Reset the AI Target
            OnTargetSet.Invoke(target);

           // Debug.LogWarning("SetTarget:", target);

            if (target != null)
            {
                TargetLastPosition = target.position; //Since is a new Target "Reset the Target last position"
                DestinationPosition = target.position;                           //Update the Target Position 
                IsAITarget = target.FindComponent<IAITarget>();

                HasArrived = false;

                StoppingDistance = GetTargetStoppingDistance();
                DestinationPosition = GetTargetPosition();

                IsWayPoint = target.GetComponent<IWayPoint>();
                NextTarget = IsWayPoint?.NextTarget();
                RemainingDistance = float.MaxValue;
                MoveAgent = Move;

                MoveToDestination();
            }
            else
            {
                Stop(); //Means the Target is null so Stop the Animal
            }
        }

        public virtual Vector3 GetTargetPosition() => IsAITarget != null ? IsAITarget.GetPosition() : target.position;

        public virtual float GetTargetStoppingDistance() => IsAITarget != null ? IsAITarget.StopDistance() : DefaultStopDistance;

        /// <summary>Set the Target from  on the NextTargets Stored on the Waypoints or Zones</summary>
        public virtual void SetNextTarget()
        {
            if (NextTarget == null)
            {
                Debuging("There's no Next Target");
                Stop();
                return;
            }

            if (IsWayPoint != null)
            {
                if (I_WaitToNextTarget != null) StopCoroutine(I_WaitToNextTarget); //if there's a coroutine active then stop it

                I_WaitToNextTarget = C_WaitToNextTarget(IsWayPoint.WaitTime, NextTarget); //IMPORTANT YOU NEED TO WAIT 1 FRAME ALWAYS TO GO TO THE NEXT WAYPOINT
                StartCoroutine(I_WaitToNextTarget);
            }
            else
            {
                SetTarget(NextTarget);
            }
        }

        /// <summary> Check if the Next Target is a Air Target</summary>
        protected virtual void CheckAirTarget()
        {
            if (IsWayPoint != null && IsWayPoint.TargetType == WayPointType.Air)    //If the animal can fly, there's a new wayPoint & is on the Air
            {
                Debuging(": NextTarget is AIR", NextTarget.gameObject);
                animal.State_Activate(StateEnum.Fly);
                FreeMove = true;
            }
        }
        #endregion


        /// <summary>Set the next Destination Position without having a target</summary>   
        public virtual void SetDestination(Vector3 newDestination, bool Move)
        {
            if (newDestination == DestinationPosition) return; //Means that you're already going to the same point


            IsWaiting = false;
            animal.Mode_Interrupt();             //In Case it was making any Mode;

            if (!target) ResetStoppingDistance(); //If there's no target reset the Stopping Distance


            Debuging("is travelling to : " + newDestination);

            IsWayPoint = null;

            if (I_WaitToNextTarget != null)
                StopCoroutine(I_WaitToNextTarget);                          //if there's a coroutine active then stop it

            MoveAgent = Move;

            DestinationPosition = newDestination;                           //Update the Target Position 
            ResumeAgent();
        }

        /// <summary>Set the next Destination Position without having a target</summary>   
        public virtual void SetDestination(Vector3Var newDestination) => SetDestination(newDestination.Value);
        public virtual void SetDestination(Vector3 PositionTarget) => SetDestination(PositionTarget, true);

        /// <summary> Stop the Agent and the Animal</summary>
        public virtual void Stop()
        {
            //  Debug.Log("Stop");

            if (Agent.isOnNavMesh)
            {
                Agent.isStopped = true;
                Agent.ResetPath();
            }
            if (IsOnMode) animal.Mode_Interrupt(); //Only Stop if the Animal was on a mode

            animal.StopMoving();

            // IsWaiting = false;
            MoveAgent = false;
        }


        /// <summary>Check the Status of the Next Target</summary>
        protected virtual void Arrive_Destination()
        {
            HasArrived = true;
            RemainingDistance = 0;

            OnTargetArrived.Invoke(target);                                 //Invoke the Event On Target Arrived
            OnTargetPositionArrived.Invoke(DestinationPosition);            //Invoke the Event On Target Position Arrived

            if (target)
            {
                Debuging("has arrived to Destination: " + target.name);
            }
            else
            {
                Stop(); //The target was removed
                return;
            }

            if (IsWayPoint != null)     //If we have arrived to a WayPoint
            {
                IsWayPoint.TargetArrived(animal.gameObject);              //Call the method that the Target has arrived to the destination

                if (IsWayPoint.TargetType == WayPointType.Ground) FreeMove = false;         //if the next waypoing is on the Ground then set the free Movement to false
                if (AutoNextTarget) SetNextTarget();                                        //Set Next Target

            }
            else
            {
                Stop();
            }
        }


        public virtual void MoveToTarget()
        {
            if (IsAITarget != null)
            {
                UpdateTargetPosition = true;           // Make Sure to update the Target position on the Animal
                MoveAgent = true;                      // If the Target is mooving then Move/Resume the Agent...
                MoveAgentOnMovingTarget = true;        // If the Target is mooving then Move/Resume the Agent...
                LookAtTargetOnArrival = true;
                StoppingDistance = IsAITarget.StopDistance();  //Restore the Stopping Distance
                animal.LockMovement = false;
                ResumeAgent();
            }
        }


        /// <summary>Resume the Agent component</summary>
        public virtual void ResumeAgent()
        {
            if (!FreeMove)
                Agent.enabled = true;

            //IsGrounded = animal.Grounded;

            if (!Agent.isOnNavMesh) return;                             //No nothing if we are not on a Nav mesh or the Agent is disabled

            // if (!NullDestination)
            {
                Agent.SetDestination(DestinationPosition);                       //If there's a position to go to set it as destination
                Agent.isStopped = !MoveAgent;                                    //Start the Agent again
            }

             //Debug.LogWarning("ResumeAgent:"+ DestinationPosition); 
        }

        /// <summary>Update The Target Position </summary>
        protected virtual void Update_TargetPos()
        {
            if (UpdateTargetPosition)
            {
                // StoppingDistance = GetTargetStoppingDistance();         //Update also the Stopping Distance
                DestinationPosition = GetTargetPosition();              //Update the Target Position 
                MoveAgent = MoveAgentOnMovingTarget;

                ResumeAgent();
            }
        }


        protected virtual void FreeMovement()
        {
            if (IsWaiting) return;
            if (!target /*|| NullDestination*/) return;      //If we have no were to go then Skip the code

            RemainingDistance = target ? Vector3.Distance(animal.transform.position, target.position) : 0;

            var Direction = (target.position - animal.transform.position).normalized;

            animal.Move(Direction);

            if (RemainingDistance < StoppingDistance)   //We arrived to our destination
                Arrive_Destination();
        }

        protected virtual void CheckOffMeshLinks()
        {
            if (Agent.isOnOffMeshLink /*&& !EnterOFFMESH*/)                         //Check if the Agent is on a OFF MESH LINK
            {
                // EnterOFFMESH = true;                                            //Just to avoid entering here again while we are on a OFF MESH LINK
                OffMeshLinkData OMLData = Agent.currentOffMeshLinkData;

                if (OMLData.linkType == OffMeshLinkType.LinkTypeManual)              //Means that it has a OffMesh Link component
                {
                    OffMeshLink CurrentOML = OMLData.offMeshLink;                    //Check if the OffMeshLink is a Manually placed  Link

                    Zone IsOffMeshZone =
                        CurrentOML.GetComponentInParent<Zone>();                     //Search if the OFFMESH IS An ACTION ZONE (EXAMPLE CRAWL)

                    if (IsOffMeshZone)                                               //if the OffmeshLink is a zone and is not making an action
                    {
                        IsOffMeshZone.CurrentAnimal = animal;
                        IsOffMeshZone.ActivateZone(true);                            //Activate the Zone
                        return;
                    }


                    var DistanceEnd = (transform.position - CurrentOML.endTransform.position).sqrMagnitude;
                    var DistanceStart = (transform.position - CurrentOML.startTransform.position).sqrMagnitude;


                    //Debug.Log("OMMESH FLY");

                    if (CurrentOML.CompareTag("Fly"))
                    {
                        var FarTransform = DistanceEnd > DistanceStart ? CurrentOML.endTransform : CurrentOML.startTransform;
                        //Debug.Log("OMMESH FLY");

                        FlyOffMesh(FarTransform);
                    }
                    else if (CurrentOML.CompareTag("Climb"))
                    {
                        StartCoroutine(MTools.AlignTransform_Rotation(transform, CurrentOML.transform.rotation, 0.15f));         //Aling the Animal to the Link Position
                        ClimbOffMesh();
                    }
                    else if (CurrentOML.area == 2)  //2 is Off mesh Jump
                    {
                        var NearTransform = DistanceEnd < DistanceStart ? CurrentOML.endTransform : CurrentOML.startTransform;
                        StartCoroutine(MTools.AlignTransform_Rotation(transform, NearTransform.rotation, 0.15f));         //Aling the Animal to the Link Position
                        animal.State_Activate(StateEnum.Jump); //2 is Jump State                                                              //if the OffMesh Link is a Jump type
                    }
                }
                else if (OMLData.linkType == OffMeshLinkType.LinkTypeJumpAcross)             //Means that it has a OffMesh Link component
                {
                    animal.State_Activate(StateEnum.Jump); //2 is Jump State
                }
            }
        }

        /// <summary>Called when the Animal Enter an Action, Attack, Damage or something similar</summary>
        public virtual void OnModeStart(int ModeID, int ability)
        {
            Debuging("has Started a Mode: <B>" + animal.ActiveMode.ID.name + "</B>. Ability: <B>" + animal.ActiveMode.ActiveAbility.Name + "</B>");

            if (animal.ActiveMode.AllowMovement) return; //Don't stop the Animal if the Mode can make movements
            IsOnMode = true;
            animal.StopMoving();     //Means the Animal is making a Mode
            Agent.enabled = false;
        }

        public virtual void OnModeEnd(int ModeID, int ability)
        {
            IsOnMode = false;

            if (Agent.isOnOffMeshLink)
                Agent.CompleteOffMeshLink();

            ResumeAgent();
        }

        public virtual void OnState(int stateID)
        {
            if (animal.ActiveStateID == StateEnum.Swim)  OnGrounded(true); //Force Grounded to true when is swimming the animal *HACK*
        }

        /// <summary>Check when the Animal changes the Grounded State</summary>
        protected virtual void OnGrounded(bool grounded)
        {
            IsGrounded = grounded;

            CheckAirTarget();        //Check again the Air Target in case it was a miss Grounded.
            if (animal.ActiveStateID == StateEnum.Swim) IsGrounded = true; //Force Grounded to true when is swimming the animal

            if (IsGrounded)
            {
                if (!IsOnMode)
                {
                    Agent.enabled = true;

                    if (!Agent.isOnNavMesh)
                        return;

                    ResetFlyingOffMesh();

                    // EnterOFFMESH = false;

                    if (Agent.isOnOffMeshLink)
                        Agent.CompleteOffMeshLink();

                    FreeMove = false;

                   if (Agent.isStopped) ResumeAgent();
                }
            }
            else
            {
                if (Agent.isOnNavMesh)                  //Needs to pause the AGENT since the animal is no longer on the ground and NavMesh
                    Agent.isStopped = true;

                Agent.enabled = false;
                animal.DeltaAngle = 0;
            }
        }
        protected virtual void FlyOffMesh(Transform target)
        {
            ResetFlyingOffMesh();

            IFlyOffMesh = C_FlyOffMesh(target);
            StartCoroutine(IFlyOffMesh);
        }

        protected virtual void ClimbOffMesh()
        {
            if (IClimbOffMesh != null) StopCoroutine(IClimbOffMesh);
            IClimbOffMesh = C_Climb_OffMesh();
            StartCoroutine(IClimbOffMesh);
        }

        public virtual float StopDistance() => DefaultStopDistance;

        protected virtual void ResetFlyingOffMesh()
        {
            if (IFlyOffMesh != null)
            {
                IsMovingOffMesh = false;
                StopCoroutine(IFlyOffMesh);
                IFlyOffMesh = null;
            }
        }

        /// <summary>Change to walking when the Animal is near the Target Radius (To Avoid going forward(by intertia) while stoping </summary>
        protected virtual void CheckWalkDistance()
        {
            if (IsGrounded && walkDistance > 0)
            {
                if (walkDistance > RemainingDistance)
                {
                    animal.CurrentSpeedIndex = 1; //Set to the lowest speed.
                }
                else
                {
                    if (animal.CurrentSpeedSet != null &&
                        animal.CurrentSpeedIndex != animal.CurrentSpeedSet.StartVerticalIndex.Value)
                        animal.CurrentSpeedIndex = animal.CurrentSpeedSet.StartVerticalIndex.Value;  //Restore the Current Speed Index to the Speed Set
                }
            }
        }

        protected virtual IEnumerator C_WaitToNextTarget(float time, Transform NextTarget)
        {
            IsWaiting = true;
            Debuging("is waiting " + time.ToString("F2") + " seconds");
            Stop();

            yield return null; //SUUUUUUUUUPER  IMPORTANT!!!!!!!!!

            if (time > 0)
                yield return new WaitForSeconds(time);

            SetTarget(NextTarget);
        }

        protected virtual IEnumerator C_FlyOffMesh(Transform target)
        {
            animal.State_Activate(StateEnum.Fly); //Set the State to Fly
            IsMovingOffMesh = true;
            float distance = float.MaxValue;

            while (distance > StoppingDistance)
            {
                animal.Move((target.position - animal.transform.position).normalized);
                distance = Vector3.Distance(animal.transform.position, target.position);
                yield return null;
            }
            animal.ActiveState.AllowExit();

            Debuging("Exit Fly State Off Mesh");




            IsMovingOffMesh = false;
        }

        protected virtual IEnumerator C_Climb_OffMesh()
        {
            animal.State_Activate(StateEnum.Climb); //Set the State to Climb
            IsMovingOffMesh = true;
            yield return null;
            Agent.enabled = false;


            while (animal.ActiveState.ID == StateEnum.Climb)
            {
                animal.MoveWorld(Vector3.forward); //Move Upwards on the Climb
                                                   // distance = Vector3.Distance(animal.transform.position, target.position);
                yield return null;
            }

            Debuging("Exit Climb State Off Mesh");

            IsMovingOffMesh = false;
        }

        protected virtual void Debuging(string Log) { if (debug) Debug.Log($"<B>{animal.name}:</B> " + Log); }

        protected virtual void Debuging(string Log, GameObject obj) { if (debug) Debug.Log($"<B>{animal.name}:</B> "+ Log, obj); }

#if UNITY_EDITOR


        protected virtual void Reset()
        {
            agent = gameObject.FindComponent<NavMeshAgent>();
            animal = gameObject.FindComponent<MAnimal>();

            if (!agent && transform.parent == null)
            {
                var ChildAgent = new GameObject("AI");
                ChildAgent.transform.parent = transform;
                ChildAgent.transform.ResetLocal();
                ChildAgent.AddComponent<NavMeshAgent>();
            }
        }

        protected virtual void OnDrawGizmos()
        {
            if (!debugGizmos) return;
            if (Agent == null) { return; }
            if (Agent.path == null) { return; }

            Gizmos.color = Color.yellow;

            Vector3 pos = Agent ? Agent.transform.position : transform.position;

            for (int i = 1; i < Agent.path.corners.Length; i++)
            {
                Gizmos.DrawLine(Agent.path.corners[i - 1], Agent.path.corners[i]);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Agent.transform.position, 0.01f);

            var Pos = (Application.isPlaying && target) ? target.position : Agent.transform.position;

            if (Application.isPlaying && target != null && IsAITarget != null)
            {
                Pos = IsAITarget.GetPosition();
            }

            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(Pos, Vector3.up, walkDistance);

            UnityEditor.Handles.color = HasArrived ? Color.green : Color.red;
            UnityEditor.Handles.DrawWireDisc(Pos, Vector3.up, StoppingDistance);

            if (Application.isPlaying/* && !NullDestination*/)
            {
                if (IsWayPoint != null && IsWayPoint.TargetType == WayPointType.Air)
                    Gizmos.DrawWireSphere(DestinationPosition, StoppingDistance);
                else
                    UnityEditor.Handles.DrawWireDisc(DestinationPosition, Vector3.up, StoppingDistance);
            }
        }

#endif
    }
}