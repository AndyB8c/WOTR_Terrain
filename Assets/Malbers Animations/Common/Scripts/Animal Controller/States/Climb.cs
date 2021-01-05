using MalbersAnimations.Utilities;
using MalbersAnimations.Scriptables;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
//using Physics = RotaryHeart.Lib.PhysicsExtension.Physics;

namespace MalbersAnimations.Controller
{
    /// <summary>Climb Logic </summary>
    public class Climb : State
    {
        public readonly static int ClimbStart = Animator.StringToHash("ClimbStart");

        public static int Climb_Ledge = 1;
        public static int Climb_Off = 2;

        /// <summary>Air Resistance while falling</summary>
        [Header("Climb Parameters"), Space]
        public string Pivot = "Climb";
        [Tooltip("Walls need to have the Tag Climb to be climbable")]
        public string ClimbTag = "Climb";
        [Tooltip("Climb automatically when is near a climbable wall")]
        public bool automatic = false;
        public float WallDistance = 0.2f;
        public float AlignSmoothness = 10f;
        public float GroundDistance = 0.5f;
        public float HorizontalDistance = 0.2f;
        public float InnerAngle = 3f;
        // public float OuterAngle = 15f;

        [Header("Ledge Detection")]
        public float LedgeRays = 0.4f;
        public Vector3 LedgeUp = Vector3.up;
        [Tooltip("MinDistance to exit Climb over a ledge")]
        public float LedgeMinExit = 0.4f;

        private MPivots ClimbPivot;
        private Transform wall;
        private Vector3 platform_Pos;
        private Quaternion platform_Rot;

        //private readonly RaycastHit[] ClimbHit = new RaycastHit[1];
        //private readonly RaycastHit[] ClimbHitLeft = new RaycastHit[1];
        //private readonly RaycastHit[] ClimbHitRight = new RaycastHit[1];
        //private readonly RaycastHit[] ClimbDownHit = new RaycastHit[1];
         private readonly RaycastHit[] EdgeHit = new RaycastHit[1];


        private RaycastHit ClimbHit; 
        private RaycastHit ClimbHitLeft; 
        private  RaycastHit ClimbHitRight ;
        private  RaycastHit ClimbDownHit ;
       // private RaycastHit EdgeHit;



        private bool UsingCameraInput;
        private bool WallClimbTag;

        /// <summary> World Position of the Climb Pivot </summary>
        public Vector3 ClimbPivotPoint => ClimbPivot.World(transform);

        /// <summary> World Position of the Climb Pivot - the Horizontal Distance </summary>
        public Vector3 ClimbPivotPointLeft
        {
            get
            {
                var PointLeft = ClimbPivot.position - new Vector3(-HorizontalDistance, 0, 0);
                return transform.TransformPoint(PointLeft);
            }
        }

        /// <summary> World Position of the Climb Pivot + the Horizontal Distance </summary>
        public Vector3 ClimbPivotPointRight
        {
            get
            {
                var PointLeft = ClimbPivot.position - new Vector3(HorizontalDistance, 0, 0);
                return transform.TransformPoint(PointLeft);
            }
        }

        /// <summary>Starting point above the head of the Ledge Detection to cast a Ray</summary>
        public Vector3 LedgePivotUP => transform.TransformPoint(LedgeUp);

        public override void AwakeState()
        {
            UsingCameraInput = animal.UseCameraInput;

            base.AwakeState();

            ClimbPivot = animal.pivots.Find(x => x.name == Pivot);

            if (ClimbPivot == null)
            { Debug.LogError("The Climb State requires a Pivot named 'Climb'. Please create a new pivot on the Animal Component"); }
        }


        public override void StatebyInput()
        {
            if (InputValue)
            {
                if (IsActiveState && InCoreAnimation) //Disable Climb by Input when we are already climbing)
                {
                    AllowExit();
                    InputValue = false;
                    if (debug) Debug.Log("<B>Climb:</B> Exit with Climb Input");
                    SetStatus(Climb_Off); //Set the animation to start falling and then fall
                }
                else
                {
                    if (CheckClimbRay())  Activate();
                }
            }
        }

        public override void Activate()
        {
            base.Activate();
            animal.UseCameraInput = false;   //Climb cannot use Camera Input
            animal.DisablePivotChest();
        }

        public override void ResetStateValues()
        {
            wall = null;
            animal.EnablePivotChest();
        }
        /// <summary> CALLED BY THE ANIMATOR </summary>
        public void ClimbResetInput() //=> animal.UseCameraInput = UsingCameraInput; //Return to the default camera input on the Animal
        {
            animal.UseCameraInput = UsingCameraInput;
            Debug.Log("ClimbResetInput");
        }
        public override bool TryActivate()
        {
            if (automatic)
            {
                Debug.DrawRay(ClimbPivotPoint, animal.Forward * animal.ScaleFactor * ClimbPivot.multiplier, Color.white);
                return CheckClimbRay();
            }
            return false;
        }


        private bool CheckClimbRay()
        {
            if (Physics.Raycast(ClimbPivotPoint, animal.Forward, out ClimbHit, animal.ScaleFactor * ClimbPivot.multiplier, animal.GroundLayer, QueryTriggerInteraction.Ignore))
            {
                WallClimbTag = ClimbHit.transform.gameObject.CompareTag(ClimbTag);
                AvNormal = ClimbHit.normal;
                return WallClimbTag;
            }
            return false;
        }

        public override void OnStateMove(float deltatime)
        {
            if (InCoreAnimation)
            {
                var GroundLayer = animal.GroundLayer;
                var Forward = animal.Forward;
                var ScaleFactor = animal.ScaleFactor;
                var mult = ClimbPivot.multiplier;

                var RelativeForward = transform.InverseTransformDirection(Forward);
                var rotation = transform.rotation;

                var LeftInnerForward = rotation * Quaternion.Euler(0, -InnerAngle, 0) * RelativeForward;
                var RightInnerForward = rotation * Quaternion.Euler(0, InnerAngle, 0) * RelativeForward;

                //HitRayLeft = Physics.RaycastNonAlloc(ClimbPivotPointLeft, LeftInnerForward, ClimbHitLeft, ScaleFactor * mult, GroundLayer, QueryTriggerInteraction.Ignore) > 0;
                //HitRayRight = Physics.RaycastNonAlloc(ClimbPivotPointRight, RightInnerForward, ClimbHitRight, ScaleFactor * mult, GroundLayer, QueryTriggerInteraction.Ignore) > 0;
                //HitRayCenter = Physics.RaycastNonAlloc(ClimbPivotPoint, Forward, ClimbHit, ScaleFactor * mult, GroundLayer, QueryTriggerInteraction.Ignore) > 0;

                HitRayLeft = Physics.Raycast(ClimbPivotPointLeft, LeftInnerForward, out ClimbHitLeft, ScaleFactor * mult, GroundLayer, QueryTriggerInteraction.Ignore);
                HitRayRight = Physics.Raycast(ClimbPivotPointRight, RightInnerForward, out ClimbHitRight, ScaleFactor * mult, GroundLayer, QueryTriggerInteraction.Ignore);
 
                Debug.DrawRay(ClimbPivotPointLeft, LeftInnerForward * ScaleFactor * mult, HitRayLeft ? Color.green : Color.red);
                Debug.DrawRay(ClimbPivotPointRight, RightInnerForward * ScaleFactor * mult, HitRayRight ? Color.green : Color.red);

                if (Physics.Raycast(ClimbPivotPoint, Forward, out ClimbHit, ScaleFactor * mult, GroundLayer, QueryTriggerInteraction.Ignore))
                {
                    MovingWall(ClimbHit.transform, deltatime);
                    AlignToWall(ClimbHit.distance, deltatime);
                    WallClimbTag = ClimbHit.transform.gameObject.CompareTag(ClimbTag);
                }
                //return;


                if (HitRayLeft || HitRayRight)
                {
                    float distance = 0;
                    Vector3 OrientNormal = Forward;

                    if (HitRayLeft)
                    {
                        distance = ClimbHitLeft.distance;
                        OrientNormal = ClimbHitLeft.normal;
                    }
                    if (HitRayRight)
                    {
                        distance = (distance + ClimbHitRight.distance) / 2;
                        OrientNormal = (OrientNormal + ClimbHitRight.normal).normalized;
                    }

                    AlignToWall(distance, deltatime);
                    AvNormal = Vector3.Lerp(AvNormal, OrientNormal, deltatime * AlignSmoothness);

                    OrientToWall(AvNormal, deltatime);
                }
            }
            else if (CurrentAnimTag == ClimbStart)
            {
                OrientToWall(AvNormal, deltatime);
            }
        }

        public override void TryExitState(float DeltaTime)
        {
            if (InCoreAnimation)
            {
                var scalefactor = animal.ScaleFactor;
                var forward = animal.Forward;
                var Gravity = animal.Gravity;
                var MainPivot = ClimbPivotPoint + animal.AdditivePosition;
                Debug.DrawRay(MainPivot, Gravity * animal.ScaleFactor * GroundDistance, Color.white);

                //PRESSING DOWN
                if (animal.MovementAxisRaw.z < 0) //Means the animal is going down
                {
                    if (Physics.Raycast(MainPivot, Gravity, out ClimbDownHit, scalefactor * GroundDistance, animal.GroundLayer, QueryTriggerInteraction.Ignore)) //Means that the Animal is going down and touching the ground
                    {
                        if (debug) Debug.Log($"<B>[{animal.name}]-> [Climb]</B> -[Try Exit] when Grounded and pressing Backwards");

                        AllowExit();
                        animal.CheckIfGrounded();

                        return;
                    }
                }

                if (!HitRayLeft && !HitRayRight)
                {
                    if (debug) Debug.Log("<B>Climb:</B> Exit when there's no Rays hitting the wall");
                    AllowExit();
                    return;
                }

                if (!WallClimbTag)
                {
                    if (debug) Debug.Log("<B>Climb:</B> Exit when wall does not have the Climb Tag");
                    AllowExit();
                    return;
                }

                //Check Upper Ground legde Detection

                bool LedgeHit = Physics.RaycastNonAlloc(LedgePivotUP, forward, EdgeHit, scalefactor * LedgeRays, animal.GroundLayer, QueryTriggerInteraction.Ignore) > 0;

                Debug.DrawRay(LedgePivotUP, forward * scalefactor * LedgeRays, LedgeHit ? Color.red : Color.green);

                if (!LedgeHit)
                {
                    var SecondRayPivot = new Ray(LedgePivotUP, forward).GetPoint(LedgeRays);


                    LedgeHit = Physics.RaycastNonAlloc(SecondRayPivot, Gravity, EdgeHit, scalefactor * LedgeRays, animal.GroundLayer, QueryTriggerInteraction.Ignore) > 0;

                    Debug.DrawRay(SecondRayPivot, Gravity * scalefactor * LedgeRays, !LedgeHit ? Color.red : Color.green);

                    if (LedgeHit)
                    {
                        var hit = EdgeHit[0];

                        if (hit.distance > LedgeMinExit * scalefactor)
                        {
                            if (debug) Debug.Log("<B>Climb:</B> Exit On a Ledge");
                            SetStatus(Climb_Ledge);
                            animal.State_Force(1); //Force Locomotion State instead of FALL when climbing on a ledge
                            AllowExit();
                        }
                    }

                }
            }
        }

     

        Vector3 AvNormal;
        private bool HitRayLeft;
        private bool HitRayRight;

        public bool HitRayCenter { get; private set; }

        private void OrientToWall(Vector3 normal, float deltatime)
        {
            Quaternion AlignRot = Quaternion.FromToRotation(transform.forward, -normal) * transform.rotation;  //Calculate the orientation to Terrain 
            Quaternion Inverse_Rot = Quaternion.Inverse(transform.rotation);
            Quaternion Target = Inverse_Rot * AlignRot;
            Quaternion Delta = Quaternion.Lerp(Quaternion.identity, Target, deltatime * AlignSmoothness); //Calculate the Delta Align Rotation
            animal.AdditiveRotation *= Delta;
    }

        private void MovingWall(Transform hit, float deltatime)
        {
            if (wall == null || wall != hit)               //Platforming logic
            {
                wall = hit;
                WallClimbTag = hit.gameObject.CompareTag(ClimbTag);
                platform_Pos = wall.position;
                platform_Rot = wall.rotation;
            }

            if (wall == null) return;

            var DeltaPlatformPos = wall.position - platform_Pos;

            animal.AdditivePosition += DeltaPlatformPos;                          // Keep the same relative position.

            Quaternion Inverse_Rot = Quaternion.Inverse(platform_Rot);
            Quaternion Delta = Inverse_Rot * wall.rotation;

            if (Delta != Quaternion.identity)                                        // no rotation founded.. Skip the code below
            {
                var pos = transform.DeltaPositionFromRotate(wall, Delta);
                animal.AdditivePosition += pos;
            }

            animal.AdditiveRotation *= Delta;

            platform_Pos = wall.position;
            platform_Rot = wall.rotation;
        }

        //Align the Animal to the Wall
        private void AlignToWall(float distance, float deltatime)
        {
            float difference = distance - WallDistance * animal.ScaleFactor;

            if (!Mathf.Approximately(distance, WallDistance * animal.ScaleFactor))
            {
                Vector3 align = animal.Forward * difference * deltatime * AlignSmoothness;
                animal.AdditivePosition += align;
            }
        }


#if UNITY_EDITOR
        void Reset()
        {
            ID = MTools.GetInstance<StateID>("Climb");
            General = new AnimalModifier()
            {
                RootMotion = true,
                AdditivePosition = true,
                AdditiveRotation = true,
                Grounded = false,
                Sprint = false,
                OrientToGround = false,
                Gravity = false,
                CustomRotation = true,
                modify = (modifier)(-1), FreeMovement = false,
                IgnoreLowerStates = true, 
            };
        }
#endif
    }
}