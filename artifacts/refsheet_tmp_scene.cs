var sc = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
return "valid=" + sc.IsValid() + " name=" + sc.name + " roots=" + sc.rootCount + " active=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
