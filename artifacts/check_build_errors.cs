var t = typeof(MyClone.EditorTools.LocalReferenceDemoBuilder);
var f = t.GetField("s_LastVerifyErrors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
return "verifyErrors=" + (f == null ? "?" : f.GetValue(null));
