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

module SerializationTests =

    open System
    open System.Collections.Generic
    open NUnit.Framework

    open SharpXml
    open SharpXml.Tests.CSharp
    open SharpXml.Tests.TestHelpers
    open SharpXml.Tests.Types

    XmlConfig.Instance.EmitCamelCaseNames <- true

    [<SetUp>]
    let init() =
        // this is important if some unit tests use
        // different settings that affect the behavior of
        // cached serialization functions
        XmlConfig.Instance.ClearSerializers()

    let serialize<'a> (element : 'a) =
        XmlSerializer.SerializeToString<'a>(element)

    [<Test>]
    let ``Can serialize DateTime values``() =
        let curr = DateTime.Now
        let date = curr.Date
        serialize date |> should equal (sprintf "<dateTime>%s</dateTime>" (date.ToString("yyyy-MM-dd")))

    [<Test>]
    let ``Can serialize nullable DateTime values``() =
        let date = Nullable(DateTime.Now.Date)
        let nullValue : Nullable<DateTime> = Nullable()
        serialize date |> should equal (sprintf "<dateTime>%s</dateTime>" (date.Value.ToString("yyyy-MM-dd")))
        serialize nullValue |> should equal "<dateTime></dateTime>"

    [<Test>]
    let ``Can serialize guids``() =
        let value = Guid.NewGuid()
        serialize value |> should equal (sprintf "<guid>%s</guid>" <| value.ToString("N"))

    [<Test>]
    let ``Can serialize chars``() =
        let value = 'c'
        serialize value |> should equal "<char>c</char>"

    [<Test>]
    let ``Can serialize char arrays``() =
        let value = [| 'c'; 'h'; 'a'; 'r' |]
        serialize value |> should equal "<array>char</array>"

    [<Test>]
    let ``Can serialize booleans``() =
        let falseValue = false
        let trueValue = true
        serialize falseValue |> should equal "<boolean>false</boolean>"
        serialize trueValue |> should equal "<boolean>true</boolean>"

    [<Test>]
    let ``Can serialize floats (single)``() =
        let value = 4.5f
        serialize value |> should equal "<single>4.5</single>"

    [<Test>]
    let ``Can serialize unsigned bytes``() =
        let value = 99uy
        serialize value |> should equal "<byte>99</byte>"

    [<Test>]
    let ``Can serialize bytes``() =
        let value = 99y
        serialize value |> should equal "<sByte>99</sByte>"

    [<Test>]
    let ``Can serialize decimals``() =
        let value = 204.992m
        serialize value |> should equal "<decimal>204.992</decimal>"

    [<Test>]
    let ``Can serialize byte arrays``() =
        let value = [| 99uy; 100uy; 101uy |]
        serialize value |> should equal (sprintf "<array>%s</array>" (Convert.ToBase64String(value)))

    [<Test>]
    let ``Can serialize types``() =
        let value = typeof<string>
        serialize value |> should equal (sprintf "<type>%s</type>" value.AssemblyQualifiedName)

    [<Test>]
    let ``Can serialize exceptions``() =
        let value = NotImplementedException("not implemented yet")
        serialize value |> should equal "<notImplementedException>not implemented yet</notImplementedException>"

    [<Test>]
    let ``Can serialize flags enums``() =
        let value = FlagsEnum.Ham ||| FlagsEnum.Eggs
        serialize value |> should equal "<flagsEnum>3</flagsEnum>"

    [<Test>]
    let ``Can serialize enums with names``() =
        let value = TestEnum.Bar
        serialize value |> should equal "<testEnum>Bar</testEnum>"

    [<Test>]
    let ``Can serialize string arrays``() =
        let value = [| "foo"; "bar" |]
        serialize value |> should equal "<array><item>foo</item><item>bar</item></array>"

    [<Test>]
    let ``Can serialize nullable guids``() =
        let value = Guid()
        let nullValue : Nullable<Guid> = Nullable()
        serialize value |> should equal (sprintf "<guid>%s</guid>" <| value.ToString("N"))
        serialize nullValue |> should equal "<guid></guid>"

    [<Test>]
    let ``Can serialize doubles``() =
        let value = 2.528
        serialize value |> should equal (sprintf "<double>%.3f</double>" value)

    [<Test>]
    let ``Can serialize integers``() =
        let value = 301
        serialize value |> should equal (sprintf "<int32>%d</int32>" value)

    [<Test>]
    let ``Can serialize unsigned integers``() =
        let value = 301u
        serialize value |> should equal (sprintf "<uInt32>%d</uInt32>" value)

    [<Test>]
    let ``Can serialize short integers``() =
        let value = 301s
        serialize value |> should equal (sprintf "<int16>%d</int16>" value)

    [<Test>]
    let ``Can serialize unsigned short integers``() =
        let value = 301us
        serialize value |> should equal (sprintf "<uInt16>%d</uInt16>" value)

    [<Test>]
    let ``Can serialize longs``() =
        let value = 301L
        serialize value |> should equal (sprintf "<int64>%d</int64>" value)

    [<Test>]
    let ``Can serialize unsigned longs``() =
        let value = 301UL
        serialize value |> should equal (sprintf "<uInt64>%d</uInt64>" value)

    [<Test>]
    let ``Can serialize strings``() =
        let value = "foo bar"
        serialize value |> should equal (sprintf "<string>%s</string>" value)

    [<Test>]
    let ``Can serialize TimeSpans``() =
        let value = TimeSpan(2, 10, 5)
        serialize value |> should equal "<timeSpan>02:10:05</timeSpan>"

    [<Test>]
    let ``Can serialize DateTimeOffsets``() =
        let value = DateTimeOffset(DateTime(2000, 12, 1), TimeSpan.Zero)
        serialize value |> should equal (sprintf "<dateTimeOffset>%s</dateTimeOffset>" <| value.ToString("o"))

    [<Test>]
    let ``Can serialize nullable DateTimeOffsets``() =
        let value = Nullable(DateTimeOffset(DateTime(2000, 12, 1), TimeSpan.Zero))
        let nullValue : Nullable<DateTimeOffset> = Nullable()
        serialize value |> should equal (sprintf "<dateTimeOffset>%s</dateTimeOffset>" <| value.Value.ToString("o"))
        serialize nullValue |> should equal "<dateTimeOffset></dateTimeOffset>"

    [<Test>]
    let ``Can serialize simple classes without default constructors``() =
        let cls = TestClass(800, "foo bar")
        serialize cls |> should equal "<testClass><v1>800</v1><v2>foo bar</v2></testClass>"

    [<Test>]
    let ``Can serialize simple classes``() =
        let cls = SimpleClass(V1 = "foo bar", V2 = 800)
        serialize cls |> should equal "<simpleClass><v1>foo bar</v1><v2>800</v2></simpleClass>"

    [<Test>]
    let ``Can serialize dictionaries with string keys``() =
        let dict = Dictionary<string, int>()
        dict.Add("foo", 42)
        dict.Add("bar", 200)
        serialize dict |> should equal "<dictionary><item><key>foo</key><value>42</value></item><item><key>bar</key><value>200</value></item></dictionary>"

    [<Test>]
    let ``Can serialize dictionaries with integer keys``() =
        let dict = Dictionary<int, string>()
        dict.Add(42, "foo")
        dict.Add(200, "bar")
        serialize dict |> should equal "<dictionary><item><key>42</key><value>foo</value></item><item><key>200</key><value>bar</value></item></dictionary>"

    [<Test>]
    let ``Can serialize arrays``() =
        let array = [| 35; 200; 42 |]
        serialize array |> should equal "<array><item>35</item><item>200</item><item>42</item></array>"

    [<Test>]
    let ``Can serialize classes with nested classes as properties``() =
        let cls = NestedClass(V1 = "foobar", V2 = SimpleClass(V1 = "bar foo", V2 = 200))
        serialize cls |> should equal "<nestedClass><v1>foobar</v1><v2><v1>bar foo</v1><v2>200</v2></v2></nestedClass>"

    [<Test>]
    let ``Can serialize classes with nested classes and properties with null values``() =
        let cls = NestedClass2(V1 = "foobar", V2 = NestedClass2(V1 = "barfoo"))
        serialize cls |> should equal "<nestedClass2><v1>foobar</v1><v2><v1>barfoo</v1></v2></nestedClass2>"

    [<Test>]
    let ``Can serialize double nested classes``() =
        let cls = NestedClass2(V1 = "foobar", V2 = NestedClass2(V1 = "barfoo", V2 = NestedClass2(V1 = "ham eggs")))
        serialize cls |> should equal "<nestedClass2><v1>foobar</v1><v2><v1>barfoo</v1><v2><v1>ham eggs</v1></v2></v2></nestedClass2>"

    [<Test>]
    let ``Can serialize double nested classes containing properties with null values``() =
        let cls = NestedClass2(V1 = "foobar", V2 = NestedClass2(V1 = "barfoo", V2 = NestedClass2()))
        serialize cls |> should equal "<nestedClass2><v1>foobar</v1><v2><v1>barfoo</v1><v2></v2></v2></nestedClass2>"

    [<Test>]
    let ``Can serialize F# record types``() =
        let record : TestRecord = { Value = 99; Name = "ham & eggs" }
        serialize record |> should equal "<testRecord><value>99</value><name>ham & eggs</name></testRecord>"

    [<Test>]
    let ``Can serialize F# tuples``() =
        let tuple = 406, "foo bar test"
        serialize tuple |> should equal "<tuple><item1>406</item1><item2>foo bar test</item2></tuple>"

    [<Test>]
    let ``Can serialize F# discriminated unions #1``() =
        let union = GenericClass<TestUnion1>(V1 = 10, V2 = TestUnion1.One)
        let result = serialize union

        result |> should equal "<genericClass><v1>10</v1><v2><one></one></v2></genericClass>"

    [<Test>]
    let ``Can serialize F# discriminated unions #2``() =
        let union = GenericClass<TestUnion1>(V1 = 10, V2 = TestUnion1.Two 2)
        let result = serialize union

        result |> should equal "<genericClass><v1>10</v1><v2><two>2</two></v2></genericClass>"

    [<Test>]
    let ``Can serialize F# discriminated unions #3``() =
        let union = GenericClass<TestUnion1>(V1 = 10, V2 = TestUnion1.Three ("three", 3))
        let result = serialize union

        result |> should equal "<genericClass><v1>10</v1><v2><three><item>three</item><item>3</item></three></v2></genericClass>"

    [<Test>]
    let ``Can serialize lists of F# discriminated unions``() =
        let union = GenericClass<TestUnion1 list>(V1 = 10, V2 = [TestUnion1.One; TestUnion1.Three ("three", 3); TestUnion1.One])
        let result = serialize union

        result |> should equal "<genericClass><v2><testUnion1><one></one></testUnion1><testUnion1><item1>three</item1><item2>3</item2></testUnion1><testUnion1><one></one></testUnion1></v2><v1>10</v1></genericClass>"

    [<Test>]
    let ``Can serialize newline characters``() =
        let special = "foo\r\nbar"
        let cls = TestClass(305, special)
        serialize cls |> should equal "<testClass><v1>305</v1><v2>foo\r\nbar</v2></testClass>"

    [<Test>]
    let ``Can serialize XML encoded characters``() =
        let special = "</v2>"
        let cls = TestClass(210, special)
        serialize cls |> should equal "<testClass><v1>210</v1><v2>&lt;/v2&gt;</v2></testClass>"

    [<Test>]
    let ``Can serialize non-printable characters``() =
        let chars = string [ for i in 10 .. 30 -> char i ]
        let cls = TestClass(999, chars)
        serialize cls |> should equal (sprintf "<testClass><v1>999</v1><v2>%s</v2></testClass>" chars)

    [<Test>]
    let ``Can serialize dictionaries``() =
        let dict = Dictionary<string, int>()
        dict.Add("foo", 1)
        dict.Add("bar", 2)
        let cls = DictClass(V1 = dict, V2 = 200)
        serialize cls |> should equal "<dictClass><v1><keyValuePair><string>foo</string><int32>1</int32></keyValuePair><keyValuePair><string>bar</string><int32>2</int32></keyValuePair></v1><v2>200</v2></dictClass>"

    [<Test>]
    let ``Can serialize arrays of classes``() =
        let cls = GenericClass<Guest[]>(V1 = 984, V2 = [| Guest(1); Guest(2, FirstName = "foo", LastName = "bar") |])
        serialize cls |> should equal "<genericClass><v1>984</v1><v2><guest><id>1</id></guest><guest><firstName>foo</firstName><lastName>bar</lastName><id>2</id></guest></v2></genericClass>"

    [<Test>]
    let ``Can serialize IEnumerables``() =
        let cls = IEnumerableClass(V1 = "foo bar", V2 = List<int>(seq { 1 .. 2 }))
        serialize cls |> should equal "<iEnumerableClass><v1>foo bar</v1><v2><int32>1</int32><int32>2</int32></v2></iEnumerableClass>"

    [<Test>]
    let ``Can serialize class with instance method ToXml()``() =
        let cls = CustomParserClass(X = 200, Y = 400)
        serialize cls |> should equal "<customParserClass>200x400</customParserClass>"

    [<Test>]
    let ``Can serialize class with instance method ToXml() that returns null``() =
        let cls = CustomParserClass(X = 0, Y = 0)
        serialize cls |> should equal "<customParserClass></customParserClass>"

    [<Test>]
    let ``Can serialize class with instance method ToXml() (with special characters)``() =
        let cls = ToXmlClass("foo & bar")
        serialize cls |> should equal "<toXmlClass>foo &amp; bar</toXmlClass>"

    [<Test>]
    let ``Can serialize class with instance method ToXml() with attributes``() =
        XmlConfig.Instance.UseAttributes <- true
        let cls = CustomParserClass(X = 200, Y = 400, Attr = "test")
        serialize cls |> should equal "<customParserClass attr=\"test\">200x400</customParserClass>"

    [<Test>]
    let ``Can serialize class with static method ToXml()``() =
        let cls = CustomParserClass2(X = 200, Y = 400)
        serialize cls |> should equal "<customParserClass2>200x400</customParserClass2>"

    [<Test>]
    let ``Can serialize untyped collections``() =
        let list = System.Collections.ArrayList()
        list.Add("foo") |> ignore
        list.Add("bar") |> ignore
        let cls = ArrayListClass(V1 = 200, V2 = list)
        serialize cls |> should equal "<arrayListClass><v2><item>foo</item><item>bar</item></v2><v1>200</v1></arrayListClass>"

    [<Test>]
    let ``Can serialize untyped collections containing different types``() =
        let list = System.Collections.ArrayList()
        list.Add("foo") |> ignore
        list.Add(42) |> ignore
        let cls = ArrayListClass(V1 = 200, V2 = list)
        serialize cls |> should equal "<arrayListClass><v2><item>foo</item><item>42</item></v2><v1>200</v1></arrayListClass>"

    [<Test>]
    let ``Can serialize classes attributed with XmlElementAttribute``() =
        let cls = AttributedClass(V1 = "foo", V2 = SimpleClass(V1 = "bar", V2 = 70))
        serialize cls |> should equal "<myClass><A>foo</A><B><v1>bar</v1><v2>70</v2></B></myClass>"

    [<Test>]
    let ``Can serialize dictionaries attributed with XmlElementAttribute``() =
        let dict = Dictionary<string, int>()
        dict.Add("foo", 1)
        dict.Add("bar", 2)
        let cls = AttributedDictClass(V1 = 211, V2 = dict)
        serialize cls |> should equal "<attributedDictClass><A>211</A><B><x><k>foo</k><v>1</v></x><x><k>bar</k><v>2</v></x></B></attributedDictClass>"

    [<Test>]
    let ``Can serialize lists attributed with XmlElementAttribute``() =
        let list = List<string>([ "one"; "two"; "three"])
        let cls = AttributedListClass(V1 = 200, V2 = list)
        serialize cls |> should equal "<attributedListClass><A>200</A><v2><x>one</x><x>two</x><x>three</x></v2></attributedListClass>"

    [<Test>]
    let ``Can serialize class member names correctly``() =
        let list = List<Guest>([ Guest(10, FirstName = "foo", LastName = "bar"); Guest(20, FirstName = "ham", LastName = "eggs") ])
        let cls = Booking("testBooking", list)
        serialize cls |> should equal "<booking><name>testBooking</name><guests><guest><firstName>foo</firstName><lastName>bar</lastName><id>10</id></guest><guest><firstName>ham</firstName><lastName>eggs</lastName><id>20</id></guest></guests></booking>"

    [<Test>]
    let ``Can serialize array names correctly``() =
        let array = [| Guest(10, FirstName = "foo", LastName = "bar"); Guest(20, FirstName = "ham", LastName = "eggs") |]
        let cls = GenericClass<Guest[]>(V1 = 200, V2 = array)
        serialize cls |> should equal "<genericClass><v1>200</v1><v2><guest><firstName>foo</firstName><lastName>bar</lastName><id>10</id></guest><guest><firstName>ham</firstName><lastName>eggs</lastName><id>20</id></guest></v2></genericClass>"

    [<Test>]
    let ``Can serialize decimals with custom serializer``() =
        XmlConfig.Instance.RegisterSerializer<CustomDecimal>(fun d ->
            let dec : CustomDecimal = unbox d
            dec.Value.ToString("0,0"))
        let cls = GenericClass<CustomDecimal>(V1 = 100, V2 = CustomDecimal(200.45m))
        serialize cls |> should equal "<genericClass><v1>100</v1><v2>200</v2></genericClass>"

    [<Test>]
    let ``Can serialize DateTime with custom serializer``() =
        let now = DateTime.Now
        XmlConfig.Instance.RegisterSerializer<CustomDateTime>(fun d ->
            let date : CustomDateTime = unbox d
            date.Date.ToShortDateString())
        let cls = GenericClass<CustomDateTime>(V1 = 999, V2 = CustomDateTime(now))
        serialize cls |> should equal (sprintf "<genericClass><v1>999</v1><v2>%s</v2></genericClass>" (now.ToShortDateString()))

    [<Test>]
    let ``Can serialize empty strings``() =
        let cls = TestClass(93, String.Empty)
        serialize cls |> should equal "<testClass><v1>93</v1><v2></v2></testClass>"

    [<Test>]
    let ``Can serialize empty values``() =
        XmlConfig.Instance.IncludeNullValues <- false
        let cls = ListClass(V1 = null, V2 = 992)
        let guests = List<Guest>([ Guest(94) ])
        let booking = Booking("booking", guests)
        serialize cls |> should equal "<listClass><v2>992</v2></listClass>"
        serialize booking |> should equal "<booking><name>booking</name><guests><guest><id>94</id></guest></guests></booking>"
        XmlConfig.Instance.IncludeNullValues <- true
        serialize cls |> should equal "<listClass><v1></v1><v2>992</v2></listClass>"
        serialize booking |> should equal "<booking><name>booking</name><guests><guest><firstName></firstName><lastName></lastName><id>94</id></guest></guests></booking>"
        XmlConfig.Instance.IncludeNullValues <- false

    [<Test>]
    let ``Can serialize classes with tuples``() =
        let cls = TupleClass(V1 = 23, V2 = Tuple<string, int>("foobar", 204))
        serialize cls |> should equal "<tupleClass><v1>23</v1><v2><item1>foobar</item1><item2>204</item2></v2></tupleClass>"

    [<Test>]
    let ``Can serialize classes with F# tuples``() =
        let cls = GenericClass(V1 = 211, V2 = (100, "ham"))
        serialize cls |> should equal "<genericClass><v1>211</v1><v2><item1>100</item1><item2>ham</item2></v2></genericClass>"

    [<Test>]
    let ``Can serialize root elements with static XML attributes``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = StaticAttributeClass(Value = 24)
        serialize cls |> should equal "<staticAttributeClass foo=\"bar\"><value>24</value></staticAttributeClass>"

    [<Test>]
    let ``Can serialize root elements with inherited static XML attributes``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = InheritedStaticAttributeClass(Value = 22)
        serialize cls |> should equal "<inheritedStaticAttributeClass bar=\"foo\" foo=\"bar\"><value>22</value></inheritedStaticAttributeClass>"

    [<Test>]
    let ``Can serialize classes with XmlAttribute properties``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = AttributeClass(Value = 9, Attr = "some value")
        serialize cls |> should equal "<attributeClass attr=\"some value\"><value>9</value></attributeClass>"

    [<Test>]
    let ``Can serialize attribute values containing double quotes``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = AttributeClass(Value = 13, Attr = "some \"value\"")
        serialize cls |> should equal "<attributeClass attr=\"some &#x22;value&#x22;\"><value>13</value></attributeClass>"

    [<Test>]
    let ``Can serialize attribute values containing special characters (i.e. ampersand)``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = AttributeClass(Value = 13, Attr = "foo & bar")
        serialize cls |> should equal "<attributeClass attr=\"foo &amp; bar\"><value>13</value></attributeClass>"

    [<Test>]
    let ``Can serialize attribute values containing special characters``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = AttributeClass(Value = 13, Attr = "über")
        serialize cls |> should equal "<attributeClass attr=\"&#252;ber\"><value>13</value></attributeClass>"

    [<Test>]
    let ``Can serialize elements with static and dynamic XML attributes``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = AttributeClass2(Value = 15, Attr = "attribute value")
        serialize cls |> should equal "<attributeClass2 bar=\"foo\" attr=\"attribute value\"><value>15</value></attributeClass2>"

    [<Test>]
    let ``Can serialize integer attributes``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = AttributeOnlyClass(Value = 20)
        serialize cls |> should equal "<attributeOnlyClass value=\"20\"></attributeOnlyClass>"

    [<Test>]
    let ``Can serialize attribute-only classes``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = AttributeOnlyClass(Value = 15, Attr = "attribute value")
        serialize cls |> should equal "<attributeOnlyClass value=\"15\" attr=\"attribute value\"></attributeOnlyClass>"

    [<Test>]
    let ``Can serialize generic attribute classes``() =
        XmlConfig.Instance.UseAttributes <- true

        let cls = GenAttributeClass(Value = 2, Attr = 2052L)
        serialize cls |> should equal "<genAttributeClass attr=\"2052\"><value>2</value></genAttributeClass>"

    [<Test>]
    let ``Can serialize attributed list classes``() =
        XmlConfig.Instance.UseAttributes <- true

        let lst = AttributeList<string>(Attr = "test")
        lst.Add("first")

        let cls = GenericClass<AttributeList<string>>(V1 = 21, V2 = lst)
        serialize cls |> should equal "<genericClass><v1>21</v1><v2 attr=\"test\"><string>first</string></v2></genericClass>"

    [<Test>]
    let ``Can serialize flag enums of different base types``() =
        let test enum =
            let expected = "<genericClass><v1>10</v1><v2>2</v2></genericClass>"
            let cls = GenericClass<_>(V1 = 10, V2 = enum)
            serialize cls |> should equal expected

        test Types.Enums.ByteEnum.Two
        test Types.Enums.SByteEnum.Two
        test Types.Enums.ShortEnum.Two
        test Types.Enums.UIntEnum.Two
        test Types.Enums.ULongEnum.Two

    [<Test>]
    let ``Can serialize nullable flag enums of different base types``() =
        let test enum =
            let expected = "<genericClass><v1>10</v1><v2>2</v2></genericClass>"
            let cls = GenericClass<_>(V1 = 10, V2 = Nullable(enum))
            serialize cls |> should equal expected

        test Types.Enums.ByteEnum.Two
        test Types.Enums.SByteEnum.Two
        test Types.Enums.ShortEnum.Two
        test Types.Enums.UIntEnum.Two
        test Types.Enums.ULongEnum.Two

    [<Test>]
    let ``Can serialize object values``() =
        let input = GenericClass<_>(V1 = 14, V2 = obj())
        let result = serialize input

        result |> should equal "<genericClass><v1>14</v1><v2></v2></genericClass>"

    [<Test>]
    let ``Can serialize classes with properties by sort order``() =
        let input1 = OrderClass01(V1 = 1, V2 = "foo")
        let input2 = OrderClass02(V1 = 1, V2 = "foo")
        let input3 = OrderClass03(V1 = 1, V2 = "foo")

        serialize input1 |> should equal "<orderClass01><v1>1</v1><v2>foo</v2></orderClass01>"
        serialize input2 |> should equal "<orderClass02><v2>foo</v2><v1>1</v1></orderClass02>"
        serialize input3 |> should equal "<orderClass03><v1>1</v1><v2>foo</v2></orderClass03>"

    [<Test>]
    let ``Can serialize classes with XmlIgnoreAttribute properties``() =
        XmlConfig.Instance.UseAttributes <- true

        let input = XmlIgnoreClass("foo", Attr = "bar")
        serialize input |> should equal "<xmlIgnoreClass attr=\"bar\">foo</xmlIgnoreClass>"

    [<Test>]
    let ``Can serialize unicode characters as encoded entities``() =
        XmlConfig.Instance.SpecialCharEncoding <- UnicodeSerializationType.HexEncoded

        serialize "foo bar" |> should equal "<string>foo bar</string>"
        serialize "übermäßig" |> should equal "<string>&#x00FC;berm&#x00E4;&#x00DF;ig</string>"
        serialize "control.chars" |> should equal "<string>control&#x0003;&#x0003;.chars</string>"
