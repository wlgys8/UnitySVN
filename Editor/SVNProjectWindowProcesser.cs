using UnityEngine;
using System.Collections;
using UnityEditor;

[InitializeOnLoad]
public class SVNProjectWindowProcesser  {

	private static Texture _modifiedTex;
	private static Texture _commitedTex;
	private static Texture _newTex;
	private static Texture _addTex;
	private static Texture _conflictTex;
	private static Texture _externalTex;

	static SVNProjectWindowProcesser(){
		_commitedTex = AssetDatabase.LoadAssetAtPath<Texture>(SVNSetting.SVNPluginPath + "/assets/icon-commited.png");
		_modifiedTex = AssetDatabase.LoadAssetAtPath<Texture>(SVNSetting.SVNPluginPath + "/assets/icon-modify.png");
		_newTex = AssetDatabase.LoadAssetAtPath<Texture>(SVNSetting.SVNPluginPath + "/assets/icon-new.png");
		_addTex = AssetDatabase.LoadAssetAtPath<Texture>(SVNSetting.SVNPluginPath +  "/assets/icon-add.png");
		_conflictTex =  AssetDatabase.LoadAssetAtPath<Texture>(SVNSetting.SVNPluginPath +"/assets/icon-conficted.png");
		_externalTex =  AssetDatabase.LoadAssetAtPath<Texture>(SVNSetting.SVNPluginPath +"/assets/icon-external-link.png");
		EditorApplication.projectWindowItemOnGUI += OnProjectItemGUI;
	}

	private static void OnProjectItemGUI(string guid,Rect selectionRect){
		string assetPath = AssetDatabase.GUIDToAssetPath(guid);
		if(string.IsNullOrEmpty(assetPath)){
			return;
		}
		SVNFileType type = SVNFileStatusCache.GetFileStatus(assetPath);
		Texture icon = _commitedTex;
		switch(type){
		case SVNFileType.Added:
			icon = _addTex;
			break;
		case SVNFileType.Modify:
			icon = _modifiedTex;
			break;
		case SVNFileType.Delete:
			break;
		case SVNFileType.New:
			icon = _newTex;
			break;
		case SVNFileType.Conflict:
			icon = _conflictTex;
			break;
		case SVNFileType.External:
			icon = _externalTex;
			break;
		case SVNFileType.None:
			break;
		} 
		GUI.DrawTexture(new Rect(selectionRect.xMin-5,selectionRect.yMin-5,15,15),icon);
	}
}
