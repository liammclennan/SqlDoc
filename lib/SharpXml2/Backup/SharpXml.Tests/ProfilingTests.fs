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

module ProfilingTests =

    open System
    open System.Collections.Generic
    open System.IO
    open System.Runtime.Serialization
    open System.Text
    open System.Xml

    open NUnit.Framework

    open SharpXml
    open SharpXml.Tests.TestHelpers
    open SharpXml.Tests.Types

    XmlConfig.Instance.EmitCamelCaseNames <- true

    let serialize<'a> (element : 'a) =
        XmlSerializer.SerializeToString(element)

    let deserialize<'a> (input : string) =
        XmlSerializer.DeserializeFromString<'a>(input)

    let contractSerialize<'a> (element : 'a) =
        use ms = new MemoryStream()
        use xw = XmlWriter.Create(ms)
        let dcs = DataContractSerializer(typeof<'a>)
        dcs.WriteObject(xw, element)
        xw.Flush()
        ms.Seek(0L, SeekOrigin.Begin) |> ignore
        let reader = new StreamReader(ms)
        let output = reader.ReadToEnd()
        output

    let maxQuotas = XmlDictionaryReaderQuotas(MaxStringContentLength = 1024 * 1024)

    let contractDeserialize<'a> (input : string) =
        let bytes = Encoding.UTF8.GetBytes(input)
        let t = typeof<'a>
        use reader = XmlDictionaryReader.CreateTextReader(bytes, maxQuotas)
        let serializer = DataContractSerializer(t)
        serializer.ReadObject(reader) :?> 'a

    let buildClass count =
        let list = List<Guest>(Array.init count (fun i -> Guest(i, FirstName = "foo", LastName = "bar")))
        GenericClass<List<Guest>>(V1 = count, V2 = list)

    [<Test>]
    let ``SharpXml: Profile simple serialization``() =
        let list = List<Guest>([ Guest(10, FirstName = "foo", LastName = "bar"); Guest(20, FirstName = "ham", LastName = "eggs") ])
        let cls = Booking("testBooking", list)
        time (fun () -> serialize cls |> ignore) 1000
        time (fun () -> serialize cls |> ignore) 10000

    [<Test>]
    let ``SharpXml: Profile serialization of class with 100000 subelements``() =
        let count = 100000
        let cls = buildClass count
        timeAvg(fun () -> serialize cls |> ignore) 10
        Console.WriteLine(serialize cls)

    [<Test>]
    let ``SharpXml: Profile simple deserialization``() =
        time (fun () -> deserialize<TestClass> "<testClass><v1>42</v1><v2>bar</v2></testClass>" |> ignore) 1000
        time (fun () -> deserialize<TestClass> "<testClass><v1>42</v1><v2>bar</v2></testClass>" |> ignore) 10000

    [<Test>]
    let ``DCS: Profile serialization of class with 100000 subelements``() =
        let count = 100000
        let cls = buildClass count
        timeAvg (fun () -> contractSerialize cls |> ignore) 10
        Console.WriteLine(contractSerialize cls)
