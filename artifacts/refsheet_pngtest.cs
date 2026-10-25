int w = 4, h = 1;
var premul = new Color32[] {
	new Color32(240, 0, 240, 240),
	new Color32(0, 0, 0, 0),
	new Color32(10, 10, 10, 10),
	new Color32(19, 18, 18, 78)
};
var bg = new Color32(255, 0, 255, 255);
string dir = System.IO.Path.Combine(MyClone.EditorTools.ReferenceSheetExporter.OutputRoot, "_pngtest");
System.IO.Directory.CreateDirectory(dir);
MyClone.EditorTools.ReferenceSheetImage.SavePng(System.IO.Path.Combine(dir, "a.png"), MyClone.EditorTools.ReferenceSheetImage.ToStraight(premul), w, h);
MyClone.EditorTools.ReferenceSheetImage.SavePng(System.IO.Path.Combine(dir, "m.png"), MyClone.EditorTools.ReferenceSheetImage.OverBackground(premul, bg), w, h);
int aw, ah, mw, mh;
var a = MyClone.EditorTools.ReferenceSheetImage.LoadPng(System.IO.Path.Combine(dir, "a.png"), out aw, out ah);
var m = MyClone.EditorTools.ReferenceSheetImage.LoadPng(System.IO.Path.Combine(dir, "m.png"), out mw, out mh);
var sb = new System.Text.StringBuilder();
for (int i = 0; i < 4; i++) sb.AppendLine("in " + premul[i].r + "," + premul[i].g + "," + premul[i].b + "," + premul[i].a + "  straight " + a[i].r + "," + a[i].g + "," + a[i].b + "," + a[i].a + "  magenta " + m[i].r + "," + m[i].g + "," + m[i].b + "," + m[i].a);
return sb.ToString();
