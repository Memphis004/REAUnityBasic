var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
return "dirty=" + s.isDirty + " scenes=" + UnityEngine.SceneManagement.SceneManager.sceneCount;
