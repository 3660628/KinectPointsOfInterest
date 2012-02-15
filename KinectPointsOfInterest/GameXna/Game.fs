namespace KinectPointsOfInterest

    module Program=
        open Microsoft.Xna.Framework
        open Microsoft.Xna.Framework.Graphics
        open Microsoft.Xna.Framework.Audio

        open Microsoft.Research.Kinect.Nui
        open KinectHelperMethods

        open System

        open BodyData
        open Screens

        type XnaGame() as this =
            inherit Game()
    
            do this.Content.RootDirectory <- "XnaGameContent"
            let graphicsDeviceManager = new GraphicsDeviceManager(this)

            let screenWidth, screenHeight = 960, 600

            let mutable spriteBatch : SpriteBatch = null

            let changeScreenEvent = new Event<ChangeScreenEventArgs>()
            let login = new LoginScreen(this, changeScreenEvent)
            //let login = new StoreScreen(this, changeScreenEvent)
            //let login = new VisualisationScreen(this, "male", 0, 0, 0,0,changeScreenEvent)

            let loadNewScreen (args:ChangeScreenEventArgs)= 
                args.OldScreen.DestroyScene()
                this.Components.Remove(args.OldScreen) |> ignore
                this.Components.Add(args.NewScreen) |> ignore


            [<CLIEvent>]
            member this.ChangeScreen = changeScreenEvent.Publish


            override game.Initialize() =
                graphicsDeviceManager.GraphicsProfile <- GraphicsProfile.HiDef
                graphicsDeviceManager.PreferredBackBufferWidth <- screenWidth
                graphicsDeviceManager.PreferredBackBufferHeight <- screenHeight
                graphicsDeviceManager.ApplyChanges() 
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)

                this.ChangeScreen.Add(fun (args) -> loadNewScreen (args))

                this.Components.Add(login)
                
                base.Initialize()

            override game.LoadContent() =
                base.LoadContent()
        
            override game.Update gameTime = 
                base.Update gameTime

            override game.Draw gameTime = 
                game.GraphicsDevice.Clear(Color.CornflowerBlue)
                base.Draw gameTime

        let game = new XnaGame() //entry point
        game.Run()