#r "../build/Debug/AnyCPU/netstandard2.0/PortMidiSharp.dll"
#load "../.paket/load/net472/demos/fsnative.fsx"

open fsnative
open fsnative.Internals
let libNames = 
    match () with
    | Windows -> [|"portmidi"|]
    | OSX     -> [|"libportmidi.dylib"|]
    | Linux   -> [|"libportmidi.so"|]


let libPaths =
    match () with
    | Windows -> [|@"C:\dev\src\gitlab.com\gauthier\portmidisharp\lib\win"|]
    | OSX | Linux -> [|"/usr/local/lib";|]
let loader = LibraryLoader.withRuntimeLoader id
let library = LibraryLoader.tryLoadLibrary libNames libPaths loader
match library with
| None -> failwithf "couldn't load portmidi!"
| Some library -> printfn "loaded portmidi!"
