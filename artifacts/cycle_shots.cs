var c = UnityEngine.Object.FindObjectOfType<MyClone.LocalReference.SpineDemoController>();
if (c.fullBody.IsGray) c.ToggleGray();
var sb = new System.Text.StringBuilder();
for (int i = 0; i < c.LibraryCount - 1; i++) {
  c.NextLibrary();
  var a = c.LibraryActor;
  string shot = c.SaveScreenshot();
  string name = a.name.Replace("TreeFarm_", "").Replace("Library_", "");
  string dest = "C:/Workspace/REA/artifacts/library_" + name + ".png";
  System.IO.File.Copy(shot, dest, true);
  sb.AppendLine(name + " alpha=" + a.Graphic.color.a + " verts=" + a.Graphic.MeshGenerator.Buffers.vertexCount + " -> " + dest);
}
c.NextLibrary(); // back to body_1110901
sb.AppendLine("back to idx=" + c.LibraryIndex + " " + c.LibraryActor.name);
return sb.ToString();
