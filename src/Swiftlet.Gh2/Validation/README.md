# Header spike validation

Build the project with .NET SDK 8.0.428 or compatible SDK, then start GH2 in a dedicated Rhino 9 process. Load the build using PluginServer.ScopePlugin(path), then PluginServer.LoadPlugin(path, out failure). Restart the process before loading a rebuilt assembly; do not assume hot reload.

Place Create Http Header (IoId f98348ab-4ea6-46fc-b768-7be8cac59404), then execute SetHeaderInputs.csx through Rhino AI run_csharp. Run g2_solve_canvas and require Completed, zero errors and zero warnings. Execute ValidateHeader.csx; require all four PASS lines and no script error. Use an isolated document containing the one test component.

The scripts use reflection/dynamic to access the already-loaded assemblies, so they require no script-specific reference paths. They write a temporary binary document archive using GrasshopperIO and read it back with the generic Document reader. This validates archive persistence, not the UI File > Open workflow. Component/parameter archive clones exercise their IReader constructors, but are not claims of UI clipboard/undo coverage.

Observed 2026-11-16: build against Grasshopper2 9.0.26300.9401 succeeded with zero warnings/errors; load and the above tests passed in Rhino 9.1.26314.09001, CLR 10.0.2. Generic ReadStorableFromFile<Document> is required for the document; its untyped counterpart cannot infer Document's type from an IoId.

Remaining gate work: icons, stateful components, GH1 migration, actual clipboard/undo, cold restart and unified Yak install/upgrade tests. This is a scaffold, not a release-ready feature set.
