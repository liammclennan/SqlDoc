module Tests.RunningScripts
open System.Configuration
open Xunit
open FsUnit.Xunit
open SqlDoc

let storeSql = SqlXmlStore ConfigurationManager.AppSettings.["ConnSqlXml"]

[<Fact>]
let ``run ddl script`` () =
    let tableName = (System.Guid.NewGuid().ToString())
    let script = sprintf """
                    CREATE TABLE [dbo].[%s](
	[Id] [uniqueidentifier] NOT NULL,
	[Data] [xml] NOT NULL,
 CONSTRAINT [PK_%s] PRIMARY KEY CLUSTERED 
 (
	[Id] ASC
 )) 
                 """ tableName (System.Guid.NewGuid().ToString())
    runScript storeSql script

    runScript storeSql (sprintf "drop table [%s]" tableName)
    ()
