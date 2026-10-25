if (UnityEditor.EditorApplication.isPlaying)
  typeof(UnityEditor.EditorApplication).GetMethod("ExitPlaymode").Invoke(null, null);
return "exit requested; playing=" + UnityEditor.EditorApplication.isPlaying;
