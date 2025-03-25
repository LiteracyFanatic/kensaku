namespace Kensaku.Core

open System.Text.RegularExpressions

open Dapper
open Microsoft.Data.Sqlite

open Kensaku.Core.TypeHandlers

type KensakuConnection(connectionString: string) as this =
    inherit SqliteConnection(connectionString)

    let registerTypeHandlers () =
        SqlMapper.AddTypeHandler(OptionHandler<string>())
        SqlMapper.AddTypeHandler(RuneHandler())
        SqlMapper.AddTypeHandler(RuneOptionHandler())
        SqlMapper.AddTypeHandler(Int32Handler())
        SqlMapper.AddTypeHandler(Int32OptionHandler())

    let regexpFunction (pattern: string) (input: string) =
        not (isNull input) && Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase)

    let registerRegexpFunction (ctx: SqliteConnection) =
        ctx.CreateFunction("regexp", regexpFunction)

    do
        registerTypeHandlers ()
        registerRegexpFunction this
