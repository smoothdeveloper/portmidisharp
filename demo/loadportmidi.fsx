#r "../build/Debug/AnyCPU/netstandard2.0/PortMidiSharp.dll"
#load "../.paket/load/net472/demos/fsnative.fsx"

open fsnative
let libNames = [|"portmidi_x64.dll";"portmidi_x86.dll";"libportmidi.dylib";"libportmidi.so"|]
let libPaths = [|@"C:\dev\src\gitlab.com\gauthier\portmidisharp\lib\win";"/usr/local/lib";|]
let loader = LibraryLoader.withRuntimeLoader id
let library = LibraryLoader.tryLoadLibrary libNames libPaths loader
match library with
| None -> failwithf "couldn't load portmidi!"
| Some library -> printfn "loaded portmidi!"
