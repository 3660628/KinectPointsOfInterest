namespace KinectPointsOfInterest

open MySql.Data.MySqlClient
open System.Windows.Forms

open System
    
    module Database=

        type DatabaseAccess()=
            
            let connectionStr = "Data Source=kinectedfashion.db.7010757.hostedresource.com; Port=3306; User ID=kinectedfashion; Password=shAke87n;"
            let connection = new MySql.Data.MySqlClient.MySqlConnection(connectionStr)
            
            let connect =
                try
                    connection.Open()
                with 
                    | :? MySqlException as ex -> MessageBox.Show("Error connecting to database, please check your internet connection and try again.\r\n Error:"+ ex.Message) |> ignore

            do connect

            member this.DoSQL query=
                let cmd = new MySqlCommand("garments")
                cmd.Connection <- connection
                cmd.CommandType <- System.Data.CommandType.TableDirect
                let reader = cmd.ExecuteReader()
                reader
                