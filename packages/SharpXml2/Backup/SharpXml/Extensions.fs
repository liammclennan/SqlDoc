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

module internal Extensions =

    open System
    open System.Reflection

    let caseDiff = (int 'A') - (int 'a')

    /// Extension methods for System.String
    type System.String with

        /// Convert the given string to CamelCase form
        member x.ToCamelCase() =
            if x <> null && x.Length > 0 && x.[0] >= 'A' && x.[0] <= 'Z' then
                (char (int x.[0] - caseDiff)).ToString() + x.Substring(1)
            else
                x

    let dataContract = "DataContractAttribute"
    let dataMember = "DataMemberAttribute"

    let hasAttribute (t : MemberInfo) (attrName : string) =
        t.GetCustomAttributes(true)
        |> Array.exists (fun x -> x.GetType().Name = attrName)

    let hasAttributeType (t : MemberInfo) (attrType : Type) =
        t.GetCustomAttributes(attrType, true).Length > 0

    let getAttribute<'a> (t : MemberInfo) =
        let attr = t.GetCustomAttributes(typeof<'a>, true)
        if attr.Length > 0 then Some (attr.[0] :?> 'a) else None

    let getAttributes<'a>(t : MemberInfo) =
        t.GetCustomAttributes(typeof<'a>, true)
        |> Array.map (fun x -> x :?> 'a)

    /// Extension methods for System.Reflection.MemberInfo
    type System.Reflection.MemberInfo with

        member x.IsDTO() =
            not(hasAttributeType x typeof<System.SerializableAttribute>) && hasAttribute x dataContract

        member x.IsDataMember() =
            hasAttribute x dataMember

        member x.HasAttribute(attributeName : string) =
            hasAttribute x attributeName

        member x.HasAttribute(attributeType : Type) =
            hasAttributeType x attributeType

    let getUnderlyingType (t : Type) =
        let nullable = Nullable.GetUnderlyingType(t)
        if nullable <> null then nullable else t

    /// Extension methods for System.Type
    type System.Type with

        member x.NullableUnderlying() =
            getUnderlyingType x

        member x.HasInterface(interfaceType : Type) =
            x.GetInterfaces()
            |> Array.exists ((=) interfaceType)

        member x.IsNumericType() =
            if not x.IsValueType then false else
            x.IsIntegerType() || x.IsRealNumberType()

        member x.IsIntegerType() =
            if not x.IsValueType then false else
            let underlying = getUnderlyingType x
            underlying = typeof<byte> ||
            underlying = typeof<sbyte> ||
            underlying = typeof<int16> ||
            underlying = typeof<uint16> ||
            underlying = typeof<int> ||
            underlying = typeof<uint32> ||
            underlying = typeof<int64> ||
            underlying = typeof<uint64>

        member x.IsRealNumberType() =
            if not x.IsValueType then false else
            let underlying = getUnderlyingType x
            underlying = typeof<float> ||
            underlying = typeof<double> ||
            underlying = typeof<decimal>

    let matchInterface (interfaceType : Type) (t : Type) =
        t <> typeof<obj> &&
            (t.IsAssignableFrom(interfaceType) || t.HasInterface(interfaceType))
