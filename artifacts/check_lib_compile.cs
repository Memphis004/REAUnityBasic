var c = typeof(MyClone.LocalReference.SpineDemoController);
var b = typeof(MyClone.EditorTools.LocalReferenceDemoBuilder);
var m = c.GetMethod("NextLibrary");
var f = c.GetField("libraryExtras");
return "compiling=" + UnityEditor.EditorApplication.isCompiling +
  " NextLibrary=" + (m != null) + " libraryExtras=" + (f != null) +
  " builderBuild=" + (b.GetMethod("Build") != null);
