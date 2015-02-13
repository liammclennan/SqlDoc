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

type EmptyConstructor = delegate of unit -> obj
type GetterFunc = delegate of obj -> obj
type GetterFunc<'T> = delegate of 'T -> obj
type SetterFunc = delegate of obj * obj -> unit
type SetterFunc<'T> = delegate of 'T * obj -> unit

module internal ReflectionHelpers =

    open System
    open System.Collections.Generic
    open System.Linq.Expressions
    open System.Reflection
    open System.Reflection.Emit
    open System.Runtime.Serialization

    open SharpXml.Extensions

    let publicFlags =
        BindingFlags.FlattenHierarchy |||
        BindingFlags.Public |||
        BindingFlags.Instance

    let getProps (t : Type) = t.GetProperties(publicFlags)

    let getInterfaceProperties (t : Type) =
        if not t.IsInterface then failwithf "Type '%s' is no interface type" t.FullName
        let map = HashSet<PropertyInfo>(getProps t)
        t.GetInterfaces()
        |> Array.map getProps
        |> Array.concat
        |> Array.iter (map.Add >> ignore)
        Seq.toArray map

    let getPublicProperties (t : Type) =
        if t.IsInterface then getInterfaceProperties t else getProps t

    let getSerializableProperties (t : Type) =
        if t.IsDTO() then
            getPublicProperties t
            |> Array.filter (fun p ->
                p.IsDataMember() &&
                p.GetGetMethod() <> null &&
                p.GetIndexParameters().Length = 0)
        else
            getPublicProperties t
            |> Array.filter (fun p ->
                p.GetGetMethod() <> null &&
                p.GetIndexParameters().Length = 0 &&
                not (hasAttributeType p typeof<SharpXml.Common.XmlIgnoreAttribute>) &&
                not (hasAttribute p "IgnoreDataMemberAttribute"))

    let hasValidSetter (p : PropertyInfo) =
        let setter = p.GetSetMethod()
        setter <> null && setter.GetParameters().Length = 1

    let getDeserializableProperties (t : Type) =
        if t.IsDTO() then
            getPublicProperties t
            |> Array.filter (fun p -> p.IsDataMember() && hasValidSetter p)
        else
            getPublicProperties t
            |> Array.filter (fun p ->
                hasValidSetter p &&
                not (hasAttributeType p typeof<SharpXml.Common.XmlIgnoreAttribute>) &&
                not (hasAttribute p "IgnoreDataMemberAttribute"))

    let getEmptyConstructor (t : Type) =
        let ctor = t.GetConstructor(Type.EmptyTypes)
        if ctor <> null then
            let dm = DynamicMethod("CustomCtor", t, Type.EmptyTypes, t.Module, true)
            let il = dm.GetILGenerator()

            il.Emit(OpCodes.Nop)
            il.Emit(OpCodes.Newobj, ctor)
            il.Emit(OpCodes.Ret)

            dm.CreateDelegate(typeof<EmptyConstructor>) :?> EmptyConstructor
        else
            let func =
                // special case 'string': no default constructor
                if t = typeof<string> then fun () -> box String.Empty
                // this one is for types that do not have an empty constructor
                else fun () -> FormatterServices.GetUninitializedObject(t)
            EmptyConstructor(func)

    let constructorCache = ref (Dictionary<Type, EmptyConstructor>())

    let getConstructorMethod (t : Type) =
        match (!constructorCache).TryGetValue t with
        | true, ctor -> ctor
        | _ ->
            let ctor = getEmptyConstructor t
            if ctor <> null then Atom.updateAtomDict constructorCache t ctor else null

    let constructorNameCache = ref (Dictionary<string, EmptyConstructor>())

    let getConstructorMethodByName (name : string) =
        match (!constructorNameCache).TryGetValue name with
        | true, ctor -> ctor
        | _ ->
            let ctor =
                match Assembly.findType name with
                | Some t -> getEmptyConstructor t
                | _ -> null
            if ctor <> null then Atom.updateAtomDict constructorNameCache name ctor else null

    let areStringOrValueTypes types =
        Seq.forall (fun t -> t = typeof<string> || t.IsValueType) types

    let defaultValueCache = ref (Dictionary<Type, obj>())

    let determineDefaultValue (t : Type) =
        if not t.IsValueType then null
        elif t.IsEnum then Enum.ToObject(t, 0) else
        match Type.GetTypeCode(t) with
        | TypeCode.Empty
        | TypeCode.DBNull
        | TypeCode.String -> null
        | TypeCode.Boolean -> box false
        | TypeCode.Byte -> box 0uy
        | TypeCode.Char -> box '\000'
        | TypeCode.DateTime -> box DateTime.MinValue
        | TypeCode.Decimal -> box 0m
        | TypeCode.Double -> box 0.0
        | TypeCode.Int16 -> box 0s
        | TypeCode.Int32 -> box 0l
        | TypeCode.Int64 -> box 0L
        | TypeCode.SByte -> box 0y
        | TypeCode.Single -> box 0.0f
        | TypeCode.UInt16 -> box 0us
        | TypeCode.UInt32 -> box 0ul
        | TypeCode.UInt64 -> box 0UL
        | TypeCode.Object
        | _ -> Activator.CreateInstance t

    let getDefaultValue (t : Type) =
        if not t.IsValueType then null else
        match (!defaultValueCache).TryGetValue t with
        | true, value -> value
        | _ ->
            let defVal = determineDefaultValue t
            Atom.updateAtomDict defaultValueCache t defVal

    /// Build a getter expression function for the
    /// specified PropertyInfo
    let getGetter<'a> (p : PropertyInfo) =
        let inst = Expression.Parameter(p.DeclaringType, "i")
        let prop = Expression.Property(inst, p)
        let conv = Expression.Convert(prop, typeof<obj>)
        Expression.Lambda<GetterFunc<'a>>(conv, inst).Compile()

    /// Build an object based getter expression function for the
    /// specified PropertyInfo
    let getObjGetter (p : PropertyInfo) =
        let inst = Expression.Parameter(typeof<obj>, "i")
        let icon = Expression.TypeAs(inst, p.DeclaringType)
        let prop = Expression.Property(icon, p)
        let conv = Expression.Convert(prop, typeof<obj>)
        Expression.Lambda<GetterFunc>(conv, inst).Compile()

    /// Build a setter expression function for the
    /// specified PropertyInfo
    let getSetter<'a> (p : PropertyInfo) =
        if typeof<'a> <> p.DeclaringType then
            invalidArg "p" "Type does not match the properties' declaring type"
        let inst = Expression.Parameter(p.DeclaringType, "i")
        let arg = Expression.Parameter(typeof<obj>, "a")
        let setter = Expression.Call(inst, p.GetSetMethod(), Expression.Convert(arg, p.PropertyType))
        Expression.Lambda<SetterFunc<'a>>(setter, inst, arg).Compile()

    /// Build an object based setter expression function for the
    /// specified PropertyInfo
    let getObjSetter (p : PropertyInfo) =
        let inst = Expression.Parameter(typeof<obj>, "i")
        let icon = Expression.TypeAs(inst, p.DeclaringType)
        let arg = Expression.Parameter(typeof<obj>, "a")
        let setter = Expression.Call(icon, p.GetSetMethod(), Expression.Convert(arg, p.PropertyType))
        Expression.Lambda<SetterFunc>(setter, inst, arg).Compile()