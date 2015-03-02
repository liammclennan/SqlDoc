module Tests.RunningScripts
open System.Configuration
open Xunit
open FsUnit.Xunit
open PostgresDoc

let storeSql = SqlStore ConfigurationManager.AppSettings.["ConnSql"]

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

[<Fact>]
let ``run SimpleAuth script`` ()=
    runScript (SqlStore "Server=.\sql2014;Database=SimpleAuth;Trusted_Connection=True;") "
--IF (not EXISTS (SELECT * 
--                 FROM INFORMATION_SCHEMA.TABLES 
--                 WHERE TABLE_SCHEMA = 'dbo' 
--                 AND  TABLE_NAME = 'user'))
--BEGIN
CREATE TABLE [dbo].[user](
	[Id] [uniqueidentifier] NOT NULL,
	[Data] [xml] NOT NULL,
 CONSTRAINT [PK_card] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))
--END
"
    ()