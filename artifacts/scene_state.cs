var s = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
return "scene=" + s.path + " dirty=" + s.isDirty + " playing=" + UnityEditor.EditorApplication.isPlaying + " compiling=" + UnityEditor.EditorApplication.isCompiling;
