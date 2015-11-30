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

namespace SharpXml.Common

open System

/// Attribute that allows customization of the serialization
/// and deserialization behavior of XML elements
[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Property)>]
type XmlElementAttribute(name : string) =
    inherit Attribute()

    let mutable name = name
    let mutable itemName = Unchecked.defaultof<string>
    let mutable keyName = Unchecked.defaultof<string>
    let mutable valueName = Unchecked.defaultof<string>
    let mutable ns = Unchecked.defaultof<string>
    let mutable order = Int32.MaxValue

    new() = XmlElementAttribute(null)

    /// Name to override the property name
    member x.Name
        with get() = name
        and set(v) = name <- v

    /// Name to override item names of collections
    member x.ItemName
        with get() = itemName
        and set(v) = itemName <- v

    /// Name to override key names of key-value collections
    member x.KeyName
        with get() = keyName
        and set(v) = keyName <- v

    /// Name to override value names of key-value collections
    member x.ValueName
        with get() = valueName
        and set(v) = valueName <- v

    /// Optional order number (default 0) - will be used on serialization
    member x.Order
        with get() = order
        and set(v) = order <- v

/// Attribute to mark properties that should be associated
/// with XML attributes
[<AttributeUsage(AttributeTargets.Property)>]
type XmlAttributeAttribute(name: string) =
    inherit Attribute()

    new() = XmlAttributeAttribute(null)

    /// Name to override the property/attribute name
    member x.Name
        with get() = name

/// Attribute to mark public properties that should be ignored
/// on (de)serialization
[<AttributeUsage(AttributeTargets.Property)>]
type XmlIgnoreAttribute() =
    inherit Attribute()

/// Attribute that allows to specify a number of static
/// XML attributes (i.e. namespace definitions) that should be used on serialization
[<AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)>]
type XmlNamespaceAttribute(name: string, value: string) =
    inherit Attribute()

    /// Name of the namespace or static attribute
    member x.Name
        with get() = name

    /// Value of the namespace or static attribute
    member x.Value
        with get() = value
