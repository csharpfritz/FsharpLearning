module Data

open System
open System.ComponentModel.DataAnnotations
open Microsoft.EntityFrameworkCore

[<CLIMutable>]
type StreamViews = {

    [<Key>]
    Id: int
    MeasurementTime: DateTime
    Viewers: decimal
    NewFollowers: int
    Chatters: int
}

type ViewerData (options) = 
    inherit DbContext (options)

    [<DefaultValue>] val mutable streamviews : DbSet<StreamViews>
    member __.StreamViews with get() = __.streamviews and set v = __.streamviews <- v
