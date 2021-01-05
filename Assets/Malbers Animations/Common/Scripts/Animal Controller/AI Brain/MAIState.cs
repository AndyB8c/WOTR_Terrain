using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Threading.Tasks;
using System.Linq;
using MalbersAnimations.Scriptables;

#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
#endif

namespace MalbersAnimations.Controller.AI
{
    [CreateAssetMenu(menuName = "Malbers Animations/Pluggable AI/AI State", order = 1000, fileName = "New AI State")]
    public class MAIState : ScriptableObject
    {
        [Tooltip("ID of the AI State. This is used on the AI Brain On AIStateChanged Event")]
        public IntReference ID = new IntReference();

        [FormerlySerializedAs("actions")]
        public MTask[] tasks;
        public MAITransition[] transitions;
        public Color GizmoStateColor = Color.gray;

        [HideInInspector] public bool CreateTaskAsset = true;
        [HideInInspector] public bool CreateDecisionAsset = true;


        public void Update_State(MAnimalBrain brain)
        {
            Update_Tasks(brain);
            Update_Transitions(brain);
        }

        private void Update_Transitions(MAnimalBrain brain)
        {
            for (int i = 0; i < transitions.Length; i++)
            {
                if (this != brain.currentState) return; //BUG BUG BUG FIXed

                var transition = transitions[i];
                var decision = transition.decision;
                if (decision == null) return; //Ignore Code

                if (decision.interval > 0)
                {
                    if (brain.CheckIfDecisionsCountDownElapsed(decision.interval, i))
                    {
                        brain.ResetDecisionTime(i);
                        Decide(brain, i, transition);
                    }
                }
                else
                {
                    Decide(brain, i, transition);
                }
            }
        }
        private void Decide(MAnimalBrain brain, int i, MAITransition transition)
        {
            bool decisionSucceded = transition.decision.Decide(brain, i);
            brain.TransitionToState(decisionSucceded ? transition.trueState : transition.falseState, decisionSucceded, transition.decision);
        }

        /// <summary>When a new State starts this method is called for each Tasks</summary>
        public void Start_AIState(MAnimalBrain brain)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                StartTaks(brain, i);
            }
        }

        internal void StartTaks(MAnimalBrain brain, int i)
        {
            if (i == 0 || !tasks[i].WaitForPreviousTask ||  tasks[i].WaitForPreviousTask && brain.TasksDone[i-1] && !brain.TasksStarted[i])
            {
                tasks[i].StartTask(brain, i);
                brain.TasksStarted[i] = true;
                brain.SetElapsedTaskTime(i);
                int taskID = tasks[i].MessageID;
                if (taskID != 0) brain.OnTaskStarted.Invoke(taskID);
            }
        }

        /// <summary>When a new State starts this method is called for each Decisions</summary>
        public void Prepare_Decisions(MAnimalBrain brain)
        {
            for (int i = 0; i < transitions.Length; i++)
            {
                transitions[i].decision?.PrepareDecision(brain, i);
            }
        }

        private void Update_Tasks(MAnimalBrain brain)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                if (brain.TasksStarted[i])
                {
                    if (!brain.TasksDone[i]) tasks[i].UpdateTask(brain, i);
                }
            }
        }

        public void Finish_Tasks(MAnimalBrain brain)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].ExitAIState(brain, i);
        }


        public void Finish_Decisions(MAnimalBrain brain)
        {
            for (int i = 0; i < transitions.Length; i++)
                transitions[i].decision.FinishDecision(brain, i);
        }


        #region Target Event Listeners for Tasks 
        public void OnTargetArrived(MAnimalBrain brain, Transform target)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnTargetArrived(brain, target, i);
        }


        public void OnAnimalEventDecisionListen(MAnimalBrain brain, MAnimal animal, int index)
        {
            transitions[index].decision.OnAnimalEventDecisionListen(brain, animal, index);
        }


        public void OnPositionArrived(MAnimalBrain brain, Vector3 Position)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnPositionArrived(brain, Position, i);
        }
        #endregion

        #region Self Animal Listen Events
        public void OnAnimalStateEnter(MAnimalBrain brain, State state)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnAnimalStateEnter(brain, state, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnAnimalStateEnter(brain, state, i);
        }

        public void OnAnimalStateExit(MAnimalBrain brain, State state)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnAnimalStateExit(brain, state, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnAnimalStateExit(brain, state, i);
        }

        public void OnAnimalStanceChange(MAnimalBrain brain, int Stance)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnAnimalStanceChange(brain, Stance, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnAnimalStanceChange(brain, Stance, i);
        }


        public void OnAnimalModeStart(MAnimalBrain brain, Mode mode)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnAnimalModeStart(brain, mode, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnAnimalModeStart(brain, mode, i);
        }

        public void OnAnimalModeEnd(MAnimalBrain brain, Mode mode)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnAnimalModeEnd(brain, mode, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnAnimalModeEnd(brain, mode, i);
        }
        #endregion

        #region Target Animal Listen Events
        public void OnTargetAnimalStateEnter(MAnimalBrain brain, State state)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnTargetAnimalStateEnter(brain, state, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnTargetAnimalStateEnter(brain, state, i);
        }

        public void OnTargetAnimalStateExit(MAnimalBrain brain, State state)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnTargetAnimalStateExit(brain, state, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnTargetAnimalStateExit(brain, state, i);
        }

        public void OnTargetAnimalStanceChange(MAnimalBrain brain, int Stance)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnTargetAnimalStanceChange(brain, Stance, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnTargetAnimalStanceChange(brain, Stance, i);
        }


        public void OnTargetAnimalModeStart(MAnimalBrain brain, Mode mode)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnTargetAnimalModeStart(brain, mode, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnTargetAnimalModeStart(brain, mode, i);
        }

        public void OnTargetAnimalModeEnd(MAnimalBrain brain, Mode mode)
        {
            for (int i = 0; i < tasks.Length; i++)
                tasks[i].OnTargetAnimalModeEnd(brain, mode, i);

            //for (int i = 0; i < transitions.Length; i++)
            //    transitions[i].decision.OnTargetAnimalModeEnd(brain, mode, i);
        }
        #endregion


        //#if UNITY_EDITOR
        //        private void OnDestroy()
        //        {
        //            foreach (var task in tasks)
        //            {
        //                string taskPath = AssetDatabase.GetAssetPath(task);
        //                if (taskPath == null || string.IsNullOrEmpty(taskPath))
        //                {
        //                    Destroy(tasks);

        //                }
        //            }
        //        }
        //#endif
    }

    [System.Serializable]
    public class MAITransition
    {
        public MAIDecision decision;
        public MAIState trueState;
        public MAIState falseState;
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(MAIState))]
    public class MAIStateEditor : Editor
    {
        MAIState m;
        private SerializedProperty tasks, transitions, GizmoStateColor, ID, CreateTaskAsset, CreateDecisionAsset;
        private ReorderableList Reo_List_Tasks, Reo_List_Transitions;


        private void OnEnable()
        {
            m = (MAIState)target;
            tasks = serializedObject.FindProperty("tasks");
            transitions = serializedObject.FindProperty("transitions");
            GizmoStateColor = serializedObject.FindProperty("GizmoStateColor");
            ID = serializedObject.FindProperty("ID");

            GizmoStateColor = serializedObject.FindProperty("GizmoStateColor");
            CreateTaskAsset = serializedObject.FindProperty("CreateTaskAsset");
            CreateDecisionAsset = serializedObject.FindProperty("CreateDecisionAsset");


            TasksList();

            TransitionList();
        }

        private void TasksList()
        {
            Reo_List_Tasks = new ReorderableList(serializedObject, tasks, true, true, true, true)
            {
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = tasks.GetArrayElementAtIndex(index);

                    var r = new Rect(rect) { y = rect.y + 2, height = EditorGUIUtility.singleLineHeight };

                    var empty = element.objectReferenceValue == null;

                    if (empty) r.width -= 22;


                    EditorGUI.PropertyField(r, element, GUIContent.none);



                    if (empty)
                    {
                        var AddButtonRect = new Rect(rect) { x = rect.width + 22, width = 22, y = rect.y + 2, height = EditorGUIUtility.singleLineHeight };

                        if (GUI.Button(AddButtonRect, new GUIContent("+", "Create new Task")))
                        {
                            ///if (CreateTaskAsset.boolValue)
                            MTools.AddScriptableAssetContextMenu(element, typeof(MTask), MTools.GetSelectedPathOrFallback());
                            //   else
                            //  MTools.AddScriptableAssetContextMenuInternal(element, typeof(MTask));
                        }
                    }
                },

                drawHeaderCallback = rect =>
                {
                    var r = new Rect(rect);
                    var ColorRect = new Rect(rect)
                    {
                        x = rect.width - 40,
                        width = 60,
                        height = rect.height - 2,
                        y = rect.y + 1,
                    };

                    EditorGUI.LabelField(rect, new GUIContent("   Tasks", "Tasks for the state"));
                    EditorGUI.PropertyField(ColorRect, GizmoStateColor, GUIContent.none);
                },
                onAddCallback = list => tasks.AddObjectToArray<MTask>(null),

                onRemoveCallback = list =>
                {
                    tasks.RemoveObjectFromArrayAt(list.index);

                    list.index -= 1;

                    if (list.index == -1 && tasks.arraySize > 0)  //In Case you remove the first one
                        list.index = 0;
                }
            };
        }

        private void TransitionList()
        {
            Reo_List_Transitions = new ReorderableList(serializedObject, transitions, true, true, true, true)
            {
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var r1 = new Rect(rect) { y = rect.y + 2, height = EditorGUIUtility.singleLineHeight };
                    var r2 = new Rect(r1);
                    var r3 = new Rect(r2);

                    var element = transitions.GetArrayElementAtIndex(index);

                    var decision = element.FindPropertyRelative("decision");
                    var TrueState = element.FindPropertyRelative("trueState");
                    var FalseState = element.FindPropertyRelative("falseState");


                    var empty = decision.objectReferenceValue == null;
                    if (empty) r1.width -= 30;



                    EditorGUI.PropertyField(r1, decision, new GUIContent("[" + index + "] Decision", "If the Decision is true it will go to the True State, else it will go to the False state"));

                    if (empty)
                    {
                        var AddButtonRect = new Rect(r1) { x = rect.width + 12, width = 25, height = EditorGUIUtility.singleLineHeight };

                        if (GUI.Button(AddButtonRect, "+"))
                            MTools.AddScriptableAssetContextMenu(decision, typeof(MAIDecision), MTools.GetSelectedPathOrFallback());
                    }
                     
                    

                    empty = TrueState.objectReferenceValue == null;

                    if (empty) r2.width -= 30;

                    r2.y += EditorGUIUtility.singleLineHeight + 3;
                    EditorGUI.PropertyField(r2, TrueState, new GUIContent("True State", "If the Decision is TRUE, It will execute this state"));


                    if (empty)
                    {
                        var AddButtonRect = new Rect(r2) { x = rect.width + 12, width = 25, height = EditorGUIUtility.singleLineHeight };
                        if (GUI.Button(AddButtonRect, "+"))
                            MTools.CreateScriptableAsset(TrueState, typeof(MAIState), MTools.GetSelectedPathOrFallback());
                    }


                  

                    empty = FalseState.objectReferenceValue == null;
                    if (empty) r3.width -= 30;
                    r3.y += (EditorGUIUtility.singleLineHeight + 3)*2;
                    EditorGUI.PropertyField(r3, FalseState, new GUIContent("False State", "If the Decision is FALSE, It will execute this state"));

                    if (empty)
                    {
                        var AddButtonRect = new Rect(r3) { x = rect.width + 12, width = 25, height = EditorGUIUtility.singleLineHeight };
                        if (GUI.Button(AddButtonRect, "+"))
                            MTools.CreateScriptableAsset(FalseState, typeof(MAIState), MTools.GetSelectedPathOrFallback());
                    }
                },

                drawHeaderCallback = rect => EditorGUI.LabelField(rect, new GUIContent("   Decisions", "Transitions for other States")),

                onAddCallback = list =>
                {
                    if (m.transitions == null)
                    {
                        m.transitions = new MAITransition[1];
                    }
                    else
                    {
                        Array.Resize<MAITransition>(ref m.transitions, m.transitions.Length + 1); //Hack for non UNITY  OBJECTS ARRAYS
                    }
                    EditorUtility.SetDirty(target);
                },

                onRemoveCallback = list =>
                {
                    transitions.DeleteArrayElementAtIndex(list.index);

                    list.index -= 1;

                    if (list.index == -1 && transitions.arraySize > 0)  //In Case you remove the first one
                        list.index = 0;
                },
                elementHeightCallback = (index) =>
                {
                    var DefaultHeight = EditorGUIUtility.singleLineHeight + 5;

                    //if (Reo_List_Transitions.index == index)
                    return DefaultHeight * 3;
                    //else
                    //    return DefaultHeight;
                },
            };
        }



        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(target.name, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(ID);
            EditorGUILayout.Space();

            Reo_List_Tasks.DoLayoutList();

            if (Reo_List_Tasks.index != -1) Reo_List_Transitions.index = -1;

            Reo_List_Transitions.DoLayoutList();

            if (Reo_List_Transitions.index != -1) Reo_List_Tasks.index = -1;


            var indexTasks = Reo_List_Tasks.index;
            var indexTrans = Reo_List_Transitions.index;

            if (indexTasks != -1)
            {
                var element = tasks.GetArrayElementAtIndex(indexTasks);

                if (element != null && element.objectReferenceValue != null)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Task: " + element.objectReferenceValue.name, EditorStyles.boldLabel);
                    MTools.DrawObjectReferenceInspector(element); ;
                    EditorGUILayout.EndVertical();
                }
            }

            if (indexTrans != -1)
            {
                var element = transitions.GetArrayElementAtIndex(indexTrans);
                var decision = element.FindPropertyRelative("decision");

                if (decision != null && decision.objectReferenceValue != null)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Decision: " + decision.objectReferenceValue.name, EditorStyles.boldLabel);
                    MTools.DrawObjectReferenceInspector(decision);
                    EditorGUILayout.EndVertical();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
