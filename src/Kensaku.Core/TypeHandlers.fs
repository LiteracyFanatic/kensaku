namespace Kensaku.Core.TypeHandlers

open System
open System.Text

open Dapper

open Kensaku.Core

type internal OptionHandler<'T>() =
    inherit SqlMapper.TypeHandler<option<'T>>()

    override this.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box x
            | None -> null

        param.Value <- valueOrNull

    override this.Parse value =
        if isNull value || value = box DBNull.Value then
            None
        else
            Some(value :?> 'T)

type internal RuneHandler() =
    inherit SqlMapper.TypeHandler<Rune>()

    override this.SetValue(param, value) = param.Value <- string value

    override this.Parse(value) = rune value

type internal RuneOptionHandler() =
    inherit SqlMapper.TypeHandler<option<Rune>>()

    override this.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box (string x)
            | None -> null

        param.Value <- valueOrNull

    override this.Parse value =
        if isNull value || value = box DBNull.Value then
            None
        else
            Some(rune value)

type internal Int32Handler() =
    inherit SqlMapper.TypeHandler<int32>()

    override this.SetValue(param, value) = param.Value <- value

    override this.Parse value = int (value :?> int64)

type internal Int32OptionHandler() =
    inherit SqlMapper.TypeHandler<option<int32>>()

    override this.SetValue(param, value) =
        let valueOrNull =
            match value with
            | Some x -> box x
            | None -> null

        param.Value <- valueOrNull

    override this.Parse value =
        if isNull value || value = box DBNull.Value then
            None
        else
            Some(int (value :?> int64))
