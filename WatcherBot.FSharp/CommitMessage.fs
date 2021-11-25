module WatcherBot.FSharp.CommitMessage

open System
open System.Text.RegularExpressions
open System.Threading.Tasks
open Octokit

let ParseHash (str: string) : Result<string, string> =
    let (|HashMatch|_|) (input: string) : Group option =
        /// Preceding character must not be part of a hash.
        /// Hash must be 5-40 characters.
        /// Optional `/` for URLs, then end of string.
        let regex =
            Regex @"(?<![0-9a-fA-F])(?<hash>[0-9a-fA-F]{5,40})\/?$"

        match isNull input with
        | true -> None
        | false ->
            let m = regex.Match(input)

            match m.Success with
            | true -> m.Groups.["hash"] |> Some
            | false -> None

    match str with
    | HashMatch hash -> Ok hash.Value
    | _ -> Error "Error executing !commitmsg: argument is invalid"

let DeDuplicate (arr: Result<string, string> []) : Result<string, string> [] =
    let matches (s1: string) (s2: string) : bool =
        s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase)
        || s2.StartsWith(s1, StringComparison.OrdinalIgnoreCase)

    let guard (s1: string) (acc: Result<string, string> list) : bool =
        acc
        |> List.exists
            (function
            | Ok v2 -> matches s1 v2
            | Error v2 -> matches s1 v2)

    let folder (elt: Result<string, string>) (acc: Result<string, string> list) : Result<string, string> list =
        if
            match elt with
            | Ok v1
            | Error v1 when guard v1 acc -> true
            | _ -> false
        then
            acc
        else
            elt :: acc

    Array.foldBack folder arr [] |> Array.ofList

/// hash is a Result<string, string> here so that it can be integrated with Async nicely
let TryGetCommit (client: GitHubClient) (hash: Result<string, string>) : Async<Result<GitHubCommit, string>> =
    async {
        return
            hash
            |> Result.bind
                (fun h ->
                    try
                        client.Repository.Commit.Get("Regalis11", "Barotrauma-development", h)
                        |> Async.AwaitTask
                        |> Async.RunSynchronously
                        |> Ok
                    with
                    | :? AggregateException as e ->
                        e.InnerExceptions
                        |> Seq.map (fun e -> e.Message)
                        |> String.concat ", "
                        |> sprintf "Error executing !commitmsg: %s"
                        |> Error
                    | e -> Error $"Error executing !commitmsg: %s{e.Message}")
    }

let GetCommitMessages (client: GitHubClient) (args: string []) : Task<string> =
    async {
        return
            args
            |> Array.map ParseHash
            // De-dupe parsed hashes
            |> DeDuplicate
            |> Array.map (TryGetCommit client)
            // Fetch everything at once
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.map
                (function
                | Ok commit -> $"`{commit.Sha.Substring(0, 10)}: {commit.Commit.Message}`"
                | Error message -> message)
            |> String.concat "\n"
    }
    |> Async.StartAsTask
