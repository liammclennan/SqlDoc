//  Copyright 2012-2013 Gregor Uhlenheuer
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

using System;
using System.Collections.Generic;
using SharpXml.Common;

namespace SharpXml.Tests.CSharp
{
    public class Types
    {
        public class UnknownPropertyClass
        {
            public int Id { get; set; }
            public int? RecipientListId { get; set; }
            public List<string> Recipients { get; set; }
            public int Reference { get; set; }
            public string Message { get; set; }
        }

        public class Enums
        {
            [Flags]
            public enum UIntEnum : uint
            {
                Zero,
                One,
                Two,
            }

            [Flags]
            public enum ULongEnum : ulong
            {
                Zero,
                One,
                Two,
            }

            [Flags]
            public enum ByteEnum : byte
            {
                Zero,
                One,
                Two,
            }

            [Flags]
            public enum SByteEnum : sbyte
            {
                Zero,
                One,
                Two,
            }

            [Flags]
            public enum ShortEnum : short
            {
                Zero,
                One,
                Two,
            }
        }

        public class AttrListClass
        {
            public object Success { get; set; }

            [XmlElement(ItemName = "attr")]
            public List<AttributeSubClass> AttributeList
            {
                get; set;
            }

            public class AttributeSubClass
            {
                [XmlAttribute]
                public string One { get; set; }

                [XmlAttribute]
                public string Two { get; set; }

                public string Text { get; set; }

                public AttributeSubClass(string text)
                {
                    Text = text;
                }
            }
        }

    }
}
