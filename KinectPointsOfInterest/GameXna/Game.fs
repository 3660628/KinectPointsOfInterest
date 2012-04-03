namespace KinectPointsOfInterest

    module Program=
        open Microsoft.Xna.Framework
        open Microsoft.Xna.Framework.Graphics
        open Microsoft.Xna.Framework.Audio

        open Microsoft.Kinect
        open KinectHelperMethods

        open System

        open BodyData
        open Screens

        type XnaGame() as this =
            inherit Game()
    
            do this.Content.RootDirectory <- "XnaGameContent"
            let graphicsDeviceManager = new GraphicsDeviceManager(this)

            let screenWidth, screenHeight = 1024, 768

            let mutable spriteBatch : SpriteBatch = null

            let kinectUI = new Kinect.KinectCursor(this)
            do kinectUI.DrawOrder <- 99
            

            let changeScreenEvent = new Event<ChangeScreenEventArgs>()
            let login = new RecentUsersScreen(this, changeScreenEvent, kinectUI)
            //let login = new KinectTextInputScreen(this, null, changeScreenEvent, null, kinectUI)
            //do login.KinectUI <- kinectUI //pass the kinectUI object to the first screen
            //let login = new StoreScreen(this, changeScreenEvent)
            //let login = new VisualisationScreen(this, "male", 0, 0, 0,0,changeScreenEvent, kinectUI)

            let loadNewScreen (args:ChangeScreenEventArgs)= 
                args.OldScreen.DestroyScene()
                System.Diagnostics.Debug.WriteLine("lastScreen"+ string (this.Components.Remove(args.OldScreen)))
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
                this.Components.Add(kinectUI)
                
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