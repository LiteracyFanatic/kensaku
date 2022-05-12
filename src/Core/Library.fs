module Kensaku.Core

open System.Data.Common
open Dapper
open Kensaku.Domain

let getCharactersWithStrokeCount (strokeCount: int) (ctx: DbConnection) =
    ctx.Query<Character>(
        "select *
        from Characters as c
        where c.StrokeCount = @StrokeCount",
        {|
            StrokeCount = strokeCount
        |})