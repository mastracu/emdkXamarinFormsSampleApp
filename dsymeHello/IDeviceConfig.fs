namespace IDeviceConfig

open System.Collections.Generic

type IDeviceConfig =
   interface
      abstract ClockEvent : IEvent<string*string*string>
      abstract SetClock : string*string*string -> unit
   end