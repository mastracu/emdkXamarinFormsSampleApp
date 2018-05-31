namespace clockApp

// Copyright 2018 Elmish.XamarinForms contributors. See LICENSE.md for license.

open System.Diagnostics
open Elmish.XamarinForms
open Elmish.XamarinForms.DynamicViews
open Xamarin.Forms

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

    let init () = initModel, Cmd.none

    let update msg model =
        match msg with
        | SetClock ->
           let deviceConfig = DependencyService.Get<IDeviceConfig.IDeviceConfig>()
           deviceConfig.SetClock(model.TimeZone, model.Date, model.Time)
           { model with Status = "SetClock submitted to MX" }, Cmd.none
        | StatusUpdate str ->
           { model with Status = str }, Cmd.none        
        | TimeUpdate str ->
           { model with Time = str }, Cmd.none        
        | DateUpdate str ->
           { model with Date = str }, Cmd.none        
        | TimeZoneUpdate str ->
           { model with TimeZone = str }, Cmd.none        
        | Reset -> initModel, Cmd.none

    let view (model: Model) dispatch =
        Xaml.ContentPage(
          content=Xaml.StackLayout(padding=20.0, spacing = 5.0,
                  children=[
                    Xaml.Label(text= "Enter Time:", fontSize = "Large")
                    Xaml.Entry(text= model.Time, fontSize = "Large", textChanged=fixf(fun text -> dispatch (TimeUpdate text.NewTextValue)))
                    Xaml.Label(text= "Enter Date:", fontSize = "Large")
                    Xaml.Entry(text= model.Date, fontSize = "Large", textChanged=fixf(fun text -> dispatch (DateUpdate text.NewTextValue)))
                    Xaml.Label(text= "Enter TimeZone:", fontSize = "Large")
                    Xaml.Entry(text= model.TimeZone, fontSize = "Large", textChanged=fixf(fun text -> dispatch (TimeZoneUpdate text.NewTextValue)))
                    Xaml.Button(text="Reset", command=fixf(fun () -> dispatch Reset), canExecute = (model.Time <> initModel.Time || model.Date <> initModel.Date || model.TimeZone <> initModel.TimeZone))
                    Xaml.Button(text="Submit Profile", command= fixf (fun () -> dispatch SetClock))
                    Xaml.Label(text=sprintf "Status: %s" model.Status) 
                  ]))

open App


type ClockApp () as app = 
    inherit Application ()

    let emdkStatus dispatch = 
        let statusUpdateAction dispatch = new System.Action<ClockApp,string>(fun app arg -> dispatch (StatusUpdate arg) )
        MessagingCenter.Subscribe<ClockApp, string> (Xamarin.Forms.Application.Current, "Status", statusUpdateAction dispatch)

    let program = Program.mkProgram init update view
    let runner = 
        program
        |> Program.withSubscription (fun _ -> Cmd.ofSub emdkStatus)
        |> Program.withConsoleTrace
        |> Program.withDynamicView app
        |> Program.run

