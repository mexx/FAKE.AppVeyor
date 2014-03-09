#I @"tools/FAKE/tools/"
#r @"FakeLib.dll"

open Fake

TraceEnvironmentVariables()

RestorePackages()
//if buildServer = BuildServer.AppVeyor then
//    MSBuildDefaults <- { MSBuildDefaults with 
//                            FileLoggers = Some [{ Number = 1; Filename = None; Verbosity = None; Parameters = None}];
//                            Properties = ["logger","C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"]
//        }

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

Target "Default" DoNothing

"Clean"
    ==> "BuildSolution"
    ==> "Test"
    ==> "Default"

RunTargetOrDefault "Default"