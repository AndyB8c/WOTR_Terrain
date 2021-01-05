using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations.HAP
{
    public class RiderFPC : MRider
    {
        [Header("First Person Properties"), Tooltip("Offset Position to move the FPC on the Mount Point")]
        public Vector3 MountOffset = Vector3.up;
        [Tooltip("If True the FPC will rotate with the Animal")]
        public bool FollowRotation = false;

        private float TimeToMountDismount = 0.5f;
        private float CurrentTime;

        public override void MountAnimal()
        {
            if (!CanMount) return;

            if (!MTools.ElapsedTime(TimeToMountDismount, CurrentTime)) return;

            if (debug) Debug.Log($"<b>{name}:<color=cyan> [Mount Animal] </color> </b>");  //Debug


            Start_Mounting();

            CurrentTime = Time.time;

            UpdateRiderTransform();
            Vector3 AnimalForward = Vector3.ProjectOnPlane(Montura.transform.forward, Montura.Animal.UpVector);
            transform.rotation = Quaternion.LookRotation(AnimalForward, -Physics.gravity);
        }

        public override void DismountAnimal()
        {
            if (!CanDismount) return;
            if (!MTools.ElapsedTime(TimeToMountDismount, CurrentTime)) return;

            if (debug) Debug.Log($"<b>{name}:<color=cyan> [Dismount Animal] </color> </b>");  //Debug

            CurrentTime = Time.time;
            transform.position = new Vector3(MountTrigger.transform.position.x, transform.position.y, MountTrigger.transform.position.z);
            Start_Dismounting();

           
            if (RB) RB.velocity = Vector3.zero;
        }

        void Update()
        {
            if ((LinkUpdate & UpdateMode.Update) == UpdateMode.Update) UpdateRiderTransform();
        }

        private void LateUpdate()
        {
            if ((LinkUpdate & UpdateMode.LateUpdate) == UpdateMode.LateUpdate || ForceLateUpdateLink) UpdateRiderTransform();
        }

        private void FixedUpdate()
        {
            if ((LinkUpdate & UpdateMode.FixedUpdate) == UpdateMode.FixedUpdate) UpdateRiderTransform();
        }


        /// <summary>Updates the Rider Position to the Mount Point</summary>
        public override void UpdateRiderTransform()
        {
            if (IsRiding)
            {
                transform.position = Montura.MountPoint.TransformPoint(MountOffset);

                if (FollowRotation)
                {
                    transform.rotation *= Quaternion.FromToRotation(transform.up, Vector3.up) * Montura.Animal.transform.rotation;
                }
            }
        }
    }





#if UNITY_EDITOR
    [CustomEditor(typeof(RiderFPC), true)]
    public class MRiderFPCEd : MRiderEd
    {

        private SerializedProperty MountOffset, FollowRotation;
        protected override void OnEnable()
        {
            base.OnEnable();
            MountOffset = serializedObject.FindProperty("MountOffset");
            FollowRotation = serializedObject.FindProperty("FollowRotation");
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginVertical(MalbersEditor.StyleGray);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(MountOffset);
                EditorGUILayout.PropertyField(FollowRotation);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}