module Tests.Types
open PostgresDoc.Doc

type Person = 
    { _id: System.Guid; age: int; name: string }
    interface IDocument with
        member x.tableName() = "People"
        member x.id() = x._id

