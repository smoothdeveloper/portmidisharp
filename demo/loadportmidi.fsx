//#if HAS_MIDINETTE
//#r "../build/Debug/AnyCPU/net45/Midinette.dll"
//#endif
#r "../build/Debug/AnyCPU/net45/PortMidiSharp.dll"


open System.Runtime.InteropServices
open System.IO
open System

module Kernel =
    [<DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern IntPtr LoadLibrary(string lpFileName);

let dllPath = Path.Combine(__SOURCE_DIRECTORY__, "..")
match IntPtr.Size with
| 4 -> Kernel.LoadLibrary(Path.Combine(dllPath, "portmidi_x86.dll")) |> ignore
| 8 -> Kernel.LoadLibrary(Path.Combine(dllPath, "portmidi_x64.dll")) |> ignore
| otherwise -> failwithf "intptr size: %i" otherwise