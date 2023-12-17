// For more information see https://aka.ms/fsharp-console-apps
open Avalonia.FuncUI
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Elmish
open System.Threading.Tasks
open global.Elmish
open System.IO

type FullPath = string
type OrdersDetail = {
    index: int
    approved: bool
    nation: string
    name: string option // defaults to nation but can be renamed to describe the kind of orders, e.g. kamikaze vs. cautious. Will show up in name of generated games.
    }
type FileDetail =
    | Trn
    | Orders of OrdersDetail
    | Other
type GameFile = {
    frozenPath: FullPath // not subject to change directly by Dom5.exe because it's in a temp directory, hence "frozen"
    detail: FileDetail
    }
    with
    member this.Name = (match this.detail with Orders detail -> detail.name |> Option.defaultValue (detail.nation + detail.index.ToString()) | Trn | Other -> Path.GetFileName this.frozenPath) // defaults to file path but can be renamed to describe the kind of orders, e.g. kamikaze vs. cautious. Will show up in name of generated games.
    member this.Nation = match this.detail with Orders _ | Trn -> Some (Path.GetFileNameWithoutExtension this.frozenPath) | Other -> None
type Status = NotStarted | InProgress | Complete
type Permutation = {
    name: string
    status: Status
    }
type Game = {
    name: string
    files: GameFile list
    children: Permutation list
    }
open System
type Model = {
    games: Game list
    }
type FileSystemMsg =
| NewGame of gameName:string
| Approve of string * string
| Refresh

let update msg model =
    match msg with
    | NewGame(game) ->
        printfn "Update NewGame: %A" msg
        let gameName = game//.ToLowerInvariant()
        { model with games = { name = game.ToLowerInvariant(); files = []; children = [] }::model.games }, Elmish.Cmd.Empty
    | Refresh -> model, Cmd.Empty
    | Approve(gameName, ordersName) ->
        let gameName = gameName// .ToLowerInvariant()
        let game = (model.games |> List.find (fun g -> g.name = gameName))
        let transform game =
            if game.name <> gameName then game
            else
            {   game
                with
                    files =
                        game.files
                        |> List.map (function
                            | { detail = Orders det } as f when f.Name = ordersName -> { f with detail = Orders { det with approved = true } }
                            | otherwise -> otherwise
                        )
            }
        { model with games = model.games |> List.map transform }, Elmish.Cmd.Empty
let init _ =
    let model = {
        games = [
            {
                name = "bar"
                files = [
                    { frozenPath = @"C:\Users\wilso\AppData\Local\Temp\Fenris\ftherlnd"; detail = Other };
                    { frozenPath = @"C:\Users\wilso\AppData\Local\Temp\Fenris\early_niefelheim.trn"; detail = Trn };
                    { frozenPath = @"C:\Users\wilso\AppData\Local\Temp\Fenris\early_agartha.trn"; detail = Trn };
                    { frozenPath = @"C:\Users\wilso\AppData\Local\Temp\Fenris\early_niefelheim.2h"
                      detail = Orders { index = 1
                                        approved = false
                                        nation = "early_niefelheim"
                                        name = None } };
                    { frozenPath = @"C:\Users\wilso\AppData\Local\Temp\Fenris\early_agartha.2h"
                      detail = Orders { index = 1
                                        approved = false
                                        nation = "early_agartha"
                                        name = None } }
                    ]
                children = []
                }
            {
                name = "foo"
                files = [
                    { frozenPath = "agartha.trn"; detail = Trn }
                    { frozenPath = "niefelheim.trn"; detail = Trn }
                    { frozenPath = "ftherlnd"; detail = Trn }
                    { frozenPath = "niefelheim"; detail = Orders { index = 1; approved = false; nation = "niefelheim"; name = None } }
                    { frozenPath = "agartha"; detail = Orders { index = 1; approved = false; nation = "agartha"; name = None } }
                    ]
                children = []
                }
            ]
        }
    model, Cmd.Empty

let button game (file: GameFile) det dispatch =
    let name = file.Name + "_" + System.Guid.NewGuid().ToString()
    let txt = if det.approved then "Unapprove" else $"Approve {game.name}/{name}"
    Button.create [
        Button.content txt
        Button.onClick (fun _ -> printfn $"Approve {game.name}/{name}/{file.Name}"; dispatch (Approve(game.name, file.Name)))
        ]
let view (model: Model) dispatch : IView =
    StackPanel.create [
        StackPanel.children [
            TextBlock.create [
                TextBlock.classes ["title"]
                TextBlock.text $"Games"
                ]
            for game in model.games do
                TextBlock.create [
                    TextBlock.classes ["subtitle"]
                    TextBlock.text game.name
                    // TextBox.onTextChanged (fun txt -> exePath.Set (Some txt); exePathValid.Set ((String.IsNullOrWhiteSpace txt |> not) && File.Exists txt))
                    ]
                for file in game.files do
                    match file.detail with
                    | Orders det ->
                        StackPanel.create [
                            StackPanel.orientation Orientation.Horizontal
                            StackPanel.children [
                                TextBlock.create [
                                    TextBlock.text (file.Name)
                                    ]
                                //if not det.approved then
                                let name = file.Name + "_" + System.Guid.NewGuid().ToString()
                                button game file det dispatch
                                ]
                            ]
                    | _ -> ()
            ]
        ]

type MainWindow() as this =
    inherit HostWindow()
    do
        base.Title <- "Mandrake for Dom5"

        Elmish.Program.mkProgram init update view
        |> Program.withHost this
        |> Elmish.Program.withSubscription (fun model ->
            Elmish.Sub.batch [
                [[], fun dispatch ->
                        // dispatch Refresh
                        dispatch (NewGame "Fenris") // this somehow is a key step in the repro
                        { new System.IDisposable with member this.Dispose() = () }
                    ]
                // match model.acceptance.gameTurns, model.fileSettings with
                // | Some turns, { exePath = Some exePath; dataDirectory = Some dataDirectory } ->
                //     // we want to resubscribe if either the settings change or a new game gets created
                //     let prefix = turns |> List.map (fun gt -> gt.name) |> List.append [dataDirectory; exePath] |> String.concat ";"
                //     Sub.map prefix Acceptance (UI.AcceptanceQueue.subscribe turns model.fileSettings)
                // | _ -> ()
                ]
            )
#if DEBUG
        // |> Program.withConsoleTrace
#endif
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
