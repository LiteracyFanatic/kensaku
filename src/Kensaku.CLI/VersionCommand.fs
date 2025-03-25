namespace Kensaku.CLI

module VersionCommand =
    open System.Linq
    open System.Reflection

    let versionHandler () =
        let version =
            Assembly
                .GetEntryAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(fun a -> a.Key = "GitTag")
                .Value

        printfn "%s" version
