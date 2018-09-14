### What is this
A tool to help the conversion from UnityScript -> C#

You can read more about in [this blog post](https://blogs.unity3d.com/pt/2017/08/11/unityscripts-long-ride-off-into-the-sunset/).

### Where can I get help?

If you hit any issues or have any questions feel free to send a message in the conversion forum thread: https://forum.unity.com/threads/unityscript-2-csharp-conversion-tool.487753/

### How to use

First, download the tool from [here](https://github.com/Unity-Technologies/unityscript2csharp/releases).

Before running the conversion tool:

1. Backup your project

1. Keep in mind that you'll have best results (i.e, a smoother conversion process) if your UnityScripts have  *#pragma strict* applied to them.

1. Launch Unity editor (**2017.3 ~ 2018.1 versions are supported; 2018.1 is recommended**) and make sure you allow APIUpdater to run and update any obsolete API usages. This is necessary to avoid compilation errors during the conversion.

Next step is to run the converter. For that we recomend trying the _Editor Unity Package Integration_ first:

1. Install the package (downloaded from [here](https://github.com/Unity-Technologies/unityscript2csharp/releases))
1. Select _Tools/Convert Project from UnityScript to C#_
1. Follow the steps.

If you need more control over the options used during the conversion, you can use the application (UnityScript2CSharp.exe) passing the path to the project (**-p**) the Unity root installation folder (**-u**) and any additional assembly references (**-r**) used by the UnityScript scripts. Bellow you can find a list of valid command line arguments and their meaning:

| Argument | Meaning |
|----------------|-----------------------------------|
| -u, --unityPath | Required. Unity installation path. |
| -p, --projectPath | Required. Path of project to be converted. |
| -r, --references  | Assembly references required by the scripts (space separated list).|
| -g, --gameassemblies | References previously built game assemblies (Assembly-*-firstpass.dll under Library/).|
| -s, –symbols |	A (comma separated) list of custom symbols to be defined.|
| -o, –deleteOriginals | Deletes original files (default is to rename to .js.old)|
| -d | Dumps out the list of scripts being processed|
|-i	| Ignore compilation errors. This allows the conversion process to continue instead of aborting. |
| --skipcomments | (Default: False) Do not try to preserve comments (Use this option if processing comments cause any issues).
| --showorphancomments  | Show a list of comments that were not written to the converted sources (used to help identifying issues with the comment processing code).
|-v, –verbose |	Show verbose messages |
|-n, –dry-run |	Run the conversion but do not change/create any files on disk. |
|–help	| Display this help screen. |
     
**Example:**

UnityScript2CSharp.exe **-p** m:\Work\Repo\4-0_AngryBots **-u** M:\Work\Repo\unity\build **-r** "m:\AngryBot Assemblies\Assembly-CSharp.dll" "m:\AngryBot Assemblies\Assembly-UnityScript.dll" **-s** UNITY_ANDROID,UNITY_EDITOR

### Limitations

* Some comments may not be preserved or be misplaced.

* *Guarded code* (#if … )

    * UnityScript parser simply ignores guarded code when the condition evaluates to false leaving no traces of the original code when we are visiting the generated AST. The alternative for now is to run the tool multiple times in order to make sure all guarded code will eventually be processed. Each time the tool is executed user is required to merge the generated code manually.

* Formatting is not preserved

* UnityScript.Lang.Array (a.k.a *Array*) methods are not fully supported. We convert such type to  *object[]* (this means that if your scripts relies on such methods you'll need to replace the variable declarations / instantiation with some other type (like *List<T>*, *Stack<T>*, etc) and fix the code.

* Type inference in *anonymous function declarations* are inaccurate in some scenarios and may infer the wrong parameter / return type.

* Local variable scope sometimes  gets messed up due to the way UnityScript scope works.

* Types with hyphens in the name (like *This-Is-Invalid*) are converted as *as-it-is* but they are not valid in C# 

* Missing return values are not inject automatically (i.e, on a *non void* method, a *return;* statement will cause a compilation error in the converted code)

* Automatic conversion from **object** to *int/long/float/bool* etc is not supported (limited support for *int* -> *bool* conversion in conditional expressions is in place though).

* *for( init; condition; increment)* is converted to ***while***

* Methods **with same name as the declaring class** (invalid in C#) are converted *as-it-is*

* Invalid operands to **as** operators (which always yield **null** in US) are considered errors by the C# compiler.

* Equality comparison against *null* used as **statement expressions** generate errors in C# (eg: *foo == null;*) (this code in meaningless, but harmless in US)

* Code that changes *foreach* loop variable (which is not allowed in C#) are converted *as-is*, which means the resulting code will not compile cleanly.

* Not supported features
    * Property / Event definition
    * Macros
    * Literals
        * Regular expressions

    * Exception handling

Note that any unsupported language construct (a.k.a, AST node type), will inject a comment in the converted source including the piece of code that is not supported and related source/line information.

### How to build

In case you want to build the tool locally, *"all"* you need to do is:

1. Clone the repository
2. In a console, change directory to the cloned repo folder
3. Restore **nuget** packages  (you can download nuget [here](https://dist.nuget.org/index.html))
-- run **nuget.exe restore** 
4. Build using **msbuild**
-- msbuild UnityScript2CSharp.sln /target:clean,build


### How to run tests

All tests (in UnityScript2CSharp.Tests.csproj project) can be run with NUnit runner (recommended to use latest version).

### Windows
If you have Unity installed most likely you don't need any extra step; in case the tests fail to find Unity installation you can follow steps similar to the ones required for OSX/Linux

### OSX / Linux
The easiest way to get the tests running is by setting the environment variable **UNITY_INSTALL_FOLDER** to point to the Unity installation folder and launch **Unit** rest runner.


### FAQ

#### **Q**: During conversion, the following error is shown in the console: 
"*Conversion aborted due to compilation errors:*"

 And then some compiler errors complaining about types not being valid.

#### **A**: You are missing some assembly reference; if the type in question is define in the project scripts it is most likely Assembly-CSharp.dll or Assembly-UnityScript.dll (just run the conversion tool again passing **-r** *path_to_assembly_csharp path_to_assembly_unityscript* as an argument.

----
#### **Q**: Some of my UnityScript code is not included in the converted CSharp

#### **A**: Most likely this is code *guarded* by *SYMBOLS*. Look for **#if ** / **#else** directives in the original UnityScript  and run the conversion tool passing the right symbols. Note that in some cases one or more symbols may introduce mutually exclusive source snippets which means that no matter if you specify the symbol or not, necessarily some region of the code will be excluded, as in the example:

    #if !SYMBOL_1
    // Snippet 1
    #endif
    
    #if SYMBOL_1
    // Snippet 2
    #endif
In the example above,  if you run the conversion tool specifying the symbol *SYMBOL_1* , **Snippet 1** will be skipped (because it is guarded by a **!SYMBOL_1**) and **Snippet 2** will be included. If you don't, **Snippet 1** will be included but **Snippet 2** will not (because *SYMBOL_1* is not defined). 

The best way to workaround this limitation is to set-up a local VCS repository (git, mercurial or any other of your option) and run the conversion tool with a set of *symbols* then commit the generated code, revert the changes to the UnityScript scripts (i.e, restore the original scripts), run the conversion tool again with a different set of *Symbols* and merge the new version of the converted scripts.

----