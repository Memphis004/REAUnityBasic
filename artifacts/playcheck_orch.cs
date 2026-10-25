var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (!UnityEditor.EditorApplication.isPlaying)
    return "NOT PLAYING (scene=" + scene.path + ")";
var mi = typeof(MyClone.EditorTools.LocalReferenceDemoBuilder).GetMethod("RunPlayChecks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
if (mi == null) return "RunPlayChecks not found";
object ok = mi.Invoke(null, null);
return "RunPlayChecks=" + ok;
