namespace Tekmerion.Domain.Tests

open System
open Xunit
open Tekmerion.Domain

/// SDE tiers 1 and 2 must not know about hosts (FOUR-TIER-ARCHITECTURE,
/// TEK-ARC-005). These fail if the domain acquires an infrastructure dependency.
module ArchitectureTests =

    let referenced =
        typeof<Artifact>.Assembly.GetReferencedAssemblies() |> Array.map (fun reference -> reference.Name)

    [<Fact>]
    let ``TEK-ARC-003 the domain has no third party dependency`` () =
        let allowed = [ "System"; "FSharp.Core"; "netstandard"; "mscorlib" ]

        let foreign =
            referenced
            |> Array.filter (fun name -> not (allowed |> List.exists (fun prefix -> name.StartsWith(prefix, StringComparison.Ordinal))))

        Assert.Empty foreign

    [<Fact>]
    let ``TEK-ARC-005 the domain references no host assembly`` () =
        let hostAssemblies =
            [ "System.IO.FileSystem"; "System.Net"; "System.Text.Json"; "System.Console"; "System.Diagnostics.Process"; "Microsoft.JSInterop" ]

        let hosts =
            referenced
            |> Array.filter (fun name -> hostAssemblies |> List.exists (fun host -> name.StartsWith(host, StringComparison.Ordinal)))

        Assert.Empty hosts

    [<Fact>]
    let ``TEK-ARC-005 no domain type exposes a stream, file or clock`` () =
        let forbidden = [ typeof<IO.Stream>; typeof<IO.FileInfo>; typeof<IO.DirectoryInfo>; typeof<TimeProvider> ]

        let offenders =
            typeof<Artifact>.Assembly.GetTypes()
            |> Array.collect (fun t -> t.GetMethods() |> Array.map (fun m -> t, m))
            |> Array.filter (fun (_, m) ->
                forbidden |> List.exists (fun f -> m.ReturnType = f || m.GetParameters() |> Array.exists (fun p -> p.ParameterType = f)))
            |> Array.map (fun (t, m) -> $"{t.FullName}.{m.Name}")

        Assert.Empty offenders
