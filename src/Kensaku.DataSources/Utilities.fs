namespace Kensaku.DataSources

[<AutoOpen>]
module private Utilities =
    open FSharp.Control
    open System.IO
    open System.Text
    open System.Threading
    open System.Xml
    open System.Xml.Linq

    module Option =
        let collect f = Option.map f >> Option.flatten

    let isHiragana (c: char) = c >= '\u3040' && c <= '\u309f'

    let isKatakana (c: char) = c >= '\u30a0' && c <= '\u30ff'

    let isKana (c: char) = isHiragana c || isKatakana c

    let inline rune input = Rune.GetRuneAt(string input, 0)

    let streamXmlElementsAsync (elementName: string) (stream: Stream) (closeStream: bool) (ct: CancellationToken) =
        taskSeq {
            // Parse the DTD and expand all entities
            let settings =
                XmlReaderSettings(
                    DtdProcessing = DtdProcessing.Parse,
                    MaxCharactersFromEntities = 0L,
                    Async = true,
                    CloseInput = closeStream
                )

            let reader = XmlReader.Create(stream, settings)
            let! _ = reader.MoveToContentAsync()

            try
                while! reader.ReadAsync() do
                    if reader.NodeType = XmlNodeType.Element && reader.Name = elementName then
                        let! xNode = XElement.ReadFromAsync(reader, ct)
                        yield xNode :?> XElement
            finally
                reader.Dispose()
        }

    let parseElementList (elementName: string) (f: XElement -> 'a) (el: XElement) =
        el.Elements(elementName) |> Seq.map f |> Seq.toList

    type XAttribute with
        member this.TryGetValue() =
            this |> Option.ofObj |> Option.map (_.Value)
