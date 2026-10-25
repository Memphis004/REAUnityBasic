var t = typeof(MyClone.EditorTools.LocalReferenceDemoBuilder);
var m = t.GetMethod("RunPlayChecks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
if (m == null) return "RunPlayChecks not found";
bool ok = (bool)m.Invoke(null, null);
return "play checks ok=" + ok;
