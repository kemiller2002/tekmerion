namespace Tekmerion.Core

/// Versions recorded in every machine contract (TEK-CON-001, TEK-PUB-002).
module Version =

    [<Literal>]
    let Tekmerion = "0.1.0-dev"

    /// Major version of the `/data/v1/` machine-readable contracts.
    [<Literal>]
    let ContractSchema = "1.0.0"

    /// Parser profile recorded against each artifact (TEK-ING-007).
    [<Literal>]
    let ParserProfile = "ros-markdown/1"
