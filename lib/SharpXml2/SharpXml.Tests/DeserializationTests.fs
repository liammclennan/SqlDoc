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

module DeserializationTests =

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Collections.ObjectModel
    open System.Collections.Specialized
    open System.Diagnostics
    open NUnit.Framework

    open SharpXml
    open SharpXml.Tests.TestHelpers
    open SharpXml.Tests.Types
    open SharpXml.Tests.CSharp

    let deserialize<'a> input =
        XmlSerializer.DeserializeFromString<'a>(input)

    [<SetUp>]
    let init() =
        XmlConfig.Instance.ClearDeserializers()

    [<Test>]
    let ``Can deserialize a simple class``() =
        let out = deserialize<TestClass> "<testClass><v1>42</v1><v2>bar</v2></testClass>"
        out.V1 |> should equal 42
        out.V2 |> should equal "bar"

    [<Test>]
    let ``Can deserialize string arrays``() =
        let out = deserialize<TestClass3> "<testClass><v1><item>foo</item><item>bar</item></v1><v2>42</v2></testClass>"
        out.V1.Length |> should equal 2
        out.V1 |> should equal [| "foo"; "bar" |]
        out.V2 |> should equal 42

    [<Test>]
    let ``Can deserialize string arrays with empty elements``() =
        let out = deserialize<TestClass3> "<testClass><v1><item /><item>foo</item><item/></v1><v2>42</v2></testClass>"
        out.V1.Length |> should equal 3
        out.V1 |> should equal [| ""; "foo"; "" |]
        out.V2 |> should equal 42

    [<Test>]
    let ``Can deserialize integer arrays``() =
        let out = deserialize<GenericClass<int[]>> "<testClass><v1>201</v1><v2><item>1</item><item>2</item></v2></testClass>"
        out.V1 |> should equal 201
        out.V2.Length |> should equal 2
        out.V2 |> should equal [| 1; 2 |]

    [<Test>]
    let ``Can deserialize char arrays from strings``() =
        let out = deserialize<char[]> "<array>char</array>"
        out |> should equal [| 'c'; 'h'; 'a'; 'r' |]

    [<Test>]
    let ``Can deserialize byte arrays``() =
        let array = [| 99uy; 100uy; 101uy |]
        let bytes = Convert.ToBase64String(array)
        let out = deserialize<byte[]> (sprintf "<array>%s</array>" bytes)
        out |> should equal array

    [<Test>]
    let ``Can deserialize class arrays``() =
        let out = deserialize<TestClass4> "<testClass4><v1><item><v1>42</v1><v2>foo</v2></item><item><v1>200</v1><v2>bar</v2></item></v1><v2>99</v2></testClass4>"
        out.V1.Length |> should equal 2
        out.V1.[0].V1 |> should equal 42
        out.V1.[1].V2 |> should equal "bar"
        out.V2 |> should equal 99

    [<Test>]
    let ``Can deserialize class lists``() =
        let out = deserialize<ListClass> "<listClass><v1><item><v1>42</v1><v2>foo</v2></item><item><v1>200</v1><v2>bar</v2></item></v1><v2>99</v2></listClass>"
        out.V1.Count |> should equal 2
        out.V1.[0].V1 |> should equal 42
        out.V1.[1].V2 |> should equal "bar"
        out.V2 |> should equal 99

    [<Test>]
    let ``Can deserialize string-keyed dictionaries``() =
        let out = deserialize<DictClass> "<dictClass><v1><item><key>foo</key><value>100</value></item><item><key>bar</key><value>200</value></item></v1><v2>99</v2></dictClass>"
        out.V1.Count |> should equal 2
        out.V1.["foo"] |> should equal 100
        out.V1.["bar"] |> should equal 200
        out.V2 |> should equal 99

    [<Test>]
    let ``Can deserialize enums``() =
        let out = deserialize<EnumClass> "<enumClass><v1>Foo</v1><v2>99</v2></enumClass>"
        out.V1 |> should equal TestEnum.Foo
        out.V2 |> should equal 99

    [<Test>]
    let ``Can deserialize untyped ArrayLists``() =
        let out = deserialize<ArrayListClass> "<arrayListClass><v1>937</v1><v2><item>ham</item><item>eggs</item></v2></arrayListClass>"
        out.V1 |> should equal 937
        out.V2.Count |> should equal 2
        out.V2.[0] |> should equal "ham"
        out.V2.[1] |> should equal "eggs"

    [<Test>]
    let ``Can deserialize generic custom list types``() =
        let out = deserialize<CustomListClass> "<customListClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></customListClass>"
        out.V1 |> should equal 100
        out.V2 |> shouldBe notNull
        out.V2.Count |> should equal 2
        out.V2.[0] |> should equal "foo"
        out.V2.[1] |> should equal "bar"

    [<Test>]
    let ``Can deserialize generic classes with generic lists``() =
        let out = deserialize<GenericListClass<string>> "<genericListClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></genericListClass>"
        out.V1 |> should equal 100
        out.V2 |> shouldBe notNull
        out.V2.Count |> should equal 2
        out.V2.[0] |> should equal "foo"
        out.V2.[1] |> should equal "bar"

    [<Test>]
    let ``Can deserialize classes with a static ParseXml function``() =
        let out = deserialize<CustomParserClass> "<customParserClass>200x400</CustomParserClass>"
        out.X |> should equal 200
        out.Y |> should equal 400

    [<Test>]
    let ``Can deserialize classes with a list of ParseXml-like classes``() =
        let out = deserialize<GenericListClass<CustomParserClass>> "<genericListClass><v1>99</v1><v2><item>100x200</item><item>200x400</item></v2></genericListClass>"
        out.V1 |> should equal 99
        out.V2.Count |> should equal 2
        out.V2.[0].Y |> should equal 200
        out.V2.[1].X |> should equal 200

    [<Test>]
    let ``Can deserialize classes with string constructors``() =
        let out = deserialize<StringCtorClass> "<stringCtorClass>300x50</stringCtorClass>"
        out.X |> should equal 300
        out.Y |> should equal 50

    [<Test>]
    let ``Can deserialize HashSets``() =
        let out = deserialize<GenericClass<HashSet<string>>> "<genericClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2 |> Seq.head  |> should equal "foo"
        out.V2 |> Seq.nth 1  |> should equal "bar"

    [<Test>]
    let ``Can deserialize Queues``() =
        let out = deserialize<GenericClass<Queue<string>>> "<genericClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2 |> Seq.head |> should equal "foo"
        out.V2 |> Seq.nth 1 |> should equal "bar"

    [<Test>]
    let ``Can deserialize Stacks``() =
        let out = deserialize<GenericClass<Stack<string>>> "<genericClass><v1>100</v1><v2><item>foo</item><item>bar</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2 |> Seq.head |> should equal "bar"
        out.V2 |> Seq.nth 1 |> should equal "foo"

    [<Test>]
    let ``Can deserialize NameValueCollections``() =
        let out = deserialize<GenericClass<NameValueCollection>> "<genericClass><v1>100</v1><v2><item><key>one</key><value>foo</value></item><item><key>two</key><value>bar</value></item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2.["one"] |> should equal "foo"
        out.V2.["two"] |> should equal "bar"

    [<Test>]
    let ``Can deserialize custom NameValueCollections``() =
        let out = deserialize<GenericClass<CustomNameValueCollection>> "<genericClass><v1>100</v1><v2><item><key>one</key><value>foo</value></item><item><key>two</key><value>bar</value></item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2.["one"] |> should equal "foo"
        out.V2.["two"] |> should equal "bar"

    [<Test>]
    let ``Can deserialize untyped hash tables``() =
        let out = deserialize<GenericClass<Hashtable>> "<genericClass><v1>100</v1><v2><item><key>one</key><value>foo</value></item><item><key>two</key><value>bar</value></item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        out.V2.["one"] |> should equal "foo"
        out.V2.["two"] |> should equal "bar"

    [<Test>]
    let ``Can deserialize linked lists``() =
        let out = deserialize<GenericClass<LinkedList<string>>> "<genericClass><v1>100</v1><v2><item>one</item><item>two</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        Seq.head out.V2 |> should equal "one"
        Seq.nth 1 out.V2 |> should equal "two"

    [<Test>]
    let ``Can deserialize readonly collections``() =
        let out = deserialize<GenericClass<ReadOnlyCollection<string>>> "<genericClass><v1>100</v1><v2><item>one</item><item>two</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        Seq.head out.V2 |> should equal "one"
        Seq.nth 1 out.V2 |> should equal "two"

    [<Test>]
    let ``Can deserialize arrays of classes``() =
        let out = deserialize<GenericClass<Guest[]>> "<genericClass><v1>984</v1><v2><guest><firstName>ham</firstName></guest><guest><firstName>foo</firstName><lastName>bar</lastName><id>2</id></guest></v2></genericClass>"
        out.V1 |> should equal 984
        out.V2.Length |> should equal 2
        out.V2.[0].Id |> should equal 0
        out.V2.[0].FirstName |> should equal "ham"
        out.V2.[1].Id |> should equal 0
        out.V2.[1].FirstName |> should equal "foo"
        out.V2.[1].LastName |> should equal "bar"

    [<Test>]
    let ``Can deserialize sorted sets``() =
        let out = deserialize<GenericClass<SortedSet<string>>> "<genericClass><v1>100</v1><v2><item>one</item><item>two</item></v2></genericClass>"
        out.V1 |> should equal 100
        out.V2.Count |> should equal 2
        Seq.head out.V2 |> should equal "one"
        Seq.nth 1 out.V2 |> should equal "two"

    [<Test>]
    let ``Can deserialize string attributes with special chars``() =
        let out = deserialize<TestClass> "<testClass><v1 attr=\"http://url.com\">42</v1><v2>bar</v2></testClass>"
        out.V1 |> should equal 42
        out.V2 |> should equal "bar"

    [<Test>]
    let ``Can correctly skip single/empty fields``() =
        let out = deserialize<GenericClass<LinkedList<string>>> "<genericClass><v1 /><v2><item>one</item><item>two</item></v2></genericClass>"
        out.V1 |> should equal 0
        out.V2.Count |> should equal 2
        Seq.head out.V2 |> should equal "one"

    [<Test>]
    let ``Can correctly skip empty elements in untyped collections``() =
        let out = deserialize<GenericClass<ArrayList>> "<genericClass><v1 /><v2><item /><item>one</item><item /></v2></genericClass>"
        out.V1 |> should equal 0
        out.V2.Count |> should equal 1
        out.V2.[0] |> should equal "one"

    [<Test>]
    let ``Can deserialize immutable F# list``() =
        let out = deserialize<FSharpListClass> "<fSharpListClass><v1>4</v1><v2><item>one</item><item>two</item></v2></fSharpListClass>"
        out.V1 |> should equal 4
        out.V2.Length |> should equal 2
        out.V2 |> List.head |> should equal "one"

    [<Test>]
    let ``Can deserialize F# records``() =
        let out = deserialize<TestRecord> "<testRecord><value>842</value><name>foobar</name></testRecord>"
        out.Value |> should equal 842
        out.Name |> should equal "foobar"

    [<Test>]
    let ``Can deserialize F# records in random order``() =
        let out = deserialize<LargerRecord> "<largerRecord><value>842</value><id>foobar</id><bar>bar</bar><foo>foo</foo></largerRecord>"
        out.Value |> should equal 842
        out.Id |> should equal "foobar"
        out.Bar |> should equal "bar"
        out.Foo |> should equal "foo"

    [<Test>]
    let ``Can deserialize F# records with missing fields``() =
        let out = deserialize<LargerRecord> "<largerRecord><value>842</value><id>foobar</id></largerRecord>"
        out.Value |> should equal 842
        out.Id |> should equal "foobar"
        out.Bar |> shouldBe Null
        out.Foo |> shouldBe Null

    [<Test>]
    let ``Can deserialize lists of F# records``() =
        let out = deserialize<List<LargerRecord>> "<list><item><largerRecord><value>842</value><id>foobar</id></largerRecord></item><item><largerRecord><value>1</value><id>bar</id></largerRecord></item></list>"

        out.Count |> should equal 2

        let first = out.[0]
        let second = out.[1]

        first.Value |> should equal 842
        first.Id |> should equal "foobar"
        first.Bar |> shouldBe Null
        first.Foo |> shouldBe Null

        second.Value |> should equal 1
        second.Id |> should equal "bar"

    [<Test>]
    let ``Can deserialize classes with tuples``() =
        let out = deserialize<TupleClass> "<tupleClass><v1>53</v1><v2><item1>something</item1><item2>40</item2></v2></tupleClass>"
        out.V1 |> should equal 53
        out.V2.Item1 |> should equal "something"
        out.V2.Item2 |> should equal 40

    [<Test>]
    let ``Can deserialize classes with F# tuples``() =
        let out = deserialize<GenericClass<string * int>> "<genericClass><v1>53</v1><v2><item1>something</item1><item2>40</item2></v2></genericClass>"
        out.V1 |> should equal 53
        out.V2 |> fst |> should equal "something"
        out.V2 |> snd |> should equal 40

    [<Test>]
    let ``Can deserialize lists of F# tuples``() =
        let input = "<list><item><a>foo</a><b>40</b></item><item><a>bar</a><b>50</b></item></list>"
        let out = deserialize<List<string * int>> input

        out.Count |> should equal 2

        let first = out.[0]
        let second = out.[1]

        first |> fst |> should equal "foo"
        first |> snd |> should equal 40

        second |> fst |> should equal "bar"
        second |> snd |> should equal 50

    [<Test>]
    let ``Can correctly skip unknown elements``() =
        let testString = @"<items><item><recipient>unknown</recipient><message>foobar</message><reference>2414059</reference></item></items>"
        let out = deserialize<List<Types.UnknownPropertyClass>> testString
        out.Count |> should equal 1
        out.[0].Reference |> should equal 2414059
        out.[0].Message |> should equal "foobar"

    [<Test>]
    let ``Can correctly skip unknown collection elements``() =
        let testString = @"<items><item><recipient><item>unknown</item></recipient><message>foobar</message><reference>2414059</reference></item></items>"
        let out = deserialize<List<Types.UnknownPropertyClass>> testString
        out.Count |> should equal 1
        out.[0].Reference |> should equal 2414059
        out.[0].Message |> should equal "foobar"

    [<Test>]
    let ``Can deserialize using a custom deserializer``() =
        let input = "<genericClass><v1>100</v1><v2>100.532</v2></genericClass>"

        // parse using a german culture
        let culture = Globalization.CultureInfo.GetCultureInfo("de")
        XmlConfig.Instance.RegisterDeserializer<decimal>(fun str ->
            Decimal.Parse(str, culture) |> box)

        let german = deserialize<GenericClass<decimal>> input
        german.V1 |> should equal 100
        german.V2 |> should equal 100532m

    [<Test>]
    let ``Can deserialize root classes with attributes``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<attrClass attr=\"test value\"><value>932</value></attrClass>"
        let result = deserialize<AttributeClass> input
        result.Value |> should equal 932
        result.Attr |> should equal "test value"

    [<Test>]
    let ``Can deserialize root classes with attributes in single quotes``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<attrClass attr='test  value'><value>100</value></attrClass>"
        let result = deserialize<AttributeClass> input
        result.Value |> should equal 100
        result.Attr |> should equal "test  value"

    [<Test>]
    let ``Can deserialize sub classes with attributes``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<class><v2 attr=\"some attribute value\"><value>91</value></v2><v1>932</v1></class>"
        let result = deserialize<GenericClass<AttributeClass>> input
        result.V1 |> should equal 932
        result.V2.Value |> should equal 91
        result.V2.Attr |> should equal "some attribute value"

    [<Test>]
    let ``Can deserialize attributed properties from single XML tags``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<class><v2 attr=\"some attribute value\" /><v1>932</v1></class>"
        let result = deserialize<GenericClass<AttributeClass>> input
        result.V1 |> should equal 932
        result.V2.Value |> should equal 0
        result.V2.Attr |> should equal "some attribute value"

    [<Test>]
    let ``Can deserialize lists of classes with attributes``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<class><v2><item attr=\"first\"><value>1</value></item><item attr=\"second\"><value>2</value></item></v2><v1>932</v1></class>"
        let result = deserialize<GenericClass<List<AttributeClass>>> input
        result.V1 |> should equal 932
        result.V2.Count |> should equal 2
        result.V2.[0].Attr |> should equal "first"
        result.V2.[1].Attr |> should equal "second"
        result.V2.[0].Value |> should equal 1
        result.V2.[1].Value |> should equal 2

    [<Test>]
    let ``Can deserialize lists of classes with attributes from single XML tags``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<class><v2><item attr=\"first\" /><item attr=\"second\" /></v2><v1>932</v1></class>"
        let result = deserialize<GenericClass<List<AttributeClass>>> input
        result.V1 |> should equal 932
        result.V2.Count |> should equal 2
        result.V2.[0].Attr |> should equal "first"
        result.V2.[1].Attr |> should equal "second"

    [<Test>]
    let ``Can deserialize lists with attributes``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<class><v2 attr=\"listattr\"><item>1</item><item>2</item></v2><v1>932</v1></class>"
        let result = deserialize<GenericClass<AttributeList<int>>> input
        result.V1 |> should equal 932
        result.V2.Count |> should equal 2
        result.V2.Attr |> should equal "listattr"
        result.V2.[0] |> should equal 1
        result.V2.[1] |> should equal 2

    [<Test>]
    let ``Can deserialize GUIDs``() =
        let input = "<genericClass><v1>10</v1><v2>313d9a94-4c7e-46d3-b0ba-70d163969e5b</v2></genericClass>"
        let result = deserialize<GenericClass<Guid>> input
        result.V1 |> should equal 10
        result.V2 |> should equal (Guid.Parse("313d9a94-4c7e-46d3-b0ba-70d163969e5b"))

    [<Test>]
    let ``Can deserialize GUIDs without dashes``() =
        let input = "<genericClass><v1>10</v1><v2>313d9a944c7e46d3b0ba70d163969e5b</v2></genericClass>"
        let result = deserialize<GenericClass<Guid>> input
        result.V1 |> should equal 10
        result.V2 |> should equal (Guid.Parse("313d9a94-4c7e-46d3-b0ba-70d163969e5b"))

    [<Test>]
    let ``Can deserialize using a TypeResolver function``() =
        let resolver = TypeResolver(fun _ -> typeof<TestClass>)
        let input = "<testClass><v1>10</v1><v2>test string</v2></testClass>"
        let result = XmlSerializer.DeserializeFromString(input, resolver)

        result.GetType() |> should equal typeof<TestClass>

    [<Test>]
    let ``Deserializer passes a correct XmlInfo instance to the TypeResolver``() =
        let resolver = TypeResolver(fun info ->
            info.Name |> should equal "testClass"
            info.HasAttributes |> should equal false
            info.Attributes.Count |> should equal 0

            typeof<TestClass>)

        let input = "<testClass><v1>10</v1><v2>test string</v2></testClass>"
        let result = XmlSerializer.DeserializeFromString(input, resolver)

        result.GetType() |> should equal typeof<TestClass>

    [<Test>]
    let ``Deserializer passes a correct XmlInfo instance with attributes to the TypeResolver``() =
        XmlConfig.Instance.UseAttributes <- true

        let resolver = TypeResolver(fun info ->
            info.Name |> should equal "testClass"
            info.HasAttributes |> should equal true
            info.Attributes.Count |> should equal 2
            info.Attributes.[0].Key |> should equal "two"
            info.Attributes.[0].Value |> should equal "bar"

            typeof<TestClass>)

        let input = "<testClass one=\"foo\" two=\"bar\"><v1>10</v1><v2>test string</v2></testClass>"
        let result = XmlSerializer.DeserializeFromString(input, resolver)

        result.GetType() |> should equal typeof<TestClass>

    [<Test>]
    let ``Can deserialize empty single tags``() =
        let input = "<objectPropClass><v1>value</v1><success /></objectPropClass>"

        let result = deserialize<ObjectPropClass> input
        result.V1 |> should equal "value"
        result.Success |> shouldBe notNull
        result.IsSuccess |> should equal true

    [<Test>]
    let ``Can deserialize not existant empty single tags``() =
        let input = "<objectPropClass><v1>value</v1></objectPropClass>"

        let result = deserialize<ObjectPropClass> input
        result.V1 |> should equal "value"
        result.Success |> should equal null
        result.IsSuccess |> should equal false

    [<Test>]
    let ``Can deserialize nullable attribute types``() =
        let input = "<nullableAttributeClass attr=\"23\"><value>35</value></nullableAttributeClass>"

        let result = deserialize<NullableAttributeClass>(input)
        result.Attr |> should equal 23
        result.Value |> should equal 35

    [<Test>]
    let ``Can deserialize string attributes into enum values``() =
        XmlConfig.Instance.UseAttributes <- true
        let input = "<genericClass><v1>100</v1><v2 attr=\"Bar\"></v2></genericClass>"

        let result = deserialize<GenericClass<AttrEnumClass>>(input)
        result.V1 |> should equal 100
        result.V2.Attr |> should equal TestEnum.Bar

    [<Test>]
    let ``Can deserialize complex attribute list classes``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<attributeListClass><success></success><attributeList><attr one=\"1\" two=\"2\">some text</attr></attributeList></attributeListClass>"
        let result = deserialize<Types.AttrListClass> input

        result.Success |> shouldBe notNull
        result.AttributeList.Count |> should equal 1
        result.AttributeList.[0].One |> should equal "1"
        result.AttributeList.[0].Two |> should equal "2"
        result.AttributeList.[0].Text |> should equal "some text"

    [<Test>]
    let ``Can deserialize attribute classes with XmlIgnoreAttribute properties``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<xmlIgnoreClass attr=\"foo\">bar</xmlIgnoreClass>"
        let result = deserialize<XmlIgnoreClass> input

        result.Attr |> should equal "foo"
        result.Value |> should equal "bar"

    [<Test>]
    let ``Can deserialize attribute classes with XmlIgnoreAttribute properties and ignore matching values``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<xmlIgnoreClass attr=\"foo\"><value>bar</value></xmlIgnoreClass>"
        let result = deserialize<XmlIgnoreClass> input

        result.Attr |> should equal "foo"
        result.Value |> should equal String.Empty

    [<Test>]
    let ``Can deserialize string arrays with multiple empty elements``() =
        let input = "<genericClass><v2><string /><string /></v2><v1>352</v1></genericClass>"
        let result = deserialize<GenericClass<string[]>>(input)

        result.V1 |> should equal 352
        result.V2.Length |> should equal 2
        result.V2 |> should equal [| ""; "" |]

    [<Test>]
    let ``Can correctly deserialize while leaving non-existing properties null``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<genericClass><v1>105</v1></genericClass>"
        let result = deserialize<GenericClass<string>> input

        result.V1 |> should equal 105
        result.V2 |> shouldBe Null

    [<Test>]
    let ``Can correctly deserialize while leaving non-existing classes null``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<genericClass><v1>105</v1></genericClass>"
        let result = deserialize<GenericClass<GenericClass<string>>> input

        result.V1 |> should equal 105
        result.V2 |> shouldBe Null

    [<Test>]
    let ``Can correctly deserialize while leaving empty classes empty``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<genericClass><v1>105</v1><v2></v2></genericClass>"
        let result = deserialize<GenericClass<GenericClass<string>>> input

        result.V1 |> should equal 105
        result.V2 |> shouldBe notNull


    [<Test>]
    let ``Can correctly deserialize while leaving empty single-tag classes empty``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = "<genericClass><v1>105</v1><v2 /></genericClass>"
        let result = deserialize<GenericClass<GenericClass<string>>> input

        result.V1 |> should equal 105
        result.V2 |> shouldBe notNull

    [<Test>]
    let ``Can correctly deserialize discriminated unions #1``() =
        let input = "<genericClass><v1>100</v1><v2><one></one></v2></genericClass>"
        let result = deserialize<GenericClass<TestUnion1>> input

        result.V1 |> should equal 100
        result.V2 |> should equal TestUnion1.One

    [<Test>]
    let ``Can correctly deserialize discriminated unions #2``() =
        let input = "<genericClass><v1>100</v1><v2><one/></v2></genericClass>"
        let result = deserialize<GenericClass<TestUnion1>> input

        result.V1 |> should equal 100
        result.V2 |> should equal TestUnion1.One

    [<Test>]
    let ``Can correctly deserialize discriminated unions #3``() =
        let input = "<genericClass><v1>100</v1><v2><two>239</two></v2></genericClass>"
        let result = deserialize<GenericClass<TestUnion1>> input

        result.V1 |> should equal 100
        result.V2 |> should equal (TestUnion1.Two 239)

    [<Test>]
    let ``Can correctly deserialize discriminated unions #4``() =
        let input = "<genericClass><v1>100</v1><v2><three><item1>first</item1><item2>2</item2></three></v2></genericClass>"
        let result = deserialize<GenericClass<TestUnion1>> input

        result.V1 |> should equal 100
        result.V2 |> should equal (TestUnion1.Three ("first", 2))

    [<Test>]
    let ``Can correctly deserialize discriminated unions #5``() =
        let input = "<genericClass><v1>100</v1><v2><four><item>1</item><item>2</item></four></v2></genericClass>"
        let result = deserialize<GenericClass<TestUnion1>> input

        result.V1 |> should equal 100
        result.V2 |> should equal (TestUnion1.Four ["1"; "2"])

    [<Test>]
    let ``Can correctly deserialize lists of different discriminated unions``() =
        let input = "<genericClass><v1>100</v1><v2><item><one /></item><item><four><item>one</item><item>two</item></four></item><item><one></one></item></v2></genericClass>"
        let result = deserialize<GenericClass<TestUnion1 list>> input

        result.V1 |> should equal 100
        result.V2 |> should equal [ TestUnion1.One; TestUnion1.Four ["one"; "two"]; TestUnion1.One ]

    [<Test>]
    let ``Can deserialize lists of discriminated unions with unknown cases #1``() =
        let input = "<genericClass><v1>100</v1><v2><item><five /></item><item><four><item>one</item><item>two</item></four></item><item><one></one></item></v2></genericClass>"
        let result = deserialize<GenericClass<TestUnion1 list>> input

        result.V1 |> should equal 100
        result.V2 |> should equal [ TestUnion1.Four ["one"; "two"]; TestUnion1.One ]

    [<Test>]
    let ``Can deserialize lists of discriminated unions with unknown cases #2``() =
        let input = "<genericClass><v1>100</v1><v2><item><five></five></item><item><four><item>one</item><item>two</item></four></item><item><one></one></item></v2></genericClass>"
        let result = deserialize<GenericClass<TestUnion1 list>> input

        result.V1 |> should equal 100
        result.V2 |> should equal [ TestUnion1.Four ["one"; "two"]; TestUnion1.One ]