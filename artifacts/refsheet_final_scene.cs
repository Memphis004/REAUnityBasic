var sb = new System.Text.StringBuilder();
sb.AppendLine("scenes=" + UnityEngine.SceneManagement.SceneManager.sceneCount);
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) sb.AppendLine("  " + UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path);
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
int leftover = 0;
foreach (var go in scene.GetRootGameObjects()) if (go.name.StartsWith("~ReferenceSheet")) leftover++;
sb.AppendLine("leftovers=" + leftover + " isPlaying=" + UnityEditor.EditorApplication.isPlaying);
return sb.ToString();
