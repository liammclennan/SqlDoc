#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open System.IO

Target "Build" <| fun _ ->
    let projects =
        [
            "SqlDoc/SqlDoc.fsproj"
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

//Target "Test" (fun _ ->
//    !! (testDir + "/Tests.dll")
//      |> NUnit (fun p ->
//          {p with
//             DisableShadowCopy = true;
//             OutputFile = testDir + "TestResults.xml" })
//)

// start build
RunTargetOrDefault "Build"
