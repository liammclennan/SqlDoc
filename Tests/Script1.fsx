#r "../packages/SharpXml.1.5.0.0/lib/net40/SharpXml.dll"
#load "../PostgresDoc/Serializer.fs"

open SharpXml

type Person = { _id: System.Guid; age: int; name: string }
let id = System.Guid.NewGuid() 
let o = { _id = id; age = 45; name = "Cecile" }

let s = Serializer.serializeXml({_id = System.Guid.NewGuid(); age = 45; name = "Cecile";})
s |> System.Console.WriteLine

let o2 = XmlSerializer.DeserializeFromString<Person>(s)

printfn "%+A" o



