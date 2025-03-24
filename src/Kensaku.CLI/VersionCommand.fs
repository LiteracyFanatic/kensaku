namespace Kensaku.CLI

module VersionCommand =
    open System.Reflection
    open System.Linq

    let versionHandler () =
        let version =
            Assembly
                .GetEntryAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(fun a -> a.Key = "GitTag")
                .Value

        printfn "%s" version
