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

module XmlParserTests =

    open System
    open System.Diagnostics

    open NUnit.Framework

    open SharpXml
    open SharpXml.Tests.TestHelpers
    open SharpXml.XmlParser

    let private eat (input : string) =
        let info = ParserInfo(input.ToCharArray())
        let name, tag = eatTag info
        info.Index, name, tag

    let private eatAt (input : string) at =
        let info = ParserInfo(input.ToCharArray())
        info.Index <- at
        let name, tag = eatTag info
        info.Index, name, tag

    let private eatSome (input : string) =
        let info = ParserInfo(input.ToCharArray())
        eatSomeTag info |> fst

    let private eatRoot (input : string) =
        let info = ParserInfo(input.ToCharArray())
        eatRoot info |> ignore
        info.Index

    let private eatUnknown (input : string) =
        let info = ParserInfo(input.ToCharArray())
        eatUnknownTilClosing info |> ignore
        info

    let private getContent (input : string) at =
        let info = ParserInfo(input.ToCharArray())
        info.Index <- at
        let content = eatText info
        info.Index, content

    let private getAttr(input : string) =
        let info = ParserInfo(input.ToCharArray())
        eatTagWithAttributes info

    let private eatText (input : string) =
        let info = ParserInfo(input.ToCharArray())
        eatText info

    [<Test>]
    let eatTag01() =
        let input = " foo <testTag rest/> <hamEggs/>"
        let index, value, t = eat input
        value |> should equal "testTag"
        index |> should equal 20
        t |> should equal TagType.Single

    [<Test>]
    let eatTag02() =
        let input = "<testTag />"
        let index, value, t = eat input
        value |> should equal "testTag"
        index |> should equal 11
        t |> should equal TagType.Single


    [<Test>]
    let eatTag03() =
        let input = "< fooBar /> < testTag />"
        let index, value, t = eatAt input 10
        value |> should equal "testTag"
        index |> should equal 24
        t |> should equal TagType.Single

    [<Test>]
    let eatTag04() =
        let input = "xxxx<fooBar>xxxxxxxxxx"
        let index, value, t = eat input
        value |> should equal "fooBar"
        index |> should equal 12
        t |> should equal TagType.Open

    [<Test>]
    let eatTag05() =
        let input = "< fooBar / >"
        let index, value, t = eat input
        value |> should equal "fooBar"
        index |> should equal 12
        t |> should equal TagType.Single

    [<Test>]
    let eatTag06() =
        let input = "</fooBar>"
        let index, value, t = eat input
        value |> should equal "fooBar"
        index |> should equal input.Length
        t |> should equal TagType.Close

    [<Test>]
    let eatTag07() =
        let input = "<one/><two/><three/><four/>"
        let index, value, t = eatAt input 12
        value |> should equal "three"
        index |> should equal 20
        t |> should equal TagType.Single

    [<Test>]
    let eatContent01() =
        let input = "<one>this is a small test</test>"
        let index, result = getContent input 5
        result |> should equal "this is a small test"
        index |> should equal 25

    [<Test>]
    let eatContent02() =
        let input = "<one>this is &lt;b&gt;a&lt;/b&gt; small test</test>"
        let index, result = getContent input 5
        result |> should equal "this is <b>a</b> small test"
        index |> should equal 44

    [<Test>]
    let eatContent03() =
        let input = "<one>foo bar</one><two>ham eggs</two>"
        let index, result = getContent input 23
        result |> should equal "ham eggs"
        index |> should equal 31

    [<Test>]
    let eatContent04() =
        let input = "<one>foo bar</one><two>ham <![CDATA[</some><crazy></crazy> <?content?>]]>eggs</two>"
        let index, result = getContent input 23
        result |> should equal "ham </some><crazy></crazy> <?content?>eggs"
        index |> should equal (input.IndexOf("</two>"))

    [<Test>]
    let eatContent05() =
        let input = "<one></one><two><![CDATA[</some><crazy></crazy> <?content?>]]></two>"
        let index, result = getContent input 16
        result |> should equal "</some><crazy></crazy> <?content?>"
        index |> should equal (input.IndexOf("</two>"))

    [<Test>]
    let eatClosingTag01() =
        let input = "<one>ham eggs</one><two>foo bar</two>"
        let index, _, _ = eatAt input 5
        index |> should equal 19

    [<Test>]
    let eatClosingTag02() =
        let input = "<one>ham eggs</one>"
        let index, _, _ = eatAt input 4
        index |> should equal input.Length
        
    [<Test>]
    let eatRoot01() =
        eatRoot "<root><one>ham eggs</one></root>"
        |> should equal 6

    [<Test>]
    let eatRoot02() =
        eatRoot "   < root><one>ham eggs</one></ root>"
        |> should equal 10

    [<Test>]
    let eatRoot03() =
        eatRoot "<?xml version=\"1.0\"?><root><one>ham eggs</one></ root>"
        |> should equal 27

    [<Test>]
    let eatRoot04() =
        eatRoot "  <?xml version=\"1.0\"?> < root><one>ham eggs</one></ root>"
        |> should equal 31

    [<Test>]
    let eatRoot05() =
        let input = "<?xml version=\"1.0\"?><?process instruction?><root><one>ham eggs</one></ root>"
        eatRoot input
        |> should equal (input.IndexOf("<one>"))

    [<Test>]
    let eatRoot06() =
        let input = "  <?xml version=\"1.0\"?> <?process instruction ?>  < root><one>ham eggs</one></ root>"
        eatRoot input
        |> should equal (input.IndexOf("<one>"))

    [<Test>]
    let eatRoot07() =
        let input = "<?xml version=\"1.0\"?><!--some comment--><root><one>ham eggs</one></ root>"
        eatRoot input
        |> should equal (input.IndexOf("<one>"))

    [<Test>]
    let eatRoot08() =
        let input = "<?xml version=\"1.0\"?><!--some comment--><!DOCTYPE something><root><one>ham eggs</one></ root>"
        eatRoot input
        |> should equal (input.IndexOf("<one>"))

    [<Test>]
    let eatRoot09() =
        let input = "<?xml version=\"1.0\"?><!DOCTYPE something><!--some comment--><root><one>ham eggs</one></ root>"
        eatRoot input
        |> should equal (input.IndexOf("<one>"))

    [<Test>]
    let eatSomeTag01() =
        eatSome "<foo>" |> should equal TagType.Open

    [<Test>]
    let eatSomeTag02() =
        eatSome "</foo>" |> should equal TagType.Close

    [<Test>]
    let eatSomeTag03() =
        eatSome "<foo />" |> should equal TagType.Single

    [<Test>]
    let eatUnknownTag01() =
        let input = "<item>unknown</item></recipient><message>foobar</message><reference>2414059</reference></item></items>"
        let info = eatUnknown input
        info.Index |> should equal 32

    [<Test>]
    let eatUnknownTag02() =
        let input = "unknown</item></recipient><message>foobar</message><reference>2414059</reference></item></items>"
        let info = eatUnknown input
        info.Index |> should equal 14

    [<Test>]
    let eatUnknownTag03() =
        let input = "<item><inner>unknown</inner><inner></inner></item></recipient><message>foobar</message><reference>2414059</reference></item></items>"
        let info = eatUnknown input
        info.Index |> should equal 62

    [<Test>]
    let eatAttributes01() =
        let input = "<tag one=\"1\" two=\"2\" three=\"3\">"
        let name, tag, attr = getAttr input
        name |> should equal "tag"
        tag |> should equal TagType.Open
        attr.Length |> should equal 3
        attr.[0] |> should equal ("three", "3")
        attr.[1] |> should equal ("two", "2")
        attr.[2] |> should equal ("one", "1")

    [<Test>]
    let eatAttributes02() =
        let input = "< tag  one = \"1\" two = \"2\" three = \"3\" >"
        let name, tag, attr = getAttr input
        name |> should equal "tag"
        tag |> should equal TagType.Open
        attr.Length |> should equal 3
        attr.[0] |> should equal ("three", "3")
        attr.[1] |> should equal ("two", "2")
        attr.[2] |> should equal ("one", "1")

    [<Test>]
    let eatAttributes03() =
        let input = "< tag  one = \" some attribute value \" /  >"
        let name, tag, attr = getAttr input
        name |> should equal "tag"
        tag |> should equal TagType.Single
        attr.Length |> should equal 1
        attr.[0] |> should equal ("one", " some attribute value ")

    [<Test>]
    let eatAttributes04() =
        let input = "<tag/>"
        let name, tag, attr = getAttr input
        name |> should equal "tag"
        tag |> should equal TagType.Single
        attr.Length |> should equal 0

    [<Test>]
    let eatAttributes05() =
        let input = "<tag >"
        let name, tag, attr = getAttr input
        name |> should equal "tag"
        tag |> should equal TagType.Open
        attr.Length |> should equal 0

    [<Test>]
    let skipComments01() =
        let input = "<!-- c --><some> "
        let index, value, t = eat input
        value |> should equal "some"
        t |> should equal TagType.Open
        index |> should equal 16

    [<Test>]
    let skipComments02() =
        let input = " <!-- -scary-comment- --> </close> "
        let index, value, t = eat input
        value |> should equal "close"
        t |> should equal TagType.Close
        index |> should equal 34

    [<Test>]
    let skipComments03() =
        let input = " <!-- -scary-comment- --> <!-- -scary-comment- --> <single /> "
        let index, value, t = eat input
        value |> should equal "single"
        t |> should equal TagType.Single
        index |> should equal 61

    [<Test>]
    let encodeUnicode01() =
        let input = "no special character at all"

        eatText input |>  should equal input

    [<Test>]
    let encodeUnicode02() =
        let input = "fob &#x26; bar"

        eatText input |>  should equal "fob & bar"

    [<Test>]
    let encodeUnicode03() =
        let input = "foo &#x0026; bar"

        eatText input |>  should equal "foo & bar"

    [<Test>]
    let encodeUnicode04() =
        let input = "foo &#38; bar"

        eatText input |>  should equal "foo & bar"

    [<Test>]
    let encodeUnicode05() =
        let input = "foo && bar"

        eatText input |>  should equal "foo && bar"

    [<Test>]
    let encodeUnicode06() =
        let input = "foo &;& bar"

        eatText input |>  should equal "foo &;& bar"

    [<Test>]
    let encodeUnicode07() =
        let input = "foo &&amp;& bar"

        eatText input |>  should equal "foo &&& bar"

    [<Test>]
    let eatCDATA01() =
        let input = "foo <![CDATA[<some><thing>]]> bar"

        eatText input |>  should equal "foo <some><thing> bar"

    [<Test>]
    let eatCDATA02() =
        let input = "<![CDATA[<some><thing>]]>"

        eatText input |>  should equal "<some><thing>"

    [<Test>]
    let eatCDATA03() =
        let input = "foo <![CDATA[<some>]] ><thing>]]> bar"

        eatText input |>  should equal "foo <some>]] ><thing> bar"

