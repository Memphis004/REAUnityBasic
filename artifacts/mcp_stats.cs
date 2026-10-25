string outPath = System.IO.Path.Combine(MyClone.EditorTools.SpineLibraryTool.LibraryRoot, "_runtime_stats.json");
int ok = MyClone.EditorTools.SpineLibraryTool.DumpRuntimeStats(outPath);
return "stats ok=" + ok + " -> " + outPath;
