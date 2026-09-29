namespace Tekmerion.Cli

open Aegis

/// Aegis classification for Tekmerion's host boundaries. Expected domain
/// outcomes (findings, illegal transitions) never pass through here; only
/// unexpected operational failures do (doc 18 §2.5).
module Faults =

    let private classify code category impact recovery message =
        fun (config: AegisConfig) (scope: Scope) (ex: exn) ->
            Aegis.faultOf config scope (FaultCode code) category FaultSeverity.Error impact RequiresIntervention recovery message ex

    let readSource config =
        classify "TEKMERION.SOURCE.READ_FAILED" InfrastructureFailure OperationOnly ManualIntervention "A research source file could not be read." config

    let readConfig config =
        classify "TEKMERION.CONFIG.READ_FAILED" ConfigurationFailure FeatureUnavailable ManualIntervention "The Tekmerion configuration could not be read." config

    let enumerate config =
        classify "TEKMERION.SOURCE.ENUMERATE_FAILED" InfrastructureFailure FeatureUnavailable ManualIntervention "The research repository could not be listed." config

    let writeOutput config =
        classify "TEKMERION.OUTPUT.WRITE_FAILED" InfrastructureFailure FeatureUnavailable AbortOperation "The publication could not be written. The previous publication is unchanged." config

    let promote config =
        classify "TEKMERION.OUTPUT.PROMOTE_FAILED" InfrastructureFailure FeatureUnavailable ManualIntervention "The new publication could not replace the previous one." config

    /// A one-line, secret-free description for the terminal.
    let describe (fault: Fault) =
        let (FaultCode code) = fault.Code
        $"{code}: {fault.UserMessage}"
