using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations.Scriptables
{
    [CreateAssetMenu(menuName = "Malbers Animations/Scriptables/Collections/Game Object Collection", fileName = "New GameObject Collection")]
    public class RuntimeGameObjects : RuntimeCollection<GameObject> 
    {
        /// <summary>Return the Closest game object from an origin</summary>
       
        public GameObject Item_GetClosest(GameObject origin)
        {
            GameObject closest = null;

            float minDistance = float.MaxValue;

            foreach (var item in items)
            {
                var Distance = Vector3.Distance(item.transform.position, origin.transform.position);

                if (Distance < minDistance)
                {
                    closest = item;
                    minDistance = Distance;
                }
            }
            return closest;
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(RuntimeGameObjects))]
    public class RuntimeGameObjectsEditor : Editor
    {
        RuntimeGameObjects M;

        private void OnEnable()
        {
            M = (RuntimeGameObjects)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();


            if (Application.isPlaying)
            {
                MalbersEditor.DrawHeader( M.name + " - List");
                EditorGUI.BeginDisabledGroup(true);

                for (int i = 0; i < M.Items.Count; i++)
                {
                    EditorGUILayout.ObjectField("Item " + i, M.Items[i], typeof(GameObject), false);
                }

                EditorGUI.EndDisabledGroup();
            }

        }
    }
#endif

}

