namespace ResearchPublisher.Lifecycle.Core.Tests

open Xunit
open ResearchPublisher.Lifecycle.Core

/// Research Publisher -> Tekmerion identity migration (GH-15, TEK-MIG-001..004).
module IdentityMigrationTests =

    let private version = "1.0.0"

    /// A configuration-version-2 installation exactly as a pre-Tekmerion
    /// release left it: the manifest at .echelon/research-publisher.json under
    /// the tool name research-publisher.
    let private legacyInstallation (repository: TestRepository) =
        TestPackage.useCheckout ()
        repository.WriteMinimalPackageJson()
        Api.initialize version repository.Root |> ignore

        let legacy =
            repository
                .Read(Identity.ManifestPath)
                .Replace(sprintf "\"tool\": \"%s\"" Identity.ToolName, sprintf "\"tool\": \"%s\"" Identity.LegacyToolName)
                .Replace(sprintf "\"configurationVersion\": %d" Identity.CurrentConfigurationVersion, "\"configurationVersion\": 2")
                .Replace(Identity.ManifestPath, Identity.LegacyManifestPath)

        repository.Delete Identity.ManifestPath
        repository.Write(Identity.LegacyManifestPath, legacy)

    let private inspect (repository: TestRepository) = Api.inspectRepository repository.Root

    let private plan (repository: TestRepository) =
        TestPackage.useCheckout ()
        repository |> inspect |> Api.planUpgrade version

    /// Snapshot as path -> content hash.
    let private files (repository: TestRepository) =
        repository.Snapshot().Split('\n')
        |> Array.filter (fun line -> line <> "")
        |> Array.map (fun line -> let i = line.LastIndexOf '=' in line.Substring(0, i), line.Substring(i + 1))
        |> Map.ofArray

    let private kinds (plan: Plan) = plan.Steps |> List.map (fun step -> PlannedChange.kind step.Change)

    [<Fact>]
    let ``TEK-MIG-001 a Research Publisher installation is detected and needs the 2->3 migration`` () =
        use repository = new TestRepository()
        legacyInstallation repository
        let inspection = inspect repository
        Assert.True inspection.ManifestIsLegacy
        Assert.Equal(Some Identity.LegacyToolName, inspection.Manifest |> Option.map (fun m -> m.Tool))

        match Api.getInstallationState version inspection with
        | UpgradeRequired(current, _) -> Assert.Equal(2, current.ConfigurationVersion)
        | other -> failwithf "expected UpgradeRequired, got %A" other

        let upgrade = plan repository
        Assert.Equal<MigrationId list>([ { FromVersion = 2; ToVersion = 3 } ], upgrade.Migrations)
        Assert.Equal<string list>([ "run-migration"; "write-manifest"; "retire-legacy-manifest" ], kinds upgrade)

    [<Fact>]
    let ``TEK-MIG-002 planning is a dry run: nothing on disk changes`` () =
        use repository = new TestRepository()
        legacyInstallation repository
        let before = files repository
        plan repository |> ignore
        Assert.Equal<Map<string, string>>(before, files repository)

    [<Fact>]
    let ``TEK-MIG-002 applying moves the record and changes no repository content`` () =
        use repository = new TestRepository()
        legacyInstallation repository
        let before = files repository |> Map.remove Identity.LegacyManifestPath
        let result = Api.apply (plan repository)
        Assert.True result.Succeeded

        Assert.False(repository.Exists Identity.LegacyManifestPath)
        let after = files repository |> Map.remove Identity.ManifestPath
        Assert.Equal<Map<string, string>>(before, after)

        match Manifest.parse (repository.Read Identity.ManifestPath) with
        | Ok manifest ->
            Assert.Equal(Identity.ToolName, manifest.Tool)
            Assert.Equal(Identity.CurrentConfigurationVersion, manifest.ConfigurationVersion)
            Assert.Contains(manifest.ManagedArtifacts, fun a -> a.Path = Identity.ManifestPath)
        | Result.Error problem -> failwithf "%A" problem

    [<Fact>]
    let ``TEK-MIG-002 the migration is idempotent`` () =
        use repository = new TestRepository()
        legacyInstallation repository
        Assert.True (Api.apply (plan repository)).Succeeded
        let again = plan repository
        Assert.Empty again.Steps

        match Api.getInstallationState version (inspect repository) with
        | Installed _ -> ()
        | other -> failwithf "expected Installed, got %A" other

    [<Fact>]
    let ``TEK-MIG-002 an interrupted migration resumes from the new record`` () =
        use repository = new TestRepository()
        legacyInstallation repository
        let legacy = repository.Read Identity.LegacyManifestPath
        Assert.True (Api.apply (plan repository)).Succeeded
        // State after WriteManifest succeeded but before the legacy record was removed.
        repository.Write(Identity.LegacyManifestPath, legacy)

        let inspection = inspect repository
        Assert.False inspection.ManifestIsLegacy
        Assert.True inspection.LegacyManifestPresent

        match Api.getInstallationState version inspection with
        | UpgradeRequired _ -> ()
        | other -> failwithf "expected UpgradeRequired, got %A" other

        let resume = plan repository
        Assert.Equal<string list>([ "retire-legacy-manifest" ], kinds resume)
        Assert.True (Api.apply resume).Succeeded
        Assert.False(repository.Exists Identity.LegacyManifestPath)

    [<Fact>]
    let ``TEK-MIG-002 locally modified shared and user-owned files survive the migration`` () =
        use repository = new TestRepository()
        legacyInstallation repository
        repository.Write(Desired.MarkingPromptPath, "LOCALLY EDITED PROMPT\n")
        repository.Write(Desired.ConfigPath, "export default { site: { title: 'Mine' } };\n")
        Assert.True (Api.apply (plan repository)).Succeeded
        Assert.Equal("LOCALLY EDITED PROMPT\n", repository.Read Desired.MarkingPromptPath)
        Assert.Equal("export default { site: { title: 'Mine' } };\n", repository.Read Desired.ConfigPath)

    [<Fact>]
    let ``a legacy-location manifest that belongs to another tool is refused, not adopted`` () =
        use repository = new TestRepository()
        legacyInstallation repository
        repository.Write(Identity.LegacyManifestPath, repository.Read(Identity.LegacyManifestPath).Replace(Identity.LegacyToolName, "sde"))

        match Api.getInstallationState version (inspect repository) with
        | Invalid problems -> Assert.Contains(problems, fun p -> p.Path = Some Identity.LegacyManifestPath)
        | other -> failwithf "expected Invalid, got %A" other
