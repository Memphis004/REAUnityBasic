var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/_LocalReference/Spine/body_1110901/body_1110901_SkeletonData.asset");
UnityEditor.Selection.activeObject = asset;
bool ok = UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Local Reference/Export Reference Sheet");
return "menu=" + ok + " selection=" + (UnityEditor.Selection.activeObject == null ? "null" : UnityEditor.Selection.activeObject.name);
