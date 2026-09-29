namespace Tekmerion.Core.Tests

open System
open System.IO
open Aegis
open Tekmerion.Domain
open Tekmerion.Core
open Tekmerion.Cli

module Support =

    let rec private findRoot (directory: DirectoryInfo) =
        if File.Exists(Path.Combine(directory.FullName, "ros.json")) then directory.FullName
        else findRoot directory.Parent

    let repositoryRoot = findRoot (DirectoryInfo AppContext.BaseDirectory)

    /// The pinned real-corpus snapshot (tests/fixtures/visual-engineering/SOURCE.json).
    let fixtureRoot = Path.Combine(repositoryRoot, "tests", "fixtures", "visual-engineering")
    let fixtureConfig = Path.Combine(fixtureRoot, "tekmerion.config.json")

    let ok (result: Result<'T, string>) =
        match result with
        | Ok value -> value
        | Error reason -> failwith reason

    let repository = RepositoryId.create "kemiller2002/visual-engineering" |> ok
    let path raw = RepoPath.create raw |> ok

    let read (file: string) (text: string) = Reading.read repository (path file) text

    let quietAegis () =
        let collector = Sinks.Collector()
        { Aegis.configure "Tekmerion.Tests" None [ collector.Sink() ] with Persistence = PersistenceMode.Blocking }, collector

    let options out =
        { ConfigPath = fixtureConfig
          Root = None
          Out = out
          Json = true }

    /// The golden corpus, ingested once through the real host path.
    let golden =
        lazy
            (let aegis, _ = quietAegis ()

             match Commands.load aegis (options None) with
             | Ok corpus -> corpus
             | Error result -> failwithf "fixture failed to load: %A" result.Messages)

    let artifactById (corpus: Corpus) (id: string) =
        corpus.Artifacts
        |> List.map (fun a -> a.Reading.Artifact)
        |> List.find (fun a -> ArtifactKey.value a.Key = id)

    let tempDirectory () =
        let directory = Path.Combine(Path.GetTempPath(), "tekmerion-tests", Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory directory |> ignore
        directory
