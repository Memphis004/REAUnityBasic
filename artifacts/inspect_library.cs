var c = UnityEngine.Object.FindObjectOfType<MyClone.LocalReference.SpineDemoController>();
var sb = new System.Text.StringBuilder();
sb.AppendLine("playing=" + UnityEngine.Application.isPlaying + " idx=" + c.LibraryIndex + "/" + c.LibraryCount);
var all = UnityEngine.Object.FindObjectsOfType<MyClone.SpineSystem.SpineUiActor>();
foreach (var a in all) {
  var g = a.Graphic;
  int vc = g == null ? 0 : g.MeshGenerator.Buffers.vertexCount;
  sb.AppendLine(a.name + " built=" + a.IsBuilt + " alpha=" + (g == null ? -1 : g.color.a) + " verts=" + vc +
    " scale=" + a.transform.localScale.x.ToString("0.###") +
    " canvasRenderers=" + a.GetComponentsInChildren<UnityEngine.CanvasRenderer>(true).Length);
}
return sb.ToString();
