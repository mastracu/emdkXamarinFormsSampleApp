namespace FsEmdkXamarinForms.Droid

open System
open System.IO
open System.Xml

open Android.App
open Android.Content.PM
open Android.OS
open Xamarin.Forms
open Xamarin.Forms.Platform.Android
open Symbol.XamarinEMDK

type Resources = FsEmdkXamarinForms.Droid.Resource

[<Activity (Label = "SetClock", Icon = "@drawable/icon", Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type MainActivity() =
    inherit FormsAppCompatActivity()

    let mutable emdkManager = None
    let mutable profileManager = None
    
    let resultIsOk (results:EMDKResults) =
        let errorFoundXML statusString =
            let mutable failure = false
            use reader =  XmlReader.Create (new StringReader(statusString))
            while reader.Read() do
                match reader.Name with
                | "parm-eror" -> (failure <- true)
                | "characteristic-error" -> (failure <- true)
                | _ -> ()
            failure    
        results.StatusCode = EMDKResults.STATUS_CODE.Success || ( not (errorFoundXML results.StatusString) )

    let notification = new Event<String> ()
    [<CLIEvent>]
    member this.EmdkNotification = notification.Publish 
               
    member this.EmdkInit () = 
        let results = EMDKManager.GetEMDKManager (Application.Context, this)
        do notification.Trigger ("GetEMDKManager" + (if (results.StatusCode <> EMDKResults.STATUS_CODE.Success) then " KO" else " OK"))

    member this.EmdkSetClock (timeZone, date, time) =
        let profileName = "Profile1"
        let featureName = "Clock-1"
        let modifyData1 = ProfileManager.CreateNameValuePair(featureName, "TimeZone", timeZone  )
        let modifyData2 = ProfileManager.CreateNameValuePair(featureName, "Date", date  )
        let modifyData3 = ProfileManager.CreateNameValuePair(featureName, "Time", time  )
        do this.EmdkProcessProfile (profileName, [|modifyData1; modifyData2; modifyData3|]) |> ignore 

    member this.EmdkProcessProfile (profileName, p2:string[]) =
        let asyncProcessJobAndCheck (pm:ProfileManager) =
            use results = pm.ProcessProfileAsync (profileName, ProfileManager.PROFILE_FLAG.Set, p2)
            do notification.Trigger ("ProcessProfileAsync" + if (results.StatusCode = EMDKResults.STATUS_CODE.Processing) then "OK" else  "KO")
        profileManager |> Option.map asyncProcessJobAndCheck

    member this.Release () =
        do emdkManager |> Option.map (fun (em:EMDKManager) -> em.Release()) |> ignore
        do profileManager <- None
        do emdkManager <- None

    interface EMDKManager.IEMDKListener with        
        member this.OnClosed () =
            match emdkManager with 
            | None -> () 
            | Some (em:EMDKManager) -> do em.Release(); do emdkManager <- None; do profileManager <- None
            notification.Trigger "emdkManager has closed"

        member this.OnOpened emdkManagerInstance = 
            do emdkManager <- Some emdkManagerInstance
            try 
                let pm = emdkManagerInstance.GetInstance (EMDKManager.FEATURE_TYPE.Profile) :?> ProfileManager
                do pm.Data.Subscribe this.ProfileManagerData |> ignore
                do profileManager <- Some pm       
                do notification.Trigger "GetInstance success"
            with | _ -> notification.Trigger "GetInstance failed"
   
    member this.ProfileManagerData (e:ProfileManager.DataEventArgs) =
        notification.Trigger (if resultIsOk (e.P0.Result) then "Profile succesfully applied" else "Profile application failed")

    override this.OnCreate (bundle: Bundle) =
        FormsAppCompatActivity.TabLayoutResource <- Resources.Layout.Tabbar
        FormsAppCompatActivity.ToolbarResource <- Resources.Layout.Toolbar

        base.OnCreate (bundle)
        Xamarin.Forms.Forms.Init (this, bundle)
        this.LoadApplication (new clockApp.ClockApp ())

        do this.EmdkNotification.Subscribe (fun str -> 
            this.RunOnUiThread (fun () -> MessagingCenter.Send<clockApp.ClockApp, string>(Xamarin.Forms.Application.Current :?> clockApp.ClockApp, "Status", str))
            Android.Util.Log.Info ("MessagingCenter", str) |> ignore
            ) |> ignore
             
        do this.EmdkInit ()

        let deviceCfg = DependencyService.Get<IDeviceConfig.IDeviceConfig>()
        deviceCfg.ClockEvent.Add (this.EmdkSetClock)
            
