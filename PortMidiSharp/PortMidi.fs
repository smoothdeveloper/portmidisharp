namespace PortMidi
open Midi
open PortMidi.Native
open System.Collections.Generic
open System.Runtime.InteropServices
open System
open System.IO

type MidiDeviceInfo internal (id: int, inner: PmDeviceInfo) as this =
  let interfaceName = Marshal.PtrToStringAnsi inner.Interface
  let name = Marshal.PtrToStringAnsi inner.Name
  member x.DeviceId = id
  member x.InterfaceName = interfaceName
  member x.Name = name
  member x.SupportsInput = inner.Input = 1
  member x.SupportsOutput = inner.Output = 1
  override x.ToString () = sprintf "Device %i: %s, %s %s%s" id name interfaceName (if x.SupportsInput then "Input" else "") (if x.SupportsOutput then "Output" else "")
  interface IDeviceInfo with
    member x.Name = this.Name
    member x.DeviceId = this.DeviceId
open Midi.MidiMessageTypeIdentifaction
open Midi.Registers

type internal OpenedDevice(device: MidiDeviceInfo, stream: IntPtr) =
    let mutable sysex  : MemoryStream = Unchecked.defaultof<_>
    member __.Stream = stream
    member __.Device = device
    interface Midi.Registers.ISysexInputState with
      member __.BeginSysex () = sysex <- new MemoryStream(); sysex.WriteByte 0xf0uy;
      member __.DisposeSysex () = sysex.Dispose(); sysex <- null
      member __.SysexInProgress = not (isNull sysex)
      member __.WriteSysexByte b = sysex.WriteByte b
      member __.SysexData = sysex.ToArray()

module Runtime =
  open Platform
  let internal sizeOfEvent = Marshal.SizeOf(typeof<PmEvent>)
  let init () = Pm_Initialize()
  let terminate () = Pm_Terminate()
  let pmTimeProc = Native.PmTimeProc(fun _ -> PortTime.Native.Platform.Pt_Time())
  let inline internal getDeviceById id =
    let deviceInfo : PmDeviceInfo = Marshal.PtrToStructure(Pm_GetDeviceInfo id, typeof<PmDeviceInfo>) :?> _
    MidiDeviceInfo(id, deviceInfo)

  let getDevices () = Array.init (Pm_CountDevices()) getDeviceById
  
  let defaultOutputDevice =
    let id = Pm_GetDefaultOutputDeviceID()
    if id < 0 then None else Some(getDeviceById id)

  let defaultInputDevice =
    let id = Pm_GetDefaultInputDeviceID()
    if id < 0 then None else Some(getDeviceById id)

  let internal getErrorText err = Pm_GetErrorText err |> Marshal.PtrToStringAnsi

  let internal hasHostError (device: OpenedDevice) = Pm_HasHostError device.Stream = 1

  let internal getHostErrorText () =
    let msg = Marshal.AllocHGlobal(256)
    Pm_GetHostErrorText msg 256u
    let text = Marshal.PtrToStringAnsi msg
    Marshal.FreeHGlobal msg
    text

  open System.Threading
  open Midi.Registers

  let internal inputDeviceGate = obj()
  let internal inputDevices = Dictionary<int,OpenedDevice>()
  let internal registerOpenInputDevice deviceInfo stream =
    let d = OpenedDevice(deviceInfo, stream)
    lock inputDeviceGate (fun () -> inputDevices.[deviceInfo.DeviceId] <- d)
  let internal discardOpenInputDevice (deviceInfo: MidiDeviceInfo) =
    lock inputDeviceGate (fun () -> inputDevices.Remove deviceInfo.DeviceId) |> ignore

//  let Error = new Event<_>()
//  let RealtimeMessageReceived = new Event<_>()
//  let SystemMessageReceived = new Event<_>()
//  let ChannelMessageReceived = new Event<_>()
//  let SysexReceived = new Event<_>()
#if RAW_IMPL
  let internal getMessageType message : MidiMessageType =
    if message <= 14 then (message &&& 240) else (message &&& 255)
    |> byte
    |> LanguagePrimitives.EnumOfValue 

  let internal completeSysex (device: OpenedDevice) =
    let sysexInput : Midi.Registers.ISysexInputState = device :> _
    if sysexInput.SysexData.Length > 5 then
      (device.Device, sysexInput.SysexData) |> SysexReceived.Trigger
    sysexInput.DisposeSysex ()

  let internal processSysexMessage (device: OpenedDevice) message =
    let sysexInput : Midi.Registers.ISysexInputState = device :> _
    let mutable endEncountered = false
    for i in 0 .. 3 do
      if not endEncountered then
        let b = (message >>> (i * 8)) &&& 255 |> byte
        if b < 128uy || b = 247uy then
          sysexInput.WriteSysexByte b
        if b = 247uy then
          completeSysex device
          endEncountered <- true
  
  let internal processEvents (device: OpenedDevice) (events: PmEvent array) =
    let sysexInput : Midi.Registers.ISysexInputState = device :> _
    for e in events do
      let messageType = getMessageType e.Message
      if isRealtimeMessage messageType then
        (device.Device, MidiEvent(MidiMessage.FromWord e.Message, e.Timestamp)) |> RealtimeMessageReceived.Trigger
      elif sysexInput.SysexInProgress && messageType <> MidiMessageType.SysEx then
          processSysexMessage device e.Message
      elif isSystemMessage messageType then
        if messageType = MidiMessageType.SysEx then
          if sysexInput.SysexInProgress then
            // accomodate for incomplete sysex
            completeSysex device
          sysexInput.BeginSysex()
          processSysexMessage device e.Message
        else
          (device.Device, MidiEvent(MidiMessage.FromWord e.Message, e.Timestamp)) |> SystemMessageReceived.Trigger 
      else
        (device.Device, MidiEvent(MidiMessage.FromWord e.Message, e.Timestamp)) |> ChannelMessageReceived.Trigger
#else

  let internal processEvents (device: OpenedDevice) (events: PmEvent array) platform =
    Midi.Registers.PlatformImplHelp.processEvents 
      device 
      events 
      (fun d -> d.Device) 
      platform 
      (fun m -> MidiEvent(MidiMessage.FromWord m.Message, m.Timestamp)) 
      (fun m -> m.Message)
      (fun d -> d:> ISysexInputState)
    
#endif

  let internal read stream bufferSize = 
    let buffer = Marshal.AllocHGlobal (sizeOfEvent * bufferSize)
    let result = 
      match Pm_Read stream buffer bufferSize with
      | count when count >= 0 ->
        let mutable ptr = buffer
        Array.init count (fun _ ->
          let r : PmEvent = Marshal.PtrToStructure(ptr, typeof<PmEvent>) :?> _
          ptr <- ptr + (nativeint sizeOfEvent)
          r) |> Choice1Of2
      | err -> Choice2Of2 (getErrorText (err |> LanguagePrimitives.EnumOfValue))
    Marshal.FreeHGlobal buffer
    result
      
  let processMidiEvents (readBufferSize: int) (platform: MidiPlatformTrigger<_,_,_>) =
      let devices = lock (inputDeviceGate) (fun () -> inputDevices.Values |> Seq.toArray)
      for d in devices do
        if hasHostError (d) then
          platform.NoticeError(d.Device, getHostErrorText ())

        match Platform.Pm_Poll(d.Stream) with
        | PmError.GotData -> 
          match read d.Stream readBufferSize with
          | Choice2Of2 message -> platform.NoticeError(d.Device, message)
          | Choice1Of2 events ->
            processEvents d events platform
        | PmError.NoData -> ()
        | err ->
          platform.NoticeError(d.Device, getErrorText err)

  (*let threadHandler (pollInterval: TimeSpan) (readBufferSize: int) =
    while true do
      processMidiEvents readBufferSize
      Thread.Sleep pollInterval*)

  let internal checkError deviceInfo (platform: MidiPlatformTrigger<_,_,_>) errnum =
    match errnum with
    | PmError.NoError -> ()
    | _ -> platform.NoticeError (deviceInfo, (Platform.Pm_GetErrorText errnum |> Marshal.PtrToStringAnsi))

  let inline internal refEqual a b = Object.ReferenceEquals(a, b)
  let inline internal filterEvent deviceInfo e =
    e
    |> Event.filter (fst >> (refEqual deviceInfo))
    |> Event.map snd

  let getDevice nameFilter isInput =
    let devices = getDevices()
    devices |> Seq.tryFind (fun d -> d.Name.Contains nameFilter && d.SupportsInput = isInput && d.SupportsOutput = not isInput)       
  

  let midiCallback = Event<_>()
  let mutable callbackCounter = 0
  let callback platform (timestamp: PortTime.Native.PtTimestamp) (data : IntPtr) = 
    callbackCounter <- callbackCounter + 1
    processMidiEvents 256 platform
    midiCallback.Trigger timestamp
  let makePtCallback platform =
    let ptCallback = PortTime.Native.PtCallback(callback platform)
    PortTime.Native.Platform.Pt_Start (1) ptCallback IntPtr.Zero |> ignore
    let midiPlatform : Midi.Registers.IMidiPlatform<MidiDeviceInfo,_,_> = platform :> _

    midiPlatform.Error.Add(fun (device, m) -> 
      if device.SupportsInput then
        printfn "ERROR INPUT %s %s" device.Name  m
      if device.SupportsOutput then
        printfn "ERROR OUTPUT %s %s" device.Name  m
    )
    ptCallback

  let ptGetTime = Native.PmTimeProc(fun data -> PortTime.Native.Platform.Pt_Time())
  init() |> ignore

type MidiInput<'timestamp>(deviceInfo: MidiDeviceInfo, pmTimeProc:PmTimeProc, platform: MidiPlatformTrigger<_,_,'timestamp>) as this =
  let mutable stream = IntPtr.Zero
  let isOpen () = stream <> IntPtr.Zero
  let midiPlatform = platform :> Midi.Registers.IMidiPlatform<_,_,_>
  let realtimeMessageReceived =
    midiPlatform.RealtimeMessageReceived
    |> Runtime.filterEvent deviceInfo
  
  let sysexReceived =
    midiPlatform.SysexReceived
    |> Runtime.filterEvent deviceInfo

  let systemMessageReceived =
    midiPlatform.SystemMessageReceived
    |> Runtime.filterEvent deviceInfo

  let channelMessageReceived =
    midiPlatform.ChannelMessageReceived
    |> Runtime.filterEvent deviceInfo
  
  let error =
    midiPlatform.Error
    |> Runtime.filterEvent deviceInfo

  member x.DeviceInfo = deviceInfo
  member x.Open bufferSize timeProc =
    if isOpen () then ()
    else
      let driverInfo = IntPtr.Zero
      let timeInfo = IntPtr.Zero
      Platform.Pm_OpenInput &stream deviceInfo.DeviceId driverInfo bufferSize timeProc timeInfo |> Runtime.checkError deviceInfo platform
      if isOpen() then
        Runtime.registerOpenInputDevice deviceInfo stream

  interface IMidiInput<'timestamp> with
    [<CLIEvent>] member x.Error = error
    [<CLIEvent>] member x.ChannelMessageReceived = channelMessageReceived
    [<CLIEvent>] member x.SystemMessageReceived = systemMessageReceived
    [<CLIEvent>] member x.SysexReceived = sysexReceived
    [<CLIEvent>] member x.RealtimeMessageReceived = realtimeMessageReceived
    member x.DeviceInfo = this.DeviceInfo :> _
    member x.Open bufferSize =
      //let timeProc = PmTimeProc(fun _ -> timeProc)
      this.Open bufferSize pmTimeProc

    member x.Close () =
      if not (isOpen ()) then ()
      else
        Runtime.discardOpenInputDevice deviceInfo
        Platform.Pm_Close stream |> Runtime.checkError deviceInfo platform
        stream <- IntPtr.Zero

type MidiOutput(deviceInfo: MidiDeviceInfo, pmTimeProc:PmTimeProc, platform: MidiPlatformTrigger<_,MidiEvent<int>,int>) as this =
  let mutable stream = IntPtr.Zero
  let isOpen () = stream <> IntPtr.Zero
  member x.DeviceInfo = deviceInfo

  member x.Open bufferSize latency timeProc =
    if isOpen () then ()
    else
      let driverInfo = IntPtr.Zero
      let timeInfo = IntPtr.Zero
      Platform.Pm_OpenOutput &stream deviceInfo.DeviceId driverInfo bufferSize timeProc timeInfo latency |> Runtime.checkError deviceInfo platform
  
  interface IMidiOutput<int> with
    member x.DeviceInfo = this.DeviceInfo :> _
    member __.Open bufferSize latency =
      this.Open bufferSize latency pmTimeProc

    member __.Close () =
      if not (isOpen ()) then ()
      else
        Platform.Pm_Close stream |> Runtime.checkError deviceInfo platform
        stream <- IntPtr.Zero

    member x.WriteMessages timestamp (messages: MidiMessage array) =
      let events =
        messages
        |> Array.map (fun m ->
          let mutable e = Unchecked.defaultof<PmEvent>
          e.Timestamp <- timestamp
          e.Message <- m.Word
          e)
      Platform.Pm_Write stream events (Array.length messages) |> Runtime.checkError deviceInfo platform

    member x.WriteMessage timestamp (message: MidiMessage) =
      Platform.Pm_WriteShort stream timestamp message.Word |> Runtime.checkError deviceInfo platform

    member x.WriteSysex timestamp data =
      Platform.Pm_WriteSysEx stream timestamp data |> Runtime.checkError deviceInfo platform


type PortMidiPlatform() =
    let platform = MidiPlatformTrigger<_,_,_>()
    member x.Platform = platform
    member x.MidiPlatform = platform :> IMidiPlatform<_,_,_>
    member x.Now = Runtime.ptGetTime.Invoke IntPtr.Zero
    member x.GetNow () = x.Now 
    
    member x.InputDevices =
        Runtime.getDevices()
        |> Array.filter (fun d -> d.SupportsInput)

    member x.OutputDevices =
        Runtime.getDevices()
        |> Array.filter (fun d -> d.SupportsOutput)

    member x.OpenOutputDevice bufferSize latency deviceInfo =
        let mOut = MidiOutput(deviceInfo, Runtime.ptGetTime, platform) :> IMidiOutput<_>
        mOut.Open bufferSize latency
        mOut

    member x.OpenInputDevice bufferSize deviceInfo =
        let mIn = MidiInput(deviceInfo, Runtime.ptGetTime, platform) :> IMidiInput<_>
        mIn.Open bufferSize
        mIn