var c = UnityEngine.Object.FindObjectOfType<MyClone.LocalReference.SpineDemoController>();
c.NextLibrary();
var a = c.LibraryActor;
var prev = c.treeFarm;
string shot = c.SaveScreenshot();
return "idx=" + c.LibraryIndex + " active=" + a.name + " activeAlpha=" + a.Graphic.color.a +
  " treeAlpha=" + prev.Graphic.color.a + " shot=" + shot;
