#r "nuget: FSharp.Data"

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open FSharp.Data
open System.Threading.Tasks

let hasKanjiRegex =
    Regex(@"[\p{IsCJKUnifiedIdeographs}\p{IsCJKUnifiedIdeographsExtensionA}\p{IsCJKCompatibilityIdeographs}]")

let pandoc (fromFormat: string) (toFormat: string) (stream: Stream) =
    let psi = ProcessStartInfo()
    psi.FileName <- "pandoc"
    psi.ArgumentList.Add("--from")
    psi.ArgumentList.Add(fromFormat)
    psi.ArgumentList.Add("--to")
    psi.ArgumentList.Add(toFormat)
    psi.RedirectStandardInput <- true
    psi.RedirectStandardOutput <- true
    use p = Process.Start(psi)
    stream.CopyTo(p.StandardInput.BaseStream)
    p.StandardInput.Close()
    let resultStream = new MemoryStream()
    p.StandardOutput.BaseStream.CopyTo(resultStream)
    resultStream.Position <- 0L
    p.WaitForExit()
    resultStream

let mecab (input: Stream) =
    let psi = ProcessStartInfo()
    psi.FileName <- "mecab"
    psi.ArgumentList.Add("-F")
    psi.ArgumentList.Add("%f[6]\\n")
    psi.ArgumentList.Add("-U")
    psi.ArgumentList.Add("%m\\n")
    psi.RedirectStandardInput <- true
    psi.RedirectStandardOutput <- true
    psi.UseShellExecute <- false

    use p = Process.Start(psi)

    let stdinTask =
        input
            .CopyToAsync(p.StandardInput.BaseStream)
            .ContinueWith(fun _ -> p.StandardInput.Close())

    let resultStream = new MemoryStream()

    let stdoutTask = p.StandardOutput.BaseStream.CopyToAsync(resultStream)

    Task.WaitAll([| stdinTask; stdoutTask |])

    p.WaitForExit()

    resultStream.Position <- 0L
    resultStream

let rec removeRuby (node: HtmlNode) =
    match node with
    | HtmlElement _ ->
        let attrs = node.Attributes() |> List.map (fun a -> a.Name(), a.Value())

        let children =
            node.Elements()
            |> List.filter (fun n -> not (n.HasName "rt"))
            |> List.map removeRuby
            |> List.collect (fun (n: HtmlNode) -> if n.HasName "ruby" then n.Elements() else [ n ])

        HtmlNode.NewElement(node.Name(), attrs, children)
    | _ -> node

module Stream =
    let toLines (stream: Stream) =
        let reader = new StreamReader(stream)

        seq {
            while not reader.EndOfStream do
                yield reader.ReadLine()

            reader.Dispose()
        }

    let fromString (s: string) =
        new MemoryStream(Text.Encoding.UTF8.GetBytes s)

let processFile (fileName: string) =
    use inputStream = File.OpenRead fileName
    use htmlStream = pandoc "epub" "html" inputStream

    let doc = HtmlDocument.Load htmlStream

    let stripped =
        doc.Elements()
        |> List.map (removeRuby >> sprintf "%O")
        |> String.concat ""
        |> Stream.fromString

    use plainText = pandoc "html" "plain" stripped

    let wordsStream = mecab plainText

    let words =
        wordsStream
        |> Stream.toLines
        |> Seq.filter hasKanjiRegex.IsMatch
        |> Seq.countBy id
        |> Seq.sortByDescending snd

    for word, count in words do
        printfn "%i,%s" count word

if Environment.GetCommandLineArgs().Length = 3 then
    processFile (Environment.GetCommandLineArgs()[2])
else
    printfn $"Usage: dotnet fsi %s{Environment.GetCommandLineArgs()[1]} <file>"
