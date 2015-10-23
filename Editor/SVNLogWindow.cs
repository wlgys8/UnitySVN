using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class SVNLogWindow :EditorWindow{


	private static SVNLogWindow _current;

	public static SVNLogWindow Open(){
	//	_current = ScriptableWizard.DisplayWizard<SVNLogWindow>("Log");
		_current =  EditorWindow.CreateInstance<SVNLogWindow>();
		_current.Focus();
		_current.ShowUtility();
		return _current;
	}

	public static SVNLogWindow current{
		get{
			if(_current == null){
				Open();
			}
			return _current;
		}
	}

	private List<string> _msgs = new List<string>();

	public void Log(string msg){
		_msgs.Add(msg);
		this.Repaint();
	}


	private Vector2 _scrollPos;
	void OnGUI(){

		GUILayout.BeginHorizontal(EditorStyles.toolbar);

		
		if(GUILayout.Button("Clear",EditorStyles.toolbarButton)){
			_msgs.Clear();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
		foreach(string msg in _msgs){
			GUILayout.Label(msg);
		}
		EditorGUILayout.EndScrollView();
	}
}
