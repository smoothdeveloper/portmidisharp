namespace PortMidi
open PortMidi.Native
open System.Collections.Generic
open System.Runtime.InteropServices
open System
open System.IO

type MidiDeviceInfo internal (id: int, inner: PmDeviceInfo) =
  let interfaceName = Marshal.PtrToStringAnsi inner.Interface
  let name = Marshal.PtrToStringAnsi inner.Name
  member x.DeviceId = id
  member x.InterfaceName = interfaceName
  member x.Name = name
  member x.SupportsInput = inner.Input = 1
  member x.SupportsOutput = inner.Output = 1
  override x.ToString () = sprintf "Device %i: %s, %s %s%s" id name interfaceName (if x.SupportsInput then "Input" else "") (if x.SupportsOutput then "Output" else "")

type MidiNote =
| C      = 0x0uy
| CSharp = 0x1uy
| D      = 0x2uy
| DSharp = 0x3uy
| E      = 0x4uy
| F      = 0x5uy
| FSharp = 0x6uy
| G      = 0x7uy
| GSharp = 0x8uy
| A      = 0x9uy
| ASharp = 0xauy
| B      = 0xbuy

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
open MidiMessageTypeIdentifaction

type [<Struct>] MidiMessage private(value:int) =
  static member StatusWithChannel (messageType: MidiMessageType) channel = byte messageType + channel
  static member NoteWithOctave (note: MidiNote) (octave: byte) = (octave * 12uy) + (byte note)
  static member GetNoteAndOctave (midiNoteNumber: byte) =
    let octave = midiNoteNumber / 12uy
    let note : MidiNote = midiNoteNumber % 12uy |> LanguagePrimitives.EnumOfValue
    note, octave

  static member Encode (status: byte) (data1: byte) (data2: byte) =
    MidiMessage( 
      (((int data2) <<< 16) &&& 0xff0000)
      ||| (((int data1) <<< 8) &&& 0xff00)
      ||| ((int status) &&& 0xff)
    )

  static member EncodeChannelMessage (messageType: MidiMessageType) (channel: byte) (data1: byte) (data2: byte) =
    MidiMessage.Encode (MidiMessage.StatusWithChannel messageType channel) data1 data2

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
        let noteName =
          match note with
          | MidiNote.A | MidiNote.B | MidiNote.C | MidiNote.D | MidiNote.E | MidiNote.F | MidiNote.G -> note.ToString()
          | MidiNote.ASharp -> "A#"
          | MidiNote.CSharp -> "C#"
          | MidiNote.DSharp -> "D#"
          | MidiNote.FSharp -> "F#"
          | MidiNote.GSharp -> "G#"
          | _ -> failwithf "note %i" (byte note)
            
        sprintf "%A (channel:%i) note (%i): %s octave: %i velocity %i" x.MessageType x.Channel.Value x.Data1 noteName octave x.Data2
      | _ ->
        sprintf "%A (channel:%i) %i %i" x.MessageType x.Channel.Value x.Data1 x.Data2
    else
      sprintf "%A %i %i" ((x.Status |> LanguagePrimitives.EnumOfValue): MidiMessageType) x.Data1 x.Data2

type [<Struct>] MidiEvent (message: MidiMessage, timestamp: int) =
  member __.Message = message
  member __.Timestamp = timestamp

type internal OpenedDevice(device: MidiDeviceInfo, stream: IntPtr) =
    let mutable sysex  : MemoryStream = Unchecked.defaultof<_>
    member __.Stream = stream
    member __.Device = device
    member __.BeginSysex () = sysex <- new MemoryStream(); sysex.WriteByte 0xf0uy;
    member __.DisposeSysex () = sysex.Dispose(); sysex <- null
    member __.SysexInProgress = not (isNull sysex)
    member __.WriteSysexByte b = sysex.WriteByte b
    member __.SysexData = sysex.ToArray()

module Runtime =
  open Platform
  let internal sizeOfEvent = Marshal.SizeOf<PmEvent>()
  let init () = Pm_Initialize()
  let terminate () = Pm_Terminate()

  let inline internal getDeviceById id =
    let deviceInfo : PmDeviceInfo = Pm_GetDeviceInfo id |> Marshal.PtrToStructure
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

  let internal inputDeviceGate = obj()
  let internal inputDevices = Dictionary<int,OpenedDevice>()
  let internal registerOpenInputDevice deviceInfo stream =
    let d = OpenedDevice(deviceInfo, stream)
    lock inputDeviceGate (fun () -> inputDevices.[deviceInfo.DeviceId] <- d)
  let internal discardOpenInputDevice (deviceInfo: MidiDeviceInfo) =
    lock inputDeviceGate (fun () -> inputDevices.Remove deviceInfo.DeviceId) |> ignore

  let Error = new Event<_>()
  let RealtimeMessageReceived = new Event<_>()
  let SystemMessageReceived = new Event<_>()
  let ChannelMessageReceived = new Event<_>()
  let SysexReceived = new Event<_>()

  let internal getMessageType message : MidiMessageType =
    if message <= 14 then (message &&& 240) else (message &&& 255)
    |> byte
    |> LanguagePrimitives.EnumOfValue 

  let internal completeSysex (device: OpenedDevice) =
    if device.SysexData.Length > 5 then
      (device.Device, device.SysexData) |> SysexReceived.Trigger
    device.DisposeSysex ()

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
  
  let internal processEvents (device: OpenedDevice) (events: PmEvent array) =
    for e in events do
      let messageType = getMessageType e.Message
      if isRealtimeMessage messageType then
        (device.Device, MidiEvent(MidiMessage.FromWord e.Message, e.Timestamp)) |> RealtimeMessageReceived.Trigger
      elif device.SysexInProgress && messageType <> MidiMessageType.SysEx then
          processSysexMessage device e.Message
      elif isSystemMessage messageType then
        if messageType = MidiMessageType.SysEx then
          if device.SysexInProgress then
            // accomodate for incomplete sysex
            completeSysex device
          device.BeginSysex()
          processSysexMessage device e.Message
        else
          (device.Device, MidiEvent(MidiMessage.FromWord e.Message, e.Timestamp)) |> SystemMessageReceived.Trigger 
      else
        (device.Device, MidiEvent(MidiMessage.FromWord e.Message, e.Timestamp)) |> ChannelMessageReceived.Trigger

  let internal read stream bufferSize = 
    let buffer = Marshal.AllocHGlobal (sizeOfEvent * bufferSize)
    let result = 
      match Pm_Read stream buffer bufferSize with
      | count when count >= 0 ->
        let mutable ptr = buffer
        Array.init count (fun _ ->
          let r : PmEvent = Marshal.PtrToStructure ptr
          ptr <- ptr + (nativeint sizeOfEvent)
          r) |> Choice1Of2
      | err -> Choice2Of2 (getErrorText (err |> LanguagePrimitives.EnumOfValue))
    Marshal.FreeHGlobal buffer
    result
      
  let processMidiEvents (readBufferSize: int) =
      let devices = lock (inputDeviceGate) (fun () -> inputDevices.Values |> Seq.toArray)
      for d in devices do
        if hasHostError (d) then
          Error.Trigger (d.Device, getHostErrorText ())

        match Platform.Pm_Poll(d.Stream) with
        | PmError.GotData -> 
          match read d.Stream readBufferSize with
          | Choice2Of2 message -> Error.Trigger(d.Device, message)
          | Choice1Of2 events ->
            processEvents d events
        | PmError.NoData -> ()
        | err ->
          Error.Trigger(d.Device, getErrorText err)

  let threadHandler (pollInterval: TimeSpan) (readBufferSize: int) =
    while true do
      processMidiEvents readBufferSize
      Thread.Sleep pollInterval

  let internal checkError deviceInfo errnum =
    match errnum with
    | PmError.NoError -> ()
    | _ -> Error.Trigger (deviceInfo, (Platform.Pm_GetErrorText errnum |> Marshal.PtrToStringAnsi))

  let inline internal refEqual a b = Object.ReferenceEquals(a, b)
  let inline internal filterEvent deviceInfo e =
    e
    |> Event.filter (fst >> (refEqual deviceInfo))
    |> Event.map snd
       
type MidiInput(deviceInfo: MidiDeviceInfo) =
  let mutable stream = IntPtr.Zero
  let isOpen () = stream <> IntPtr.Zero

  let realtimeMessageReceived =
    Runtime.RealtimeMessageReceived.Publish
    |> Runtime.filterEvent deviceInfo
  
  let sysexReceived =
    Runtime.SysexReceived.Publish
    |> Runtime.filterEvent deviceInfo

  let systemMessageReceived =
    Runtime.SystemMessageReceived.Publish
    |> Runtime.filterEvent deviceInfo

  let channelMessageReceived =
    Runtime.ChannelMessageReceived.Publish
    |> Runtime.filterEvent deviceInfo
  
  let error =
    Runtime.Error.Publish
    |> Runtime.filterEvent deviceInfo

  [<CLIEvent>] member x.Error = error
  [<CLIEvent>] member x.ChannelMessageReceived = channelMessageReceived
  [<CLIEvent>] member x.SystemMessageReceived = systemMessageReceived
  [<CLIEvent>] member x.SysexReceived = sysexReceived
  [<CLIEvent>] member x.RealtimeMessageReceived = realtimeMessageReceived

  member x.Open bufferSize timeProc =
    if isOpen () then ()
    else
      let driverInfo = IntPtr.Zero
      let timeInfo = IntPtr.Zero
      Platform.Pm_OpenInput &stream deviceInfo.DeviceId driverInfo bufferSize timeProc timeInfo |> Runtime.checkError deviceInfo
      if isOpen() then
        Runtime.registerOpenInputDevice deviceInfo stream

  member x.Close () =
    if not (isOpen ()) then ()
    else
      Runtime.discardOpenInputDevice deviceInfo
      Platform.Pm_Close stream |> Runtime.checkError deviceInfo
      stream <- IntPtr.Zero

type MidiOutput(deviceInfo: MidiDeviceInfo) =
  let mutable stream = IntPtr.Zero
  let isOpen () = stream <> IntPtr.Zero
  member x.Open bufferSize latency timeProc =
    if isOpen () then ()
    else
      let driverInfo = IntPtr.Zero
      let timeInfo = IntPtr.Zero
      Platform.Pm_OpenOutput &stream deviceInfo.DeviceId driverInfo bufferSize timeProc timeInfo latency |> Runtime.checkError deviceInfo
  
  member __.Close () =
    if not (isOpen ()) then ()
    else
      Platform.Pm_Close stream |> Runtime.checkError deviceInfo
      stream <- IntPtr.Zero

  member x.WriteMessages timestamp (messages: MidiMessage array) =
    let events =
      messages
      |> Array.map (fun m ->
        let mutable e = Unchecked.defaultof<PmEvent>
        e.Timestamp <- timestamp
        e.Message <- m.Word
        e)
    Platform.Pm_Write stream events (Array.length messages) |> Runtime.checkError deviceInfo

  member x.WriteMessage timestamp (message: MidiMessage) =
    Platform.Pm_WriteShort stream timestamp message.Word |> Runtime.checkError deviceInfo

  member x.WriteSysex timestamp data =
    Platform.Pm_WriteSysEx stream timestamp data |> Runtime.checkError deviceInfo


