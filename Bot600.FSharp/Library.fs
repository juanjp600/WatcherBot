module Bot600.FSharp

open System
open System.Text.RegularExpressions
open FSharpPlus
open FSharpPlus.Data
open Octokit

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
    
let deDupe (arr: Result<string, string>[]) =
    let matches (s1: string) (s2: string) =
        s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase)
        || s2.StartsWith(s1, StringComparison.OrdinalIgnoreCase)
        
    let guard s1 acc =
                acc |> List.exists (function
                | Ok v2 -> matches s1 v2
                | Error v2 -> matches s1 v2)
    
    let folder (elt: Result<string, string>) (acc: Result<string, string> list) =
        if
            match elt with
            | Ok v1
            | Error v1 when guard v1 acc -> true
            | _ -> false
            then acc
            else elt :: acc
        
    Array.foldBack folder arr []
    |> Array.ofList

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
    |> deDupe
    |> Array.map
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
