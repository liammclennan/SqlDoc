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

#nowarn "9"
#nowarn "51"

module internal XmlParser =

    open System
    open System.Collections.Generic
    open System.Globalization
    open System.Text
    open Microsoft.FSharp.NativeInterop

    type ParserInfo(value : char[]) =
        [<DefaultValue>]
        val mutable Index : int
        [<DefaultValue>]
        val mutable Depth : int
        member x.Value = value
        member x.Length = value.Length
        member x.IsEnd with get() = x.Index >= x.Length

    type TagType =
        | Open = 0
        | Single = 1
        | Close = 2

    type ParseState =
        | Start = 0
        | Tag = 1
        | TagName = 2
        | InTag = 3
        | AttrName = 4
        | StartString = 5
        | InString = 6

    type CommentState =
        | Start = 0
        | StartHyphen = 1
        | InComment = 2
        | Hyphen = 3
        | EndHyphen = 4

    let whitespaceChars =
        let whitespace = [| ' '; '\t'; '\r'; '\n' |]
        let max =  Array.max whitespace |> int
        Array.init (max+1) (fun c -> Array.exists ((=) (char c)) whitespace)

    let entities =
        let xmlEntities = [
            ("gt", ">");
            ("lt", "<");
            ("amp", "&");
            ("apos", "'");
            ("quot", "\"") ]
        xmlEntities
        |> List.fold (fun (d : Dictionary<string, string>) (k, v) -> d.Add(k, v); d)
            (new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))

    let tryFindEntity key =
        match entities.TryGetValue key with
        | true, value -> Some value
        | _           -> None

    let parseRawString (input : string) =
        box input

    let inline isWhitespace (c : char) =
        let i = int c
        i < whitespaceChars.Length && whitespaceChars.[i]

    let rec skipWhitespace (input : string) index =
        if index >= input.Length || not (isWhitespace input.[index]) then index
        else skipWhitespace input (index + 1)

    let lookAhead (str : string) (input : ParserInfo) =
        if input.Index + str.Length <= input.Length then
            let rec inner i =
                if str.Length <= i then true
                elif input.Value.[input.Index + i] <> str.[i] then false
                else inner (i+1)
            inner 0
        else false

    let skipComment buffer index length =
        let mutable state = CommentState.Start
        let mutable b = buffer
        let mutable i = index
        let mutable skip = 0
        let mutable found = false
        while i < length && not found do
            let chr = NativePtr.read b
            i <- i + 1
            skip <- skip + 1
            b <- NativePtr.add b 1
            match state with
            | CommentState.Start ->
                if chr = '-' then state <- CommentState.StartHyphen
                else skip <- 0; found <- true
            | CommentState.StartHyphen ->
                if chr = '-' then state <- CommentState.InComment
            | CommentState.InComment ->
                if chr = '-' then state <- CommentState.Hyphen
            | CommentState.Hyphen ->
                if chr = '-'
                then state <- CommentState.EndHyphen
                else state <- CommentState.InComment
            | CommentState.EndHyphen ->
                if chr = '>' then found <- true
        skip

    let skipCData buffer (input : ParserInfo) (sb : StringBuilder)=
        if lookAhead "<![CDATA[" input then
            let mutable found = false
            let mutable skip = 8
            let mutable b = NativePtr.add buffer 8
            input.Index <- input.Index + 8
            while not input.IsEnd && not found do
                b <- NativePtr.add b 1
                input.Index <- input.Index + 1
                skip <- skip + 1
                let chr = NativePtr.read b

                // possible end of CDATA
                if chr = ']' then
                    if lookAhead "]]>" input then
                        skip <- skip + 2
                        input.Index <- input.Index + 2
                        found <- true
                    else sb.Append(chr) |> ignore
                else sb.Append(chr) |> ignore
            skip
        else 0

    /// Eat a closing XML tag
    let eatClosingTag (input : ParserInfo) =
        if not input.IsEnd then
            let mutable buffer = &&input.Value.[input.Index]
            let mutable state = ParseState.Start
            let mutable found = false

            while input.Index < input.Length && not found do
                let chr = NativePtr.read buffer
                input.Index <- input.Index + 1
                buffer <- NativePtr.add buffer 1
                match state with
                | ParseState.Start ->
                    if chr = '<' then state <- ParseState.Tag
                | _ ->
                    if chr = '>' then found <- true

    /// Eat a XML tag and return its name and
    /// type being one of Open, Close or Single
    let eatTag (input : ParserInfo) =
        let mutable name = Unchecked.defaultof<string>
        let mutable nameStart = 0
        let mutable enclosingType = '"'
        let mutable tag = TagType.Single
        let mutable buffer = &&input.Value.[input.Index]
        let mutable close = false
        let mutable state = ParseState.Start
        let mutable found = false

        while input.Index < input.Length && not found do
            let chr = NativePtr.read buffer
            input.Index <- input.Index + 1
            buffer <- NativePtr.add buffer 1
            match state with
            | ParseState.Start ->
                if chr = '<' then state <- ParseState.Tag
            | ParseState.Tag ->
                if not (isWhitespace chr) then
                    if chr = '/' then
                        close <- true
                        nameStart <- input.Index
                        state <- ParseState.TagName
                    elif chr = '!' then
                        let skip = skipComment buffer input.Index input.Length
                        input.Index <- input.Index + skip
                        buffer <- NativePtr.add buffer skip
                        state <- ParseState.Start
                    else
                        nameStart <- input.Index - 1
                        state <- ParseState.TagName
            | ParseState.TagName ->
                if isWhitespace chr then
                    if not close then
                        name <- String(input.Value, nameStart, (input.Index-nameStart-1))
                        tag <- TagType.Open
                        state <- ParseState.InTag
                elif chr = '/' then
                    name <- String(input.Value, nameStart, (input.Index-nameStart-1))
                    tag <- TagType.Single
                    state <- ParseState.InTag
                elif chr = '>' then
                    name <- String(input.Value, nameStart, (input.Index-nameStart-1))
                    tag <- if close then TagType.Close else TagType.Open
                    found <- true
            | ParseState.InString ->
                if chr = enclosingType then
                    state <- ParseState.InTag
            | _ ->
                if chr = '>' then
                    found <- true
                elif chr = '"' || chr = '\'' then
                    enclosingType <- chr
                    state <- ParseState.InString
                elif chr = '/' then
                    tag <- TagType.Single

        name, tag

    /// Eat a XML tag and return its name, type and a
    /// list of attributes
    let eatTagWithAttributes (input : ParserInfo) =
        let mutable name = Unchecked.defaultof<string>
        let mutable nameStart = 0
        let mutable tag = TagType.Single
        let mutable buffer = &&input.Value.[input.Index]
        let mutable close = false
        let mutable state = ParseState.Start
        let mutable found = false
        let mutable attr = Unchecked.defaultof<string>
        let mutable attributes = []
        let mutable enclosingType = '"'

        // TODO: this function is a copy of 'eatTag'
        // TODO: some refactoring required!

        while input.Index < input.Length && not found do
            let chr = NativePtr.read buffer
            input.Index <- input.Index + 1
            buffer <- NativePtr.add buffer 1
            match state with
            | ParseState.Start ->
                if chr = '<' then state <- ParseState.Tag
            | ParseState.Tag ->
                if not (isWhitespace chr) then
                    if chr = '/' then
                        close <- true
                        nameStart <- input.Index
                        state <- ParseState.TagName
                    elif chr = '!' then
                        let skip = skipComment buffer input.Index input.Length
                        input.Index <- input.Index + skip
                        buffer <- NativePtr.add buffer skip
                        state <- ParseState.Start
                    else
                        nameStart <- input.Index - 1
                        state <- ParseState.TagName
            | ParseState.TagName ->
                if isWhitespace chr then
                    if not close then
                        name <- String(input.Value, nameStart, (input.Index-nameStart-1))
                        tag <- TagType.Open
                        state <- ParseState.InTag
                elif chr = '/' then
                    name <- String(input.Value, nameStart, (input.Index-nameStart-1))
                    tag <- TagType.Single
                    state <- ParseState.InTag
                elif chr = '>' then
                    name <- String(input.Value, nameStart, (input.Index-nameStart-1))
                    tag <- if close then TagType.Close else TagType.Open
                    found <- true
            | ParseState.AttrName ->
                if chr = '=' || isWhitespace chr then
                    attr <- String(input.Value, nameStart, (input.Index-nameStart-1))
                    state <- ParseState.StartString
            | ParseState.StartString ->
                if chr = '"' || chr = '\'' then
                    enclosingType <- chr
                    state <- ParseState.InString
                    nameStart <- input.Index
            | ParseState.InString ->
                if chr = enclosingType then
                    state <- ParseState.InTag
                    attributes <- (attr, (String(input.Value, nameStart, (input.Index-nameStart-1)))) :: attributes
            | _ ->
                if chr = '>' then
                    found <- true
                elif chr = '/' then
                    tag <- TagType.Single
                elif not (isWhitespace chr) then
                    state <- ParseState.AttrName
                    nameStart <- input.Index - 1

        name, tag, attributes

    /// Eat a XML tag and return its type being one of Open, Close or Single
    let eatSomeTag (input : ParserInfo) =
        let mutable tag = TagType.Open
        let mutable buffer = &&input.Value.[input.Index]
        let mutable close = false
        let mutable state = ParseState.Start
        let mutable found = false
        let mutable beforeTag = input.Index

        while not input.IsEnd && not found do
            let chr = NativePtr.read buffer
            input.Index <- input.Index + 1
            buffer <- NativePtr.add buffer 1
            match state with
            | ParseState.Start ->
                if chr = '<' then
                    state <- ParseState.Tag
                    beforeTag <- input.Index - 1
            | ParseState.Tag ->
                if chr = '>' then
                    found <- true
                elif chr = '/' then
                    close <- true
                    tag <- TagType.Close
                    state <- ParseState.TagName
                elif chr = '!' then
                    let skip = skipComment buffer input.Index input.Length
                    input.Index <- input.Index + skip
                    buffer <- NativePtr.add buffer skip
                    state <- ParseState.Start
                elif not (isWhitespace chr) then
                    state <- ParseState.TagName
            | ParseState.InString ->
                if chr = '"' then state <- ParseState.TagName
            | _ ->
                if chr = '>' then
                    found <- true
                elif chr = '"' then
                    state <- ParseState.InString
                elif chr = '/' then
                    tag <- TagType.Single
        tag, beforeTag

    /// Eat all content until the last closed tag
    let eatUnknownTilClosing (input : ParserInfo) =
        let rec inner level =
            if input.IsEnd then input.Index else
            let tag, before = eatSomeTag input
            match tag with
            | TagType.Close when level = 0 -> before
            | TagType.Close -> inner (level - 1)
            | TagType.Open -> inner (level + 1)
            | _ -> inner level
        inner 0

    /// Parse the XML root node and the optional <?xml ?> tag
    let private eatRootFunc func (input : ParserInfo) =
        let mutable buffer = &&input.Value.[input.Index]
        let mutable state = ParseState.Start
        let mutable start = input.Index
        let mutable found = false

        // eat optional xml doc tag
        while not input.IsEnd && not found do
            let chr = NativePtr.read buffer
            input.Index <- input.Index + 1
            buffer <- NativePtr.add buffer 1

            match state with
            | ParseState.Start ->
                if chr = '<' then
                    state <- ParseState.Tag
                    start <- input.Index - 1
            | ParseState.Tag ->
                if chr = '?' then
                    state <- ParseState.TagName
                elif chr = '!' then
                    let skip = skipComment buffer input.Index input.Length
                    input.Index <- input.Index + skip
                    if skip = 0 then state <- ParseState.TagName
                elif not (isWhitespace chr) then
                    input.Index <- start
                    found <- true
            | _ ->
                if chr = '>' then state <- ParseState.Start

        func input

    /// Parse the XML root node and the optional <?xml ?> tag
    let eatRoot input =
        let name, _ = eatRootFunc eatTag input
        name, []

    /// Parse the XML root node and the optional <?xml ?> tag
    /// along with optional attributes on the root node
    let eatRootWithAttributes input =
        let name, _, attr = eatRootFunc eatTagWithAttributes input
        name, attr

    let inline decode (str : string) =
        str.Replace("&gt;", ">").Replace("&lt;", "<")

    let inline stripCData (str : string) =
        str.Replace("<![CDATA[", null).Replace("]]>", null)

    let indexOfEndEntity buffer (index : int) (length : int) =
        let mutable found = false
        let mutable i = index
        let mutable buf = buffer
        while not found && i < length do
            buf <- NativePtr.add buf 1
            let chr = NativePtr.read buf
            if chr = '&' || chr = ';' then found <- true
            else i <- i + 1
        if found then i else 0

    /// Eat the inner content of a XML tag and return the string value
    let eatText (input : ParserInfo) =
        let mutable found = false
        let mutable buffer = &&input.Value.[input.Index]
        let sb = StringBuilder()

        while not found && input.Index < input.Length do
            let chr = NativePtr.read buffer

            // end of the inner text of start of CDATA
            if chr = '<' then
                // look for a CDATA entity
                if input.Index + 1 < input.Length && input.Value.[input.Index+1] = '!' then
                    let skip = skipCData buffer input sb

                    // this is a CDATA element -> skip the number of characters returned
                    if skip > 0 then
                        buffer <- NativePtr.add buffer skip
                // end of the inner text
                else found <- true

            // possible start of special entity
            elif chr = '&' && input.Index + 1 < input.Length then

                // find next ';' or '&'
                let nextEnd = indexOfEndEntity buffer (input.Index+1) input.Length
                if nextEnd > 0 && input.Value.[nextEnd] = ';' then
                    let entity = new String(input.Value, input.Index+1, nextEnd - input.Index - 1)

                    // entity is a unicode encoding
                    if entity.Length > 1 && entity.[0] = '#' then
                        let parsed = ref 0us
                        let invariant = NumberFormatInfo.InvariantInfo
                        match entity.[1] with
                        | 'x'
                        | 'X' -> UInt16.TryParse(entity.Substring(2), NumberStyles.AllowHexSpecifier, invariant, parsed) |> ignore
                        | _   -> UInt16.TryParse(entity.Substring(1), NumberStyles.Integer, invariant, parsed) |> ignore

                        if !parsed > 0us then
                            sb.Append(char !parsed) |> ignore

                            buffer <- NativePtr.add buffer (nextEnd - input.Index)
                            input.Index <- nextEnd
                    // entity could be an xml/html entity
                    else
                        buffer <- NativePtr.add buffer (nextEnd - input.Index)
                        input.Index <- nextEnd

                        match tryFindEntity entity with
                        | Some e -> sb.Append(e) |> ignore
                        | _      -> sb.Append('&') |> ignore
                                    sb.Append(entity) |> ignore
                                    sb.Append(';') |> ignore
                else sb.Append(chr) |> ignore
            else sb.Append(chr) |> ignore

            if not found then
                buffer <- NativePtr.add buffer 1
                input.Index <- input.Index + 1

        sb.ToString()
