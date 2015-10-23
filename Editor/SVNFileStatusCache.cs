using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SVNFileStatusCache{

	private static Dictionary<string,SVNFileType> _statusCache = new Dictionary<string, SVNFileType>();

	public static void Refresh(System.Action onDone = null){

		Debug.Log("[SVN] Update File Status"); 

		/*
		ShellHelper.ShellRequest propReq = SVNTools.CMDPropGet("svn:externals","","","-R");
		System.Text.StringBuilder propOutput = new System.Text.StringBuilder();
		propReq.onDone += delegate() {
			string log = propOutput.ToString();
			if(string.IsNullOrEmpty(log)){
				return;
			}
			int idx = log.LastIndexOf(" - ");
			if(idx == -1){
				Debug.LogError("parse error:"+log);
				return;
			}
			string file = log.Substring(0,idx);
			string property = log.Substring(idx+3);
			
			
			System.IO.StringReader read = new System.IO.StringReader(property);
			
			string line = null;
			do{
				line = read.ReadLine();
				if(line == null || line.Trim().Length == 0){
					break; 
				}
				string[] vars = line.Split(' ');
				string local = vars[0];
				string remote = vars[1];
				string fullPath = null;
				if(string.IsNullOrEmpty(file) || file == "." ){
					fullPath = local;
				}else{
					fullPath = file + "/" + local;
				}
				SetFileStatus(fullPath,SVNFileType.External);
			}while(line != null);
		};
		propReq.onLog += delegate(int arg1, string log) {
			if(arg1 != 0){
				Debug.LogError(log);
				return;
			}

			propOutput.AppendLine(log);

		};
*/
		_statusCache.Clear();
		ShellHelper.ShellRequest req =  SVNTools.CMDStatus("",new string[]{"Assets"},new string[]{"--ignore-externals"});
		req.onLog += delegate(int level,string log) {
			SVNFileStatus file = SVNTools.ParseFromLog(log);
			if(file == null){
				return;
			}
			SetFileStatus(file.path,file.status);
		};
		req.onDone += delegate() {
			if(onDone != null){
				onDone();
			}
		};
	}

	private static void SetFileStatus(string path,SVNFileType status){
		if(_statusCache.ContainsKey(path)){
			if(_statusCache[path] == SVNFileType.Conflict){

			}else{
				_statusCache[path] = status;
			}
		}else{
			_statusCache.Add(path,status);
		}

		string parent = System.IO.Path.GetDirectoryName(path);
		if(string.IsNullOrEmpty(parent)){
			return;
		}
		if(status == SVNFileType.External){
			return;
		}
		SVNFileType parentStatus = SVNFileType.Modify;
		if(status == SVNFileType.Conflict){
			parentStatus = SVNFileType.Conflict;
		}
		SetFileStatus(parent,parentStatus);
	}

	public static SVNFileType GetFileStatus(string path){
		if(string.IsNullOrEmpty(path)){
			return SVNFileType.None;
		}
		if(!_statusCache.ContainsKey(path)){
			SVNFileType rootType = GetFileStatus(System.IO.Path.GetDirectoryName(path));
			if(rootType == SVNFileType.New){
				return SVNFileType.New;
			}else if(rootType == SVNFileType.External){
				return SVNFileType.External;
			}
			return SVNFileType.None;
		}
		return _statusCache[path];
	}

	public static Dictionary<string,SVNFileType> Filter(string workDir,string prefix){
		Dictionary<string,SVNFileType> ret = new Dictionary<string, SVNFileType>();
		foreach(string key in _statusCache.Keys){
			if(key.StartsWith(System.IO.Path.Combine(workDir,prefix))){
				string rPath = key.Substring(workDir.Length);
				if(rPath.StartsWith("/")){
					rPath = rPath.Substring(1);
				}
				ret.Add(rPath,_statusCache[key]);
			}
		}
		return ret;
	}
}
