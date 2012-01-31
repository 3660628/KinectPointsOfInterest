namespace KinectPointsOfInterest

    module Program=
        open Microsoft.Xna.Framework
        open Microsoft.Xna.Framework.Graphics
        open Microsoft.Xna.Framework.Audio

        open Microsoft.Research.Kinect.Nui
        open KinectHelperMethods

        open System

        open BodyData

        type XnaGame() as this =
            inherit Game()
    
            do this.Content.RootDirectory <- "XnaGameContent"
            let graphicsDeviceManager = new GraphicsDeviceManager(this)

            let screenWidth, screenHeight = 960, 480

            let mutable sprite : Texture2D = null
            let mutable spriteBatch : SpriteBatch = null

            let kinect = new Kinect(this)
    
            let mutable timer = 10000.0f
            let mutable finished = false
            let mutable frontBody:Body[] = Array.zeroCreate 100
            let mutable backBody:Body[] = Array.zeroCreate 100
            let mutable sideBody:Body[] = Array.zeroCreate 100

            //vertical points of interest. all dimensions are in visualisation space
            let mutable topOfHeadY = 0.0f
            let mutable bottomOfFeetY = 0.0f
            let mutable waistY = 0.0f
            let mutable shouldersY = 0.0f

            //depth image map
            let mutable depthImage:Texture2D = null
            let mutable clickSound:SoundEffect = null
            let mutable beepSound:SoundEffect = null

            override game.Initialize() =
                graphicsDeviceManager.GraphicsProfile <- GraphicsProfile.HiDef
                graphicsDeviceManager.PreferredBackBufferWidth <- screenWidth
                graphicsDeviceManager.PreferredBackBufferHeight <- screenHeight
                graphicsDeviceManager.ApplyChanges() 
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
                game.Components.Add(kinect)
                base.Initialize()

            override game.LoadContent() =
                sprite <- game.Content.Load<Texture2D>("Sprite")
                clickSound <- game.Content.Load<SoundEffect>("click_1")
                beepSound <- game.Content.Load<SoundEffect>("BEEP1A")
                base.LoadContent()
        
            override game.Update gameTime = 

                timer <- timer - float32 gameTime.ElapsedGameTime.TotalMilliseconds

                if timer <= -10000.0f && backBody.[0] = null then
                    clickSound.Play() |> ignore
                    for i = 0 to 99 do
                        backBody.[i] <- kinect.CaptureBody
                    beepSound.Play() |> ignore
                else if timer <= -5000.0f && sideBody.[0] = null then
                    clickSound.Play() |> ignore
                    for i = 0 to 99 do
                        sideBody.[i] <- kinect.CaptureBody
                    beepSound.Play() |> ignore
                else if timer <= 0.0f && frontBody.[0] = null then
                    clickSound.Play() |> ignore
                    for i = 0 to 99 do
                        frontBody.[i] <- kinect.CaptureBody
                    beepSound.Play() |> ignore
                    
                if frontBody.[99] <> null && backBody.[99] <> null && sideBody.[99] <> null && not finished then
                    game.Components.Add(new BodyMeasurements(this, kinect, frontBody, sideBody, backBody))
                    finished <- true
                base.Update gameTime

            override game.Draw gameTime = 
                game.GraphicsDevice.Clear(Color.CornflowerBlue)
                
                base.Draw gameTime

        let game = new XnaGame()
        game.Run()
