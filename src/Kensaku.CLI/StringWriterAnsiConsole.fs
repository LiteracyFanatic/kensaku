namespace Kensaku.CLI

open System
open System.IO
open Spectre.Console
open Spectre.Console.Rendering

type NonBreakingRenderable(renderable: IRenderable) =
    new(text: string) = NonBreakingRenderable(Text(text))

    interface IRenderable with
        member _.Measure(options: RenderOptions, maxWidth: int) =
            renderable.Measure(options, Int32.MaxValue)

        member _.Render(options: RenderOptions, maxWidth: int) =
            renderable.Render(options, Int32.MaxValue)

type StringWriterAnsiConsole() =
    let writer = new StringWriter()
    let settings = AnsiConsoleSettings()

    do
        settings.Ansi <-
            if Console.IsOutputRedirected then
                AnsiSupport.No
            else
                AnsiSupport.Detect

    do settings.Out <- AnsiConsoleOutput(writer)
    let console = AnsiConsole.Create(settings)

    interface IAnsiConsole with
        member this.Profile = console.Profile
        member this.Cursor = console.Cursor
        member this.Input = console.Input
        member this.ExclusivityMode = console.ExclusivityMode
        member this.Pipeline = console.Pipeline
        override this.Clear(home: bool) = console.Clear(home)
        override this.Write(value: IRenderable) = console.Write(value)

    member this.WriteNonBreaking(value: string) =
        console.Write(NonBreakingRenderable(value))

    member this.WriteLineNonBreaking(value: string) =
        console.Write(NonBreakingRenderable($"%s{value}\n"))

    member this.MarkupNonBreaking(value: string) =
        console.Write(NonBreakingRenderable(Markup(value)))

    member this.MarkupLineNonBreaking(value: string) =
        console.Write(NonBreakingRenderable(Markup($"%s{value}\n")))

    override this.ToString() = writer.ToString()
