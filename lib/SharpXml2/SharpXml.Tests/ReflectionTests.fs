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

module ReflectionTests =

    open System
    open NUnit.Framework

    open SharpXml
    open SharpXml.Tests.Types
    open SharpXml.Tests.TestHelpers

    [<Test>]
    let ``Can get public properties``() =
        let props = ReflectionHelpers.getPublicProperties typeof<TestClass>
        props.Length |> should equal 2

    [<Test>]
    let ``Can get public properties of interfaces``() =
        let props = ReflectionHelpers.getInterfaceProperties typeof<ITestInterface>
        props.Length |> should equal 1
        props.[0].Name |> should equal "Member1"
        props.[0].PropertyType |> should equal typeof<int>

    [<Test>]
    let ``Can get public properties of inherited interfaces``() =
        let props = ReflectionHelpers.getInterfaceProperties typeof<IAnotherInterface>
        props.Length |> should equal 2

    [<Test>]
    let ``Can get various default constructors``() =
        ReflectionHelpers.getDefaultValue typeof<TestClass> |> shouldBe Null
        ReflectionHelpers.getDefaultValue typeof<DateTime> |> should equal DateTime.MinValue
        ReflectionHelpers.getDefaultValue typeof<byte> |> should equal 0uy
        ReflectionHelpers.getDefaultValue typeof<IAnotherInterface> |> shouldBe Null

    [<Test>]
    let ``Can determine value types``() =
        let t = [ typeof<string>; typeof<int>; typeof<char> ]
        ReflectionHelpers.areStringOrValueTypes t |> shouldBe True
        ReflectionHelpers.areStringOrValueTypes [ typeof<TestClass> ] |> shouldBe False

    [<Test>]
    let ``Can get empty constructors of different classes``() =
        let ctor1 = ReflectionHelpers.getConstructorMethod typeof<TestClass>
        let ctor2 = ReflectionHelpers.getConstructorMethod typeof<SimpleClass>
        ctor1 |> shouldBe notNull
        ctor2 |> shouldBe notNull

    [<Test>]
    let ``Can get empty constructors by class names``() =
        let ctor1 = ReflectionHelpers.getConstructorMethodByName "System.String"
        let ctor2 = ReflectionHelpers.getConstructorMethodByName "SharpXml.Tests.Types+SimpleClass"
        ctor1 |> shouldBe notNull
        ctor2 |> shouldBe notNull

    [<Test>]
    let ``Can determine property getters``() =
        let cls = TestClass(200, "foobar")
        [ "V1", box 200; "V2", box "foobar" ]
        |> List.iter (fun (n, v) ->
            let pi = typeof<TestClass>.GetProperty(n)
            let getter = ReflectionHelpers.getGetter pi
            let ret = getter.Invoke(cls)
            ret |> should equal v)

    [<Test>]
    let ``Can determine property setters``() =
        let cls = TestClass(200, "foobar")
        [ "V1", box 42, (fun (x:TestClass) -> box x.V1) ; "V2", box "barfoo", (fun (x:TestClass) -> box x.V2) ]
        |> List.iter (fun (n, v, g) ->
            let pi = typeof<TestClass>.GetProperty(n)
            let setter = ReflectionHelpers.getSetter pi
            setter.Invoke(cls, v)
            g(cls) |> should equal v)