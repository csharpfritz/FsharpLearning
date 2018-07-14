#if !COMPILED
    #I "lib/"
    // Default path for azure-functions-core-tools dlls when installed with chocolatey
    #I "C:/ProgramData/chocolatey/lib/azure-functions-core-tools/tools/"

    #r "System.ComponentModel.DataAnnotations"
    #r "System.Data"
    #r "System.Net.Http"
    #r "System.Net.Http.Formatting"
    #r "System.Web.Http"
    #r "Microsoft.Azure.Webjobs.Host"
    #r "EntityFramework"
    #r "FSharp.Data"

    open System
    open System.Net.Http
    open Microsoft.Azure.WebJobs.Host
#else
    #r "System.Configuration"
    #r "System.ComponentModel.DataAnnotations"
    #r "System.Data"
    #r "System.Net.Http"
    #r "EntityFramework"
    #r "FSharp.Data"
#endif

open System.ComponentModel.DataAnnotations
open System.Net
open System.Data.Entity
open FSharp.Data

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

type GitHubDataContext (name: string) =
    inherit DbContext (name: string)

    [<DefaultValue>]
    val mutable metrics : DbSet<Metric>
    member __.Metrics with get() = __.metrics and set v = __.metrics <- v

[<Literal>]
let PushEvent = __SOURCE_DIRECTORY__ + "./push.json"

type PushWebHook = JsonProvider<PushEvent>

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
            log.Info "Handling a GitHub WebHook"

            let dbContext = new GitHubDataContext("GitHubData")

            let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask
            let webhook = PushWebHook.Parse data
            let repository = webhook.Repository.Name

            let addCommit (commit : PushWebHook.Commit) =
                log.Info (sprintf "Processing commit: %s" commit.Id)

                let newRecord = {
                    Id              = 0
                    CommitId        = webhook.After
                    Repository      = repository
                    DateStamp       = webhook.HeadCommit.Timestamp
                    Name            = commit.Author.Username
                    NumFilesChanged = commit.Added.Length + commit.Removed.Length + commit.Modified.Length
                }
                dbContext.Metrics.Add newRecord |> ignore

            webhook.Commits
                // Remove final merge commit
                // This might not be correct, but with attached example it look like it will work
                |> Seq.filter (fun c -> c.Id <> webhook.HeadCommit.Id)
                |> Seq.iter addCommit

            let! loaded = dbContext.SaveChangesAsync() |> Async.AwaitTask
            log.Info (sprintf "Loaded %d records" loaded)

            return req.CreateResponse(HttpStatusCode.OK, sprintf "Handled Push Webhook from %s" repository)

    } |> Async.RunSynchronously
