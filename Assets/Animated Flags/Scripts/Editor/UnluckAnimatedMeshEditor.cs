//	Unluck Software	
// 	www.chemicalbliss.com

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UnluckAnimatedMesh))]
[CanEditMultipleObjects]
[System.Serializable]
public class UnluckAnimatedMeshEditor: Editor {	
    public override void OnInspectorGUI() {
		UnluckAnimatedMesh target_cs= (UnluckAnimatedMesh)target;
        DrawDefaultInspector();
		
		if(GUILayout.Button("Force Change Mesh")){
			target_cs.FillCacheArray();
		}
        if (GUI.changed){ 
	        target_cs.CheckIfMeshHasChanged();
	        EditorUtility.SetDirty(target_cs);
        }
    }
}