﻿using UnityEngine;
using System.Collections;
using UnityEditor;

public class SVNAssetModifyProcesser  : AssetPostprocessor{

	static SVNAssetModifyProcesser(){
		SVNTools.onCommitSuccess += OnCommitSuccess;
	}

	static void OnCommitSuccess(){
		SVNFileStatusCache.Refresh(delegate() {
			EditorApplication.RepaintProjectWindow();
		}); 
	}

	public static void OnPostprocessAllAssets(string[] importedAssets,string[] deletedAssets,string[] movedAssets,string[] movedFromPath){
		SVNFileStatusCache.Refresh(delegate() {
			EditorApplication.RepaintProjectWindow();
		});
	}

	[UnityEditor.Callbacks.DidReloadScripts]
	public static void OnDidReloadScripts(){
		SVNFileStatusCache.Refresh(delegate() {
			EditorApplication.RepaintProjectWindow();
		});
	} 
}
