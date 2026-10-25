var t = System.Type.GetType("MyClone.LocalReferenceParts.SliceAtlasParts, Assembly-CSharp-Editor");
if (t == null) return "TYPE MISSING";
var atlas = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/_LocalReference/Spine/body_1110901/body_1110901.atlas.txt");
if (atlas == null) return "ATLAS MISSING";
var checkFn = t.GetMethod("CheckAtlasImport");
var sb = new System.Text.StringBuilder();

System.Func<object,string> describe = delegate(object o) {
  if (o == null) return "(null)";
  var ot = o.GetType();
  bool passed = (bool)ot.GetProperty("Passed").GetValue(o, null);
  return "passed=" + passed + "\n" + (string)ot.GetMethod("Summary").Invoke(o, null);
};

sb.AppendLine("--- clean state, check only ---");
sb.AppendLine(describe(checkFn.Invoke(null, new object[] { atlas, false })));

string png = "Assets/_LocalReference/Spine/body_1110901/body_1110901.png";
var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(png);
imp.alphaIsTransparency = true;
imp.mipmapEnabled = true;
imp.SaveAndReimport();
var broken = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(png);
sb.AppendLine("--- broken on purpose: alphaIsTransparency=" + broken.alphaIsTransparency + " mipmap=" + broken.mipmapEnabled + " ---");

sb.AppendLine(describe(checkFn.Invoke(null, new object[] { atlas, false })));
sb.AppendLine(describe(checkFn.Invoke(null, new object[] { atlas, true })));

var after = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(png);
sb.AppendLine("importer now: alphaIsTransparency=" + after.alphaIsTransparency + " mipmap=" + after.mipmapEnabled +
  " compression=" + after.textureCompression + " alphaSource=" + after.alphaSource + " spriteMode=" + after.spriteImportMode +
  " maxSize=" + after.maxTextureSize);
return sb.ToString();
