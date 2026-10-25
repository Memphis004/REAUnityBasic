var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/_LocalReference/Spine/body_1110901/body_1110901_SkeletonData.asset");
if (asset == null) return "asset missing";
var options = new MyClone.EditorTools.ReferenceSheetOptions();
options.data = asset;
options.outputId = "body_1110901";
options.skins = new MyClone.EditorTools.ReferenceSheetSkinPart[] {
  new MyClone.EditorTools.ReferenceSheetSkinPart { part = "body", skin = "body_1110901_1" },
  new MyClone.EditorTools.ReferenceSheetSkinPart { part = "face", skin = "face_1110901_1" },
  new MyClone.EditorTools.ReferenceSheetSkinPart { part = "head", skin = "head_1110901_1" },
};
var scenes = "";
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
  scenes += UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path + " | ";
var report = MyClone.EditorTools.ReferenceSheetExporter.Export(options);
var after = "";
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
  after += UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path + " | ";
return "scenes before: " + scenes + "\nscenes after: " + after + "\n" + (report ?? "EXPORT RETURNED NULL");
