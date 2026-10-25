var t = System.Type.GetType("MyClone.LocalReferenceParts.SliceAtlasParts, Assembly-CSharp-Editor");
if (t == null) return "TYPE MISSING";
var atlas = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/_LocalReference/Spine/body_1110901/body_1110901.atlas.txt");
var slice = t.GetMethod("Slice");
var files = (System.Collections.IList)slice.Invoke(null, new object[] { atlas, 100, true });
var prop = t.GetProperty("LastImportCheck");
return "sliced=" + files.Count + " files\n--- window/last check ---\n" + (string)prop.GetValue(null, null);
