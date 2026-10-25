var t = typeof(MyClone.LocalReference.SpineDemoController);
var f = t.GetField("startWithPortrait");
return "compiling=" + UnityEditor.EditorApplication.isCompiling + " hasField=" + (f != null) + " scene=" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
