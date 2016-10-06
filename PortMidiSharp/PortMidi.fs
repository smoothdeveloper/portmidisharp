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
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_WriteSysex               (IntPtr stream, int timestamp, byte[] msg)

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
  [<DllImport(dllName, CallingConvention = CallingConvention.Cdecl)>] extern PmError Pm_WriteSysex               (IntPtr stream, int timestamp, byte[] msg)

module Platform =
  let is64bit = Environment.Is64BitProcess

  let Pm_Abort stream                = if is64bit then Platform64.Pm_Abort stream else Platform32.Pm_Abort stream
  let Pm_Close stream                = if is64bit then Platform64.Pm_Close stream else Platform32.Pm_Close stream
  let Pm_CountDevices ()             = if is64bit then Platform64.Pm_CountDevices () else Platform32.Pm_CountDevices ()
  let Pm_GetDefaultInputDeviceID ()  = if is64bit then Platform64.Pm_GetDefaultInputDeviceID () else Platform32.Pm_GetDefaultInputDeviceID ()
  let Pm_GetDefaultOutputDeviceID () = if is64bit then Platform64.Pm_GetDefaultOutputDeviceID () else Platform32.Pm_GetDefaultOutputDeviceID ()
  let Pm_GetDeviceInfo id            = if is64bit then Platform64.Pm_GetDeviceInfo id else Platform32.Pm_GetDeviceInfo id
  let Pm_GetErrorText errnum         = if is64bit then Platform64.Pm_GetErrorText errnum else Platform32.Pm_GetErrorText errnum
  let Pm_GetHostErrorText msg len    = if is64bit then Platform64.Pm_GetHostErrorText(msg, len) else Platform32.Pm_GetHostErrorText(msg, len)
  let Pm_HasHostError stream         = if is64bit then Platform64.Pm_HasHostError stream else Platform32.Pm_HasHostError stream
  let Pm_Initialize ()               = if is64bit then Platform64.Pm_Initialize () else Platform32.Pm_Initialize ()
  let Pm_OpenInput (stream: byref<IntPtr>) inputDevice inputDriverInfo bufferSize timeProc timeInfo = if is64bit then Platform64.Pm_OpenInput(&stream, inputDevice, inputDriverInfo, bufferSize, timeProc, timeInfo) else Platform32.Pm_OpenInput(&stream, inputDevice, inputDriverInfo, bufferSize, timeProc, timeInfo)
  let Pm_OpenOutput (stream: byref<IntPtr>) inputDevice inputDriverInfo bufferSize timeProc timeInfo latency = if is64bit then Platform64.Pm_OpenOutput(&stream, inputDevice, inputDriverInfo, bufferSize, timeProc, timeInfo, latency) else Platform32.Pm_OpenOutput(&stream, inputDevice, inputDriverInfo, bufferSize, timeProc, timeInfo, latency)
  let Pm_Poll stream = if is64bit then Platform64.Pm_Poll stream else Platform32.Pm_Poll stream
  let Pm_Read stream buffer length = if is64bit then Platform64.Pm_Read(stream, buffer, length) else Platform32.Pm_Read(stream, buffer, length)
  let Pm_SetChannelMask stream mask = if is64bit then Platform64.Pm_SetChannelMask(stream, mask) else Platform32.Pm_SetChannelMask(stream, mask)
  let Pm_SetFilter stream filters = if is64bit then Platform64.Pm_SetFilter(stream, filters) else Platform32.Pm_SetFilter(stream, filters)
  let Pm_Terminate () = if is64bit then Platform64.Pm_Terminate() else Platform32.Pm_Terminate()
  let Pm_Write stream buffer length = if is64bit then Platform64.Pm_Write(stream, buffer, length) else Platform32.Pm_Write(stream, buffer, length)
  let Pm_WriteShort stream timestamp msg = if is64bit then Platform64.Pm_WriteShort(stream, timestamp, msg) else Platform32.Pm_WriteShort(stream, timestamp, msg)
  let Pm_WriteSysex stream timestamp msg = if is64bit then Platform64.Pm_WriteSysex(stream, timestamp, msg) else Platform32.Pm_WriteSysex(stream, timestamp, msg)

namespace PortMidiSharp
open PortMidiSharp.Native
open System.Collections.Generic
open System.Runtime.InteropServices
open System

type MidiDeviceInfo(id: int, inner: PmDeviceInfo) =
  let interfaceName = Marshal.PtrToStringAnsi inner.Interface
  let name = Marshal.PtrToStringAnsi inner.Name
  member x.DeviceId = id
  member x.InterfaceName = interfaceName
  member x.Name = name
  member x.SupportsInput = inner.Input = 1
  member x.SupportsOutput = inner.Output = 1
  override x.ToString () = sprintf "Device %i: %s, %s %s%s" id name interfaceName (if x.SupportsInput then "Input" else "") (if x.SupportsOutput then "Output" else "")

module PortMidi =
  let init () = Platform.Pm_Initialize()
  let terminate () = Platform.Pm_Terminate()
  let inline internal getDeviceById id =
    let deviceInfo : PmDeviceInfo = Platform.Pm_GetDeviceInfo id |> Marshal.PtrToStructure
    MidiDeviceInfo(id, deviceInfo)

  let getDevices () =
    [|
    for i in 0 .. (Platform.Pm_CountDevices() - 1) do
      yield getDeviceById i
    |]
  let defaultOutputDevice =
    let id = Platform.Pm_GetDefaultOutputDeviceID()
    if id < 0 then None else Some(getDeviceById id)
  let defaultInputDevice =
    let id = Platform.Pm_GetDefaultInputDeviceID()
    if id < 0 then None else Some(getDeviceById id)


module Handling =
  type OpenedDevice =
    struct
      val mutable stream : IntPtr
      val mutable device : MidiDeviceInfo
    end

  let inputDevices = Dictionary<int,OpenedDevice>()
  let registerOpenInputDevice deviceInfo stream =
    let mutable d = Unchecked.defaultof<OpenedDevice>
    d.device <- deviceInfo
    d.stream <- stream
    inputDevices.[deviceInfo.DeviceId] <- d
  
type MidiInput(deviceInfo: MidiDeviceInfo) =
  let mutable stream = IntPtr.Zero
  let isOpen () = stream <> IntPtr.Zero
  let checkError errnum =
    match errnum with
    | PmError.NoError -> ()
    | _ -> failwithf "%s (%s)" (string errnum) (Platform.Pm_GetErrorText errnum |> Marshal.PtrToStringAnsi)

  member x.Open bufferSize =
    if isOpen () then ()
    else
      let driverInfo = IntPtr.Zero
      let timeInfo = IntPtr.Zero
      let timeProc = null
      Platform.Pm_OpenInput &stream deviceInfo.DeviceId driverInfo bufferSize timeProc timeInfo |> checkError

  member x.Close () =
    if not (isOpen ()) then ()
    else
      Platform.Pm_Close stream |> checkError
      stream <- IntPtr.Zero