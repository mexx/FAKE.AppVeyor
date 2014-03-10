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

let private sendToAppVeyor args =
    ExecProcess (fun info -> 
                info.FileName <- "appveyor"
                info.Arguments <- args) (System.TimeSpan.MaxValue)
    |> ignore

let private add msg category =
    sprintf "AddMessage %s -Category %s" msg category
    |> sendToAppVeyor

let private addNoCategory msg =
    sprintf "AddMessage %s" msg
    |> sendToAppVeyor

// Add trace listener to track messages
if buildServer = BuildServer.AppVeyor then
    listeners.Add({new ITraceListener with
        member this.Write msg =
            match msg with
            | ErrorMessage x -> add x "Error"
            | ImportantMessage x -> add x "Warning"
            | LogMessage (x, _) -> add x "Information"
            | TraceMessage (x, _) -> if not enableProcessTracing then addNoCategory x
            | StartMessage | FinishedMessage
            | OpenTag (_, _) | CloseTag _ -> ()})

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