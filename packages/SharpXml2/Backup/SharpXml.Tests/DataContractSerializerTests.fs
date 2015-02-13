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

module DataContractSerializerTests =

    open System
    open System.Collections.Generic
    open System.Diagnostics
    open System.IO
    open System.Runtime.Serialization
    open System.Text
    open System.Text.RegularExpressions
    open System.Xml

    open NUnit.Framework

    open SharpXml.Tests.TestHelpers
    open SharpXml.Tests.Types

    let stripNamespaces input =
        let rgx = Regex(@"\s*xmlns(?::\w+)?=""[^""]+""")
        rgx.Replace(input, String.Empty)

    let stripXmlHeader input =
        let rgx = Regex(@"^<[^>]+>")
        rgx.Replace(input, String.Empty)

    let strip = stripXmlHeader >> stripNamespaces

    let maxQuotas = XmlDictionaryReaderQuotas(MaxStringContentLength = 1024 * 1024)

    let contractSerialize<'a> (element : 'a) =
        use ms = new MemoryStream()
        use xw = XmlWriter.Create(ms)
        let dcs = DataContractSerializer(typeof<'a>)
        dcs.WriteObject(xw, element)
        xw.Flush()
        ms.Seek(0L, SeekOrigin.Begin) |> ignore
        let reader = new StreamReader(ms)
        let output = reader.ReadToEnd() |> strip
        Debug.WriteLine(output)
        output

    let contractDeserialize<'a> (input : string) =
        let bytes = Encoding.UTF8.GetBytes(input)
        let t = typeof<'a>
        use reader = XmlDictionaryReader.CreateTextReader(bytes, maxQuotas)
        let serializer = DataContractSerializer(t)
        serializer.ReadObject(reader) :?> 'a

    [<Test>]
    let ``DCS: can serialize simple class``() =
        let cls = ContractClass(V1 = "foo", V2 = 42)
        contractSerialize cls |> should equal "<ContractClass><V1>foo</V1><V2>42</V2></ContractClass>"

    [<Test>]
    let ``DCS: can serialize arrays``() =
        let arr = [| "foo"; "bar" |]
        let cls = ContractClass3(V1 = arr, V2 = 99)
        contractSerialize cls |> should equal "<ContractClass3><V1><d2p1:string>foo</d2p1:string><d2p1:string>bar</d2p1:string></V1><V2>99</V2></ContractClass3>"

    [<Test>]
    let ``DCS: can serialize dictionaries``() =
        let dict = Dictionary<string,int>()
        dict.Add("foo", 42)
        let cls = new ContractClass2(V1 = "bar", V2 = dict)
        contractSerialize cls |> should equal "<ContractClass2><V1>bar</V1><V2><d2p1:KeyValueOfstringint><d2p1:Key>foo</d2p1:Key><d2p1:Value>42</d2p1:Value></d2p1:KeyValueOfstringint></V2></ContractClass2>"

    [<Test>]
    let ``DCS: can serialize newline characters``() =
        let special = "foo\r\nbar"
        let cls = new ContractClass(V1 = special, V2 = 200)
        contractSerialize cls |> should equal "<ContractClass><V1>foo\r\nbar</V1><V2>200</V2></ContractClass>"

    [<Test>]
    let ``DCS: can serialize XML encoded characters``() =
        let special = "</v2>"
        let cls = new ContractClass(V1 = special, V2 = 210)
        contractSerialize cls |> should equal "<ContractClass><V1>&lt;/v2&gt;</V1><V2>210</V2></ContractClass>"

    [<Test>]
    let ``DCS: can deserialize simple classes``() =
        let cls = contractDeserialize<ContractClass> "<ContractClass><V1>foo</V1><V2>42</V2></ContractClass>"
        cls.V1 |> should equal "foo"
        cls.V2 |> should equal 42

    [<Test>]
    let ``DCS: profile simple deserialization``() =
        time (fun () -> contractDeserialize<ContractClass> "<ContractClass><V1>foo</V1><V2>42</V2></ContractClass>" |> ignore) 1000
        time (fun () -> contractDeserialize<ContractClass> "<ContractClass><V1>foo</V1><V2>42</V2></ContractClass>" |> ignore) 10000

    [<Test>]
    let ``DCS: profile simple serialization``() =
        let list = List<Guest>([ Guest(10, FirstName = "foo", LastName = "bar"); Guest(20, FirstName = "ham", LastName = "eggs") ])
        let cls = Booking("testBooking", list)
        time (fun () -> contractSerialize cls |> ignore) 1000
        time (fun () -> contractSerialize cls |> ignore) 10000