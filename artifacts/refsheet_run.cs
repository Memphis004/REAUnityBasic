var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/_LocalReference/Spine/body_1110901/body_1110901_SkeletonData.asset");
if (asset == null) return "asset missing";
var options = new MyClone.EditorTools.ReferenceSheetOptions();
options.data = asset;
options.outputId = "body_1110901";
return MyClone.EditorTools.ReferenceSheetExporter.Export(options);
