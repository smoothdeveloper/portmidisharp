namespace PortMidiSharp.Native

open System
open System.Runtime.InteropServices

type [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>] PmTimeProc = delegate of IntPtr -> int

type PmError =
| NoError            = 0
| NoData             = 0
| GotData            = 1
| HostError          = -10000
| InvalidDeviceId    = -9999
| InsufficientMemory = -9998
| BufferTooSmall     = -9997
| BufferOverflow     = -9996
| BadPointer         = -9995
| BadData            = -9994
| InternalError      = -9993
| BufferMaxSize      = -9992

type [<Struct>] PmDeviceInfo =
  val mutable StructVersion : int
  val mutable Interface     : IntPtr
  val mutable Name          : IntPtr
  val mutable Input         : int
  val mutable Output        : int
  val mutable Opened        : int

type [<Struct>] PmEvent =
  val mutable Message   : int
  val mutable Timestamp : int

module Platform32 =
  let [<Literal>] dllName = "portmidi_x86.dll"
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Abort                    (IntPtr stream)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Close                    (IntPtr stream)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_CountDevices             ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_GetDefaultInputDeviceID  ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_GetDefaultOutputDeviceID ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern IntPtr  Pm_GetDeviceInfo            (int id)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern IntPtr  Pm_GetErrorText             (PmError errnum)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern unit    Pm_GetHostErrorText         (IntPtr msg, uint32 len)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_HasHostError             (IntPtr stream)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Initialize               ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_OpenInput                (IntPtr& stream, int inputDevice, IntPtr inputDriverInfo, int bufferSize, [<MarshalAs(UnmanagedType.FunctionPtr)>] PmTimeProc timeProc, IntPtr timeInfo)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_OpenOutput               (IntPtr& stream, int outputDevice, IntPtr outputDriverInfo, int bufferSize, [<MarshalAs(UnmanagedType.FunctionPtr)>] PmTimeProc timeProc, IntPtr timeInfo, int latency)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Poll                     (IntPtr stream)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_Read                     (IntPtr stream, IntPtr buffer, int length)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_SetChannelMask           (IntPtr stream, int mask)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_SetFilter                (IntPtr stream, int filters)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Terminate                ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Write                    (IntPtr stream, PmEvent[] buffer, int length)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_WriteShort               (IntPtr stream, int timestamp, int msg)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_WriteSysEx               (IntPtr stream, int timestamp, byte[] msg)

module Platform64 =
  let [<Literal>] dllName = "portmidi_x64.dll"
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Abort                    (IntPtr stream)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Close                    (IntPtr stream)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_CountDevices             ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_GetDefaultInputDeviceID  ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_GetDefaultOutputDeviceID ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern IntPtr  Pm_GetDeviceInfo            (int id)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern IntPtr  Pm_GetErrorText             (PmError errnum)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern unit    Pm_GetHostErrorText         (IntPtr msg, uint32 len)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_HasHostError             (IntPtr stream)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Initialize               ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_OpenInput                (IntPtr& stream, int inputDevice, IntPtr inputDriverInfo, int bufferSize, [<MarshalAs(UnmanagedType.FunctionPtr)>] PmTimeProc timeProc, IntPtr timeInfo)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_OpenOutput               (IntPtr& stream, int outputDevice, IntPtr outputDriverInfo, int bufferSize, [<MarshalAs(UnmanagedType.FunctionPtr)>] PmTimeProc timeProc, IntPtr timeInfo, int latency)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Poll                     (IntPtr stream)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern int     Pm_Read                     (IntPtr stream, IntPtr buffer, int length)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_SetChannelMask           (IntPtr stream, int mask)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_SetFilter                (IntPtr stream, int filters)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Terminate                ()
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_Write                    (IntPtr stream, PmEvent[] buffer, int length)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_WriteShort               (IntPtr stream, int timestamp, int msg)
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_WriteSysEx               (IntPtr stream, int timestamp, byte[] msg)

module Platform =
  let is64bit = Environment.Is64BitProcess

  let Pm_Abort stream                    = if is64bit then Platform64.Pm_Abort stream else Platform32.Pm_Abort stream
  let Pm_Close stream                    = if is64bit then Platform64.Pm_Close stream else Platform32.Pm_Close stream
  let Pm_CountDevices ()                 = if is64bit then Platform64.Pm_CountDevices () else Platform32.Pm_CountDevices ()
  let Pm_GetDefaultInputDeviceID ()      = if is64bit then Platform64.Pm_GetDefaultInputDeviceID () else Platform32.Pm_GetDefaultInputDeviceID ()
  let Pm_GetDefaultOutputDeviceID ()     = if is64bit then Platform64.Pm_GetDefaultOutputDeviceID () else Platform32.Pm_GetDefaultOutputDeviceID ()
  let Pm_GetDeviceInfo id                = if is64bit then Platform64.Pm_GetDeviceInfo id else Platform32.Pm_GetDeviceInfo id
  let Pm_GetErrorText errnum             = if is64bit then Platform64.Pm_GetErrorText errnum else Platform32.Pm_GetErrorText errnum
  let Pm_GetHostErrorText msg len        = if is64bit then Platform64.Pm_GetHostErrorText(msg, len) else Platform32.Pm_GetHostErrorText(msg, len)
  let Pm_HasHostError stream             = if is64bit then Platform64.Pm_HasHostError stream else Platform32.Pm_HasHostError stream
  let Pm_Initialize ()                   = if is64bit then Platform64.Pm_Initialize () else Platform32.Pm_Initialize ()
  let Pm_Poll stream                     = if is64bit then Platform64.Pm_Poll stream else Platform32.Pm_Poll stream
  let Pm_Read stream buffer length       = if is64bit then Platform64.Pm_Read(stream, buffer, length) else Platform32.Pm_Read(stream, buffer, length)
  let Pm_SetChannelMask stream mask      = if is64bit then Platform64.Pm_SetChannelMask(stream, mask) else Platform32.Pm_SetChannelMask(stream, mask)
  let Pm_SetFilter stream filters        = if is64bit then Platform64.Pm_SetFilter(stream, filters) else Platform32.Pm_SetFilter(stream, filters)
  let Pm_Terminate ()                    = if is64bit then Platform64.Pm_Terminate() else Platform32.Pm_Terminate()
  let Pm_Write stream buffer length      = if is64bit then Platform64.Pm_Write(stream, buffer, length) else Platform32.Pm_Write(stream, buffer, length)
  let Pm_WriteShort stream timestamp msg = if is64bit then Platform64.Pm_WriteShort(stream, timestamp, msg) else Platform32.Pm_WriteShort(stream, timestamp, msg)
  let Pm_WriteSysEx stream timestamp msg = if is64bit then Platform64.Pm_WriteSysEx(stream, timestamp, msg) else Platform32.Pm_WriteSysEx(stream, timestamp, msg)
  let Pm_OpenInput (stream: byref<IntPtr>) inputDevice inputDriverInfo bufferSize timeProc timeInfo = if is64bit then Platform64.Pm_OpenInput(&stream, inputDevice, inputDriverInfo, bufferSize, timeProc, timeInfo) else Platform32.Pm_OpenInput(&stream, inputDevice, inputDriverInfo, bufferSize, timeProc, timeInfo)
  let Pm_OpenOutput (stream: byref<IntPtr>) inputDevice inputDriverInfo bufferSize timeProc timeInfo latency = if is64bit then Platform64.Pm_OpenOutput(&stream, inputDevice, inputDriverInfo, bufferSize, timeProc, timeInfo, latency) else Platform32.Pm_OpenOutput(&stream, inputDevice, inputDriverInfo, bufferSize, timeProc, timeInfo, latency)

namespace PortMidiSharp
open PortMidiSharp.Native
open System.Collections.Generic
open System.Runtime.InteropServices
open System
open System.IO

type MidiDeviceInfo(id: int, inner: PmDeviceInfo) =
  let interfaceName = Marshal.PtrToStringAnsi inner.Interface
  let name = Marshal.PtrToStringAnsi inner.Name
  member x.DeviceId = id
  member x.InterfaceName = interfaceName
  member x.Name = name
  member x.SupportsInput = inner.Input = 1
  member x.SupportsOutput = inner.Output = 1
  override x.ToString () = sprintf "Device %i: %s, %s %s%s" id name interfaceName (if x.SupportsInput then "Input" else "") (if x.SupportsOutput then "Output" else "")

type OpenedDevice(device: MidiDeviceInfo, stream: IntPtr) =
    let mutable sysex  : MemoryStream = Unchecked.defaultof<_>
    member x.Stream = stream
    member x.Device = device
    member x.BeginSysex () = sysex <- new MemoryStream(); sysex.WriteByte 0xf0uy;
    member x.DisposeSysex () = sysex.Dispose(); sysex <- null
    member x.SysexInProgress = not (isNull sysex)
    member x.WriteSysexByte b = sysex.WriteByte b
    member x.SysexData = sysex.ToArray()

module PortMidi =
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

  let getErrorText err = Pm_GetErrorText err |> Marshal.PtrToStringAnsi

  let hasHostError (device: OpenedDevice) = Pm_HasHostError device.Stream = 1

  let getHostErrorText () =
    let msg = Marshal.AllocHGlobal(256)
    Pm_GetHostErrorText msg 256u
    let text = Marshal.PtrToStringAnsi msg
    Marshal.FreeHGlobal msg
    text

  let read stream bufferSize = 
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
            
        sprintf "%A (channel:%i) note (%i): %s octave: %i velocity %i" x.MessageType x.Channel.Value x.Data1 noteName octave x.Data2
      | _ ->
        sprintf "%A (channel:%i) %i %i" x.MessageType x.Channel.Value x.Data1 x.Data2
    else
      sprintf "%A %i %i" ((x.Status |> LanguagePrimitives.EnumOfValue): MidiMessageType) x.Data1 x.Data2


  
module Handling =
  open System.IO
  open System.Threading

  let internal gate = obj()
  let inputDevices = Dictionary<int,OpenedDevice>()
  let registerOpenInputDevice deviceInfo stream =
    let d = OpenedDevice(deviceInfo, stream)
    lock(gate) (fun () -> inputDevices.[deviceInfo.DeviceId] <- d)
  let discardOpenInputDevice (deviceInfo: MidiDeviceInfo) =
    lock(gate) (fun () -> inputDevices.Remove deviceInfo.DeviceId) |> ignore
  let Error = new Event<_>()
  let RealtimeMessageReceived = new Event<_>()
  let SystemMessageReceived = new Event<_>()
  let ChannelMessageReceived = new Event<_>()
  let SysexReceived = new Event<_>()

  let getMessageType message : MidiMessageType =
    if message <= 14 then (message &&& 240) else (message &&& 255)
    |> byte
    |> LanguagePrimitives.EnumOfValue 
  let processSysexMessage (device: OpenedDevice) message =
    let mutable endEncountered = false
    for i in 0 .. 3 do
      if not endEncountered then
        let b = (message >>> (i * 8)) &&& 255 |> byte
        if b < 128uy || b = 247uy then
          device.WriteSysexByte b
        if b = 247uy then
          (device.Device, device.SysexData) |> SysexReceived.Trigger
          device.DisposeSysex ()
          endEncountered <- true
  
  let processEvents (device: OpenedDevice) (events: PmEvent array) =
    for e in events do
      let messageType = getMessageType e.Message
      if isRealtimeMessage messageType then
        (device.Device, MidiMessage.FromWord e.Message) |> RealtimeMessageReceived.Trigger
      elif device.SysexInProgress then
        //if e.Message < 127 then
          processSysexMessage device e.Message
        //else
        //  device.DisposeSysex()
      elif isSystemMessage messageType then
        if messageType = MidiMessageType.SysEx then
          device.BeginSysex()
          processSysexMessage device e.Message
        else
          (device.Device, MidiMessage.FromWord e.Message) |> SystemMessageReceived.Trigger 
      else
        (device.Device, MidiMessage.FromWord e.Message) |> ChannelMessageReceived.Trigger

  let readBufferSize = 256
  let threadHandler () =
    while true do
      let devices = lock (gate) (fun () -> inputDevices.Values |> Seq.toArray)
      for d in devices do
        if PortMidi.hasHostError (d) then
          Error.Trigger (d.Device, PortMidi.getHostErrorText ())

        match Platform.Pm_Poll(d.Stream) with
        | PmError.GotData -> 
          match PortMidi.read d.Stream readBufferSize with
          | Choice2Of2 message -> Error.Trigger(d.Device, message)
          | Choice1Of2 events ->
            processEvents d events
        | PmError.NoData -> ()
        | err ->
          Error.Trigger(d.Device, PortMidi.getErrorText err)
      Thread.Sleep (5)

  let checkError deviceInfo errnum =
    match errnum with
    | PmError.NoError -> ()
    | _ -> Error.Trigger (deviceInfo, (Platform.Pm_GetErrorText errnum |> Marshal.PtrToStringAnsi))
       
type MidiInput(deviceInfo: MidiDeviceInfo) =
  let mutable stream = IntPtr.Zero
  let isOpen () = stream <> IntPtr.Zero
  let disposables = ResizeArray<_>()

  let realtimeMessageReceived =
    Handling.RealtimeMessageReceived.Publish
    |> Event.filter (fst >> ((=) deviceInfo))
    |> Event.map snd
  
  let sysexReceived =
    Handling.SysexReceived.Publish
    |> Event.filter (fst >> ((=) deviceInfo))
    |> Event.map snd

  let systemMessageReceived =
    Handling.SystemMessageReceived.Publish
    |> Event.filter (fst >> ((=) deviceInfo))
    |> Event.map snd

  let channelMessageReceived =
    Handling.ChannelMessageReceived.Publish
    |> Event.filter (fst >> ((=) deviceInfo))
    |> Event.map snd
  
  let error =
    Handling.Error.Publish
    |> Event.filter (fst >> ((=) deviceInfo))
    |> Event.map snd

  [<CLIEvent>] member x.Error = error
  [<CLIEvent>] member x.ChannelMessageReceived = channelMessageReceived
  [<CLIEvent>] member x.SystemMessageReceived = systemMessageReceived
  [<CLIEvent>] member x.SysexReceived = sysexReceived
  [<CLIEvent>] member x.RealtimeMessageReceived = realtimeMessageReceived

  member x.Open bufferSize =
    if isOpen () then ()
    else
      let driverInfo = IntPtr.Zero
      let timeInfo = IntPtr.Zero
      let timeProc = null
      Platform.Pm_OpenInput &stream deviceInfo.DeviceId driverInfo bufferSize timeProc timeInfo |> Handling.checkError deviceInfo
      Handling.registerOpenInputDevice deviceInfo stream
      
      //.Subscribe(fun (d, e) -> () )) |> disposables.Add

  member x.Close () =
    if not (isOpen ()) then ()
    else
      Handling.discardOpenInputDevice deviceInfo
      Platform.Pm_Close stream |> Handling.checkError  deviceInfo
      stream <- IntPtr.Zero

type MidiOutput(deviceInfo: MidiDeviceInfo) =
  let mutable stream = IntPtr.Zero
  let isOpen () = stream <> IntPtr.Zero
  member x.Open bufferSize latency =
    if isOpen () then ()
    else
      let driverInfo = IntPtr.Zero
      let timeInfo = IntPtr.Zero
      let timeProc = null
      Platform.Pm_OpenOutput &stream deviceInfo.DeviceId driverInfo bufferSize timeProc timeInfo latency |> Handling.checkError deviceInfo
  
  member x.WriteMessages timestamp (messages: MidiMessage array) =
    let events =
      messages
      |> Array.map (fun m ->
        let mutable e = Unchecked.defaultof<PmEvent>
        e.Timestamp <- timestamp
        e.Message <- m.Word
        e)
    Platform.Pm_Write stream events (Array.length messages) |> Handling.checkError deviceInfo

  member x.WriteMessage timestamp (message: MidiMessage) =
    Platform.Pm_WriteShort stream timestamp message.Word |> Handling.checkError deviceInfo

  member x.WriteSysex timestamp data =
    Platform.Pm_WriteSysEx stream timestamp data |> Handling.checkError deviceInfo


