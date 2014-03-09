#I @"tools/FAKE/tools/"
#r @"FakeLib.dll"

open Fake

TraceEnvironmentVariables()

RestorePackages()

if buildServer = BuildServer.AppVeyor then
    MSBuildLoggers <- @"""C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll""" :: MSBuildLoggers

Target "Clean" (fun _ ->
    !! ("**/bin/**/*.*")
    |> DeleteFiles
)

Target "BuildSolution" (fun _ ->
    MSBuildWithDefaults "Build" ["./Library.sln"]
    |> Log "AppBuild-Output: "
)

Target "Test" (fun _ ->
    !! ("**/bin/*/*.Tests.dll")
    |> xUnit (fun p ->
            {p with
                HtmlOutput = true;
                XmlOutput = true;})
)

type Category =
    | Information
    | Warning
    | Error

/// AppVeyor build agent API message 
type Message =
    { /// The message
      Message : string
      /// The category of the message
      Category : Category option
      /// The details for this message
      Details : string option }

let AppVeyor args =
    ExecProcess (fun info -> 
                info.FileName <- "appveyor"
                info.Arguments <- args) (System.TimeSpan.MaxValue)

let AddMessage x =
    let args =
        seq {
            yield "AddMessage"
            yield quoteIfNeeded x.Message
            let category = function
                | Information -> "Information"
                | Warning -> "Warning"
                | Error -> "Error"
            yield defaultArg (x.Category |> Option.map (category >> sprintf "-Category %s")) ""
            yield defaultArg (x.Details |> Option.map (quoteIfNeeded >> sprintf "-Details %s")) ""
        }
        |> separated " "
    AppVeyor args |> ignore

type AppVeyorTraceListener() =
    let add msg category =
        AddMessage { Message = msg; Category = Some category; Details = None }
    interface ITraceListener with
        member this.Write msg =
            match msg with
            | StartMessage | FinishedMessage -> ()
            | ImportantMessage x -> add x Warning
            | ErrorMessage x -> add x Error
            | LogMessage (x, _) -> add x Information
            | TraceMessage (_, _) | OpenTag (_, _) | CloseTag _ -> ()

listeners.Add(AppVeyorTraceListener())            

Target "AppVeyor" (fun _ ->
    AddMessage { Message = "This is a message"; Category = None; Details = None }
    
    AddMessage { Message = "This is an info"; Category = Some Information; Details = Some "Some info details" }
    
    AddMessage { Message = "This is a warning"; Category = Some Warning; Details = Some "Some warning details" }
    
    AddMessage { Message = "This is an error"; Category = Some Error; Details = Some "Some error details" }
)

Target "Default" DoNothing

"Clean"
    ==> "BuildSolution"
    ==> "Test"
    =?> ("AppVeyor", (buildServer = BuildServer.AppVeyor))
    ==> "Default"

RunTargetOrDefault "Default"