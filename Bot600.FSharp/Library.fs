module Bot600.FSharp

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Text.RegularExpressions
open FSharpPlus
open FSharpPlus.Data
open Octokit

type HashResultComparer() =
    interface IEqualityComparer<Result<string, string>> with
        member this.Equals(x, y) = x = y

        member this.GetHashCode(obj) = obj.GetHashCode()

let parseHash (str: string) =
    let (|HashMatch|_|) input =
        /// Preceding character must not be part of a hash.
        /// Hash must be 5-40 characters.
        /// Optional `/` for URLs, then end of string.
        let regex = Regex @"(?<![0-9a-fA-F])(?<hash>[0-9a-fA-F]{5,40})\/?$"
        match isNull input with
        | true -> None
        | false ->
            let m = regex.Match(input)
            
            match m.Success with
            | true -> m.Groups.["hash"] |> Some
            | false -> None

    match str with
    | HashMatch hash -> hash |> fun g -> g.Value |> Ok
    | _ -> Error "Error executing !commitmsg: argument is invalid"

/// hash is a Result<string, string> here so that it can be integrated with Async nicely
let tryGetCommit (client: GitHubClient) hash =
    hash
    |> Result.bind
        (fun h ->
            try
                // Put the Async on the inside, so that the outer type is a Result
                client.Repository.Commit.Get("Jlobblet", "Bot600", h)
                |> Async.AwaitTask
                |> Ok
            with
            | :? AggregateException as e ->
                e.InnerExceptions
                |> Seq.map (fun e -> e.Message)
                |> String.concat ", "
                |> sprintf "Error executing !commitmsg: %s"
                |> Error
            | e -> Error $"Error executing !commitmsg: %s{e.Message}")

let getCommitMessages client args =
    args
    |> Array.map parseHash
    // De-dupe parsed hashes
    |> fun hs -> hs.ToImmutableHashSet(HashResultComparer())
    |> Seq.map
        (fun h ->
            // Turn Result<Async<_>, _> into Async<Result<_, _>>
            async {
                let commit =
                    tryGetCommit client h
                    |> (Result.map Async.RunSynchronously)

                return commit
            })
    // Fetch everything at once
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Array.map (function
            | Ok commit -> $"`{commit.Sha.Substring(0, 10)}: {commit.Commit.Message}`"
            | Error message -> message
    )
    |> String.concat "\n"
