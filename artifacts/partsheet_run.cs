var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/_LocalReference/Spine/body_1110901/body_1110901_SkeletonData.asset");
if (asset == null) return "asset missing";
var options = new MyClone.EditorTools.PartSheetOptions();
options.data = asset;
options.outputId = "body_1110901";
options.skins = new[] {
	new MyClone.EditorTools.ReferenceSheetSkinPart { part = "body", skin = "body_1110901_1" },
	new MyClone.EditorTools.ReferenceSheetSkinPart { part = "head", skin = "head_1110901_1" },
	new MyClone.EditorTools.ReferenceSheetSkinPart { part = "face", skin = "face_1110901_1" },
};
return MyClone.EditorTools.PartSheetExporter.Export(options);
