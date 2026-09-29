namespace Tekmerion.Domain

open System

/// Where in canonical source a fact came from (TEK-PRV-001). Repository and
/// path are always present; the finer locators are present only when known.
type SourceLocation =
    { Repository: RepositoryId
      Path: RepoPath
      Line: int option
      FrontMatterKey: string option
      HeadingPath: string list
      /// Reserved from the first slice; `None` until repository history is
      /// attached (TEK-PRV-003). Never a placeholder value.
      Commit: string option }

[<RequireQualifiedAccess>]
module SourceLocation =

    let ofFile (repository: RepositoryId) (path: RepoPath) =
        { Repository = repository
          Path = path
          Line = None
          FrontMatterKey = None
          HeadingPath = []
          Commit = None }

    let atKey (key: string) (line: int option) (location: SourceLocation) =
        { location with FrontMatterKey = Some key; Line = line }

/// Identity of an agent runtime. Provider and runtime are required: an agent
/// that cannot say what it is cannot be recorded as anything else
/// (TEK-AGT-003). Model is knowable because some runtimes do not expose it.
type AgentIdentity =
    { Provider: string
      Runtime: string
      Model: Knowable<string>
      Session: Knowable<string> }

/// Who performed or asserted something (TEK-AGT-004, TEK-REV-001).
type Actor =
    | Human of name: string
    | Agent of AgentIdentity

/// A human or agent judgement recorded with provenance (TEK-CAN-005,
/// TEK-UNC-002). Assertions are never produced by derivation.
type Assertion =
    { Actor: Actor
      At: DateTimeOffset
      Reason: string
      Location: SourceLocation }

/// Every value Tekmerion exposes has exactly one origin (TEK-CAN-005).
type Origin =
    /// Declared in canonical research at this location.
    | Authored of SourceLocation
    /// Deterministically computed by a named rule from these authored facts.
    | Derived of rule: string * basis: NonEmpty<SourceLocation>
    /// Recorded judgement.
    | Asserted of Assertion

/// A value with its origin attached.
type Sourced<'T> = { Value: 'T; Origin: Origin }
