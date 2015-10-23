using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;


public enum SVNFileType{
	None,
	New,
	Added,
	Modify,
	Delete,
	Conflict,
	Missing,
	External,
}

public class SVNTools {

	public static event System.Action onCommitSuccess;
	public static event System.Action onUpdateCompleted;
	private static ShellHelper _shell;

	static SVNTools(){
		_shell = new ShellHelper();
#if UNITY_EDITOR_OSX
		_shell.AddEnvironmentVars(SVNSetting.Instance.osxSVNPath);
#elif UNITY_EDITOR_WIN
		_shell.AddEnvironmentVars(SVNSetting.Instance.winSVNPath);
#endif
	}

	public static ShellHelper.ShellRequest CMDUpdate(string workDir,params string[] paths){
		System.Text.StringBuilder cmd = new System.Text.StringBuilder();
		cmd.Append("svn update ");
		for(int i = 0;i<paths.Length;i++){
			cmd.Append(" "+paths[i]);
		}
		
		ShellHelper.ShellRequest shellReq = _shell.ProcessCMD(cmd.ToString(),workDir);
		return shellReq;
	}


	public static void Update(string workDir,params string[] paths){
		SVNLogWindow logWindow = SVNLogWindow.current;
		ShellHelper.ShellRequest req = CMDUpdate(workDir,paths);
		req.onLog += delegate(int logLevel,string obj) {
			logWindow.Log(obj);
		};
	}

	public static ShellHelper.ShellRequest CMDStatus(string workDir,string[] files,string[] op = null){
		System.Text.StringBuilder cmd = new System.Text.StringBuilder();
		cmd.Append("svn status ");
		for(int i = 0;i<files.Length;i++){
			cmd.Append(" "+files[i]);
		}
		if(op != null){
			for(int i= 0;i<op.Length;i++){
				cmd.Append(" " +op[i]);
			}
		}
		Debug.Log(cmd.ToString());
		ShellHelper.ShellRequest req = _shell.ProcessCMD(cmd.ToString(),workDir);
		return req;
	}

	public static ShellHelper.ShellRequest Status(string workDir,params string[] files){
		SVNLogWindow logWindow = SVNLogWindow.Open();
		ShellHelper.ShellRequest req =  SVNTools.CMDStatus(workDir,files,new string[]{"--ignore-externals"});
		req.onLog += delegate(int level,string log) {
			logWindow.Log(log);
		};
		return req;
	}

	public static void ShowCommitWindow(string workDir,params string[] files){
		SVNCommitWindow commitWindow = SVNCommitWindow.Open();
		commitWindow.workDir = workDir;
		commitWindow.isFetching = true;
		SVNFileStatusCache.Refresh(delegate() {
			commitWindow.isFetching = false;
			for(int i = 0;i<files.Length;i++){
				Dictionary<string,SVNFileType> changed = SVNFileStatusCache.Filter(workDir,files[i]);
				foreach(string path in changed.Keys){
					commitWindow.Add(path,changed[path]);
				}
			}
		});
	}

	public static ShellHelper.ShellRequest Commit(string workDir,string msg,params string[] files){
		System.Text.StringBuilder cmd = new System.Text.StringBuilder();
		cmd.Append("svn commit ");
		for(int i = 0;i<files.Length ;i++){
			cmd.Append(" "+files[i].Replace(" ","\\ "));
		}
		if(string.IsNullOrEmpty(msg)){
			msg = "No Message";
		}
		msg = msg.Replace(" ","\\ ");
		cmd.Append(" -m "+msg);
		SVNLogWindow logWindow = SVNLogWindow.current;
		logWindow.Log("Commiting...");
		ShellHelper.ShellRequest req =  _shell.ProcessCMD(cmd.ToString(),workDir);
		req.onLog += delegate(int arg1, string arg2) {
			SVNLogWindow.current.Log(arg2);
		};
		req.onDone += delegate() {
			if(onCommitSuccess != null){
				onCommitSuccess();
			}
		};
		return req;

	}

	public static ShellHelper.ShellRequest Add(string workDir,params string[] files){
		if(files.Length == 0){
			Debug.LogException(new System.Exception("No Enough Arguments"));
			return null;
		}
		System.Text.StringBuilder cmd = new System.Text.StringBuilder();
		cmd.Append("svn add ");
		for(int i = 0;i<files.Length ;i++){
			cmd.Append(" "+files[i].Replace(" ","\\ "));
		}
		SVNLogWindow logWindow = SVNLogWindow.current;
		ShellHelper.ShellRequest req = _shell.ProcessCMD(cmd.ToString(),workDir);
		req.onLog += delegate(int arg1, string arg2) {
			logWindow.Log(arg2);
		};
		return req;
	}

	public static ShellHelper.ShellRequest CMDCleanup(string workDir,params string[] paths){
		System.Text.StringBuilder cmd = new System.Text.StringBuilder();
		cmd.Append("svn cleanup ");
		for(int i = 0;i<paths.Length ;i++){
			cmd.Append(" "+paths[i].Replace(" ","\\ "));
		}
		ShellHelper.ShellRequest req = _shell.ProcessCMD(cmd.ToString(),workDir);
		return req;
	}

	public static ShellHelper.ShellRequest Cleanup(string workDir,params string[] paths){
		ShellHelper.ShellRequest req = CMDCleanup(workDir,paths);
		SVNLogWindow.current.Log("Cleanup...");
		req.onLog += delegate(int arg1, string arg2) {
			SVNLogWindow.current.Log(arg2);
		};
		req.onDone += delegate() {
			SVNLogWindow.current.Log("Completed!");
		};
		return req;
	}

	public static ShellHelper.ShellRequest CMDDelete(string workDir,params string[] paths){
		System.Text.StringBuilder cmd = new System.Text.StringBuilder();
		cmd.Append("svn delete ");
		for(int i = 0;i<paths.Length ;i++){
			cmd.Append(" "+paths[i].Replace(" ","\\ "));
		}
		ShellHelper.ShellRequest req = _shell.ProcessCMD(cmd.ToString(),workDir);
		return req;
	}

	public static ShellHelper.ShellRequest Delete(string workDir,params string[] paths){
		ShellHelper.ShellRequest req = CMDDelete(workDir,paths);
		req.onLog += delegate(int arg1, string arg2) {
			SVNLogWindow.current.Log(arg2);
		};
		return req;
	}

	public static ShellHelper.ShellRequest CMDPropList(string workDir,string path){
		System.Text.StringBuilder cmd = new System.Text.StringBuilder();
		cmd.Append("svn proplist ");
		cmd.Append(path);
		ShellHelper.ShellRequest req = _shell.ProcessCMD(cmd.ToString(),workDir);
		return req;
	}

	public static ShellHelper.ShellRequest CMDPropGet(string propName,string workDir,string path,params string[] param){
		System.Text.StringBuilder cmd = new System.Text.StringBuilder();
		cmd.Append("svn propget ");
		cmd.Append(propName);
		cmd.Append(" " );
		cmd.Append(path);
		for(int i =0;i<param.Length;i++){
			cmd.Append(param[i]);
			cmd.Append(" ");
		}
		ShellHelper.ShellRequest req = _shell.ProcessCMD(cmd.ToString(),workDir);
		return req;
	}

	public static ShellHelper.ShellRequest CMDPropSet(string propName,string propValue,string workDir,string path){
		System.Text.StringBuilder cmd = new System.Text.StringBuilder();
		cmd.Append("svn propset ");
		cmd.Append(propName);
		cmd.Append(" " );
		cmd.Append("\'");
		cmd.Append(propValue);
		cmd.Append(" \'");
		cmd.Append(" ");
		cmd.Append(path);
		ShellHelper.ShellRequest req = _shell.ProcessCMD(cmd.ToString(),workDir);
		return req;
	}


	public static ShellHelper.ShellRequest ShowProp(string workDir,string path){
		SVNPropWindow win = SVNPropWindow.Open();
		win.SetPath(workDir,path);
		ShellHelper.ShellRequest req =  CMDPropGet("svn:externals",workDir,path);
		req.onLog += delegate(int arg1, string arg2) {
			if(arg1!=0){
				Debug.LogError(arg2);
				return;
			}
			arg2 = arg2.Trim();
			int idx = arg2.LastIndexOf(" ");
			if(idx == -1){
				return;
			}
			string local = arg2.Substring(0,idx).Trim();
			string remote = arg2.Substring(idx).Trim();
			win.AddExternal(local,remote);
		};
		return null;
	}

	[MenuItem("Assets/SVN/Update",true)]
	public static bool IsValid(){
		Object[] objs = Selection.GetFiltered(typeof(Object),SelectionMode.TopLevel|SelectionMode.Assets);
		for(int i = 0;i<objs.Length;i++){
			Object o = objs[i];
			string path = AssetDatabase.GetAssetPath(o);
			if(!string.IsNullOrEmpty(path)){
				return true;
			}
		}
		return false;
	}

	private static List<string> GetSelectionPaths(){
		List<string> paths = new List<string>();
		Object[] objs = Selection.GetFiltered(typeof(Object),SelectionMode.TopLevel|SelectionMode.Assets);
		for(int i = 0;i<objs.Length;i++){
			Object o = objs[i];
			string path = AssetDatabase.GetAssetPath(o);
			if(!string.IsNullOrEmpty(path)){
				paths.Add(path);
			}
		}
		return paths;
	}

	private static List<string> GetIndependentSelectionPaths(){
		List<string> paths = GetSelectionPaths();
		HashSet<int> toBeRemovedIndexs = new HashSet<int>();
		for(int i = 0;i<paths.Count;i++){
			for(int j=0;j<paths.Count;j++){
				if(i == j){
					continue;
				}
				if(paths[j].StartsWith(paths[i])){
					toBeRemovedIndexs.Add(j);
				}
			}
		}

		for(int i = paths.Count -1;i>=0;i--){
			if(toBeRemovedIndexs.Contains(i)){
				paths.RemoveAt(i);
			}
		}
		return paths;
	}

	private static string GetRoot(List<string> paths){
		string root = null;
		foreach(string path in paths){
			if(root == null){
				root = System.IO.Path.GetDirectoryName(path);
			}else{
				root = GetSamePrefix(root,System.IO.Path.GetDirectoryName(path));
			}
		}
		return root;
	}

	private static List<string> GetRelativePathsUnderCommonRoot(List<string> paths,out string root){
		root = GetRoot(paths);
		if(string.IsNullOrEmpty(root)){
			return paths;
		}
		List<string> files = new List<string>();
		foreach(string path in paths){
			string rPath = path.Substring(root.Length);
			if(rPath.StartsWith("/")){
				rPath = rPath.Substring(1);
				files.Add(rPath);
			}
		}
		return files;
	}

	private static string GetSamePrefix(string str1,string str2){
		string[] str1s = str1.Split('/');
		string[] str2s = str2.Split('/');
		int len = Mathf.Min(str1s.Length,str2s.Length);
		System.Text.StringBuilder ret = new System.Text.StringBuilder();
		for(int i = 0 ;i<len;i++){
			if(str1s[i] == str2s[i]){
				ret.Append(str1s[i]+"/");
			}
		}
		if(ret.Length == 0){
			return "";
		}
		ret.Remove(ret.Length-1,1);
		return ret.ToString();
	}



	[MenuItem("Assets/SVN/Update")]
	public static void UpdateCurrent(){
		List<string> paths = GetIndependentSelectionPaths();
		string root = null;
		List<string> files = GetRelativePathsUnderCommonRoot(paths,out root);
		Update(root,files.ToArray());
	}

	[MenuItem("Assets/SVN/Status")]
	public static void StatusCurrent(){ 
		List<string> paths = GetIndependentSelectionPaths();
		string root = null;
		List<string> files = GetRelativePathsUnderCommonRoot(paths,out root);
		Status(root,files.ToArray());
	}

	[MenuItem("Assets/SVN/Commit")]
	public static void CommitCurrent(){  
		List<string> paths = GetIndependentSelectionPaths();
		string root = null;
		List<string> files = GetRelativePathsUnderCommonRoot(paths,out root);
		ShowCommitWindow(root,files.ToArray());
	}

	[MenuItem("Assets/SVN/Cleanup")]
	public static void CleanupCurrent(){  
		List<string> paths = GetIndependentSelectionPaths();
		string root = null;
		List<string> files = GetRelativePathsUnderCommonRoot(paths,out root);
		Cleanup(root,files.ToArray());
	}


	[MenuItem("Assets/SVN/Properity")]
	public static void ShowProperityCurrent(){ 

		List<string> paths = GetIndependentSelectionPaths();
		string root = null;
		List<string> files = GetRelativePathsUnderCommonRoot(paths,out root);
		if(files.Count == 0){
			Debug.LogError("You must select a folder or file");
			return;
		}
		if(files.Count == 0){
			Debug.LogError("You should select only one folder or file");
			return;
		}

		ShowProp(root,files[0]);
	}

	public static SVNFileStatus ParseFromLog(string log){
		SVNFileStatus file = new SVNFileStatus();
		string path = null;
		SVNFileType type = SVNFileType.None;
		if(string.IsNullOrEmpty(log) || log.StartsWith(" ")){
			return null;
		}else{

			string symbols = log.Substring(0,7);
			path = log.Substring(7).Trim();
			if(symbols[0]=='M'){
				type = SVNFileType.Modify;
			}else if(symbols[0] == 'A'){
				type = SVNFileType.Added;
			}else if(symbols[0] == '?'){
				type = SVNFileType.New;
			}else if(symbols[0] == 'D'){
				type = SVNFileType.Delete;
			}else if(symbols[0] == 'C'){
				type = SVNFileType.Conflict;
			}else if(symbols[0] == '!'){
				type = SVNFileType.Missing;
			}else if(symbols[0] == 'X'){
				type = SVNFileType.External;
			}
		}
		if(type != SVNFileType.None){
			file.status = type;
			file.path = path;
			return file;
		}
		return null;
	}

}

public class SVNFileStatus{
	public SVNFileType status;
	public string path;
}
