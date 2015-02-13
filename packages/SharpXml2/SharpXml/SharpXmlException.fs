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

/// Application exception thrown during SharpXml
/// serialization and deserialization
type SharpXmlException(msg : string, ex : Exception) =
    inherit Exception(msg, ex)

    new() = SharpXmlException(null, null)
    new(msg) = SharpXmlException(msg, null)
