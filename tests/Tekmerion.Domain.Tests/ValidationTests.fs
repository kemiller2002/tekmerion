namespace Tekmerion.Domain.Tests

open Xunit
open Tekmerion.Domain
open Tekmerion.Domain.Tests.Fixtures

module ValidationTests =

    let everyCode =
        [ DuplicateId "x"
          DuplicateUrl "/a/x/"
          UnreadableSource "io"
          OutputPathEscape "../x"
          LostPublishedUrl "/research/x/"
          NonPublicContent "visibility: private"
          DanglingReference(IdReference "RP-X")
          BareFilenameReference "x.md"
          InvalidDeclaredId("a b", "whitespace")
          MalformedFrontMatter "yaml"
          ContradictoryMetadata [ "artifactType"; "document_type" ]
          DependencyCycle [ "a"; "b" ]
          SupersessionCycle [ "a"; "b" ]
          NewerFormatVersion "9"
          MissingId
          MissingDeclaredType
          NoFrontMatter
          UnknownKey "llm_ingest"
          OutOfScopeReference "content/concepts/x.md"
          Orphan ]

    [<Fact>]
    let ``TEK-VAL-002 exactly the enumerated conditions block`` () =
        let blocking =
            everyCode |> List.filter (fun code -> Policy.severity code = Blocking) |> List.map Policy.code

        Assert.Equal<string list>(
            [ "duplicate-id"
              "duplicate-url"
              "unreadable-source"
              "output-path-escape"
              "lost-published-url"
              "non-public-content" ],
            blocking
        )

    [<Fact>]
    let ``TEK-VAL-003 legitimate incomplete research never blocks`` () =
        for code in [ DanglingReference(IdReference "RP-X"); MissingId; MissingDeclaredType; Orphan; UnknownKey "k" ] do
            Assert.NotEqual(Blocking, Policy.severity code)

    [<Fact>]
    let ``TEK-VAL-001 finding codes are unique`` () =
        let codes = everyCode |> List.map Policy.code
        Assert.Equal(codes.Length, codes |> List.distinct |> List.length)

    [<Fact>]
    let ``TEK-STA-001 warnings alone permit publication`` () =
        let assessment = Assessment.evaluate [ finding MissingId; finding (DanglingReference(IdReference "RP-X")) ]
        Assert.True((Assessment.permitsPublication assessment).IsSome)
        Assert.True(Assessment.hasWarnings assessment)
        Assert.Equal(2, (Assessment.obligations assessment).Length)

    [<Fact>]
    let ``TEK-STA-001 one blocking finding withholds the publication evidence`` () =
        let assessment = Assessment.evaluate [ finding MissingId; finding (DuplicateId "EX-COMP-011") ]
        Assert.True((Assessment.permitsPublication assessment).IsNone)
        Assert.Equal("duplicate-id", Policy.code (Assessment.blocking assessment |> List.exactlyOne).Finding.Code)
