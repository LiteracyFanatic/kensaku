module Database

open System.IO
open Microsoft.Data.Sqlite

let createSchema (connection: SqliteConnection) =
    let cmd = connection.CreateCommand()
    cmd.CommandText <- File.ReadAllText("sql/schema.sql")
    cmd.ExecuteNonQuery() |> ignore

let populateRadicals (connection: SqliteConnection) (radkEntries: DataParsing.RadkEntry list) =
    use transation = connection.BeginTransaction()
    let cmd = connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO RADICALS ('value', 'strokeCount') VALUES (@value, @strokeCount)"
    let value = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@value"))
    let strokeCount = cmd.Parameters.Add(cmd.CreateParameter(ParameterName = "@strokeCount"))
    for entry in radkEntries do
        value.Value <- entry.Radical
        strokeCount.Value <- entry.StrokeCount
        cmd.ExecuteNonQuery() |> ignore
    transation.Commit()

let populateTables (connection: SqliteConnection) =
    let radkEntries = DataParsing.getRadkEntries ()
    populateRadicals connection radkEntries
