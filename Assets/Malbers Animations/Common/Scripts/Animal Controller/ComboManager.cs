using MalbersAnimations.Scriptables;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using MalbersAnimations.Events;

#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
#endif

namespace MalbersAnimations.Controller
{
    public class ComboManager : MonoBehaviour
    {
        public  MAnimal animal { get; private set; }

        public int Branch = 0;
        public List<Combo> combos;

        public int CurrentComboIndex { get; private set; }
        public int CurrentComboSequence { get; internal set; }
        public Combo CurrentCombo { get; private set; }

       

       // public IntEvent ComboEnded = new IntEvent();

        private void Awake()
        {
            animal = GetComponent<MAnimal>();
            animal.OnModeEnd.AddListener(OnModeEnd);
        }

        private void OnModeEnd(int modeID, int Ability)
        {
            var EndingMode = modeID * 1000 + Ability;

           // Debug.Log("OnModeEnd:" + EndingMode + "||||||| CurrentComboSequence: " + CurrentComboSequence);
            if (CurrentComboSequence == EndingMode &&  animal.IsPlayingMode)
            {
               // ComboEnded.Invoke(CurrentComboSequence);
                Restart();//meaning it got to the end of the combo
            }
        }

        private void OnDisable()
        { animal.OnModeEnd.RemoveListener(OnModeEnd); }


        public virtual void Activate(int index)
        {
            if (animal.Sleep) return;
            if (!enabled) return;

            if (!animal.IsPlayingMode && !animal.IsPreparingMode)
                Restart();   //Means is not Playing any mode so Restart

            if (CurrentComboIndex == -1) //First Entry
            {
                CurrentComboIndex = index;
                CurrentCombo = combos[CurrentComboIndex];
            }


            if (CurrentCombo != null && CurrentCombo.Active)
                CurrentCombo.Play(this);
        }



        public virtual void Activate_XXYY(int index)
        {
            var branch = index / 100;
            var combo = index % 100;

            SetBranch(branch);
            Activate(combo);
        }



        public virtual void Restart()
        {
            CurrentCombo = null;
            CurrentComboIndex = -1;
            CurrentComboSequence = 0;

            foreach (var m in combos)
                foreach (var seq in m.Sequence)
                    seq.Used = false;
          //  Debug.Log("COMBO RESTART");
        }

        public virtual void SetBranch(int value) => Branch = value;
        public virtual void Combo_Disable(int index) => combos[index].Active = false;
        public virtual void Combo_Enable(int index) => combos[index].Active = true;


        [HideInInspector] public int selectedCombo = -1;
    }

    [System.Serializable]
    public class Combo
    {
        public ModeID Mode;
        public string Name = "Combo1";
        public BoolReference m_Active = new BoolReference(true);

        public bool Active { get => m_Active.Value; set => m_Active.Value = value; }

        public List<ComboSequence> Sequence = new List<ComboSequence>();

        public void Play(ComboManager M)
        {
            var animal = M.animal;

            if (!animal.IsPlayingMode) //Means is starting the combo
            {
                var Starter = Sequence.Find(s => !s.Used && s.Branch == M.Branch && s.PreviewsAbility == 0);

                if (Starter != null && animal.Mode_TryActivate(Mode, Starter.Ability))
                {
                    PlaySequence(M, Starter);
                }
            }
            else
            {
                if (animal.IsPlayingMode)               //Means is Playing a mode so check for the Next sequence on the Combo
                {
                    var aMode = animal.ActiveMode;      //Get the Animal Active Mode 

                    var NextSequence = Sequence.Find(s => !s.Used && s.Branch == M.Branch && s.PreviewsAbility == aMode.AbilityIndex && s.Activation.IsInRange(animal.ModeTime));

                    if (NextSequence != null && animal.Mode_ForceActivate(Mode, NextSequence.Ability)) //Play the nex animation on the sequence
                    {
                        PlaySequence(M, NextSequence);
                    }
                }
            }
        }

        private void PlaySequence(ComboManager M, ComboSequence sequence)
        {
            M.CurrentComboSequence = Mode.ID * 1000 + sequence.Ability;
            sequence.Used = true;
            sequence.OnSequencePlay.Invoke(M.CurrentComboSequence);
            //Debug.Log("Sequence: [" + M.CurrentComboSequence + "] Time: " + Time.time.ToString("F3"));
        }
    }


    [System.Serializable]
    public class ComboSequence
    {
        [MinMaxRange(0, 1)]
        public RangedFloat Activation = new RangedFloat(0.3f, 0.6f);
        public int PreviewsAbility = 0;
        public int Ability = 0;
        public int Branch = 0;
        public bool Used;
        public IntEvent OnSequencePlay = new IntEvent(); 
    }



#if UNITY_EDITOR
    [CustomEditor(typeof(ComboManager))]

    public class ComboEditor : Editor
    {
        public static GUIStyle StyleGray => MTools.Style(new Color(0.5f, 0.5f, 0.5f, 0.3f));
        public static GUIStyle StyleBlue => MTools.Style(new Color(0, 0.5f, 1f, 0.3f));

        private int branch, prev, current;

        SerializedProperty Branch, combos, selectedCombo;
        private Dictionary<string, ReorderableList> SequenceReordable = new Dictionary<string, ReorderableList>();
        private ReorderableList CombosReor;

        private int abiliIndex;

        private void OnEnable()
        {
            combos = serializedObject.FindProperty("combos");
            Branch = serializedObject.FindProperty("Branch");
            selectedCombo = serializedObject.FindProperty("selectedCombo");

            CombosReor = new ReorderableList(serializedObject, combos, true, true, true, true)
            {
                drawElementCallback = Draw_Element_Combo,
                drawHeaderCallback = Draw_Header_Combo,
                onSelectCallback = Selected_ComboCB,
                onRemoveCallback = OnRemoveCallback_Mode
            };
        }

        private void Selected_ComboCB(ReorderableList list)
        {
            selectedCombo.intValue = list.index;
        }

        private void OnRemoveCallback_Mode(ReorderableList list)
        {
            // The reference value must be null in order for the element to be removed from the SerializedProperty array.
            combos.DeleteArrayElementAtIndex(list.index);
            list.index -= 1;

            if (list.index == -1 && combos.arraySize > 0) list.index = 0;   //In Case you remove the first one

            selectedCombo.intValue--;

            list.index = Mathf.Clamp(list.index, 0, list.index - 1);

            EditorUtility.SetDirty(target);
        }

        private void Draw_Header_Combo(Rect rect)
        {
            float half = rect.width / 2;
            var IDIndex = new Rect(rect.x, rect.y, 45, EditorGUIUtility.singleLineHeight);
            var IDName = new Rect(rect.x + 45, rect.y, half - 15 - 45, EditorGUIUtility.singleLineHeight);
            var IDRect = new Rect(rect.x + half + 10, rect.y, half - 10, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(IDIndex, "Index");
            EditorGUI.LabelField(IDName, " Name");
            EditorGUI.LabelField(IDRect, "  Mode");
        }

        private void Draw_Element_Combo(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = combos.GetArrayElementAtIndex(index);
            var Mode = element.FindPropertyRelative("Mode");
            var Name = element.FindPropertyRelative("Name");
            rect.y += 2;

            float half = rect.width / 2;

            var IDIndex = new Rect(rect.x, rect.y, 25, EditorGUIUtility.singleLineHeight);
            var IDName = new Rect(rect.x + 25, rect.y, half - 15 - 25, EditorGUIUtility.singleLineHeight);
            var IDRect = new Rect(rect.x + half + 10, rect.y, half - 10, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(IDIndex, "(" + index.ToString() + ")");
            EditorGUI.PropertyField(IDName, Name, GUIContent.none);
            EditorGUI.PropertyField(IDRect, Mode, GUIContent.none);
        }

        private void DrawSequence(int ModeIndex, SerializedProperty combo, SerializedProperty sequence)
        {
            ReorderableList Reo_AbilityList;
            string listKey = combo.propertyPath;

            if (SequenceReordable.ContainsKey(listKey))
            {
                Reo_AbilityList = SequenceReordable[listKey]; // fetch the reorderable list in dict
            }
            else
            {
                Reo_AbilityList = new ReorderableList(combo.serializedObject, sequence, true, true, true, true)
                {
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        rect.y += 2;

                        var Height = EditorGUIUtility.singleLineHeight;
                        var element = sequence.GetArrayElementAtIndex(index);

                        //var Activation = element.FindPropertyRelative("Activation");
                        var PreviewsAbility = element.FindPropertyRelative("PreviewsAbility");
                        var Ability = element.FindPropertyRelative("Ability");
                        var Branch = element.FindPropertyRelative("Branch");
                        var useD = element.FindPropertyRelative("Used");

                        var IDRect = new Rect(rect) { height = Height };

                        float wid = rect.width / 3;

                        var IndexRect = new Rect(IDRect) { width = 25 };
                        var BranchRect = new Rect(IDRect) { x = IDRect.x + 30, width = wid - 40 };
                        var PrevARect = new Rect(IDRect) { x = wid + 40, width = wid - 15 };
                        var AbilityRect = new Rect(IDRect) { x = wid * 2 + 50, width = wid - 15 };

                        var style = new GUIStyle(EditorStyles.label);

                        if (!useD.boolValue && Application.isPlaying)
                        {
                            style.normal.textColor = Color.green;
                        }

                        EditorGUI.LabelField(IndexRect, "(" + index.ToString() + ")", style);
                        var oldColor = GUI.contentColor;
                        if (PreviewsAbility.intValue <= 0) GUI.contentColor = Color.green;
                        EditorGUI.PropertyField(BranchRect, Branch, GUIContent.none);
                        EditorGUI.PropertyField(PrevARect, PreviewsAbility, GUIContent.none);
                        EditorGUI.PropertyField(AbilityRect, Ability, GUIContent.none);
                        GUI.contentColor = oldColor;

                        if (index == abiliIndex)
                        {
                            branch = Branch.intValue;
                            prev = PreviewsAbility.intValue;
                            current = Ability.intValue;
                        }

                        //if (index == abiliIndex)
                        //{
                        //    IDRect.y += Height + 2;

                        //    EditorGUI.PropertyField(IDRect, Activation, new GUIContent("Activation"));
                        //}
                    },

                    drawHeaderCallback = rect =>
                    {
                        var Height = EditorGUIUtility.singleLineHeight;
                        var IDRect = new Rect(rect) { height = Height };

                        float wid = rect.width / 3;

                        var IndexRect = new Rect(IDRect) { width = 38 };
                        var BranchRect = new Rect(IDRect) { x = IDRect.x + 40, width = wid - 40 };
                        var PrevARect = new Rect(IDRect) { x = wid + 40, width = wid - 15 };
                        var AbilityRect = new Rect(IDRect) { x = wid * 2 + 50, width = wid - 15 };

                        EditorGUI.LabelField(IndexRect, "Index");
                        EditorGUI.LabelField(BranchRect, " Branch");
                        EditorGUI.LabelField(PrevARect, "Activation Ability");
                        EditorGUI.LabelField(AbilityRect, "Next Ability");
                    },

                    //elementHeightCallback = (index) =>
                    //{
                    //    Repaint();

                    //    if (index == abiliIndex)

                    //        return EditorGUIUtility.singleLineHeight * 3;
                    //    else
                    //        return EditorGUIUtility.singleLineHeight + 5;
                    //}
                };

                SequenceReordable.Add(listKey, Reo_AbilityList);  //Store it on the Editor
            }

            Reo_AbilityList.DoLayoutList();

            abiliIndex = Reo_AbilityList.index;

            if (abiliIndex != -1)
            {
                var element = sequence.GetArrayElementAtIndex(abiliIndex);

                var Activation = element.FindPropertyRelative("Activation");
                var OnSequencePlay = element.FindPropertyRelative("OnSequencePlay");

                var lbl = "B[" + branch + "] AA[" + prev + "] NA[" + current + "]";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {

                    EditorGUILayout.LabelField("Sequence Properties - " + lbl);
                    EditorGUILayout.PropertyField(Activation, new GUIContent("Activation", "Range of the Preview Animation the Sequence can be activate"));
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.PropertyField(OnSequencePlay, new GUIContent("Sequence Play - " + lbl));
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MalbersEditor.DrawDescription("Combo Manager. Use Combos Sequences to create combos by using the Modes from the Animal Controller\nActivate the combos using Activate(int) or Activate_XXYY(int) XX(Branch) YY(Combo Index)");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(Branch, new GUIContent("Branch", "Current Branch ID for the Combo Sequence, if this value change then the combo will play different sequences"));
            }
            EditorGUILayout.EndVertical();


            CombosReor.DoLayoutList();

            CombosReor.index = selectedCombo.intValue;
            int IndexCombo = CombosReor.index;

            if (IndexCombo != -1)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    var combo = combos.GetArrayElementAtIndex(IndexCombo);

                    if (combo != null)
                    {
                        var name = combo.FindPropertyRelative("Name");
                        EditorGUILayout.LabelField(name.stringValue, EditorStyles.boldLabel);
                        var active = combo.FindPropertyRelative("m_Active");
                        EditorGUILayout.PropertyField(active, new GUIContent("Active", "is the Combo Active?"));
                        EditorGUILayout.HelpBox("Green Sequences are starters combos",  MessageType.None);
                        EditorGUILayout.LabelField("Combo Sequence List", EditorStyles.boldLabel);
                        var sequence = combo.FindPropertyRelative("Sequence");
                        DrawSequence(IndexCombo, combo, sequence);
                    }
                }
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}