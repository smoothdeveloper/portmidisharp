#load "loadportmidi.fsx"
open System.Collections.Generic
open System
open PortMidi.Native
open PortTime.Native

module ConsoleHelper =
  let combineTry f g =
    fun i ->
    match f i with
    | true, v -> if g v then true, v else false, v
    | false, v -> false, v
  let rec ask (message: string) f =
      Console.WriteLine(message)
      match f (Console.ReadLine()) with 
      | true, v -> v
      | false, _ -> ask message f
  let askIntPredicate message p = ask message (combineTry (Int32.TryParse) p)
  let askChoices message (choices: IDictionary<_,_>) =
    if choices.Count = 0 then
      failwithf "need choices as input... %s" message
    let sortedKeys = choices.Keys |> Seq.toArray |> Array.sort
    let message =
      sortedKeys
      |> Array.indexed
      |> Array.map (fun (i,o) -> sprintf "- %i. %s" (i+1) o)
      |> String.concat Environment.NewLine
      |> ((+) message)
    let index = (askIntPredicate message (fun a -> a >= 1 && a <= choices.Count)) - 1
    choices.[sortedKeys.[index]]
open ConsoleHelper

let PM_FILT_ACTIVE = (1 <<< 0x0E)
let PM_FILT_CLOCK = (1 <<< 0x08)
let HIST_LEN = 1000 // how many 1ms bins in the histogram
let inputBufferSize = 100
let outputBufferSize = 100

let checkPmError e = 
  match e with
  | PmError.NoError -> ()
  | e -> printfn "pmerror %A" e

let checkPtError e = 
  match e with
  | PtError.NoError -> ()
  | e -> printfn "pterror %A" e

let nullptr = IntPtr.Zero

type Parameters = { period: int; output_period: int; testIn: bool; testOut: bool }

type Output = {
  /// how many points outside of HIST_LEN
  mutable outOfRange : int
  mutable maxLatency : int
  histogram : int array
}

type State = {
  mutable parameters : Parameters
  mutable previous_callback_time: int
  mutable iteration: int
  mutable isNoteOn: bool
  mutable outPort: IntPtr
  mutable inPort: IntPtr
}

let state =
  let parameters = {period = 0; output_period = 0; testIn = false; testOut = false}
  { previous_callback_time = 0
    iteration = 0 
    parameters = parameters
    isNoteOn = false
    inPort = nullptr
    outPort = nullptr
  }

let output = {outOfRange = 0; maxLatency = 0; histogram = Array.zeroCreate HIST_LEN }

let ptCallback timestamp ptDataPointer =
  let difference = timestamp - state.previous_callback_time - state.parameters.period
  state.previous_callback_time <- timestamp

  // allow 5 seconds for the system to settle down
  if timestamp < 5000 then () else

  state.iteration <- state.iteration + 1

  if state.parameters.testOut && ((state.iteration % state.parameters.output_period) = 0) then
    let event = PmEvent(Timestamp = Platform.Pt_Time (), Message = Platform.Pm_Message(0x90,60,if state.isNoteOn then 0 else 100))
    state.isNoteOn <- not state.isNoteOn
    Platform.Pm_Write state.outPort (Array.singleton event) 1 |> checkPmError
    state.iteration <- 0

  // read all waiting events (if user requested)
  if state.parameters.testIn then
    let mutable status = PmError.NoError
    let buffer : PmEvent array = Array.zeroCreate 1
    let toDo () =
      status <- Platform.Pm_Poll state.inPort
      if status = PmError.GotData then
        match PortMidi.Runtime.read state.inPort 1 with
        | Ok [|event|] -> ()
        | otherwise -> printfn "odd, should be one: %A" otherwise

    toDo ()
    while status = PmError.GotData do toDo ()

  // ignore when system is "catching up"
  if difference < 0 then () else

  // update the histogram 
  if difference < HIST_LEN then
    output.histogram.[difference] <- output.histogram.[difference] + 1  
  else
    output.outOfRange <- output.outOfRange + 1
  if output.maxLatency < difference then
    output.maxLatency <- difference


printfn "Latency histogram."
let period =
    askIntPredicate
        "Choose timer period (in ms, >= 1): "
        (fun a -> 0 < a)

let testIn,testOut =
  [| "No MIDI traffic"       , (false,false)
     "MIDI input"           , (true,false)
     "MIDI output"          , (false,true)
     "MIDI input and output", (true,true)  
  |] 
  |> dict
  |> askChoices "Benchmark with:\n"

if testIn || testOut then

  let getFilteredDevicesChoices p =
    [| 0 .. (Platform.Pm_CountDevices() - 1) |]
    |> Array.map (fun i -> i, PortMidi.Interop.getDeviceById i)
    |> Array.filter (snd >> p)
    |> Array.map (fun (i,device) -> sprintf "%s %s" device.InterfaceName device.Name, device)
    |> dict
  
  let inputChoices = getFilteredDevicesChoices (fun d -> d.SupportsInput)
  let outputChoices = getFilteredDevicesChoices (fun d -> d.SupportsOutput)
  let timeProc = PmTimeProc(fun v -> Platform.Pt_Time())

  if testIn then
    let input = askChoices "MIDI input device number: " inputChoices
    Platform.Pm_OpenInput &state.inPort input.DeviceId nullptr inputBufferSize timeProc nullptr |> checkPmError
    // turn on filtering; otherwise, input might overflow in the 
    // 5-second period before timer callback starts reading midi
    Platform.Pm_SetFilter state.inPort (PM_FILT_ACTIVE ||| PM_FILT_CLOCK) |> checkPmError

  let outputPeriod =
    if testOut then
      let output = askChoices "MIDI output device number: " outputChoices
      Platform.Pm_OpenOutput &state.outPort output.DeviceId nullptr outputBufferSize timeProc nullptr 0 |> checkPmError
      
      askIntPredicate "MIDI out should be sent every __ callback iterations: " (fun a -> a >= 1)
    else
      0

  state.parameters <- { output_period = outputPeriod; period = period; testIn = testIn; testOut = testOut }
  let ptCallback = PtCallback ptCallback
  Console.WriteLine("Latency measurements will start in 5 seconds. Type return to stop: ")
  Platform.Pt_Start period ptCallback nullptr |> checkPtError
  Console.ReadLine() |> ignore
  let stop = Platform.Pt_Time()
  Platform.Pt_Stop () |> checkPtError

  // courteously turn off the last note, if necessary
  if state.isNoteOn then
    let event = PmEvent(Timestamp = Platform.Pt_Time (), Message = Platform.Pm_Message(0x90, 60, 0))
    Platform.Pm_Write state.outPort [|event|] 1 |> checkPmError

  // print the histogram
  printfn "Duration of test: %g seconds" (float (max 0 (stop - 5000) ) * 0.001)
  printfn "Latency(ms)  Number of occurrences"
  match output.histogram |> Array.tryFindIndexBack ((<>) 0) with
  | Some len ->
    for i in 0 .. len do
      printfn "%2d      %10d" i output.histogram.[i]
    printfn "Maximum latency: %d milliseconds" output.maxLatency
    printfn "\nNote that due to rounding, actual latency can be 1ms higher"
    printfn "than the numbers reported here."
    printfn "Type return to exit..."
    Console.ReadLine() |> ignore
    if testIn  then Platform.Pm_Close(state.inPort)  |> checkPmError
    if testOut then Platform.Pm_Close(state.outPort) |> checkPmError

  | None -> 
    printfn "histogram empty? %A" output.histogram
