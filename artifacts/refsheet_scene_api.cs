var t = typeof(UnityEditor.SceneManagement.EditorSceneManager);
var names = new System.Collections.Generic.List<string>();
foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
	if (m.Name.IndexOf("Dirty", System.StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("PreviewScene", System.StringComparison.OrdinalIgnoreCase) >= 0)
		names.Add(m.Name);
return string.Join(", ", names.ToArray());
