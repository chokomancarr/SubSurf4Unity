using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.VersionControl;
using System.IO;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(SubSurfMod))]
public class SubSurfEditor : Editor {
	bool wrong = true;
	List<string> objs;
	bool hasObj;
    bool debug;

	public override void OnInspectorGUI () {
		SubSurfMod scr = (SubSurfMod)target;
		scr.oriMeshF = (MeshFilter)EditorGUILayout.ObjectField ("Simple mesh", scr.oriMeshF, typeof(MeshFilter), true);
		scr.finalMeshF = (MeshFilter)EditorGUILayout.ObjectField ("Final mesh", scr.finalMeshF, typeof(MeshFilter), true);
		EditorGUI.BeginChangeCheck ();
		scr._o = EditorGUILayout.ObjectField ("blendss file", scr._o, typeof(Object), false);
		if (EditorGUI.EndChangeCheck ()) {
			EditorUtility.SetDirty (scr);
			EditorSceneManager.MarkSceneDirty (EditorSceneManager.GetActiveScene ());
		}
			if (scr._o) {
				string p = AssetDatabase.GetAssetPath (scr._o);
				wrong = (!p.EndsWith (".blendss"));
				if (!wrong) {
					objs = new List<string> ();
					scr.oriMeshData = File.ReadAllText (p);
					scr.oriMeshDataSplit = scr.oriMeshData.Split (new char[] { ' ', '\r', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
					for (int x = 0, y = scr.oriMeshDataSplit.Length; x < y; x++) {
						if (scr.oriMeshDataSplit [x] == "obj") {
							objs.Add (scr.oriMeshDataSplit [x+1]);
							x++;
						}
					}
					hasObj = objs.Contains (scr._s);
				}
			}
			else wrong = true;
		//}
		GUIStyle style = new GUIStyle ();
		if (wrong) {
			style.normal.textColor = Color.red;
			EditorGUILayout.LabelField ("file is not blendss data", style);
		}
		else {
			//style.normal.textColor = hasObj? Color.black : Color.red;
			EditorGUI.BeginChangeCheck ();
			scr._s = EditorGUILayout.TextField ("object name", scr._s);
			if (EditorGUI.EndChangeCheck ()) {
				EditorUtility.SetDirty (scr);
				EditorSceneManager.MarkSceneDirty (EditorSceneManager.GetActiveScene ());
			}
				hasObj = objs.Contains (scr._s);
			//}
			if (!hasObj) {
				style.normal.textColor = Color.red;
				EditorGUILayout.LabelField ("Object name not found", style);
			}
		}

        scr._txl = EditorGUILayout.IntSlider("position texture texels", scr._txl, 1, 8);

        debug = EditorGUILayout.Toggle("Show default", debug);
        if (debug) DrawDefaultInspector();
    }
}
