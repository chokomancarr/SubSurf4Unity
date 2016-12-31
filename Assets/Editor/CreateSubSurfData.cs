using UnityEngine;
using UnityEditor;
using System.Collections;
using Microsoft.Win32;
using System.IO;

public class CreateSubSurfData : MonoBehaviour {

	[MenuItem("Assets/GetSS")]
	static void Execute () {
		string s = FindBlender ();
		if (s == "") {
			Debug.Log ("Blender is not installed! (?)");
			return;
		}
		Object o = Selection.activeObject;
		if (!o) {
			Debug.Log ("No asset selected!");
			return;
		}
		string path = AssetDatabase.GetAssetPath(o);
		if (!path.EndsWith(".blend")) {
			Debug.Log ("Asset is not blend file!");
			return;
		}
		string p = Application.dataPath.Remove (Application.dataPath.Length - 6) + path;
		Debug.Log ("Executing: " + p);
		//var process = new System.Diagnostics.Process ();
		if (!File.Exists (Application.dataPath + "/Editor/ExportSS.py")) {
			Debug.Log ("python script not found!");
			return;
		}
		System.Diagnostics.Process.Start (s, p + " --background --python " + Application.dataPath + "/Editor/ExportSS.py" + " -- " + new DirectoryInfo(p).Parent.FullName + "/ " + new DirectoryInfo(p).Name + "ss");

	}

	static string FindBlender () {
		string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
		using (RegistryKey key = Registry.LocalMachine.OpenSubKey (registry_key)) {
			foreach (string s in key.GetSubKeyNames())
            {
                using (RegistryKey key2 = key.OpenSubKey(s))
                {
                    if ((string)key2.GetValue("DisplayName", "") == "Blender")
                    {
                        string loc = (string)key2.GetValue("InstallLocation", "") + "Blender.exe";
                        Debug.Log("Blender (" + key2.GetValue("DisplayVersion", "unknown version") + ") found at " + loc);
                        return loc;
                    }
                }
            }
        }
        return "";
    }
}
