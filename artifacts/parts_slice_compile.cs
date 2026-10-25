UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate);
return "refreshed compiling=" + UnityEditor.EditorApplication.isCompiling + " time=" + System.DateTime.Now.ToString("HH:mm:ss");
