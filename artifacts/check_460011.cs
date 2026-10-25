var d = UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/_LocalReference/Spine/460011_1/460011_1_SkeletonData.asset");
var sd = d.GetSkeletonData(true);
var names = new System.Collections.Generic.List<string>();
foreach (var sk in sd.Skins) names.Add(sk.Name);
return "bones=" + sd.Bones.Count + " slots=" + sd.Slots.Count + " skins=" + sd.Skins.Count + " anims=" + sd.Animations.Count + " | " + string.Join(",", names.ToArray());
