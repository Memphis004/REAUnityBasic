var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
var roots = scene.GetRootGameObjects();
var leftovers = new System.Collections.Generic.List<string>();
foreach (var go in roots)
	if (go.name.StartsWith("~ReferenceSheet")) leftovers.Add(go.name);
int canvases = 0, cameras = 0;
foreach (var go in roots) {
	canvases += go.GetComponentsInChildren<UnityEngine.Canvas>(true).Length;
	cameras += go.GetComponentsInChildren<UnityEngine.Camera>(true).Length;
}
return "scene=" + scene.path + " roots=" + roots.Length + " leftovers=" + (leftovers.Count == 0 ? "none" : string.Join(",", leftovers)) +
	" cameras=" + cameras + " canvases=" + canvases + " dirty=" + scene.isDirty;
