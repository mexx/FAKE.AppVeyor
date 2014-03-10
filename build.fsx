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
    sprintf "AddMessage %s -Category %s" (quoteIfNeeded msg) (quoteIfNeeded category)
    |> sendToAppVeyor

let private addWithDetails msg category details =
    sprintf "AddMessage %s -Category %s -Details %s" (quoteIfNeeded msg) (quoteIfNeeded category) (quoteIfNeeded details)
    |> sendToAppVeyor

let private addNoCategory msg =
    sprintf "AddMessage %s" (quoteIfNeeded msg)
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
    addNoCategory "This is a message"
    
    addWithDetails "This is an info" "Information" "Some info details"
    
    addWithDetails "This is a warning" "Warning" "Some warning details"
    
    addWithDetails "This is an error" "Error" "Some error details"
)

Target "Default" DoNothing

"Clean"
    ==> "BuildSolution"
    ==> "Test"
    =?> ("AppVeyor", (buildServer = BuildServer.AppVeyor))
    ==> "Default"

RunTargetOrDefault "Default"