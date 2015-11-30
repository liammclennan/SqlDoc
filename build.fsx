#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Testing
open System.IO

Target "Build" <| fun _ ->
    let projects =
        [
            "SqlDoc/SqlDoc.fsproj"
            "Tests/Tests.fsproj"
            "SqlDocCs/SqlDocCs.csproj"
            "TestsCs/TestsCs.csproj"
        ]
    for projFile in projects do
        build (fun x ->
            { x with
                Properties =
                    [ "Optimize",      environVarOrDefault "Build.Optimize"      "True"
                      "DebugSymbols",  environVarOrDefault "Build.DebugSymbols"  "True"
                      "Configuration", environVarOrDefault "Build.Configuration" "Release" ]
                Targets =
                    [ "Rebuild" ]
                Verbosity = Some Quiet }) projFile

Target "Default" (fun _ ->
    ()
)

Target "Test" (fun _ ->
    let testDir = "Tests/"
    !! (testDir + "bin/Release/Tests.dll")
      |> xUnit (fun p -> {p with HtmlOutputPath = testDir @@ "html" |> Some; ToolPath = "packages/xunit.runner.console/tools/xunit.console.exe";NUnitXmlOutputPath = Some testDir}))

"Build"
    ==> "Test"

// start build
RunTargetOrDefault "Build"
