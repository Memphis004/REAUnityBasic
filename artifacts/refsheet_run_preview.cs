var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/_LocalReference/Spine/body_1110901/body_1110901_SkeletonData.asset");
if (asset == null) return "asset missing";
var data = asset.GetSkeletonData(true);
string anim = null;
for (int i = 0; i < data.Animations.Count; i++) {
	var n = data.Animations.Items[i].Name;
	if (!n.StartsWith("_")) { anim = n; break; }
}
if (anim == null) return "no animation";
var options = new MyClone.EditorTools.ReferenceSheetOptions();
options.data = asset;
options.outputId = "_tmp_preview";

options.animation = anim;
options.animationTime = 0.4f;
options.width = 512;
options.height = 768;
var report = MyClone.EditorTools.ReferenceSheetExporter.Export(options);
return "anim=" + anim + "\n" + report;
