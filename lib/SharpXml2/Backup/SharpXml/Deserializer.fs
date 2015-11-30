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

/// Reader function delegate
type internal ReaderFunc = (string * string) list -> XmlParser.ParserInfo -> obj

/// Record type containing the deserialization information
/// for a specific property member
type internal PropertyReaderInfo = {
    Info : System.Reflection.PropertyInfo
    Reader : ReaderFunc
    Setter : SetterFunc
    Ctor : EmptyConstructor }

/// Record type containing the deserialization information
/// for XML attribute properties
type internal AttributeReaderInfo = {
    Info : System.Reflection.PropertyInfo
    Reader : DeserializerFunc
    Setter : SetterFunc
    Ctor : EmptyConstructor }

/// Record type containing the deserialization information
/// of a specific type and all its members that have to be deserialized
type internal TypeBuilderInfo = {
    Type : System.Type
    // TODO: I would love to use case-insensitive FSharpMaps instead
    Props : System.Collections.Generic.Dictionary<string, PropertyReaderInfo>
    Attrs : System.Collections.Generic.Dictionary<string, AttributeReaderInfo>
    Ctor : EmptyConstructor }

/// Record type containing the deserialization information
/// for record types
type internal RecordBuilderInfo = {
    Type : System.Type
    Readers : Map<string, (ReaderFunc * int)>
    Ctor : obj[] -> obj
    Fields : int }

/// Record type containing the deserialization information
/// for tuple types
type internal TupleBuilderInfo = {
    Type : System.Type
    Readers : ReaderFunc array
    Ctor : obj[] -> obj
    Fields : int }

/// Record type containing the deserialization information
/// for discriminated unions
type internal UnionBuilderInfo = {
    Type : System.Type
    Readers : Map<string, ReaderFunc[] * (obj[] -> obj)> }

module internal Deserialization =

    open System

    /// Throw an exception if a deserializable element
    /// does not exist in the target type
    let inline elementNotExist name (t : Type) =
        if XmlConfig.Instance.ThrowOnUnknownElements then
            let msg = sprintf "Given XML element '%s' does not exist on type %s" name t.FullName
            raise <| SharpXmlException msg

    /// Throw an exception if the deserialization/type
    /// generation failed with an exception
    let inline deserializeError arg name (t : Type) ex =
        if XmlConfig.Instance.ThrowOnError then
            let msg = sprintf "Unable to deserialize %s '%s' of type '%s'" arg name t.FullName
            raise <| SharpXmlException(msg, ex)

    /// Throw an exception if the deserialization/type
    /// generation failed on a class property with an exception
    let inline deserializeErrorProperty name (t : Type) ex =
        deserializeError "property" name t ex

    /// Throw an exception if the deserialization/type
    /// generation failed on a record field with an exception
    let inline deserializeErrorRecord name (t : Type) ex =
        deserializeError "record field" name t ex

    /// Throw an exception if the deserialization/type
    /// generation failed on a tuple item with an exception
    let inline deserializeErrorTuple name (t : Type) ex =
        deserializeError "tuple item" name t ex

    /// Throw an exception if the creation of the specified type
    /// failed unexpectedly
    let inline deserializeErrorCreateInst (t : Type) ex =
        if XmlConfig.Instance.ThrowOnError then
            let msg = sprintf "Failed to create an instance of type %s" t.FullName
            raise <| SharpXmlException(msg, ex)

    /// Apply attribute values if necessary
    let setAttributes (builder : TypeBuilderInfo) attrs inst =
        if builder.Attrs.Count > 0 then
            List.iter (fun (k, v) ->
                match builder.Attrs.TryGetValue k with
                | true, prop ->
                    try
                        prop.Setter.Invoke(inst, prop.Reader.Invoke(v))
                    with ex -> deserializeError "attribute" k prop.Info.PropertyType ex
                | _ -> elementNotExist k builder.Type) attrs

module internal ValueTypeDeserializer =

    open System

    open SharpXml.XmlParser

    let inline buildValueReader reader = fun attr info ->
        let str = eatText info
        if not info.IsEnd then
            let value = reader str
            eatClosingTag info
            value
        else null

    let getEnumReader (t : Type) = fun () ->
        if t.IsEnum then
            fun i -> Enum.Parse(t, i)
            |> buildValueReader
            |> Some
        else None

    let getValueParser t =
        match Type.GetTypeCode(t) with
        | TypeCode.Boolean  -> Boolean.Parse  >> box |> Some
        | TypeCode.Byte     -> Byte.Parse     >> box |> Some
        | TypeCode.Int16    -> Int16.Parse    >> box |> Some
        | TypeCode.Int32    -> Int32.Parse    >> box |> Some
        | TypeCode.Int64    -> Int64.Parse    >> box |> Some
        | TypeCode.Char     -> Char.Parse     >> box |> Some
        | TypeCode.DateTime -> DateTime.Parse >> box |> Some
        | TypeCode.Decimal  -> Decimal.Parse  >> box |> Some
        | TypeCode.Double   -> Double.Parse   >> box |> Some
        | TypeCode.SByte    -> SByte.Parse    >> box |> Some
        | TypeCode.Single   -> Single.Parse   >> box |> Some
        | TypeCode.UInt16   -> UInt16.Parse   >> box |> Some
        | TypeCode.UInt32   -> UInt32.Parse   >> box |> Some
        | TypeCode.UInt64   -> UInt64.Parse   >> box |> Some
        | TypeCode.String   -> box |> Some
        | _                 -> None

    let getValueReader (t : Type) = fun () ->
        match getValueParser t with
        | Some r -> Some(buildValueReader r)
        | _ -> None

    /// String reader function
    let stringReader : (string * string) list -> ParserInfo -> obj =
        box |> buildValueReader

/// Dictionary related deserialization logic
module internal DictionaryDeserializer =

    open System.Collections
    open System.Collections.Generic
    open System.Collections.Specialized

    open SharpXml.XmlParser

    let parseKeyValueCollection invoker (keyReader : ReaderFunc) (valueReader : ReaderFunc) attr (input : ParserInfo) =
        let rec inner() =
            if not input.IsEnd then
                let itemTag, _ = eatSomeTag input
                if not input.IsEnd && itemTag <> TagType.Close then
                    // read key tag
                    eatSomeTag input |> ignore
                    let key = keyReader attr input
                    let vTag, _ = eatSomeTag input
                    if not input.IsEnd && vTag <> TagType.Close then
                        let value = valueReader attr input
                        invoker key value
                        eatClosingTag input
                        inner()
        inner()

    /// Dictionary reader function
    let dictReader<'a, 'b when 'a : equality> (keyReader : ReaderFunc) (valueReader : ReaderFunc) attr xml =
        let dictionary = Dictionary<'a, 'b>()
        let invoker (key : obj) (value : obj) =
            dictionary.[key :?> 'a] <- value :?> 'b
        parseKeyValueCollection invoker keyReader valueReader attr xml
        dictionary

    /// Reader function for non-generic NameValueCollection
    let nameValueCollectionReader (ctor : unit -> #NameValueCollection) attr xml =
        let collection = ctor()
        let invoker (key : obj) (value : obj) =
            collection.[key :?> string] <- value :?> string
        let reader = ValueTypeDeserializer.stringReader
        parseKeyValueCollection invoker reader reader attr xml
        box collection

    /// Reader function for non-generic HashTable
    let hashTableReader attr xml =
        let table = Hashtable()
        let invoker (key : obj) (value : obj) =
            table.Add(key :?> string, value :?> string)
        let reader = ValueTypeDeserializer.stringReader
        parseKeyValueCollection invoker reader reader attr xml
        box table

/// List related deserialization logic
module internal ListDeserializer =

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Collections.Specialized

    open SharpXml.XmlParser

    /// Parse one element for a deserialised list structure
    let inline parseListElement<'a> (reader : ReaderFunc) attr element =
        match reader attr element with
        | null -> None
        | x -> Some(x :?> 'a)

    let getListTag info =
        if XmlConfig.Instance.UseAttributes then
            let name, tag, attr = eatTagWithAttributes info
            tag, attr
        else
            eatSomeTag info |> fst, []

    let parseList<'a> (elemInfo : TypeBuilderInfo) (elemParser : ReaderFunc) attr (info : ParserInfo) =
        let list = List<'a>()
        let rec inner() =
            if not info.IsEnd then
                let tag, attrs = getListTag info
                if not info.IsEnd then
                    match tag with
                    | TagType.Open ->
                        let value = elemParser attrs info
                        // add only non-null values into the target list
                        // putting 'null' into lists does not make much sense anyway
                        if value <> null then
                            list.Add(value :?> 'a)
                        inner()
                    | TagType.Single ->
                        try
                            // try to instantiate a new object of the single XML tag
                            let value = elemInfo.Ctor.Invoke() :?> 'a
                            list.Add(value)
                            // try to set its attribute properties
                            if not attrs.IsEmpty then
                                Deserialization.setAttributes elemInfo attrs value
                        with ex -> Deserialization.deserializeErrorCreateInst typeof<'a> ex
                        inner()
                    | _ -> ()
        inner()
        list

    let parseListUntyped (lst : IList) (elemParser : ReaderFunc) attr (info : ParserInfo) =
        let rec inner() =
            if not info.IsEnd then
                let tag, attrs = getListTag info
                if not info.IsEnd then
                    match tag with
                    | TagType.Open ->
                        let value = elemParser attrs info
                        lst.Add(value) |> ignore
                        inner()
                    | TagType.Single -> inner()
                    | _ -> ()
        inner()

    /// Reader function for immutable F# lists
    let listReader<'a> info (reader : ReaderFunc) attr xml =
        let list = parseList<'a> info reader attr xml
        List.ofSeq list

    /// Reader function for CLR list (System.Collections.Generic.List<T>)
    let clrListReader<'a> info (reader : ReaderFunc) attr xml =
        parseList<'a> info reader attr xml

    /// Reader function for arrays
    let arrayReader<'a> info (reader : ReaderFunc) attr xml =
        let list = parseList<'a> info reader attr xml
        list.ToArray()

    /// Reader function for untyped collections
    let collectionReader (ctor : EmptyConstructor) attr xml =
        let list = ctor.Invoke() :?> IList
        parseListUntyped list ValueTypeDeserializer.stringReader attr xml
        box list

    /// Reader function for hash sets
    let hashSetReader<'a> info (reader : ReaderFunc) attr xml =
        HashSet(parseList<'a> info reader attr xml)

    /// Reader function for generic collections
    let genericCollectionReader<'a> (reader : ReaderFunc) (info : TypeBuilderInfo) elemInfo attr xml =
        let collection = info.Ctor.Invoke() :?> ICollection<'a>
        let list = parseList<'a> elemInfo reader attr xml
        list.ForEach(fun elem -> collection.Add(elem))
        Deserialization.setAttributes info attr collection
        collection

    /// Reader function for readonly collections
    let genericROReader<'a> (reader : ReaderFunc) (ctor : System.Reflection.ConstructorInfo) info attr xml =
        let list = clrListReader<'a> info reader attr xml
        ctor.Invoke([| list |])

    /// Reader function for queues
    let queueReader<'a> info (reader : ReaderFunc) attr xml =
        Queue<'a>(parseList<'a> info reader attr xml)

    /// Reader function for stacks
    let stackReader<'a> info (reader : ReaderFunc) attr xml =
        Stack<'a>(parseList<'a> info reader attr xml)

    /// Reader function for generic linked lists
    let linkedListReader<'a> info (reader : ReaderFunc) attr xml =
        LinkedList<'a>(parseList<'a> info reader attr xml)

    /// Convenience function to build a TypeBuilderInfo with
    /// type information and an empty constructor only
    let emptyTypeBuilder (t : Type) (ctor : EmptyConstructor) =
        { Type = t
          Props = Dictionary<string, PropertyReaderInfo>()
          Attrs = Dictionary<string, AttributeReaderInfo>()
          Ctor = ctor }

    /// Specialized reader function for string arrays
    let stringArrayReader attr xml =
        let ctor = EmptyConstructor(fun () -> box String.Empty)
        let info = emptyTypeBuilder typeof<string> ctor
        listReader<string> info ValueTypeDeserializer.stringReader attr xml |> List.toArray |> box

    /// Specialized reader function for integer arrays
    let intArrayReader attr xml =
        let ctor = EmptyConstructor(fun () -> box 0)
        let info = emptyTypeBuilder typeof<int> ctor
        let intReader = Int32.Parse >> box |> ValueTypeDeserializer.buildValueReader
        listReader<int> info intReader attr xml |> List.toArray |> box

    /// Specialized reader function for byte arrays
    let byteArrayReader attr xml =
        let reader = ValueTypeDeserializer.buildValueReader Convert.FromBase64String
        reader attr xml |> box

    /// Specialized reader function for char arrays
    let charArrayReader attr xml =
        let reader = ValueTypeDeserializer.buildValueReader (fun v -> v.ToCharArray())
        reader attr xml |> box

/// Deserialization logic
module internal Deserializer =

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Collections.ObjectModel
    open System.Collections.Specialized
    open System.Reflection

    open Microsoft.FSharp.Reflection

    open SharpXml.Attempt
    open SharpXml.Extensions
    open SharpXml.TypeHelper
    open SharpXml.Utils
    open SharpXml.XmlParser

    /// Name of the static parsing method
    let parseMethodName = "ParseXml"

    /// BindingFlags to find the static parse method
    let parseMethodFlags = BindingFlags.Public ||| BindingFlags.Static

    /// TypeBuilder dictionary
    let propertyCache = ref (Dictionary<Type, TypeBuilderInfo>())

    /// Reader function cache
    let readerCache = ref (Dictionary<Type, ReaderFunc>())

    /// Try to find a constructor of the specified type
    /// with a single string parameter
    let findStringConstructor (t : Type) =
        t.GetConstructors()
        |> Array.tryFind (fun ctor ->
            let ps = ctor.GetParameters()
            ps.Length = 1 && ps.[0].ParameterType = typeof<string>)

    /// String type constructor parser
    let stringTypeConstructor (ctor : ConstructorInfo) (builderInfo : TypeBuilderInfo) attrs (info: ParserInfo) =
        let reader = fun (v : string) -> ctor.Invoke([| v |])
        let valReader = ValueTypeDeserializer.buildValueReader reader
        let value = valReader attrs info
        Deserialization.setAttributes builderInfo attrs value
        value

    /// Try to retrieve the specified deserialization function of
    /// SharpXml.ListDeserializer module via reflection
    let getGenericListFunction name t =
        // TODO: I don't like this string-based reflection at all
        let flags = BindingFlags.NonPublic ||| BindingFlags.Static
        let reader = Type.GetType("SharpXml.ListDeserializer").GetMethod(name, flags)
        reader.MakeGenericMethod([| t |])

    /// Try to find the static 'ParseXml' method on the specified type
    let findStaticParseMethod (t : Type) =
        t.GetMethod(parseMethodName, parseMethodFlags, null, [| typeof<string> |], null)
        |> toOption

    /// Try to get a reader based on the type's static 'ParseXml' method
    let getStaticParseMethod (t : Type) = fun () ->
        match findStaticParseMethod t with
        | Some parse ->
            // TODO: maybe use Delegate.CreateDelegate()
            fun (v : string) -> parse.Invoke(null, [| v |])
            |> ValueTypeDeserializer.buildValueReader
            |> Some
        | _ -> None

    /// Reader function that utilizes a custom DeserializerFunc
    let customDeserializerReader (func: DeserializerFunc) = fun attr (info: ParserInfo) ->
        if not info.IsEnd then
            let start = info.Index
            let index = eatUnknownTilClosing info
            let toParse = new String(info.Value, start, index - start)
            func.Invoke(toParse)
        else null

    /// Get a reader function for NameValueCollection types
    let getNameValueCollectionReader (t : Type) =
        let ctor =
            if t = typeof<NameValueCollection> then
                fun() -> NameValueCollection()
            else
                let func = ReflectionHelpers.getConstructorMethod t
                fun() -> func.Invoke() :?> NameValueCollection
        Some <| DictionaryDeserializer.nameValueCollectionReader ctor

    /// Determine the property that should be used for
    /// deserialization purposes
    let getPropertyName pi =
        match getAttribute<SharpXml.Common.XmlElementAttribute> pi with
        | Some attr when notWhite attr.Name -> attr.Name
        | _ -> pi.Name

    /// Determine attribute reader function
    let getAttributeReader (t : Type) =
        if t.IsEnum then
            let parse x = Enum.Parse(t, x)
            Some (DeserializerFunc(parse))
        else
            match ValueTypeDeserializer.getValueParser t with
            | Some reader -> Some (DeserializerFunc(reader))
            | _ -> None

    /// Try to determine a AttributeReaderInfo record based on
    /// the specified PropertyInfo
    let getAttributeReaderInfo (p : PropertyInfo) : AttributeReaderInfo option =
        let t = p.PropertyType.NullableUnderlying()
        let reader =
            match XmlConfig.Instance.TryGetDeserializer t with
            | Some deserializer -> Some deserializer
            | None -> getAttributeReader t
        match reader with
        | Some r ->
            { Info = p;
              Reader = r;
              Setter = ReflectionHelpers.getObjSetter p;
              Ctor = ReflectionHelpers.getConstructorMethod t } |> Some
        | None ->
            System.Diagnostics.Trace.WriteLine(
                String.Format("Unable to determine deserializer for attribute property '{0}' of type {1}",
                    p.Name, t))
            None

    /// Build the PropertyReaderInfo record based on the given PropertyInfo
    let rec buildReaderInfo (p : PropertyInfo) : PropertyReaderInfo = {
        Info = p;
        Reader = getReaderFunc p.PropertyType;
        Setter = ReflectionHelpers.getObjSetter p;
        Ctor = ReflectionHelpers.getConstructorMethod p.PropertyType }

    /// Determine the property and its PropertyReaderInfo that
    /// will be used for deserialization
    and determineProperty (pDict : Dictionary<string, PropertyReaderInfo>, aDict) pi =
        let name = getPropertyName pi
        pDict.Add(name, buildReaderInfo pi)
        pDict, aDict

    /// Determine the property and its PropertyReaderInfo in case
    /// XML attributes are supported
    and determinePropertyWithAttribute (pDict : Dictionary<string, PropertyReaderInfo>, aDict : Dictionary<string, AttributeReaderInfo>) pi =
        match getAttribute<SharpXml.Common.XmlAttributeAttribute> pi with
        | Some attr ->
            let name = if notWhite attr.Name then attr.Name else pi.Name
            match getAttributeReaderInfo pi with
            | Some info -> aDict.Add(name, info)
            | _ -> ()
        | _ ->
            let name = getPropertyName pi
            pDict.Add(name, buildReaderInfo pi)
        pDict, aDict

    /// Build the TypeBuilderInfo record for the given Type
    and buildTypeBuilderInfo (t : Type) =
        let func = if XmlConfig.Instance.UseAttributes then determinePropertyWithAttribute else determineProperty
        let props, attrs =
            ReflectionHelpers.getDeserializableProperties t
            |> Array.fold func
                (Dictionary<string, PropertyReaderInfo>(StringComparer.OrdinalIgnoreCase),
                 Dictionary<string, AttributeReaderInfo>(StringComparer.OrdinalIgnoreCase))
        { Type = t
          Props = props
          Attrs = attrs
          Ctor = ReflectionHelpers.getConstructorMethod t }

    /// Determine the TypeBuilderInfo for the given Type
    and getTypeBuilderInfo (t : Type) =
        match (!propertyCache).TryGetValue t with
        | true, builder -> builder
        | _ ->
            let builder = buildTypeBuilderInfo t
            Atom.updateAtomDict propertyCache t builder

    and setSingleTagAttributes (prop : PropertyReaderInfo) attrs inst propInst =
        // TODO: get rid of this lookup
        let info = getTypeBuilderInfo prop.Info.PropertyType
        Deserialization.setAttributes info attrs propInst

    and buildGenericFunction name t =
        let mtd = getGenericListFunction name t
        let elemReader = getReaderFunc t
        let tbi = getTypeBuilderInfo t
        fun attr (xml : ParserInfo) -> mtd.Invoke(null, [| tbi; elemReader; attr; xml |])

    /// Get a reader function for generic F# lists
    and getTypedFsListReader =
        buildGenericFunction "listReader"

    /// Get a reader function for generic lists
    and getTypedListReader =
        buildGenericFunction "clrListReader"

    /// Get a reader function for arrays
    and getTypedArrayReader =
        buildGenericFunction "arrayReader"

    /// Get a reader function for hash sets
    and getHashSetReader =
        buildGenericFunction "hashSetReader"

    /// Get a reader function for queues
    and getQueueReader =
        buildGenericFunction "queueReader"

    /// Get a reader function for stacks
    and getStackReader =
        buildGenericFunction "stackReader"

    /// Get a reader function for generic linked lists
    and getLinkedListReader =
        buildGenericFunction "linkedListReader"

    /// Get a reader function for generic collections
    and getGenericCollectionReader (listType : Type) (t : Type) =
        let mtd = getGenericListFunction "genericCollectionReader" t
        let elemReader = getReaderFunc t
        let ctor = getTypeBuilderInfo listType
        let tbi = getTypeBuilderInfo t
        fun attr (xml : ParserInfo) -> mtd.Invoke(null, [| elemReader; ctor; tbi; attr; xml |])

    /// Get a reader function for generic readonly collections
    and getGenericROReader ctor (listType : Type) (t : Type) =
        let mtd = getGenericListFunction "genericROReader" t
        let elemReader = getReaderFunc t
        let tbi = getTypeBuilderInfo t
        fun attr (xml : ParserInfo) -> mtd.Invoke(null, [| elemReader; ctor; tbi; attr; xml |])

    /// Try to get a reader based on a string value constructor
    and getStringTypeConstructor (t : Type) = fun () ->
        match findStringConstructor t with
        | Some ctor ->
            let builder = getTypeBuilderInfo t
            stringTypeConstructor ctor builder |> Some
        | _ -> None

    /// Try to determine a reader function for array types
    and getArrayReader (t : Type) = fun () ->
        if not t.IsArray then None else
            if t = typeof<string[]> then Some ListDeserializer.stringArrayReader
            elif t = typeof<int[]> then Some ListDeserializer.intArrayReader
            elif t = typeof<byte[]> then Some ListDeserializer.byteArrayReader
            elif t = typeof<char[]> then Some ListDeserializer.charArrayReader
            else
                let elem = t.GetElementType()
                Some <| getTypedArrayReader elem

    /// Try to determine a reader function for list types
    and getListReader (t : Type) = fun () ->
        if isGenericType t then
            match t with
            | GenericTypeOf GenericTypes.roColl gen ->
                let param = GenericTypes.iList.MakeGenericType([| gen |])
                let ctor = t.GetConstructor([| param |])
                if ctor <> null then Some <| getGenericROReader ctor t gen else None
            | GenericTypeOf GenericTypes.hashSet gen -> Some <| getHashSetReader gen
            | GenericTypeOf GenericTypes.linkedList gen -> Some <| getLinkedListReader gen
            | GenericTypeOf GenericTypes.iColl gen ->
                if hasGenericTypeDefinitions t [| GenericTypes.list |]
                then Some <| getTypedListReader gen
                else Some <| getGenericCollectionReader t gen
            | GenericTypeOf GenericTypes.queue gen -> Some <| getQueueReader gen
            | GenericTypeOf GenericTypes.stack gen -> Some <| getStackReader gen
            | GenericTypeOf GenericTypes.fsList gen -> Some <| getTypedFsListReader gen
            | _ -> None
        elif isOrDerived t typeof<NameValueCollection> then
            getNameValueCollectionReader t
        elif matchInterface typeof<IList> t then
            let ctor = ReflectionHelpers.getConstructorMethod t
            let reader = ListDeserializer.collectionReader ctor
            Some reader
        else None

    and getTypedDictionaryReader key value =
        // TODO: this does not look sane at all
        let flags = BindingFlags.NonPublic ||| BindingFlags.Static
        let reader = Type.GetType("SharpXml.DictionaryDeserializer").GetMethod("dictReader", flags)
        let mtd = reader.MakeGenericMethod([| key; value |])
        let keyReader = getReaderFunc key
        let valueReader = getReaderFunc value
        fun attr (xml : ParserInfo) -> mtd.Invoke(null, [| keyReader; valueReader; attr; xml |])

    and getDictionaryReader (t : Type) = fun () ->
        let dictInterface = typeof<IDictionary>
        if matchInterface dictInterface t then
            match t with
            | GenericTypesOf GenericTypes.dict (k, v) ->
                Some <| getTypedDictionaryReader k v
            | _ when t = typeof<Hashtable> ->
                Some <| DictionaryDeserializer.hashTableReader
            | _ -> None
        else None

    /// Class reader function
    and readClass (builder : TypeBuilderInfo) attr (xml : ParserInfo) =
        let instance = builder.Ctor.Invoke()
        let rec inner() =
            if not xml.IsEnd then
                let name, tag = eatTag xml
                match tag with
                | TagType.Open ->
                    match builder.Props.TryGetValue name with
                    | true, prop ->
                        try
                            let reader = prop.Reader
                            prop.Setter.Invoke(instance, reader attr xml)
                        with ex ->
                            Deserialization.deserializeErrorProperty name builder.Type ex
                            eatUnknownTilClosing xml |> ignore
                    | _ ->
                        Deserialization.elementNotExist name builder.Type
                        eatUnknownTilClosing xml |> ignore
                    inner()
                | TagType.Single ->
                    match builder.Props.TryGetValue name with
                    | true, prop ->
                        try
                            let propInstance = prop.Ctor.Invoke()
                            prop.Setter.Invoke(instance, propInstance)
                        with ex ->
                            Deserialization.deserializeErrorCreateInst prop.Info.PropertyType ex
                    | _ -> Deserialization.elementNotExist name builder.Type
                    inner()
                | _ -> ()
        inner()
        instance

    /// Class reader function with XML attribute support
    and readClassWithAttributes (builder : TypeBuilderInfo) attr (xml : ParserInfo) =
        let instance = builder.Ctor.Invoke()
        let rec inner() =
            if not xml.IsEnd then
                let name, tag, attrs = eatTagWithAttributes xml
                match tag with
                | TagType.Open ->
                    match builder.Props.TryGetValue name with
                    | true, prop ->
                        try
                            let reader = prop.Reader
                            prop.Setter.Invoke(instance, reader attrs xml)
                        with ex ->
                            Deserialization.deserializeErrorProperty name builder.Type ex
                            eatUnknownTilClosing xml |> ignore
                    | _ ->
                        Deserialization.elementNotExist name builder.Type
                        eatUnknownTilClosing xml |> ignore

                    Deserialization.setAttributes builder attr instance
                    inner()
                | TagType.Single ->
                    match builder.Props.TryGetValue name with
                    | true, prop ->
                        try
                            let propInstance = prop.Ctor.Invoke()
                            prop.Setter.Invoke(instance, propInstance)
                            if not attrs.IsEmpty then
                                setSingleTagAttributes prop attrs instance propInstance
                        with ex ->
                            Deserialization.deserializeErrorCreateInst prop.Info.PropertyType ex
                    | _ -> Deserialization.elementNotExist name builder.Type
                    Deserialization.setAttributes builder attr instance
                    inner()
                | _ -> Deserialization.setAttributes builder attr instance
        inner()
        instance

    /// Try to determine a matching class reader function
    and getClassReader (t : Type) = fun () ->
        if t.IsClass && not t.IsAbstract then
            let func = if XmlConfig.Instance.UseAttributes then readClassWithAttributes else readClass
            getTypeBuilderInfo t
            |> func
            |> Some
        else None

    /// Build a RecordBuilderInfo for the specified F# record type
    and getRecordBuilderInfo (t : Type) : RecordBuilderInfo =
        let fields = FSharpType.GetRecordFields t
        let readers =
            fields
            // we have to lowercase the property name because
            // the F# map is case-sensitive
            |> Array.mapi (fun i pi -> pi.Name.ToLowerInvariant(), (getReaderFunc pi.PropertyType, i))
            |> Map.ofArray
        { Type = t; Readers = readers; Ctor = FSharpValue.PreComputeRecordConstructor t; Fields = fields.Length }

    /// Reader function for F# record types
    and readRecord (rb : RecordBuilderInfo) attr (xml : ParserInfo) =
        let objects = Array.zeroCreate<obj> rb.Fields
        let rec inner() =
            if not xml.IsEnd then
                let name, tag = eatTag xml
                match tag with
                | TagType.Open ->
                    // we have to lowercase here because the map is case-sensitive
                    let lower = name.ToLowerInvariant()
                    match rb.Readers.TryFind lower with
                    | Some (reader, i) ->
                        try
                            objects.[i] <- reader attr xml
                        with ex -> Deserialization.deserializeErrorRecord lower rb.Type ex
                    | _ -> ()
                    inner()
                | TagType.Single -> inner()
                | _ -> ()
        inner()
        eatClosingTag xml
        rb.Ctor objects

    /// Reader function for F# tuple types
    and readTuple (tb : TupleBuilderInfo) attr (xml : ParserInfo) =
        let objects = Array.zeroCreate<obj> tb.Fields
        let rec inner index  =
            if not xml.IsEnd && index < tb.Fields then
                let tag, _ = eatSomeTag xml
                match tag with
                | TagType.Open ->
                    try
                        objects.[index] <- tb.Readers.[index] attr xml
                    with ex ->
                        let name = sprintf "item %d" (index+1)
                        Deserialization.deserializeErrorTuple name tb.Type ex
                    inner (index+1)
                | TagType.Single -> inner (index+1)
                | _ -> ()
        inner 0
        eatClosingTag xml
        tb.Ctor objects

    /// Try to determine a reader function for F# record types
    and getFsRecordReader (t : Type) = fun() ->
        if FSharpType.IsRecord t then
            getRecordBuilderInfo t
            |> readRecord
            |> Some
        else None

    /// Build a TupleBuilderInfo for the specified F# tuple type
    and getTupleBuilderInfo (t : Type) : TupleBuilderInfo =
        let items = FSharpType.GetTupleElements t
        let readers =
            items
            |> Array.map getReaderFunc
        let ctor = FSharpValue.PreComputeTupleConstructor t
        { Type = t; Readers = readers; Fields = items.Length; Ctor = ctor }

    /// Try to determine a reader function for F# tuple types
    and getFsTupleReader (t : Type) = fun() ->
        if FSharpType.IsTuple t then
            getTupleBuilderInfo t
            |> readTuple
            |> Some
        else None

    /// Reader function for F# discriminated union types
    and readUnion (ui : UnionBuilderInfo) attr (xml : ParserInfo) =
        // TODO: instead of returning null on failure or
        // unknown union cases we could return a default value
        // (i.e. the union case with tag 0) instead
        if not xml.IsEnd then
            let name, tag = eatTag xml
            let lower = name.ToLowerInvariant()
            match tag with
            | TagType.Open ->
                match ui.Readers.TryFind lower with
                | Some (rs, ctor) ->
                    let parts = Array.zeroCreate<obj> rs.Length
                    let inline inject i reader = parts.[i] <- reader attr xml
                    match rs with
                    | [||] ->
                        // this is a constructor without any arguments
                        // so we can initialize the type with an empty array
                        eatClosingTag xml
                    | [| reader |] ->
                        // this is a single argument constructor
                        // so we don't have to search for 'tuple-like' xml structure
                        inject 0 reader
                    | readers ->
                        // union cases with multi-argument constructors
                        // are serialized in a 'tuple-like' xml structure
                        let parseTupleLike i reader =
                            if not xml.IsEnd then
                                let tag, _ = eatSomeTag xml
                                match tag with
                                | TagType.Open -> inject i reader
                                | _ -> ()
                        readers
                        |> Array.iteri parseTupleLike
                    // we have to eat the enclosing close tag
                    eatClosingTag xml
                    ctor parts
                // there is no union case with the current name
                | None ->
                    // first we have to skip the closing tag of the unknown union case
                    eatClosingTag xml
                    // then we have to skip the enclosing tag of the value itself
                    eatClosingTag xml
                    null
            // single xml tags may be 0-argument constructor union cases only
            | TagType.Single ->
                eatClosingTag xml
                match ui.Readers.TryFind lower with
                | Some ([||], ctor) -> ctor [||]
                | _ -> null
            | _ -> null
        else null

    /// Build a UnionBuilderInfo for the specified union type
    and getUnionBuilderInfo (t : Type) =
        let mapCase (info : UnionCaseInfo) =
            let name = info.Name.ToLowerInvariant()
            let ctor = FSharpValue.PreComputeUnionConstructor info
            let readers =
                info.GetFields()
                |> Array.map (fun pi -> getReaderFunc pi.PropertyType)
            name, (readers, ctor)
        let getUnionReaders =
            FSharpType.GetUnionCases t
            |> Array.map mapCase
            |> Map.ofArray
        { Type = t; Readers = getUnionReaders }

    /// Try to determine a F# discriminated union reader
    and getFsUnionReader (t : Type) = fun() ->
        if FSharpType.IsUnion t then
            getUnionBuilderInfo t
            |> readUnion
            |> Some
        else None

    /// Determine the ReaderFunc delegate for the given Type
    and determineReader (objType : Type) =
        let t = objType.NullableUnderlying()
        let reader = attempt {
            let! enumReader = ValueTypeDeserializer.getEnumReader t
            let! valueReader = ValueTypeDeserializer.getValueReader t
            let! arrayReader = getArrayReader t
            let! dictReader = getDictionaryReader t
            let! listReader = getListReader t
            let! staticReader = getStaticParseMethod t
            let! stringCtor = getStringTypeConstructor t
            let! parseXmlReader = getStaticParseMethod t
            let! recordReader = getFsRecordReader t
            let! tupleReader = getFsTupleReader t
            let! unionReader = getFsUnionReader t
            let! classReader = getClassReader t
            classReader }
        reader

    /// Get the ReaderFunc for the specified type.
    /// The function is either obtained from the cache or built on request
    and getReaderFunc (t : Type) =
        match XmlConfig.Instance.TryGetDeserializer t with
        | Some custom -> customDeserializerReader custom
        | None ->
            match (!readerCache).TryGetValue t with
            | true, reader -> reader
            | _ ->
                match determineReader t with
                | Some func -> Atom.updateAtomDict readerCache t func
                | _ ->
                    let err = sprintf "could not determine deserialization logic for type '%s'" t.FullName
                    raise (SharpXmlException err)

    /// Clear the deserializer cache
    let clearCache() =
        Atom.clearAtomDict readerCache
