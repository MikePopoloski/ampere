// --- Test build script --- //

// set up the build environment
Env.InputPath = "./";
Env.OutputPath = "./Output";
Env.InputResolver = Resolvers.Flatten(CreateDirectoryCache(Env.InputPath));
Env.CreateOutputDirectory = true;

// should just copy input files
Build("*.foo").From("$1.txt");

// test inline functions
void SomeFun()
{
    Console.WriteLine("Blah");
}

// start builds
Start("test.foo");