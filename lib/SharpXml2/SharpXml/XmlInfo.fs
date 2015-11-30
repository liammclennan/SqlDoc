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

namespace SharpXml

open System
open System.Collections.Generic


/// Class containing information of a specific XML attribute.
type XmlAttributeInfo internal(key: string, value: string) =

    member x.Key
        with get() = key

    member x.Value
        with get() = value


/// Class containing information of a XML root element
/// including its optional attributes.
type XmlInfo internal(name: string, attributes: (string * string) seq) =

    let attrs =
        List<XmlAttributeInfo>(
            attributes
            |> Seq.map (fun x -> XmlAttributeInfo(x))).AsReadOnly()

    member x.Name
        with get() = name

    member x.Attributes
        with get() = attrs

    member x.HasAttributes
        with get() = attrs.Count > 0


/// Delegate type that defines a type resolving function
type TypeResolver = delegate of XmlInfo -> Type
