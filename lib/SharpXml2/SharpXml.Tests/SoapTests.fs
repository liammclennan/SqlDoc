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

module SoapTests =

    open NUnit.Framework

    open SharpXml
    open SharpXml.Common
    open SharpXml.Tests.TestHelpers

    let requestString =
        @"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
             <soap:Body>
              <GetSupportedFunctions xmlns=""http://url.com/"">
               <request>
                <KioskId>KioskId</KioskId>
                <KioskType>KioskType</KioskType>
                <HotelID>Ariane</HotelID>
                <SequenceId>2320</SequenceId>
               </request>
              </GetSupportedFunctions>
             </soap:Body>
            </soap:Envelope>"

    type GetSupportedFunctions<'a>() =

        let mutable request : 'a = Unchecked.defaultof<'a>

        member x.Request
            with get() = request
            and set(v) = request <- v

    type Body<'a>() =

        let mutable functions : 'a = Unchecked.defaultof<'a>

        member x.GetSupportedFunctions
            with get() = functions
            and set(v) = functions <- v

    type Envelope<'a>() =

        let mutable body : 'a = Unchecked.defaultof<'a>

        [<XmlElement(Name="soap:Body")>]
        member x.Body
            with get() = body
            and set(v) = body <- v

    type TestContent() =

        let mutable kid = ""
        let mutable ktype = ""
        let mutable hotelid = ""
        let mutable seqId = 0

        member x.KioskId
            with get() = kid
            and set(v) = kid <- v

        member x.KioskType
            with get() = ktype
            and set(v) = ktype <- v

        member x.HotelId
            with get() = hotelid
            and set(v) = hotelid <- v

        member x.SequenceId
            with get() = seqId
            and set(v) = seqId <- v

    [<Test>]
    let ``Can deserialize simple soap content``() =
        let result = XmlSerializer.DeserializeFromString<Envelope<Body<GetSupportedFunctions<TestContent>>>>(requestString)
        result.Body |> shouldBe notNull
        result.Body.GetSupportedFunctions |> shouldBe notNull
        result.Body.GetSupportedFunctions.Request |> shouldBe notNull
        result.Body.GetSupportedFunctions.Request.SequenceId |> should equal 2320

