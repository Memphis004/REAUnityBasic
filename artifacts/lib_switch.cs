var c = UnityEngine.Object.FindObjectOfType<MyClone.LocalReference.SpineDemoController>();
c.NextLibrary();
return "idx=" + c.LibraryIndex + " active=" + c.LibraryActor.name + " treeAlpha=" + c.treeFarm.Graphic.color.a;
