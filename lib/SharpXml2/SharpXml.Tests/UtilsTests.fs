//  Copyright 2012-2014 Gregor Uhlenheuer
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

namespace SharpXml.Tests

module UtilsTests =

    open System
    open NUnit.Framework

    open SharpXml.Attempt
    open SharpXml.Tests.TestHelpers

    [<Test>]
    let ``Attempt monad 1``() =
        let runs = ref 0
        let func v = fun () -> runs := !runs + 1; v
        let result = attempt {
            let! v1 = func None
            let! v2 = func None
            let! v3 = func <| Some 20
            v3 }
        result |> should equal (Some 20)
        !runs |> should equal 3

    [<Test>]
    let ``Attempt monad 2``() =
        let runs = ref 0
        let func v = fun () -> runs := !runs + 1; v
        let result = attempt {
            let! v1 = func None
            let! v2 = func <| Some 42
            let! v3 = func None
            v3 }
        result |> should equal (Some 42)
        !runs |> should equal 2

    [<Test>]
    let ``Attempt monad 3``() =
        let runs = ref 0
        let func v = fun () -> runs := !runs + 1; v
        let result = attempt {
            let! v1 = func None
            let! v2 = func None
            let! v3 = func None
            v3 }
        result |> should equal None
        !runs |> should equal 3

    [<Test>]
    let ``Attempt monad 4``() =
        let runs = ref 0
        let func v = fun () -> runs := !runs + 1; v
        let result = attempt {
            let! v1 = func None
            let! v2 = if false then func None else func <| Some 400
            let! v3 = func None
            let! v4 = func None
            v4 }
        result |> should equal (Some 400)
        !runs |> should equal 2
