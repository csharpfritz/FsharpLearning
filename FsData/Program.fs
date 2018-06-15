// Learn more about F# at http://fsharp.org
 // #r @"bin\Release\netcoreapp2.1\publish\FSharp.Data.dll"  

open System
open System.Linq
open FSharp.Data
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Sqlite

type FsFridayViewers = CsvProvider<".\\data\\2018-06-08.csv">

let connString = "Data Source=streamdata.db"

[<EntryPoint>]
let main argv =

    let dbOptions = new DbContextOptionsBuilder()
    dbOptions.UseSqlite(connString) |> ignore
    
    let dbContext = new Data.ViewerData(dbOptions.Options)
    dbContext.Database.EnsureCreated() |> ignore

    let data = FsFridayViewers.Load ".\\data\\2018-06-08.csv" 
    data.Rows |> Seq.length |> printfn "Total measurements: %d"

    for row in data.Rows do
        let thisDate = DateTime.Parse("2018-06-08").Add(row.Timestamp.TimeOfDay)
        (thisDate, row.Viewers) ||> printfn "At %O we had %M viewers"
        
        let newRecord:Data.StreamViews = {
            Id=0;
            MeasurementTime=thisDate;
            Viewers=row.Viewers;
            NewFollowers=row.``New Followers``.GetValueOrDefault();
            Chatters=row.Chatters;
        }
        dbContext.StreamViews.Add newRecord |> ignore

    dbContext.SaveChanges() |> printfn "Loaded %d records" 

    
    0 // return an integer exit code
