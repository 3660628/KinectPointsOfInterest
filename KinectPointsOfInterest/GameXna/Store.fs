namespace KinectPointsOfInterest

open MySql.Data.MySqlClient
open System.Windows.Forms

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open System
    
    module Store=

        type Garment(game, id, name, waist)=
            //inherit DrawableGameComponent(game)

            member this.Name
                with get() = name
            member this.ID
                with get() = (int) id
        [<AllowNullLiteral>] //allow null as a proper value
        type Customer(name, email, height, waist, hips, chest)=
            member this.Height
                with get() = height
            member this.Waist
                with get() = waist
            member this.Hips
                with get() = hips
            member this.Chest
                with get() = chest

    module Database=

        type DatabaseAccess()=
            
            let connectionStr = "Network Address=localhost;" + "Initial Catalog='kinectfashion';" + "Persist Security Info=no;" + "User Name='root';" + "Password='shAke87n'"
            let connection = new MySql.Data.MySqlClient.MySqlConnection(connectionStr)
            
            member this.getCustomer email password=
                try
                    let mutable customer = null
                    connection.Open()
                    //get customer details
                    let transactionCustomer = connection.BeginTransaction()
                    let SelectGarmentsInAvailableSizes = "SELECT name, email, height, waist, hips, chest FROM users WHERE email LIKE BINARY '"+ email + "' AND password LIKE BINARY '" + password + "'"
                    let cmd = new MySqlCommand(SelectGarmentsInAvailableSizes, connection,transactionCustomer)
                    cmd.CommandTimeout <- 20
                    let rdr = cmd.ExecuteReader()
                    while (rdr.Read()) do
                        customer <- new Store.Customer(rdr.[0], rdr.[1], rdr.[2], rdr.[3], rdr.[4], rdr.[5])
                        Diagnostics.Debug.WriteLine(rdr.[0].ToString() + rdr.[1].ToString() + rdr.[2].ToString() + rdr.[3].ToString())
                    rdr.Close()
                    transactionCustomer.Dispose()
                    customer
                with 
                    | :? MySqlException as ex -> (MessageBox.Show("Error connecting to database.\n\r" + ex.Message ) |> ignore
                                                  null)
                    | ex-> (MessageBox.Show(ex.Message ) |> ignore
                            null)

            member this.getGarments customer whereClause=
                try
                    if connection.State = Data.ConnectionState.Closed then
                        connection.Open()
                    //get garments
                    let mutable garments:List<Store.Garment> = List.Empty
                    let transactionGarments = connection.BeginTransaction()
                    let SelectGarmentsInAvailableSizes = "SELECT * FROM garments NATURAL JOIN stock AS inStock JOIN size_charts ON size_chart = size_charts.id AND inStock.size = size_charts.size " + whereClause
                    let cmd = new MySqlCommand(SelectGarmentsInAvailableSizes, connection,transactionGarments)
                    cmd.CommandTimeout <- 20
                    let rdr = cmd.ExecuteReader()
                    while (rdr.Read()) do
                        garments <- garments @ [new Store.Garment("game", rdr.[0].ToString(), rdr.[1].ToString(), 100)]
                        Diagnostics.Debug.WriteLine(rdr.[0].ToString() + rdr.[1].ToString())
                    rdr.Close()
                    transactionGarments.Dispose()
                    connection.Close()
                    garments

                with 
                    | :? MySqlException as ex -> (MessageBox.Show("Error connecting to database.\n\r" + ex.Message ) |> ignore
                                                  List<Store.Garment>.Empty )
                    | ex-> (MessageBox.Show(ex.Message ) |> ignore
                            List<Store.Garment>.Empty )

        let dbA = new DatabaseAccess()