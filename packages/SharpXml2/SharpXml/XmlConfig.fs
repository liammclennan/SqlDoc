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

/// Delegate type that contains type
/// specific serialization logic
type SerializerFunc = delegate of obj -> string

/// Delegate type that contains type
/// specific deserialization logic
type DeserializerFunc = delegate of string -> obj

/// Type of the serialization logic that should be
/// used when writing special characters
type UnicodeSerializationType =
    /// Serialize the special character in the output stream as it is
    | Unencoded      = 0
    /// Serialize the hexadecimal representation of the character value
    | HexEncoded     = 1
    /// Serialize the decimal representation of the character value
    | DecimalEncoded = 2

/// Singleton configuration class containing all
/// preferences of the XmlSerializer
type XmlConfig private() =

    let mutable includeNullValues = false
    let mutable excludeTypeInfo = true
    let mutable emitCamelCaseNames = false
    let mutable writeXmlHeader = false
    let mutable throwOnError = false
    let mutable throwOnUnknownElements = false
    let mutable useAttributes = false
    let mutable specialCharEncoding = UnicodeSerializationType.Unencoded

    let serializerCache = ref (Dictionary<Type, SerializerFunc>())
    let deserializerCache = ref (Dictionary<Type, DeserializerFunc>())

    static let mutable instance = lazy(XmlConfig())

    /// XmlConfig singleton instance
    static member Instance with get() = instance.Value

    /// Whether to include null values in the serialized output
    member x.IncludeNullValues
        with get() = includeNullValues
        and set(v) = includeNullValues <- v

    /// Whether to exclude additional type information for
    /// dynamic/anonymous types in the serialized output
    member x.ExcludeTypeInfo
        with get() = excludeTypeInfo
        and set(v) = excludeTypeInfo <- v

    /// Whether to convert property names into camel case
    /// for the serialized output (i.e 'MyValue' -> 'myValue')
    member x.EmitCamelCaseNames
        with get() = emitCamelCaseNames
        and set(v) = emitCamelCaseNames <- v

    /// Whether to include a XML header sequence in the
    /// serialized output
    member x.WriteXmlHeader
        with get() = writeXmlHeader
        and set(v) = writeXmlHeader <- v

    /// Whether to throw exceptions on deserialization errors
    member x.ThrowOnError
        with get() = throwOnError
        and set(v) = throwOnError <- v

    /// Whether to throw exceptions if an element does not
    /// exist on deserialization
    member x.ThrowOnUnknownElements
        with get() = throwOnUnknownElements
        and set(v) = throwOnUnknownElements <- v

    /// Whether the serialization and deserialization process
    /// should respect and parse XML attributes
    member x.UseAttributes
        with get() = useAttributes
        and set(v) = useAttributes <- v

    /// Whether to convert special characters into XML
    /// encoded entities
    member x.SpecialCharEncoding
        with get() = specialCharEncoding
        and set(v) = specialCharEncoding <- v

    /// Register a serializer delegate for the specified type
    member x.RegisterSerializer<'T> (func : SerializerFunc) =
        Atom.updateAtomDict serializerCache typeof<'T> func |> ignore

    /// Register a deserializer delegate for the specified type
    member x.RegisterDeserializer<'T> (func : DeserializerFunc) =
        Atom.updateAtomDict deserializerCache typeof<'T> func |> ignore

    /// Unregister the serializer delegate for the specified type
    member x.UnregisterSerializer<'T>() =
        Atom.removeAtomDictElement serializerCache typeof<'T>

    /// Unregister the deserializer delegate for the specified type
    member x.UnregisterDeserializer<'T>() =
        Atom.removeAtomDictElement deserializerCache typeof<'T>

    /// Clear all registered custom serializer delegates
    member x.ClearSerializers() =
        Atom.clearAtomDict serializerCache

    /// Clear all registered custom deserializer delegates
    member x.ClearDeserializers() =
        Atom.clearAtomDict deserializerCache

    /// Try to get a serializer delegate for the specified type
    member internal x.TryGetSerializer (t : Type) =
        (!serializerCache).TryGetValue t
        |> Utils.tryToOption

    /// Try to get a deserializer delegate for the specified type
    member internal x.TryGetDeserializer (t : Type) =
        (!deserializerCache).TryGetValue t
        |> Utils.tryToOption
