namespace clockApp

open Fabulous.Core
open Fabulous.DynamicViews
open Xamarin.Forms
open MBrace.FsPickler.Json

module App = 
    type Model = 
      { Status : string
        Time: string
        Date: string
        TimeZone: string
      }

    type Msg = 
        | SetClock
        | StatusUpdate of string
        | TimeUpdate of string
        | DateUpdate of string
        | TimeZoneUpdate of string
        | Reset

    //TODO: set InitModel based on current timing
    let initModel = { Time = "01:30:30"; Date = "2018-05-27"; TimeZone = "GMT-5"; Status = "Default Settins applied" }

    let init () = initModel

    let update msg model =
        match msg with
        | SetClock ->
           let deviceConfig = DependencyService.Get<IDeviceConfig.IDeviceConfig>()
           deviceConfig.SetClock(model.TimeZone, model.Date, model.Time)
           { model with Status = "SetClock submitted to MX" }
        | StatusUpdate str ->
           { model with Status = str }        
        | TimeUpdate str ->
           { model with Time = str }        
        | DateUpdate str ->
           { model with Date = str }        
        | TimeZoneUpdate str ->
           { model with TimeZone = str }        
        | Reset -> initModel

    let view (model: Model) dispatch =
        View.ContentPage(
          content=View.StackLayout(padding=20.0, spacing = 5.0,
                  children=[
                    View.Label(text= "Enter Time:", fontSize = "Large")
                    View.Entry(text= model.Time, fontSize = "Large", textChanged=fixf(fun text -> dispatch (TimeUpdate text.NewTextValue)))
                    View.Label(text= "Enter Date:", fontSize = "Large")
                    View.Entry(text= model.Date, fontSize = "Large", textChanged=fixf(fun text -> dispatch (DateUpdate text.NewTextValue)))
                    View.Label(text= "Enter TimeZone:", fontSize = "Large")
                    View.Entry(text= model.TimeZone, fontSize = "Large", textChanged=fixf(fun text -> dispatch (TimeZoneUpdate text.NewTextValue)))
                    View.Button(text="Reset", command=fixf(fun () -> dispatch Reset), canExecute = (model.Time <> initModel.Time || model.Date <> initModel.Date || model.TimeZone <> initModel.TimeZone))
                    View.Button(text="Submit Profile", command= fixf (fun () -> dispatch SetClock))
                    View.Label(text=sprintf "Status: %s" model.Status) 
                  ]))

    let program = Program.mkSimple init update view

open App

type ClockApp () as app = 
    inherit Application ()

    let emdkStatus dispatch = 
        let statusUpdateAction dispatch = new System.Action<ClockApp,string>(fun app arg -> dispatch (StatusUpdate arg) )
        MessagingCenter.Subscribe<ClockApp, string> (Xamarin.Forms.Application.Current, "Status", statusUpdateAction dispatch)

    let runner = 
        program
        |> Program.withSubscription (fun _ -> Cmd.ofSub emdkStatus)
        |> Program.withConsoleTrace
        |> Program.runWithDynamicView app
    
    let modelId = "model"

    override this.OnSleep() = 
        app.Properties.[modelId] <- FsPickler.CreateJsonSerializer().PickleToString(runner.CurrentModel)

    override this.OnResume() = 
        try 
            match app.Properties.TryGetValue modelId with
            | true, (:? string as json) -> 
                runner.SetCurrentModel(FsPickler.CreateJsonSerializer().UnPickleOfString(json), Cmd.none)
            | _ -> ()
        with ex -> 
            program.onError("Error while restoring model found in app.Properties", ex)

    override this.OnStart() = this.OnResume()


