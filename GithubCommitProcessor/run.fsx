#r "System.Net.Http"
#r "Newtonsoft.Json"

open System.Net
open System.Net.Http
open Newtonsoft.Json
open FSharp.Data

[<Literal>]
let pushSample = __SOURCE_DIRECTORY__ + "/push.json"

type PushWebHook = JsonProvider<pushSample> 

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
        log.Info(sprintf 
            "Handling a GitHub WebHook")

        let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask
        let webhook = PushWebHook.Parse(data)

        let repository = webhook.Repository.Name

        // TODO: parse each commit's authors separately and give each of them appropriate credit
        let author = webhook.Commits.[0].Author.Username


        return req.CreateResponse(HttpStatusCode.OK, "Handled Push Webhook from " + repository + " from " + author)

    } |> Async.RunSynchronously



