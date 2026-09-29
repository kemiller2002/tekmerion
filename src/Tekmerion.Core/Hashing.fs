namespace Tekmerion.Core

open System
open System.Security.Cryptography
open System.Text

module Hashing =

    let sha256Bytes (bytes: byte array) =
        Convert.ToHexString(SHA256.HashData bytes).ToLowerInvariant()

    let sha256Text (text: string) = sha256Bytes (Encoding.UTF8.GetBytes text)

    /// Short stable identifier for derived records such as edges.
    let shortId (parts: string list) =
        (sha256Text (String.Join("\u001f", parts))).Substring(0, 16)
