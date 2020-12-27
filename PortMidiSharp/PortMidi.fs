namespace rec PortMidi
open MidiMessageTypeIdentifaction
open PortMidi.Native
open System.Collections.Generic
open System.Runtime.InteropServices
open System
open System.IO

type MidiDeviceInfo internal (id: int, inner: PmDeviceInfo) as _this =
  let interfaceName = Marshal.PtrToStringAnsi inner.Interface
  let name = Marshal.PtrToStringAnsi inner.Name
  member x.DeviceId = id
  member x.InterfaceName = interfaceName
  member x.Name = name
  member x.SupportsInput = inner.Input = 1
  member x.SupportsOutput = inner.Output = 1
  override x.ToString () = sprintf "Device %i: %s, %s %s%s" id name interfaceName (if x.SupportsInput then "Input" else "") (if x.SupportsOutput then "Output" else "")

type MidiMessageType =
| NoteOff                  = 0x80uy
| NoteOn                   = 0x90uy
| PolyKeyPressure          = 0xa0uy
| ControllerChange         = 0xb0uy
| ProgramChange            = 0xc0uy
| ChannelPressure          = 0xd0uy
| PitchBendChange          = 0xe0uy
| SysEx                    = 240uy
| MidiTimeCodeQuarterFrame = 241uy
| SongPositionPointer      = 242uy
| SongSelect               = 243uy
| TuneRequest              = 246uy
| SysExEnd                 = 247uy
| TimingClock              = 248uy
| Start                    = 250uy
| Continue                 = 251uy
| Stop                     = 252uy
| ActiveSensing            = 254uy
| SystemReset              = 255uy
module MidiMessageTypeIdentifaction =
  let inline isRealtimeMessage messageType = messageType >= MidiMessageType.TimingClock && messageType <= MidiMessageType.SystemReset
  let inline isSystemMessage messageType = messageType >= MidiMessageType.SysEx && messageType <= MidiMessageType.SysExEnd
  let inline isChannelMessage messageType = messageType >= MidiMessageType.NoteOff && messageType <= (LanguagePrimitives.EnumOfValue 239uy)
  let inline isSysexBeginOrEnd messageType = messageType = MidiMessageType.SysEx || messageType = MidiMessageType.SysExEnd


type [<Struct>] MidiMessage private(value:int) =
  static member StatusWithChannel (messageType: MidiMessageType) channel = byte messageType + channel
  (*
  static member NoteWithOctave (note: MidiNote) (octave: byte) = (octave * 12uy) + (byte note)
  *)
  static member GetNoteAndOctave (midiNoteNumber: byte) =
    let octave = midiNoteNumber / 12uy
    let note = midiNoteNumber % 12uy
    note, octave
    

  static member Encode (status: byte) (data1: byte) (data2: byte) =
    MidiMessage( 
      (((int data2) <<< 16) &&& 0xff0000)
      ||| (((int data1) <<< 8) &&& 0xff00)
      ||| ((int status) &&& 0xff)
    )

  static member EncodeChannelMessage (messageType: MidiMessageType) (channel: byte) (data1: byte) (data2: byte) =
    MidiMessage.Encode (MidiMessage.StatusWithChannel messageType channel) data1 data2
  static member NoteOn channel note velocity  = MidiMessage.EncodeChannelMessage MidiMessageType.NoteOn channel note velocity
  static member NoteOff channel note velocity = MidiMessage.EncodeChannelMessage MidiMessageType.NoteOff channel note velocity
  static member CC channel control value      = MidiMessage.EncodeChannelMessage MidiMessageType.ControllerChange channel control value
  static member FromWord word = MidiMessage word
  member x.Word = value
  member x.Status = byte (value &&& 0xff)
  member x.Data1 = byte ((value >>> 8) &&& 0xff)
  member x.Data2 = byte ((value >>> 16) &&& 0xff)
  member x.IsChannelMessage = (x.Status |> LanguagePrimitives.EnumOfValue) |> isChannelMessage
  member x.MessageType : MidiMessageType =
    let messageType =
      if x.IsChannelMessage then
        (x.Status &&& 0b11110000uy)
      else
        x.Status
    messageType |> LanguagePrimitives.EnumOfValue
    
  member x.Channel =
    if x.IsChannelMessage then
      Some (x.Status &&& 0b00001111uy)
    else
      None
  override x.ToString () = 
    if x.IsChannelMessage then
      match x.MessageType with
      | MidiMessageType.NoteOn | MidiMessageType.NoteOff ->
        let note, octave = MidiMessage.GetNoteAndOctave x.Data1
        let notes = [|"C";"C#";"D";"D#";"E";"F";"F#";"G";"G#";"A";"A#";"B"|]
        let noteName = if notes.Length > int note then notes.[int note] else failwithf "note %i" (byte note)
            
        sprintf "%20s (channel:%02i) note (%03i): %2s octave: %i velocity %i" (string x.MessageType) x.Channel.Value x.Data1 noteName octave x.Data2
      | _ ->
        sprintf "%20s (channel:%02i) %03i %03i" (string x.MessageType) x.Channel.Value x.Data1 x.Data2
    else
      sprintf "%A %i %i" ((x.Status |> LanguagePrimitives.EnumOfValue): MidiMessageType) x.Data1 x.Data2

type [<Struct>] MidiEvent (message: MidiMessage, timestamp: int) =
  member __.Message = message
  member __.Timestamp = timestamp

type MidiPlatformTrigger() =
  let error                   = new Event<_>()
  let realtimeMessageReceived = new Event<_>()
  let sysexReceived           = new Event<_>()
  let channelMessageReceived  = new Event<_>()
  let systemMessageReceived   = new Event<_>()
  member x.NoticeError(d,m)            = error.Trigger(d,m)
  member x.NoticeRealtimeMessage(d, m) = realtimeMessageReceived.Trigger (d,m)
  member x.NoticeSysex(d, m)           = sysexReceived.Trigger (d,m)
  member x.NoticeSystemMessage(d, m)   = systemMessageReceived.Trigger(d,m)
  member x.NoticeChannelMessage(d, m)  = channelMessageReceived.Trigger(d,m)
  [<CLIEvent>] member x.Error = error.Publish
  [<CLIEvent>] member x.ChannelMessageReceived  = channelMessageReceived.Publish
  [<CLIEvent>] member x.SystemMessageReceived   = systemMessageReceived.Publish
  [<CLIEvent>] member x.SysexReceived           = sysexReceived.Publish
  [<CLIEvent>] member x.RealtimeMessageReceived = realtimeMessageReceived.Publish



type 
  internal
  OpenedDevice(device: MidiDeviceInfo, stream: IntPtr) as _this =
    let mutable sysex  : MemoryStream = Unchecked.defaultof<_>
    member __.Stream = stream
    member __.Device = device
    member __.BeginSysex () = sysex <- new MemoryStream(); sysex.WriteByte 0xf0uy;
    member __.DisposeSysex () = sysex.Dispose(); sysex <- null
    member __.SysexInProgress = not (isNull sysex)
    member __.WriteSysexByte b = sysex.WriteByte b
    member __.SysexData = sysex.ToArray()

module Interop =
  open Platform
  let getDeviceById id =
    let deviceInfo : PmDeviceInfo = Marshal.PtrToStructure(Pm_GetDeviceInfo id, typeof<PmDeviceInfo>) :?> _
    MidiDeviceInfo(id, deviceInfo)
  let internal getErrorText err = Pm_GetErrorText err |> Marshal.PtrToStringAnsi
  let internal hasHostError (device: OpenedDevice) = Pm_HasHostError device.Stream = 1

  let internal getHostErrorText () =
    let msg = Marshal.AllocHGlobal(256)
    Pm_GetHostErrorText msg 256u
    let text = Marshal.PtrToStringAnsi msg
    Marshal.FreeHGlobal msg
    text

module Runtime =
  open Interop
  open Platform
  let platform = new MidiPlatformTrigger()
  let pmTimeProc = Native.PmTimeProc(fun _ -> PortTime.Native.Platform.Pt_Time())

  let getDevices () = Array.init (Pm_CountDevices()) getDeviceById
  
  let defaultOutputDevice =
    let id = Pm_GetDefaultOutputDeviceID()
    if id < 0 then None else Some(getDeviceById id)

  let defaultInputDevice =
    let id = Pm_GetDefaultInputDeviceID()
    if id < 0 then None else Some(getDeviceById id)

  let internal inputDeviceGate = obj()
  let internal inputDevices = Dictionary<int,OpenedDevice>()
  let internal registerOpenInputDevice deviceInfo stream = 
    let d = OpenedDevice(deviceInfo, stream)
    lock inputDeviceGate (fun () -> inputDevices.[deviceInfo.DeviceId] <- d)
  let internal discardOpenInputDevice (deviceInfo: MidiDeviceInfo) =
    lock inputDeviceGate (fun () -> inputDevices.Remove deviceInfo.DeviceId) |> ignore

  let getMessageType message : MidiMessageType =
    if message <= 14 then (message &&& 240) else (message &&& 255)
    |> byte
    |> LanguagePrimitives.EnumOfValue
    
  let internal completeSysex (device: OpenedDevice) =
    let sysexInput = device
    if sysexInput.SysexData.Length > 5 then
      (device.Device, sysexInput.SysexData) |> platform.NoticeSysex
    sysexInput.DisposeSysex ()
    
  let internal processSysexMessage (device: OpenedDevice) message =
    let mutable endEncountered = false
    for i in 0 .. 3 do
      if not endEncountered then
        let b = (message >>> (i * 8)) &&& 255 |> byte
        if b < 128uy || b = 247uy then
          device.WriteSysexByte b
          if b = 247uy then
            completeSysex device
            endEncountered <- true
          
  let internal genrocessEvents (deviceInfo: OpenedDevice) (events: 'event array) (platform: MidiPlatformTrigger) makeMidiEvent getMessageWord =
    for e in events do
      let word : int = getMessageWord e
      let messageType = getMessageType word
      let midiEvent = makeMidiEvent e
      if isRealtimeMessage messageType then
        (deviceInfo.Device, midiEvent) |> platform.NoticeRealtimeMessage
      elif deviceInfo.SysexInProgress && messageType <> MidiMessageType.SysEx then
        processSysexMessage deviceInfo word
      elif isSystemMessage messageType then
        if messageType = MidiMessageType.SysEx then
          if deviceInfo.SysexInProgress then
            // accomodate for incomplete sysex
            completeSysex deviceInfo
          deviceInfo.BeginSysex()
          processSysexMessage deviceInfo word
        else
          (deviceInfo.Device, midiEvent) |> platform.NoticeSystemMessage
      else
        (deviceInfo.Device, midiEvent) |> platform.NoticeChannelMessage
  
  let internal processEvents (device: OpenedDevice) (events: PmEvent array) =
      genrocessEvents
        device 
        events 
        platform
        (fun m -> MidiEvent(MidiMessage.FromWord m.Message, m.Timestamp)) 
        (fun m -> m.Message)
      
  let internal sizeOfEvent = Marshal.SizeOf(typeof<PmEvent>)

  #if NO_FSHARP_RESULT_TYPE
  type Result<'r,'e> = Ok of 'r | Error of 'e
  #endif

  let read stream bufferSize = 
    let buffer = Marshal.AllocHGlobal (sizeOfEvent * bufferSize)
    let result = 
      match Pm_Read stream buffer bufferSize with
      | count when count >= 0 ->
        let mutable ptr = buffer
        Array.init count (fun _ ->
          let r : PmEvent = Marshal.PtrToStructure(ptr, typeof<PmEvent>) :?> _
          ptr <- ptr + (nativeint sizeOfEvent)
          r) |> Result.Ok
      | err -> Result.Error (getErrorText (err |> LanguagePrimitives.EnumOfValue))
    Marshal.FreeHGlobal buffer
    result
      
  let processMidiEvents (readBufferSize: int) 
    (noticeError)
    =
      let devices = lock (inputDeviceGate) (fun () -> inputDevices.Values |> Seq.toArray)
      for d in devices do
        if hasHostError (d) then
          noticeError(d.Device, getHostErrorText ())

        match Platform.Pm_Poll(d.Stream) with
        | PmError.GotData -> 
          match read d.Stream readBufferSize with
          | Result.Error message -> noticeError(d.Device, message)
          | Result.Ok events ->
            processEvents d events 
        | PmError.NoData -> ()
        | err ->
          noticeError(d.Device, getErrorText err)

  let threadHandler (pollInterval: TimeSpan) (readBufferSize: int) =
    while true do
      processMidiEvents readBufferSize platform.NoticeError
      System.Threading.Thread.Sleep pollInterval

  let internal checkError deviceInfo noticeError errnum =
    match errnum with
    | PmError.NoError -> ()
    | _ -> noticeError (deviceInfo, (Platform.Pm_GetErrorText errnum |> Marshal.PtrToStringAnsi))

  let inline internal refEqual a b = Object.ReferenceEquals(a, b)
  

  let inline internal filterEvent (deviceInfo: MidiDeviceInfo) (e: IEvent<_>) =
    e
    |> Event.filter (fun (device: MidiDeviceInfo,_) -> device.DeviceId = deviceInfo.DeviceId)
    |> Event.map snd

  let getDevice (nameFilter: string) isInput =
    let devices = getDevices()
    devices |> Seq.tryFind (fun d -> d.Name.Contains nameFilter && d.SupportsInput = isInput && d.SupportsOutput = not isInput)       
  

  let midiCallback = Event<_>()
  let callbackCounter = ref 0
  
  let callback 
    noticeError
    (timestamp: PortTime.Native.PtTimestamp) (data : IntPtr)
    = 
    callbackCounter.Value <- callbackCounter.Value + 1
    processMidiEvents 4096 noticeError
    midiCallback.Trigger timestamp

    
    
    
  let makePtCallback
    noticeError
    =
    let ptCallback = PortTime.Native.PtCallback(callback noticeError)
    PortTime.Native.Platform.Pt_Start 1 ptCallback IntPtr.Zero |> ignore
    let noticeError (device: MidiDeviceInfo, m) = 
      if device.SupportsInput then
        printfn "ERROR INPUT %s %s" device.Name  m
      if device.SupportsOutput then
        printfn "ERROR OUTPUT %s %s" device.Name  m
    let midiPlatform = platform
    midiPlatform.Error.Add noticeError
    ptCallback

  let ptGetTime = Native.PmTimeProc(fun data -> PortTime.Native.Platform.Pt_Time())

type MidiInput(deviceInfo: MidiDeviceInfo
  , pmTimeProc:PmTimeProc
  ) as _this =
  let mutable stream = IntPtr.Zero
  let isOpen () = stream <> IntPtr.Zero
  let platform = Runtime.platform 
  let midiPlatform = Runtime.platform 
  let noticeError = platform.NoticeError
  let realtimeMessageReceived = midiPlatform.RealtimeMessageReceived |> Runtime.filterEvent deviceInfo
  let sysexReceived           = midiPlatform.SysexReceived           |> Runtime.filterEvent deviceInfo
  let systemMessageReceived   = midiPlatform.SystemMessageReceived   |> Runtime.filterEvent deviceInfo
  let channelMessageReceived  = midiPlatform.ChannelMessageReceived  |> Runtime.filterEvent deviceInfo
  let error                   = midiPlatform.Error                   |> Runtime.filterEvent deviceInfo

  let openPort bufferSize =
    if isOpen () then ()
    else
      let driverInfo = IntPtr.Zero
      let timeInfo = IntPtr.Zero
      
      Platform.Pm_OpenInput &stream deviceInfo.DeviceId driverInfo bufferSize pmTimeProc timeInfo |> Runtime.checkError deviceInfo noticeError
      if isOpen() then
        Runtime.registerOpenInputDevice deviceInfo stream
  let closePort () =
    if not (isOpen ()) then ()
    else
      Runtime.discardOpenInputDevice deviceInfo
      Platform.Pm_Close stream |> Runtime.checkError deviceInfo noticeError
      stream <- IntPtr.Zero    
  member x.DeviceInfo = deviceInfo

  [<CLIEvent>] member x.Error                   = error
  [<CLIEvent>] member x.ChannelMessageReceived  = channelMessageReceived
  [<CLIEvent>] member x.SystemMessageReceived   = systemMessageReceived
  [<CLIEvent>] member x.SysexReceived           = sysexReceived
  [<CLIEvent>] member x.RealtimeMessageReceived = realtimeMessageReceived
  
  member x.Open bufferSize = openPort bufferSize
  member x.Close ()        = closePort ()


type MidiOutput(deviceInfo: MidiDeviceInfo, pmTimeProc:PmTimeProc) as _this =
  let mutable stream = IntPtr.Zero
  let isOpen () = stream <> IntPtr.Zero

  let noticeError = Runtime.platform.NoticeError

  let writeMessage timestamp (message: MidiMessage) = Platform.Pm_WriteShort stream timestamp message.Word |> Runtime.checkError deviceInfo noticeError
  let writeMessages timestamp (messages: MidiMessage array) = 
    let events = messages |> Array.map (fun m -> PmEvent(Timestamp = timestamp, Message = m.Word))
    Platform.Pm_Write stream events (Array.length messages) |> Runtime.checkError deviceInfo noticeError
  let openPort bufferSize latency timeProc =
    if isOpen () then ()
    else
      let driverInfo = IntPtr.Zero
      let timeInfo = IntPtr.Zero
      Platform.Pm_OpenOutput &stream deviceInfo.DeviceId driverInfo bufferSize timeProc timeInfo latency |> Runtime.checkError deviceInfo noticeError
  let closePort () =
    if not (isOpen ()) then ()
    else
      Platform.Pm_Close stream |> Runtime.checkError deviceInfo noticeError
      stream <- IntPtr.Zero

  member x.DeviceInfo = deviceInfo

  member x.Open          bufferSize latency  = openPort bufferSize latency pmTimeProc
  member x.Close ()                          = closePort ()
  member x.WriteMessage  timestamp  message  = writeMessage timestamp message
  member x.WriteMessages timestamp  messages = writeMessages timestamp messages
  member x.WriteSysex timestamp data         = Platform.Pm_WriteSysEx stream timestamp data |> Runtime.checkError deviceInfo noticeError

  
type PortMidiPlatform() =
    let platform = MidiPlatformTrigger()
    
    member x.Platform     = platform
    member x.Now          = Runtime.ptGetTime.Invoke IntPtr.Zero
    member x.GetNow () = x.Now 
    
    member x.InputDevices =
        Runtime.getDevices()
        |> Array.filter (fun d -> d.SupportsInput)

    member x.OutputDevices =
        Runtime.getDevices()
        |> Array.filter (fun d -> d.SupportsOutput)

    member x.OpenOutputDevice bufferSize latency deviceInfo =
        let mOut = MidiOutput(deviceInfo, Runtime.ptGetTime)
        mOut.Open bufferSize latency
        mOut

    member x.OpenInputDevice bufferSize deviceInfo =
        let mIn = MidiInput(deviceInfo,  Runtime.ptGetTime)        
        mIn.Open bufferSize
        mIn

