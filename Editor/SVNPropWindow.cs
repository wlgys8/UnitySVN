using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class SVNPropWindow : ScriptableWizard {

	public static SVNPropWindow Open(){
		SVNPropWindow win = SVNPropWindow.DisplayWizard<SVNPropWindow>("Props");
		win.Focus();
		win.createButtonName = "Apply";
		return win;
	}

	public List<string> externals = new List<string>();
	public List<string> ingore = new List<string>();

	private string _workDir;
	private string _path;

	public void SetPath(string workDir,string path){
		_workDir = workDir;
		_path = path;
	}

	public void AddExternal(string localPath,string remoteUrl){
		externals.Add(localPath +" " +remoteUrl);
	}

	void OnWizardCreate () {
		Debug.Log("Create");
		System.Text.StringBuilder str = new System.Text.StringBuilder();
		for(int i =0;i<externals.Count ;i++){
			string line = externals[i];
			str.Append(line);
			if(i != externals.Count -1){
				str.AppendLine();
			}
		}
		ShellHelper.ShellRequest req = SVNTools.CMDPropSet("svn:externals",str.ToString(),_workDir,_path);
		req.onLog += delegate(int arg1, string arg2) {
			Debug.Log(arg2);
		};
	}

	protected override bool DrawWizardGUI ()
	{
		if(_workDir != null){
			string dir = _workDir;
			if(string.IsNullOrEmpty(dir.Trim())){
				dir = "./";
			}
			EditorGUILayout.LabelField("workDir:",dir);
		}
		if(_path != null){
			EditorGUILayout.LabelField("file:",_path);
		}
		base.DrawWizardGUI();
		return false;
	}


}
