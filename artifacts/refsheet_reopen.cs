var w = UnityEditor.EditorWindow.GetWindow<MyClone.EditorTools.ExportReferenceSheetWindow>();
w.Repaint();
return "reopened " + w.titleContent.text;
