#if !COMPILED

// You'll need to uncomment one of the following lines based on
// how you installed the Core Tools (i.e. with npm or Chocolatey)

#I @"C:/Users/smabr/AppData/Roaming/npm/node_modules/azure-functions-core-tools/bin/"
// #I @"C:/ProgramData/chocolatey/lib/azure-functions-core-tools/tools/"

#r "Microsoft.Azure.Webjobs.Host.dll"
open Microsoft.Azure.WebJobs.Host

#r "System.Net.Http.Formatting.dll"
#r "System.Web.Http.dll"
#r "System.Net.Http.dll"
#r "System.Data.dll"
#r "System.Configuration.dll"
#r "System.ComponentModel.DataAnnotations.dll"
#r "Newtonsoft.Json.dll"

#I @"c:/users/smabr/data/Functions/packages/nuget/entityframework/6.2.0/lib/net45/"
#I @"c:/users/smabr/data/Functions/packages/nuget/fsharp.data/3.0.0-beta3/lib/net45/"

#else

#r "System.Net.Http"
#r "Newtonsoft.Json"
#r "System.Data"
#r "System.Configuration"
#r "System.ComponentModel.DataAnnotations"

#I @"D:/home/data/Functions/packages/nuget/entityframework/6.2.0/lib/net45/"
#I @"D:/home/data/Functions/packages/nuget/fsharp.data/3.0.0-beta3/lib/net45/"

#endif

#r "EntityFramework.dll"
#r "FSharp.Data.dll"

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
        log.Info "Handling a GitHub WebHook"

        // if dbContext is `IDisposable` you should use `use` here
        // this will dispose `dbContext` at the end of this block (like `using` in C#)
        // if it's not `IDisposable` change to
        // let dbContext = GitHubData "fritzstreamdb"
        // - because in F# the convention is to only use `new` if it's `IDisposable`
        use dbContext = new GitHubData("fritzstreamdb")

        let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask
        let webhook = PushWebHook.Parse data

        let commitId = webhook.After
        let dateStamp = DateTime.UtcNow
        let repository = webhook.Repository.Name
        
        webhook.Commits 
        |> Seq.filter (fun c -> c.Committer.Username <> "web-flow") 
        |> Seq.iter (fun commit ->
                      let newRecord = 
                        {
                            Id = 0
                            CommitId = commitId
                            Repository = repository
                            DateStamp = dateStamp
                            Name = commit.Author.Username
                            NumFilesChanged = commit.Added.Length + commit.Removed.Length + commit.Modified.Length
                        }
                      dbContext.Metrics.Add newRecord |> ignore)
        
          
        let! loaded = dbContext.SaveChangesAsync() |> Async.AwaitTask 
        printfn "Loaded %d records" loaded
        
        return req.CreateResponse(HttpStatusCode.OK, "Handled Push Webhook from " + repository)

    } |> Async.RunSynchronously
