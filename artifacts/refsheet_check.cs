var t = System.Type.GetType("MyClone.EditorTools.ReferenceSheetExporter, Assembly-CSharp-Editor");
if (t == null) return "TYPE MISSING compiling=" + UnityEditor.EditorApplication.isCompiling;
return "ok " + t.FullName + " compiling=" + UnityEditor.EditorApplication.isCompiling;
