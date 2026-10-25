bool ok = UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Local Reference/Export Reference Sheet");
var windows = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>();
string found = "none";
foreach (var w in windows) if (w.GetType().Name == "ExportReferenceSheetWindow") { found = w.titleContent.text; w.Repaint(); }
return "menu=" + ok + " window=" + found;
