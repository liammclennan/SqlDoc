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

module Types =

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Collections.Specialized
    open System.Runtime.Serialization

    open SharpXml.Common

    type TestEnum =
        | Undefined = 0
        | Foo = 1
        | Bar = 2

    [<Flags>]
    type FlagsEnum =
        | Undefined = 0
        | Ham = 1
        | Eggs = 2

    type TestUnion1 =
        | One
        | Two of int
        | Three of string * int
        | Four of string list

    type CustomList<'T> =
        inherit List<'T>

        new () = { inherit List<'T>() }
        new (collection : IEnumerable<'T>) = { inherit List<'T>(collection) }

        member x.AddPair(first, second) =
            x.Add(first)
            x.Add(second)

    type CustomNameValueCollection =
        inherit NameValueCollection

        new () = { inherit NameValueCollection() }

        member x.AddRange (items : (string * string) seq) =
            items
            |> Seq.iter (fun (k, v) -> x.Add(k, v))

    type CustomDateTime(date : DateTime) =

        let mutable _date = date

        member x.Date
            with get() = _date
            and set(v) = _date <- v

    type CustomDecimal(value : decimal) =

        let mutable _value = value

        member x.Value
            with get() = _value
            and set(v) = _value <- v

    type ObjectPropClass() =

        let mutable v1 = Unchecked.defaultof<string>
        let mutable success = null

        member x.V1
            with get() = v1
            and set(v) = v1 <- v

        member x.Success
            with get() = success
            and set(v) = success <- v

        member x.IsSuccess
            with get() = x.Success <> null

    type TestClass(val1 : int, val2 : string) =

        let mutable v1 = val1
        let mutable v2 = val2

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type SimpleClass() =

        let mutable v1 = Unchecked.defaultof<string>
        let mutable v2 = Unchecked.defaultof<int>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type TestClass3() =

        let mutable v1 = Unchecked.defaultof<string[]>
        let mutable v2 = Unchecked.defaultof<int>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type TestClass4() =

        let mutable v1 = Unchecked.defaultof<TestClass[]>
        let mutable v2 = Unchecked.defaultof<int>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type ListClass() =

        let mutable v1 = Unchecked.defaultof<List<TestClass>>
        let mutable v2 = Unchecked.defaultof<int>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type DictClass() =

        let mutable v1 = Unchecked.defaultof<Dictionary<string, int>>
        let mutable v2 = Unchecked.defaultof<int>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type EnumClass() =

        let mutable v1 = Unchecked.defaultof<TestEnum>
        let mutable v2 = Unchecked.defaultof<int>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type NestedClass() =

        let mutable v1 = Unchecked.defaultof<string>
        let mutable v2 = Unchecked.defaultof<SimpleClass>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    [<XmlElement(Name = "myClass")>]
    type AttributedClass() =

        let mutable v1 = Unchecked.defaultof<string>
        let mutable v2 = Unchecked.defaultof<SimpleClass>

        [<XmlElement(Name = "A")>]
        member x.V1
            with get() = v1
            and set v = v1 <- v
        [<XmlElement(Name = "B")>]
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type AttributedDictClass() =

        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<Dictionary<string, int>>

        [<XmlElement(Name = "A")>]
        member x.V1
            with get() = v1
            and set v = v1 <- v
        [<XmlElement(Name = "B", ItemName = "x", KeyName = "k", ValueName = "v")>]
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type AttributedListClass() =

        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<List<string>>

        [<XmlElement(Name = "A")>]
        member x.V1
            with get() = v1
            and set v = v1 <- v
        [<XmlElement(ItemName = "x")>]
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type NestedClass2() =

        let mutable v1 = Unchecked.defaultof<string>
        let mutable v2 = Unchecked.defaultof<NestedClass2>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type IEnumerableClass() =

        let mutable v1 = Unchecked.defaultof<string>
        let mutable v2 = Unchecked.defaultof<IEnumerable<int>>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type CustomListClass() =

        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<CustomList<string>>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type StringCtorClass(xml : string) =

        let parts = xml.Split([| 'x' |])

        let mutable x = Int32.Parse(parts.[0])
        let mutable y = Int32.Parse(parts.[1])

        member this.X
            with get() = x
            and set v = x <- v
        member this.Y
            with get() = y
            and set v = y <- v

    type CustomParserClass() =

        let mutable x = Unchecked.defaultof<int>
        let mutable y = Unchecked.defaultof<int>
        let mutable attr = Unchecked.defaultof<string>

        [<XmlAttribute("attr")>]
        member this.Attr
            with get() = attr
            and set v = attr <- v

        member this.X
            with get() = x
            and set v = x <- v
        member this.Y
            with get() = y
            and set v = y <- v

        member this.ToXml() =
            if x > 0 && y > 0 then sprintf "%dx%d" this.X this.Y
            else null

        static member ParseXml(input : string) =
            match input.Split('x') with
            | [| x; y |] -> CustomParserClass(X = Int32.Parse(x), Y = Int32.Parse(y))
            | _ -> failwith "invalid input for CustomParserClass"

    type CustomParserClass2() =

        let mutable _x = Unchecked.defaultof<int>
        let mutable _y = Unchecked.defaultof<int>

        member x.X
            with get() = _x
            and set v = _x <- v
        member x.Y
            with get() = _y
            and set v = _y <- v

        static member ToXml(element : CustomParserClass2) =
            sprintf "%dx%d" element.X element.Y

        static member ParseXml(input : string) =
            match input.Split('x') with
            | [| x; y |] -> CustomParserClass(X = Int32.Parse(x), Y = Int32.Parse(y))
            | _ -> failwith "invalid input for CustomParserClass"

    type ArrayListClass() =

        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<ArrayList>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type OrderClass01() =
        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<string>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type OrderClass02() =
        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<string>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        [<XmlElementAttribute(Order = 1)>]
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type OrderClass03() =
        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<string>

        [<XmlElementAttribute(Order = 1)>]
        member x.V1
            with get() = v1
            and set v = v1 <- v
        [<XmlElementAttribute(Order = 4)>]
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type FSharpListClass() =

        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<string list>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type GenericClass<'T>() =

        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<'T>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type TupleClass() =

        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<Tuple<string, int>>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type GenericListClass<'T>() =

        let mutable v1 = Unchecked.defaultof<int>
        let mutable v2 = Unchecked.defaultof<List<'T>>

        member x.V1
            with get() = v1
            and set v = v1 <- v
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type Guest(id : int) =

        let mutable firstName = Unchecked.defaultof<string>
        let mutable lastName = Unchecked.defaultof<string>

        member x.FirstName
            with get() = firstName
            and set v = firstName <- v
        member x.LastName
            with get() = lastName
            and set v = lastName <- v
        member x.Id
            with get() = id

    type Booking(name : string, guests : List<Guest>) =

        let mutable _name = name
        let mutable _guests = guests

        member x.Name
            with get() = _name
            and set v = _name <- v
        member x.Guests
            with get() = _guests
            and set v = _guests <- v

    type AttributeClass() =

        let mutable value = Unchecked.defaultof<int>
        let mutable attr = Unchecked.defaultof<string>

        member x.Value
            with get() = value
            and set v = value <- v

        [<XmlAttribute("attr")>]
        member x.Attr
            with get() = attr
            and set v = attr <- v

    [<XmlNamespace("foo", "bar")>]
    type StaticAttributeClass() =

        let mutable value = Unchecked.defaultof<int>

        member x.Value
            with get() = value
            and set v = value <- v

    [<XmlNamespace("bar", "foo")>]
    type InheritedStaticAttributeClass() =
        inherit StaticAttributeClass()

    [<XmlNamespace("bar", "foo")>]
    type AttributeClass2() =
        let mutable value = Unchecked.defaultof<int>
        let mutable attr = Unchecked.defaultof<string>

        member x.Value
            with get() = value
            and set v = value <- v

        [<XmlAttribute("attr")>]
        member x.Attr
            with get() = attr
            and set v = attr <- v

    type GenAttributeClass<'T>() =
        let mutable value = Unchecked.defaultof<int>
        let mutable attr = Unchecked.defaultof<'T>

        member x.Value
            with get() = value
            and set v = value <- v

        [<XmlAttribute("attr")>]
        member x.Attr
            with get() = attr
            and set v = attr <- v

    type AttributeOnlyClass() =
        let mutable value = Unchecked.defaultof<int>
        let mutable attr = Unchecked.defaultof<string>

        [<XmlAttribute("value")>]
        member x.Value
            with get() = value
            and set v = value <- v

        [<XmlAttribute("attr")>]
        member x.Attr
            with get() = attr
            and set v = attr <- v

    type AttributeList<'T>() =
        inherit List<'T>()

        let mutable attr = Unchecked.defaultof<string>

        [<XmlAttribute("attr")>]
        member x.Attr
            with get() = attr
            and set(v) = attr <- v

    type AttrEnumClass() =
        let mutable attr = TestEnum.Undefined

        [<XmlAttribute("attr")>]
        member x.Attr
            with get() = attr
            and set(v) = attr <- v

    type ToXmlClass(value : string) =
        member x.ToXml() = value

    type XmlIgnoreClass(value : string) =
        let mutable attr = Unchecked.defaultof<string>

        [<XmlAttribute("attr")>]
        member x.Attr
            with get() = attr
            and set(v) = attr <- v

        [<XmlIgnore>]
        member x.Value
            with get() = value

        member x.ToXml() = value

    type NullableAttributeClass() =
        let mutable value = Unchecked.defaultof<int>
        let mutable attr = Unchecked.defaultof<Nullable<int>>

        member x.Value
            with get() = value
            and set v = value <- v

        [<XmlAttribute("attr")>]
        member x.Attr
            with get() = attr
            and set v = attr <- v

    [<DataContract(Name = "ContractClass", Namespace = "")>]
    type ContractClass() =

        let mutable v1 = Unchecked.defaultof<string>
        let mutable v2 = Unchecked.defaultof<int>

        [<DataMember>]
        member x.V1
            with get() = v1
            and set v = v1 <- v
        [<DataMember>]
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type TestRecord = {
        Value : int
        Name : string }

    type LargerRecord = {
        Id : string
        Value : int
        Foo : string
        Bar : string }

    [<DataContract(Name = "ContractClass2", Namespace = "")>]
    type ContractClass2() =

        let mutable v1 = Unchecked.defaultof<string>
        let mutable v2 = Unchecked.defaultof<Dictionary<string,int>>

        [<DataMember>]
        member x.V1
            with get() = v1
            and set v = v1 <- v
        [<DataMember>]
        member x.V2
            with get() = v2
            and set v = v2 <- v

    [<DataContract(Name = "ContractClass3", Namespace = "")>]
    type ContractClass3() =

        let mutable v1 = Unchecked.defaultof<string[]>
        let mutable v2 = Unchecked.defaultof<int>

        [<DataMember>]
        member x.V1
            with get() = v1
            and set v = v1 <- v
        [<DataMember>]
        member x.V2
            with get() = v2
            and set v = v2 <- v

    type ITestInterface =
        abstract member Member1 : int
            with get, set

    type IAnotherInterface =
        inherit ITestInterface
        abstract member Member2 : string
            with get, set
