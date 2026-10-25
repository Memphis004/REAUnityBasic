if (UnityEditor.EditorApplication.isPlaying) return "already playing";
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_LocalReference/Demo.unity");
UnityEditor.EditorApplication.EnterPlaymode();
return "entering play mode";
