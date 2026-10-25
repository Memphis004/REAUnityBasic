UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate);
return "refresh requested; compiling=" + UnityEditor.EditorApplication.isCompiling +
  " failed=" + UnityEditor.EditorUtility.scriptCompilationFailed;
