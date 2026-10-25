AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
if (EditorUtility.scriptCompilationFailed) {
	var errors = "";
	var c = System.Environment.NewLine;
	foreach (var p in new[] { "Assets/Editor/PartSheetGroups.cs", "Assets/Editor/PartSheetLayout.cs", "Assets/Editor/PartSheetImage.cs", "Assets/Editor/PartSheetExporter.cs" }) {
		var imp = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.MonoScript>(p) as UnityEditorInternal.MonoScript;
	}
	var logPath = Application.dataPath + "/../Logs/AssetImportWorker0.log";
	return "compilation failed: " + EditorUtility.scriptCompilationFailed;
}
return "compiled ok: " + typeof(MyClone.EditorTools.PartSheetExporter).FullName + " groups=" + typeof(MyClone.EditorTools.PartSheetGroups).FullName;
