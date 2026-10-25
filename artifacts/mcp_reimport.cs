AssetDatabase.ImportAsset("Assets/Editor/SpineLibraryTool.cs", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
return "reimport requested";
