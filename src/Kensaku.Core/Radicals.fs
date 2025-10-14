namespace Kensaku.Core

module Radicals =
    open System.Text

    open Dapper

    /// <summary>
    /// Represents a query for searching radicals.
    /// </summary>
    type GetRadicalQuery = {
        RadicalNumber: int option
        RadicalName: string option
        RadicalMeaning: string option
        MinStrokeCount: int option
        MaxStrokeCount: int option
    }

    /// <summary>
    /// Represents the result of a radical query including values, meanings, and associated kanji count.
    /// </summary>
    type RadicalQueryResult = {
        Id: int
        Number: int option
        StrokeCount: int
        Values: Rune list
        Meanings: string list
        Names: string list
        KanjiCount: int
    }

    [<CLIMutable>]
    type private RadicalKanjiCount = {
        RadicalId: int
        Count: int
    }

    [<CLIMutable>]
    type private RadicalNameValue = {
        RadicalId: int
        Value: string
    }

    let private getRadicalIdsAsync (query: GetRadicalQuery) (ctx: KensakuConnection) =
        task {
            return!
                ctx.QueryAsync<int>(
                    sql
                        """
                    select distinct r.Id
                    from Radicals as r
                    where true
                    and (@RadicalNumber is null or r.Number = @RadicalNumber)
                    and (
                        @RadicalName is null or exists (
                            select 1
                            from RadicalValues as rv
                            join Characters as c on c.Value = rv.Value and c.IsRadical = 1
                            join RadicalNames as rn on rn.CharacterId = c.Id
                            where rv.RadicalId = r.Id and rn.Value = @RadicalName
                        )
                    )
                    and (
                        @RadicalMeaning is null or (
                            exists (
                                select 1 from RadicalMeanings as rm
                                where rm.RadicalId = r.Id and rm.Value like @RadicalMeaning
                            )
                            or exists (
                                select 1 from RadicalValues as rv2
                                where rv2.RadicalId = r.Id and rv2.Value = @RadicalMeaning
                            )
                        )
                    )
                    and (@MinStrokeCount is null or r.StrokeCount >= @MinStrokeCount)
                    and (@MaxStrokeCount is null or r.StrokeCount <= @MaxStrokeCount)
                    order by r.Number, r.StrokeCount
                    """,
                    param = query
                )
        }

    let private getRadicalsByIdsAsync (ids: int seq) (ctx: KensakuConnection) =
        task {
            if Seq.isEmpty ids then
                return Seq.empty
            else
                let param = {| Ids = ids |}

                let! radicals = ctx.QueryAsync<Tables.Radical>(sql "select * from Radicals where Id in @Ids", param)
                let radicalsById = radicals |> Seq.map (fun x -> x.Id, x) |> Map.ofSeq

                let! radicalValues =
                    ctx.QueryAsync<Tables.RadicalValue>(
                        sql "select * from RadicalValues where RadicalId in @Ids",
                        param
                    )

                let radicalValuesByRadicalId = radicalValues |> Seq.groupBy _.RadicalId |> Map.ofSeq

                let! radicalMeanings =
                    ctx.QueryAsync<Tables.RadicalMeaning>(
                        sql "select * from RadicalMeanings where RadicalId in @Ids",
                        param
                    )

                let radicalMeaningsByRadicalId =
                    radicalMeanings |> Seq.groupBy _.RadicalId |> Map.ofSeq

                let! kanjiCounts =
                    ctx.QueryAsync<RadicalKanjiCount>(
                        sql
                            "select RadicalId, count(*) as Count from Characters_Radicals where RadicalId in @Ids group by RadicalId",
                        param
                    )

                let kanjiCountsByRadicalId =
                    kanjiCounts |> Seq.map (fun x -> int x.RadicalId, int x.Count) |> Map.ofSeq

                let! radicalNames =
                    ctx.QueryAsync<RadicalNameValue>(
                        sql
                            """
                        select rv.RadicalId, rn.Value
                        from RadicalValues as rv
                        join Characters as c on c.Value = rv.Value and c.IsRadical = 1
                        join RadicalNames as rn on rn.CharacterId = c.Id
                        where rv.RadicalId in @Ids
                        """,
                        param
                    )

                let radicalNamesByRadicalId =
                    radicalNames
                    |> Seq.groupBy _.RadicalId
                    |> Seq.map (fun (rid, items) -> rid, (items |> Seq.map _.Value |> Seq.distinct |> Seq.toList))
                    |> Map.ofSeq

                return
                    ids
                    |> Seq.map (fun id ->
                        let radical = radicalsById[id]

                        {
                            Id = id
                            Number = radical.Number
                            StrokeCount = radical.StrokeCount
                            Values =
                                radicalValuesByRadicalId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.map _.Value
                                |> Seq.toList
                            Meanings =
                                radicalMeaningsByRadicalId
                                |> Map.tryFind id
                                |> Option.defaultValue []
                                |> Seq.map _.Value
                                |> Seq.toList
                            Names = radicalNamesByRadicalId |> Map.tryFind id |> Option.defaultValue []
                            KanjiCount = kanjiCountsByRadicalId |> Map.tryFind id |> Option.defaultValue 0
                        })
        }

    /// <summary>
    /// Queries radicals based on search criteria.
    /// </summary>
    /// <param name="query">The search query parameters.</param>
    /// <param name="ctx">The database connection.</param>
    /// <returns>A task that returns a sequence of matching radical query results.</returns>
    let getRadicalsAsync (query: GetRadicalQuery) (ctx: KensakuConnection) =
        task {
            let! ids = getRadicalIdsAsync query ctx
            return! getRadicalsByIdsAsync ids ctx
        }

    /// <summary>
    /// Retrieves radical information for specific radical literals.
    /// </summary>
    /// <param name="radicals">The list of radical characters to retrieve.</param>
    /// <param name="ctx">The database connection.</param>
    /// <returns>A task that returns a sequence of radical query results.</returns>
    let getRadicalLiteralsAsync (radicals: Rune list) (ctx: KensakuConnection) =
        task {
            let! ids =
                ctx.QueryAsync<int>(
                    sql "select distinct RadicalId from RadicalValues where Value in @Radicals",
                    {| Radicals = List.map string radicals |}
                )

            return! getRadicalsByIdsAsync ids ctx
        }
