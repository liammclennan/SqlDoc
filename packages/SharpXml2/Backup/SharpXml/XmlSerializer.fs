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
open System.Globalization
open System.IO
open System.Text

open SharpXml.Utils

/// XML serializer
type XmlSerializer() =

    /// UTF-8 encoding without BOM
    static let utf8encoding = UTF8Encoding(false)

    /// Header string for XML output
    static let xmlHeader = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"

    /// Eat the first XML root node depending on the 'UseAttributes'
    /// setting either with or without attribute values.
    static let eatRoot =
        if XmlConfig.Instance.UseAttributes 
        then XmlParser.eatRootWithAttributes
        else XmlParser.eatRoot

    /// Deserialization of the input string into the specified target type
    static let deserialize input targetType =
        let reader = Deserializer.getReaderFunc targetType
        let info = XmlParser.ParserInfo input
        let _, attr = eatRoot info

        reader attr info

    /// Deserialization of the input string into the type that
    /// is determined by the specified TypeResolver
    static let deserializeByResolver input (resolver : TypeResolver) =
        let info = XmlParser.ParserInfo input
        let name, attr = eatRoot info
        let xmlInfo = XmlInfo(name, attr)
        let resolved = resolver.Invoke xmlInfo
        let reader = Deserializer.getReaderFunc resolved

        reader attr info

    /// Deserialize the input string into the specified type
    static member DeserializeFromString<'T> input =
        if notEmpty input then
            try
                let chars = input.ToCharArray()
                deserialize chars typeof<'T> :?> 'T
            with
            | :? SharpXmlException -> Unchecked.defaultof<'T>
        else
            Unchecked.defaultof<'T>

    /// Deserialize the input string into the specified type
    static member DeserializeFromString (input, targetType) =
        if notEmpty input then
            try
                let chars = input.ToCharArray()
                deserialize chars targetType
            with
            | :? SharpXmlException -> null
        else
            null

    /// Deserialize the input string into the type that
    /// is determined by the specified TypeResolver
    static member DeserializeFromString (input, resolver) =
        if notEmpty input then
            try
                let chars = input.ToCharArray()
                deserializeByResolver chars resolver
            with
            | :? SharpXmlException -> null
        else
            null

    /// Deserialize the input reader into the specified type
    static member DeserializeFromReader<'T> (reader : TextReader) =
        XmlSerializer.DeserializeFromString<'T>(reader.ReadToEnd())

    /// Deserialize the input reader into the specified type
    static member DeserializeFromReader (reader : TextReader, targetType : Type) =
        XmlSerializer.DeserializeFromString(reader.ReadToEnd(), targetType)

    /// Deserialize the input reader into the type that
    /// is determined by the specified TypeResolver
    static member DeserializeFromReader (reader : TextReader, resolver : TypeResolver) =
        XmlSerializer.DeserializeFromString(reader.ReadToEnd(), resolver)

    /// Deserialize the input stream into the specified type
    static member DeserializeFromStream<'T> (stream : Stream) =
        use reader = new StreamReader(stream, utf8encoding)
        XmlSerializer.DeserializeFromString<'T>(reader.ReadToEnd())

    /// Deserialize the input stream into the specified type
    static member DeserializeFromStream (stream : Stream, targetType : Type) =
        use reader = new StreamReader(stream, utf8encoding)
        XmlSerializer.DeserializeFromString(reader.ReadToEnd(), targetType)

    /// Deserialize the input stream into the type that
    /// is determined by the specified TypeResolver
    static member DeserializeFromStream (stream : Stream, resolver : TypeResolver) =
        use reader = new StreamReader(stream, utf8encoding)
        XmlSerializer.DeserializeFromString(reader.ReadToEnd(), resolver)

    /// Serialize the given object into a XML string
    static member SerializeToString (element : obj, targetType) =
        let sb = StringBuilder()
        use writer = new StringWriter(sb, CultureInfo.InvariantCulture)
        if XmlConfig.Instance.WriteXmlHeader then writer.Write(xmlHeader)
        Serializer.writeType writer element targetType
        sb.ToString()

    /// Serialize the given object into a XML string
    static member SerializeToString<'T> (element : 'T) =
        XmlSerializer.SerializeToString(element, typeof<'T>)

    /// Serialize the given object into XML output using the specified TextWriter
    static member SerializeToWriter (writer : TextWriter, element : obj, targetType : Type) =
        if XmlConfig.Instance.WriteXmlHeader then writer.Write(xmlHeader)
        Serializer.writeType writer element targetType

    /// Serialize the given object into XML output using the specified TextWriter
    static member SerializeToWriter<'T> (writer : TextWriter, element : 'T) =
        XmlSerializer.SerializeToWriter(writer, element, typeof<'T>)

    /// Clear the cache of deserializer functions
    static member ClearDeserializerCache() =
        Deserializer.clearCache()

    /// Clear the cache of serializer functions
    static member ClearSerializerCache() =
        Serializer.clearCache()

    /// Clear all type based serialization and deserialization functions
    static member ClearCache() =
        Serializer.clearCache()
        Deserializer.clearCache()
