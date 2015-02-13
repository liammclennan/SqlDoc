module Serializer

open SharpXml

let serializeXml o =     
    SharpXml.XmlSerializer.SerializeToString o