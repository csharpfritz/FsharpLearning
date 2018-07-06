#r "System.Net.Http"
#r "Newtonsoft.Json"
#r "System.Data"
#r "EntityFramework.dll"
#r "System.Configuration"

// #load "data.fsx"

open System.Net
open System.Net.Http
open Newtonsoft.Json
open FSharp.Data
open System
open System.Configuration
open System.ComponentModel.DataAnnotations
open System.Data.Entity



[<CLIMutable>]
type Metric = {

    [<Key>]
    Id: int
    CommitId: string
    Repository: string
    DateStamp: DateTime
    Name: string
    NumFilesChanged: int
}

type GitHubData (name: string) = 
    inherit DbContext (name: string)

    [<DefaultValue>] val mutable metrics : DbSet<Metric>
    member __.Metrics with get() = __.metrics and set v = __.metrics <- v


[<Literal>] 
let pushSample = __SOURCE_DIRECTORY__ + "/push.json"

type PushWebHook = JsonProvider<pushSample> 

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async { 
        log.Info(sprintf 
            "Handling a GitHub WebHook") 

        let dbContext = new GitHubData("fritzstreamdb")

        let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask
        let webhook = PushWebHook.Parse(data)

        let commitId = webhook.After
        let dateStamp = DateTime.UtcNow
        let repository = webhook.Repository.Name

        let filteredCommits = webhook.Commits |> Seq.filter(fun c -> c.Committer.Username <> "web-flow") 

        for commit in filteredCommits do
          
          let newRecord:Metric = {
            Id = 0;
            CommitId = commitId;
            Repository = repository;
            DateStamp = dateStamp;
            Name = commit.Author.Username;
            NumFilesChanged = commit.Added.Length + commit.Removed.Length + commit.Modified.Length;
          }

          dbContext.Metrics.Add newRecord |> ignore
          
        let! loaded = dbContext.SaveChangesAsync() |> Async.AwaitTask 
        loaded |> printfn "Loaded %d records" 
        return req.CreateResponse(HttpStatusCode.OK, "Handled Push Webhook from " + repository)

    } |> Async.RunSynchronously
