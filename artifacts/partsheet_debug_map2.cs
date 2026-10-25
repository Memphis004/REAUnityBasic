var dir = @"C:/Workspace/REA/extracted/part_sheets/body_1110901";
var outDir = dir + "/import_selftest";
var layout = MyClone.EditorTools.PartSheetLayout.Load(dir);
var manifest = UnityEngine.JsonUtility.FromJson<MyClone.EditorTools.PartSheetManifest>(System.IO.File.ReadAllText(dir + "/manifest.json"));
var cell = layout.CellOfGroup("face_open");
int cw, ch;
var cellPixels = MyClone.EditorTools.PartSheetImage.LoadPng(outDir + "/groups/face_open.png", out cw, out ch);
cellPixels = MyClone.EditorTools.PartSheetImage.FlipVertical(cellPixels, cw, ch);
var line = "cellPixels " + cw + "x" + ch + " centre px " + cellPixels[(170*204)+102].a + " content alpha nonzero ";
int nz = 0; foreach (var p in cellPixels) if (p.a > 8) nz++;
line += nz + "\n";
// native
int nw, nh;
var native = MyClone.EditorTools.PartSheetImage.LoadPng(dir + "/native/face_open_alpha.png", out nw, out nh);
native = MyClone.EditorTools.PartSheetImage.FlipVertical(native, nw, nh);
int nnz = 0; foreach (var p in native) if (p.a > 8) nnz++;
line += "native " + nw + "x" + nh + " painted " + nnz + "\n";
// page
int pw, ph;
var page = MyClone.EditorTools.PartSheetImage.LoadPng("Assets/_LocalReference/Spine/body_1110901/body_1110901.png", out pw, out ph);
page = MyClone.EditorTools.PartSheetImage.FlipVertical(page, pw, ph);
line += "page " + pw + "x" + ph + " texel(893,1)=" + page[1*pw+893].r + "," + page[1*pw+893].g + "," + page[1*pw+893].b + "," + page[1*pw+893].a + "\n";
// mapping
var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>(manifest.sourceSkeletonDataAsset);
var data = asset.GetSkeletonData(true);
var sk = new Spine.Skeleton(data);
var composite = new Spine.Skin("dbg");
foreach (var nm in manifest.skins) { var s = data.FindSkin(nm); if (s != null) composite.AddSkin(s); }
sk.SetSkin(composite); sk.SetToSetupPose(); sk.UpdateWorldTransform();
var slot = sk.FindSlot("face");
var ra = slot.Attachment as Spine.RegionAttachment;
var uvs = ra.UVs; var verts = new float[8];
ra.ComputeWorldVertices(slot.Bone, verts, 0, 2);
// fit from 4 points
double sxx=0,sxy=0,sx=0,syy=0,sy=0,bx1=0,bx2=0,bx3=0,by1=0,by2=0,by3=0;
for (int i=0;i<4;i++){ double u=uvs[i*2],v=uvs[i*2+1],x=verts[i*2],y=verts[i*2+1];
 sxx+=u*u; sxy+=u*v; sx+=u; syy+=v*v; sy+=v; bx1+=u*x; bx2+=v*x; bx3+=x; by1+=u*y; by2+=v*y; by3+=y; }
double det = sxx*(syy*4-sy*sy)-sxy*(sxy*4-sy*sx)+sx*(sxy*sy-syy*sx);
double A=(bx1*(syy*4-sy*sy)-sxy*(bx2*4-sy*bx3)+sx*(bx2*sy-syy*bx3))/det;
double B=(sxx*(bx2*4-bx3*sy)-bx1*(sxy*4-sy*sx)+sx*(sxy*bx3-bx2*sx))/det;
double C=(sxx*(syy*bx3-bx2*sy)-sxy*(sxy*bx3-bx2*sx)+bx1*(sxy*sy-syy*sx))/det;
double D=(by1*(syy*4-sy*sy)-sxy*(by2*4-sy*by3)+sx*(by2*sy-syy*by3))/det;
double E=(sxx*(by2*4-by3*sy)-by1*(sxy*4-sy*sx)+sx*(sxy*by3-by2*sx))/det;
double F=(sxx*(syy*by3-by2*sy)-sxy*(sxy*by3-by2*sx)+by1*(sxy*sy-syy*sx))/det;
line += "affine " + A.ToString("F4")+","+B.ToString("F4")+","+C.ToString("F4")+" / "+D.ToString("F4")+","+E.ToString("F4")+","+F.ToString("F4")+"\n";
for (int k=0;k<3;k++){
 int px=893+k*7, py=1+k*10;
 double u=(px+0.5)/1152.0, v=1-(py+0.5)/80.0;
 double wx=A*u+B*v+C, wy=D*u+E*v+F;
 float shx, shy; layout.TrySkeletonToSheet(cell, (float)wx, (float)wy, out shx, out shy);
 var sample = MyClone.EditorTools.PartSheetImage.SampleBilinear(cellPixels, cw, ch, shx+0.5f, shy+0.5f);
 float nx = layout.native.originXPixels + (float)wx*100f;
 float ny = nh - (layout.native.originYPixels + (float)wy*100f);
 var ns = MyClone.EditorTools.PartSheetImage.SampleBilinear(native, nw, nh, nx, ny);
 line += "px(" + px + "," + py + ") uv " + u.ToString("F4") + "," + v.ToString("F4") + " world " + wx.ToString("F4") + "," + wy.ToString("F4") + " sheet " + shx.ToString("F1") + "," + shy.ToString("F1") + " cellAlpha " + sample.a + " nativeAlpha " + ns.a + " pageAlpha " + page[py*pw+px].a + " nativeXY " + nx.ToString("F1") + "," + ny.ToString("F1") + "\n";
}
return line;
