var t = System.Type.GetType("MyClone.EditorTools.ReferenceSheetExporter, Assembly-CSharp-Editor");
return "type=" + (t != null) + " compiling=" + UnityEditor.EditorApplication.isCompiling + " failed=" + UnityEditor.EditorUtility.scriptCompilationFailed;
