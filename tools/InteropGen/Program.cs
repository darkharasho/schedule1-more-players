using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using Il2CppInterop.Generator;
using Il2CppInterop.Generator.Runners;

// Usage: InteropGen <cpp2il dummy dll dir> <GameAssembly.dll> <unity base libs dir> <output dir>
// Same options as MelonLoader's Il2CppAssemblyGenerator, so namespaces get the same Il2Cpp prefixes.
var source = Directory.GetFiles(args[0], "*.dll").Select(f => ModuleDefinition.FromFile(f).Assembly).ToList();
var options = new GeneratorOptions
{
    GameAssemblyPath = args[1],
    Source = source,
    UnityBaseLibsDir = args[2],
    OutputDir = args[3],
    Parallel = true,
    Il2CppPrefixMode = GeneratorOptions.PrefixMode.OptOut,
};
Il2CppInteropGenerator.Create(options).AddInteropAssemblyGenerator().Run();
