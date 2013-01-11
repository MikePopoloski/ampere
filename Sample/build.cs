// --- Example build script --- //
#r "SamplePlugin.dll"
using SamplePlugin;

// set up the build environment
Env.InputPath = "./Input";
Env.OutputPath = "./Output";
Env.InputResolver = Resolvers.Flatten(CreateDirectoryCache(Env.InputPath));
Env.CreateOutputDirectory = true;

// create processors
var testProcessor = new TestProcessor();

// build rules
Build("*.asset")
    .Using(testProcessor.Run)
    .From("$1.xml");

// start root build
Start("root.asset");

// when the build completes, notify of new items and run again
Notify("127.0.0.1:9001");
RunAgain();