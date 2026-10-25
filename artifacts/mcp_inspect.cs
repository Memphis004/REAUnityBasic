var d = AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>("Assets/_LocalReference/Spine/body_1110901/body_1110901_SkeletonData.asset");
if (d == null) return "data asset NULL";
var sd = d.GetSkeletonData(true);
var anims = new System.Collections.Generic.List<string>();
foreach (var a in sd.Animations) anims.Add(a.Name);
var skins = new System.Collections.Generic.List<string>();
foreach (var s in sd.Skins) skins.Add(s.Name);
var ti = (TextureImporter)AssetImporter.GetAtPath("Assets/_LocalReference/Spine/body_1110901/body_1110901.png");
var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/_LocalReference/Spine/body_1110901/body_1110901_Material.mat");
return string.Format("ver={0} bones={1} slots={2} skins={3} anims={4}\nskins=[{5}]\nanims=[{6}]\ntexture: type={7} alphaIsTransparency={8} mips={9} wrap={10} filter={11} compression={12} sRGB={13}\nmaterial: shader={14} tex={15} straight={16} keyword={17} scale={18}",
 sd.Version, sd.Bones.Count, sd.Slots.Count, sd.Skins.Count, sd.Animations.Count,
 string.Join(",", skins.ToArray()), string.Join(",", anims.ToArray()),
 ti.textureType, ti.alphaIsTransparency, ti.mipmapEnabled, ti.wrapMode, ti.filterMode, ti.textureCompression, ti.sRGBTexture,
 m.shader.name, m.mainTexture == null ? "null" : m.mainTexture.name, m.GetFloat("_StraightAlphaInput"), m.IsKeywordEnabled("_STRAIGHT_ALPHA_INPUT"), d.scale);
