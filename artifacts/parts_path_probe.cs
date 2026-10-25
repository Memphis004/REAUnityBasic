var dll = System.Type.GetType("MyClone.LocalReferenceParts.SliceAtlasParts, Assembly-CSharp-Editor").Assembly.Location;
var d = System.IO.Path.GetDirectoryName("Assets/_LocalReference/Spine/body_1110901/body_1110901.atlas.txt");
return "GetDirectoryName=" + d + "\nreplace=" + d.Replace(System.IO.Path.DirectorySeparatorChar, '/') + "\nassembly=" + dll + " written=" + System.IO.File.GetLastWriteTime(dll).ToString("HH:mm:ss") + " now=" + System.DateTime.Now.ToString("HH:mm:ss");
