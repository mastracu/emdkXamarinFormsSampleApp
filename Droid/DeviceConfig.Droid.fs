namespace DeviceConfig

open Xamarin.Forms

type DeviceConfig() =
  let clockEvent = new Event<string*string*string> ()
  interface IDeviceConfig.IDeviceConfig with
     member this.ClockEvent = clockEvent.Publish
     member this.SetClock (a,b,c) = clockEvent.Trigger (a,b,c)  

[<assembly: Dependency(typedefof<DeviceConfig>)>]
()
