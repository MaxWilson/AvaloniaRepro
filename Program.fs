// For more information see https://aka.ms/fsharp-console-apps
open Avalonia.FuncUI
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish
open global.Elmish

type Model = {
    people: string list
    chatLog: string list
    }
type Msg =
    | Speak of person:string * msg: string
    | NewPerson of name:string

let init _ = {
    people = ["Bob"]
    chatLog = []
    }

let update msg model =
    match msg with
    | Speak (person, msg) ->
        { model with chatLog = (sprintf "%s: %s" person msg) :: model.chatLog }
    | NewPerson name ->
        { model with people = name :: model.people }

let view model dispatch =
    StackPanel.create [
        StackPanel.children [
            for person in model.people do
                Button.create [Button.content $"Say something {person}"; Button.onClick (fun _ -> dispatch (Speak (person, "Blahblahblah")))]
            for msg in model.chatLog do
                TextBlock.create [TextBlock.text msg]
            if model.people.Length = 1 then
                Button.create [Button.content "Add Ryan"; Button.onClick (fun _ -> dispatch (NewPerson "Ryan"))]
            if model.people.Length = 2 then
                Button.create [Button.content "Add Sarah"; Button.onClick (fun _ -> dispatch (NewPerson "Sarah"))]
            ]
        ]

type MainWindow() as this =
    inherit HostWindow()
    do
        base.Title <- "Repro for Avalonia.FuncUI issue"
        Elmish.Program.mkSimple init update view
        |> Program.withHost this
        |> Elmish.Program.run

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add (FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark
        // this.Styles.Load "avares://Mandrake/UI/Styles.xaml"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
