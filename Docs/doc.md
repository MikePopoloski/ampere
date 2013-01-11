Ampere - Content Build Tool
------------------

Ampere is a tool designed to facilitate building game content and assets from design-time source files into efficient runtime formats. Outside of large toolsets like XNA or Unity, there exists a dearth of freely available content pipeline tools, and existing build platforms are almost exclusively targeted at source code, which has different uses and requirements.

Ampere is designed with the following ideas in mind:

1. Allow the user to write a build script to specify build rules.
2. Look up asset names on disk and run them through C# plugins, as specified by build rules.
3. Run all builds in parallel to take full advantage of machine resources.
4. Have support for notifying a running process to facilitate hot-swapping of assets.
5. Track all assets to avoid rebuilding them until their source actually changes.

### General Usage
All that is required to use Ampere is the amp.exe executable. (Note: until the full version of MS Roslyn comes out, that DLL is also required alongside the executable.)

The tool has the following command line options:
```Usage: amp [build-script]```

`build-script`: an optional path to the C# build script.

If an explicit build script is not provided, the tool will search using the following steps:
1. \<current directory name\>.cs
2. build.cs in the current directory
3. Otherwise, error.

### Build Script
The build script is written in relaxed C# syntax. A simple example is shown below:
```C#
// --- Test build script --- //
#r "Plugins.dll"
using Plugins;

// set up the build environment
Env.InputPath = "./";
Env.OutputPath = "../bin/Data/";
Env.CreateOutputDirectory = true;

// create processor
var materialParser = new MaterialParser();

// parse materials into binary blob
Build("*.material")
    .Using(materialParser.Run)
    .From("$1.xml");

// start builds
Start("test.material");
```

This script demonstrates several key concepts of Ampere. First, plugins are loaded using the `#r` preprocessor command. Plugin DLLs are assumed to be in the same directory as the amp executable. Namespaces are not automatically imported, so you'll need ```using``` statements if you don't want to fully qualify the type names.

The second section shows setting up the global build environment, which is accessed through the global object ```Env```. The ```InputPath``` property denotes the path to the root input directory, while the ```OutputPath``` denotes the root output directory. The ```CreateOutputDirectory``` option will cause the tool to automatically create the output directory if it does not exist.

Next we create a content processor object. This ```MaterialParser``` type was imported from the `Plugins` assembly. We then set up a *build rule* that indicates how a certain type of content should be built. In this case, we indicate that all assets with names ending in ".material" should be built using our ```MaterialParser```'s ```Run()``` function, using an input XML file matching the asset name.

Build rules do not cause anything to be built. Rather, they indicate what to do when an actual asset is requested to be built. The ```Start()``` function kicks off an actual build of an asset. In this case, we indicate that we want to build the "test.material" asset, which matches the build rule we set up earlier.

Listing all of your assets in the build script as ```Start()``` commands is obviously undesirable. Ideally only a handful of "root" assets will need to be listed in this way, with the content processors for each content type finding dependencies and kicking off other builds automatically. This will be demonstrated later on.

The ```Start()``` function launches an asynchronous build. When the end of the build script is reached, the tool will wait for all outstanding builds before displaying results and exiting.

##### C# Code
The build script can have any arbitrary C# calls, including variable declarations, expressions, function calls, and even inline function declarations. For example, adding the following code anywhere in the script above will compile and execute as expected:
```C#
void TestFunc()
{
	Console.WriteLine("Hello, World!");
}

TestFunc();
```

### Operators
Each method in a build rule chain is referred to as an *operator*. Any number of them can be chained together to form a single build rule. In the example above, three operators are used for the material build rule: `Build()`, `Using()`, and `From()`.

The `Build()` operator is always required to define a build rule chain, and indicates the matching expression for the content types to which it applies. By default the matching expression uses simplified wildcard matching (* and ? only), but putting a '/' symbol at the start and end of the expression allows full regex syntax.

When a build rule is matched against an asset build request, the pipline is started from the bottom of the chain and works back up to the top, where the asset is outputted to disk with the requested name in the currently set environment `OutputPath`.

A build rule chain must always begin at the bottom with a `From()` operator, which pulls files from disk into the pipeline stream. Any number of inputs can be specified here, with full use of regex replacement patterns for any captures made in the `Build()` expression.

### Content Processors
Ampere is designed first and foremost to work with C# types as content plugins. `The Using()` operator used in the build rules above takes methods matching one of the following signatures:
```C#
object Foo(BuildInstance instance, IEnumerable<object> inputs);
IEnumerable<object> Foo(BuildInstance instance, IEnumerable<object> inputs);
```

Any function or lambda expression matching one of these signatures can be used within the build process. These can either be declared right in the build script, or loaded from an external .NET assembly. If built in an external assembly, simply add the amp.exe executable as a reference to get access to the `BuildInstance` type.

The `inputs` parameter is a sequence of all the inputs from the previous pipeline stage. In the example above, the processor will receive one input, which will be a `FileStream` loaded using the `From()` operator. The processor is then free to manipulate the inputs in whatever way desired, returning any number of outputs that will be passed on to the next stage. This is done until the top of the pipeline is reached, at which point the `Build()` operator will expect all of its inputs to be of types derived from `Stream`.

### BuildInstance
The `BuildInstance` type passed to each processor encapsulates information about the currently executing build. The processor may make use of it in any way it needs. Some of the more useful methods are:

```void Log(LogLevel level, string message, params object[] args);``` - Logs information to the screen, such as errors or general build information.

```Env``` - Property that gives access to the captured environment at the time the build was started. This is necessarily a copy since the global environment can be changed after the build has started asynchronously.

```Task Start(string name);``` - Kicks off a new build using the given asset name. You can use the returned `Task` if you want to wait for that build to finish.

```BuildInstance StartTemp(string name);``` - Kicks off a *temporary* build. A temporary build is one that does not output to disk, but instead executes synchronously with respect to the current build and returns the results via the `Result` property on the returned `BuildInstance`. This is useful for embedding another asset within your own output, such as embedding a compiled shader binary inside your material asset.

### Change Detection
By default, the tool will close once it has finished all of its builds. However, if you call the `RunAgain()` method in your build script, the tool will stay open and listen for changes to content files, which will start another build pass. You can use this mode to make fast iterations to your game.

The results of each build, whether you call `RunAgain()` or not, are saved to your AppData user folder as JSON under a hashed name for your build script. You can set how change detection should be done, to control how strict the tool is with changes causing rebuilds.

The `Env` class has two properties called `InputChangeDetection` and `OutputChangeDetection` that dictate how this will occur. The possible values for these properties are any flags combination of the following enum:
```C#
[Flags]
public enum ChangeDetection
{
    None = 0,
    Length = 0x2,
    Timestamp = 0x4,
    Hash = 0x8
}
```

If the `Length` flag is set, the byte length of the file names are compared. Any differences cause a rebuild of that particular asset. `Timestamp` compares the last write time of the input files, while the `Hash` flag saves a hash of the file contents, allowing you to avoid a rebuild unless the file's contents actually change.

The defaults for these two properties are set as follows:
```C#
OutputChangeDetection = ChangeDetection.Length;
InputChangeDetection = ChangeDetection.Length | ChangeDetection.Timestamp | ChangeDetection.Hash;
```

### Name Resolution
In order to find input and output files on disk, Ampere uses *name resolvers* to convert an input name into a path. These are set on the `Env` class via the `InputResolver` and `OutputResolver` properties, which are of type ```Func<string, string>```. The input to the resolver you set is the name, while the output should be the converted path based on the name. The output from the resolver is then appended to the `InputPath` or `OutputPath` property on `Env` to form the full path where the asset will be loaded / saved.

The default resolver will simply pass through its input, meaning that the asset "Materials/test.xml" will result in a search on disk for:
```C#
OutputPath + "/Materials/test.xml"
```

There is another built-in resolver, under the `Resolvers.Flatten()` method. This resolver will flatten the entire directory tree and match requests based solely on the name. This means that any files with duplicate names in the hierarchy will cause errors. This resolver requires the use of a helper type to cache the directory structure. An example of using it is as follows:
```C#
Env.InputResolver = Resolvers.Flatten(CreateDirectoryCache(Env.InputPath));
```

This will result in a request for asset "test.xml" resolving to "Materials/test.xml", even without the directory name provided.

You can write your own resolver to do whatever kind of lookups you want. For example, you might write one that handles "namespace" syntax in your asset names; for example, "Level1.Fonts.Textures.Font1.font" as an input name might get resolved using your method to "Level1/Fonts/Textures/Font1.font".

### External Programs
It's not always feasible to use a C# processor to build your content; sometimes you need to run an external 3rd party program. This is supported through the use of the `Run()` operator, which has the following signatures:
```C#
Run(string fileName, string arguments, params string[] outputs);
Run(string fileName, string arguments, RunOptions options, params string[] outputs);
```

`fileName` - The path to the executable to run. May include environment variables.
`arguments` - A string of arguments to pass to the executable. Includes syntax for pulling in inputs from the previous stage of the pipeline. More on that below.
`options` - Various flags that control how the external tool is run.
`outputs` - A list of strings, each of which indicates an output file from the tool. The next stage of the pipeline will use these as its inputs.

The arguments and outputs strings can use any of the following patterns in them to get them replaced at runtime with the corresponding bit of information:

Pattern      | Replacement
------------ | -------------------------------------------------
$(Output)    | Path where the output asset file will be created.
$(Input[n])  | The input at the corresponding index in the pipeline. Input must be a type derived from `Stream`.
$(Name)		 | The name of the asset being built.
$(TempDir)	 | The currently set temporary directory in the `Env`.
$(TempName)	 | Appends the resolved asset name to the temporary directory path.

Here are the available option flags:
```C#
[Flags]
public enum RunOptions
{
    None = 0,
    RedirectOutput = 1,
    RedirectError = 2,
    DontCheckResultCode = 4
}
```

`RedirectOutput` and `RedirectError` redirect the standard output and error streams, respectively. `DontCheckResultCode` indicates that the return code of the tool won't be checked against 0 for an error condition. By default, `RunOptions.RedirectError` is set if you specify nothing.

### Build Notifications
Ampere can be used to notify a running program that assets have been built. This can be used to hot-reload assets on the fly for rapid iteration times. This is done over a TCP socket connection by calling the following function in your build script:
```C#
void Notify(string connectionInfo);
Notify("127.0.0.1:9001");	// example
```

Where `connectionInfo` is a DNS name or IP address with port. The listening program need only to listen for connections and receive the data, which is sent as a list of UTF8 encoded asset names, separated by the newline character '\n'.