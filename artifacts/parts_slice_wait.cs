if (UnityEditor.EditorApplication.isCompiling) return "compiling";
var t = System.Type.GetType("MyClone.LocalReferenceParts.SliceAtlasParts, Assembly-CSharp-Editor");
if (t == null) return "TYPE MISSING";
var m = t.GetMethod("CheckPageImportSettings");
return "compiled ok method=" + (m != null) + " scene=" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
