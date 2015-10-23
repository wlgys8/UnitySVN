using UnityEngine;
using System.Collections;
using UnityEditor;

public class SVNSetting : ScriptableObject {

	public static string SVNPluginPath = "Assets/Game/EditorTools/SVN";
	public string winSVNPath;

	public string osxSVNPath;



	private static SVNSetting _instance;
	public static SVNSetting Instance{
		get{
			string path = SVNPluginPath+"/Setting.asset";
			if(_instance == null){
				_instance = AssetDatabase.LoadAssetAtPath<SVNSetting>(path);
			}
			if(_instance == null){
				_instance  = ScriptableObject.CreateInstance<SVNSetting>();
				AssetDatabase.CreateAsset(_instance,path);
				AssetDatabase.ImportAsset(path);
			}
			return _instance;
		}
	}

	[MenuItem("Assets/SVN/Setting")]
	public static void Show(){
		Selection.activeObject = SVNSetting.Instance;
	}
}
