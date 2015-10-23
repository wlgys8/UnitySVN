using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class SVNCommitWindow : EditorWindow {

	public static SVNCommitWindow Open(){
		SVNCommitWindow win = EditorWindow.CreateInstance<SVNCommitWindow>();
		win.Focus();
		win.ShowUtility();
		return win;
	}

	private Texture _fallbackIcon;

	private List<Item> _topLevelItems = new List<Item>();
	private Dictionary<string,Item> _itemMap = new Dictionary<string, Item>();
	private List<Item> _focusedItems = new List<Item>();

	private GUISkin _skin;
	private bool _needRepaint = false;
	private bool _needClose = false;
	public string _workDir = "";
	public bool isFetching = false;

	public string workDir  {
		set{
			_workDir = value;
		}get{
			return _workDir;
		}
	}

	private Texture fallbackIcon{
		get{
			if(_fallbackIcon == null){
				_fallbackIcon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Game/EditorTools/SVN/icon.psd");
			}
			return _fallbackIcon;
		}
	}

	private Item AddItem(SVNFileType type,string path){
		if(_itemMap.ContainsKey(path)){
			if(type == SVNFileType.None){
				return _itemMap[path];
			}
			if(_itemMap[path].type == SVNFileType.None){
				_itemMap[path].type = type;
			}else{
				Debug.LogError("Somthing error");
			}
			return _itemMap[path];
		}

		Item item = new Item();
		item.isSelected = true;
		item.isFocused = false;
		item.type = type;
		item.path = path.Trim();
		string parent = System.IO.Path.GetDirectoryName(item.path);
		if(string.IsNullOrEmpty(parent)){//top level
			_topLevelItems.Add(item);
		}else{
			if(!_itemMap.ContainsKey(parent)){
				Item direItem = AddItem(SVNFileType.None,parent);
				direItem.isFolder = true;
			}
			_itemMap[parent].children.Add(item);
		}
		if(_itemMap.ContainsKey(item.path)){
			return item;
		}
		_itemMap.Add(item.path,item);
		return item;

	}

	public Item Add(string path,SVNFileType type){
		Item item = new Item();
		item.isSelected = true;
		item.isFocused = false;
		switch(type){
		case SVNFileType.Added:
		case SVNFileType.Modify:
		case SVNFileType.Delete:
		case SVNFileType.New:
		case SVNFileType.Missing:
			AddItem(type,path);
			break;
		}
		return item;
	}
	public Item Log(string line){
		if(line.StartsWith(" ")){
			return null;
		}
		if(string.IsNullOrEmpty(line)){
			return null;
		}
		SVNFileStatus file = SVNTools.ParseFromLog(line);
		if(file == null){
			return null;
		}
		Item item = new Item();
		item.isSelected = true;
		item.isFocused = false;
		switch(file.status){
		case SVNFileType.Added:
		case SVNFileType.Modify:
		case SVNFileType.Delete:
		case SVNFileType.New:
		case SVNFileType.Missing:
			AddItem(file.status,file.path);
			break;
		}
		return item;
	}

	private void UnFocusAll(){
		TranverseAll(delegate(Item item) {
			item.isFocused = false;
			return false;
		});
	}

	private void TranverseAll(System.Func<Item,bool> visit,bool foldout = false){
		foreach(Item item in _topLevelItems){
			Tranverse(item,visit,foldout);
		} 
	}

	private void Tranverse(Item item,System.Func<Item,bool> visit,bool foldout = false){
		visit(item);
		if(!foldout && !item.isFoldout){
			return;
		}
		foreach(Item child in item.children){
			Tranverse(child,visit,foldout);
		}
	}

	private void OnCommitClick(){

		List<string> tobeAddList = new List<string>();
		List<string> list = new List<string>();
		List<string> tobeDeleteList = new List<string>();

		HashSet<SVNFileType> commitType = new HashSet<SVNFileType>();
		commitType.Add(SVNFileType.Added);
		commitType.Add(SVNFileType.Modify);
		commitType.Add(SVNFileType.Delete);
		TranverseAll(delegate(Item item) {
			if(!item.isSelected){
				return false;
			}
			if(commitType.Contains(item.type)){
				list.Add(item.path);
			}
			if(item.type == SVNFileType.New){
				tobeAddList.Add(item.path);
			}
			if(item.type == SVNFileType.Missing){
				tobeDeleteList.Add(item.path);
			}
			return false;
		},true);

		Delete_Add_Commit(tobeDeleteList,tobeAddList,list);
		return;
	}

	private void Delete_Add_Commit(List<string> deleteList,List<string> addList,List<string> commitList){
		if(deleteList.Count > 0){
			ShellHelper.ShellRequest deleteReq = SVNTools.Delete(workDir,deleteList.ToArray());
			deleteReq.onDone += delegate() {
				commitList.AddRange(deleteList);
				Add_Commit(addList,commitList);
			};
		}else{
			Add_Commit(addList,commitList);
		}
	}
	private void Add_Commit(List<string> addList,List<string> commitList){
		if(addList.Count == 0){
			if(commitList.Count == 0){
				Debug.LogError("No files to be commited");
				return ;
			}
			SVNTools.Commit(workDir,_commitMessage, commitList.ToArray());
			_needClose = true;
			return;
		}else{
			ShellHelper.ShellRequest req = SVNTools.Add(this.workDir,addList.ToArray());
			req.onDone += delegate() {
				commitList.AddRange(addList);
				SVNTools.Commit(workDir,_commitMessage, commitList.ToArray());
				_needClose = true;
			}; 
			return;
		}
	}

	private void DrawItem(Item item,int level){

		GUIContent content = new GUIContent(item.fileName);

		if(item.isFocused){
			GUI.color = Color.blue;
			GUI.Box(item.position,"");
			GUI.color = Color.white;
		}

		var width = GUI.skin.label.CalcSize(content).x;
		EditorGUIUtility.labelWidth = width;
		EditorGUILayout.BeginHorizontal();

		for(int i=0;i<level;i++){
			GUILayout.Space(10);
		}
		string assetPath = "";
		if(!workDir.EndsWith("/") && !string.IsNullOrEmpty(workDir.Trim())){
			assetPath = workDir+"/";
		}
		assetPath += item.path;
		Texture icon = AssetDatabase.GetCachedIcon(assetPath);
		if(icon == null || icon.width <=1){
			icon = fallbackIcon;
		}
		if(item.children.Count > 0){
			GUILayout.Box(icon,_skin.customStyles[0],GUILayout.Width(13),GUILayout.Height(13));
			GUI.changed = false;
			GUIContent t = new GUIContent(item.fileName, AssetDatabase.GetCachedIcon(assetPath));
			item.isFoldout = EditorGUILayout.Foldout(item.isFoldout,t);
			if(GUI.changed){
				_needRepaint = true;
			}
		}else{
			GUILayout.Box(icon,_skin.customStyles[0],GUILayout.Width(13),GUILayout.Height(13));
			GUILayout.Space(10);
			EditorGUILayout.LabelField(item.fileName);
		}
		GUILayout.FlexibleSpace();
		Color baseColor = GUI.skin.label.normal.textColor;
		if(item.type == SVNFileType.Added){
			GUI.skin.label.normal.textColor = Color.green;
			GUILayout.Label("A",GUILayout.Width(50));
		}else if(item.type == SVNFileType.Modify){
			GUI.skin.label.normal.textColor = Color.yellow;
			GUILayout.Label("M",GUILayout.Width(50));
		}else if(item.type == SVNFileType.New){
			GUI.skin.label.normal.textColor = Color.blue;
			GUILayout.Label("?",GUILayout.Width(50));
		}else if(item.type == SVNFileType.Delete){
			GUI.skin.label.normal.textColor = Color.red;
			GUILayout.Label("D",GUILayout.Width(50));
		}else if(item.type == SVNFileType.Missing){
			GUI.skin.label.normal.textColor = Color.red;
			GUILayout.Label("Missing",GUILayout.Width(50));
		}
		else if(item.type == SVNFileType.None){
			GUILayout.Label("",GUILayout.Width(50));
		}
		GUI.skin.label.normal.textColor = baseColor; 

		GUI.changed = false;
		bool isSelected = EditorGUILayout.Toggle(item.isSelected,GUILayout.Width(25));
		if(GUI.changed){
			if(item.isFocused){
				TranverseAll(delegate(Item it) {
					if(it.isFocused){
						it.isSelected = isSelected;
					}
					return false;
				});
			}else{
				UnFocusAll();
				item.isSelected = isSelected;
			}
		}

		EditorGUILayout.EndHorizontal();
		if(Event.current.type == EventType.Repaint){
			Rect rect = GUILayoutUtility.GetLastRect();
			rect.height += EditorGUIUtility.standardVerticalSpacing;
			if(item.position != rect){
				_needRepaint = true;
			}
			item.position = rect;
		}
		if(item.isFoldout){
			foreach(Item child in item.children){
				DrawItem(child,level + 1);
			}
		}
	}


	private Vector2 _scrollPos;
	private string _commitMessage;
	private float _firstItemPosY = -1;
	void OnGUI(){ 
		if(_skin == null){
			_skin = AssetDatabase.LoadAssetAtPath<GUISkin>(SVNSetting.SVNPluginPath+"/skin.guiskin");
		}
		GUI.skin = _skin;
		EditorGUILayout.LabelField("WorkDir:",workDir);

		EditorGUILayout.BeginVertical(EditorStyles.helpBox);
		GUILayout.Space(1);
		if(Event.current.type == EventType.Repaint){
			_firstItemPosY = - GUILayoutUtility.GetLastRect().yMin - EditorGUIUtility.standardVerticalSpacing*2;
		}
		_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos,EditorStyles.inspectorFullWidthMargins);

		if(isFetching){
			EditorGUILayout.LabelField("Fetching...");
		}
		else if(_topLevelItems.Count == 0){
			EditorGUILayout.LabelField("Empty");
		}
		foreach(Item item in _topLevelItems){
			DrawItem(item,0);
		} 
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
		_commitMessage = EditorGUILayout.TextArea(_commitMessage,GUILayout.Height(50));
		if(GUILayout.Button("Commit")){
			OnCommitClick();
		}
		
		if(_needRepaint){
			this.Repaint();
			_needRepaint = false;
		}
		if(_needClose){
			_needClose = false;
			this.Close();
			return;

		}

		// EVENT DETECTOR
		if(Event.current.type == EventType.MouseDown){
			TranverseAll(delegate(Item item) {
				if(item.position.Contains( Event.current.mousePosition + _scrollPos + new Vector2(0,_firstItemPosY))){
					if(Event.current.command){
						if(item.isFocused){
							item.isFocused = false;
						}else{
							item.isFocused = true;
						}
					}else if(Event.current.shift){
						bool isIn = false;
						bool startFromSelect = false;
						Item startItem = null;
						Item endItem = null;
						TranverseAll(delegate(Item obj) {
							if(startItem == null){
								if(obj == item || obj.isFocused){
									startItem = obj;
								}
								return false;
							}else if(endItem == null){
								if((startItem == item && obj.isFocused) || (startItem != item && obj == item)){
									endItem = obj;
								}
								return false;
							}else{
								return true;
							}

						});
						if(endItem == null){
							startItem.isFocused = true;
							return true;
						}
						TranverseAll(delegate(Item obj) {
							if(!isIn && obj == startItem){
								isIn = true;
							}
							obj.isFocused = isIn;

							if(isIn && obj == endItem){
								isIn = false;
							}
							return false;
						});
					}else{
						UnFocusAll();
						item.isFocused = true;
					}
					this.Repaint();
					return true;
				}
				return false;
			});

		}
	}


	public class Item{
		public bool isFolder = false;
		public SVNFileType type;
		public string path;
		private bool _isSelected;
		public bool isFocused;
		public Rect position;
		public bool isFoldout = true;
		public List<Item> children = new List<Item>();

		public string fileName{
			get{
				return System.IO.Path.GetFileName(path);
			}
		}

		public bool isSelected{
			set{
				_isSelected = value;
				for(int i =0;i<children.Count;i++){
					children[i].isSelected = _isSelected;
				}
			}get{
				return _isSelected;
			}
		}

	}
}
