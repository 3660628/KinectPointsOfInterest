namespace KinectPointsOfInterest

    module Program=
        open Microsoft.Xna.Framework
        open Microsoft.Xna.Framework.Graphics

        open Microsoft.Research.Kinect.Nui
        open KinectHelperMethods

        open System

        type XnaGame() as this =
            inherit Game()
    
            do this.Content.RootDirectory <- "XnaGameContent"
            let graphicsDeviceManager = new GraphicsDeviceManager(this)

            let screenWidth, screenHeight = 640, 480

            let mutable sprite : Texture2D = null
            let mutable spriteBatch : SpriteBatch = null

            let kinect = new Kinect(this)
    
            //vertical points of interest. all dimensions are in visualisation space
            let mutable topOfHeadY = 0.0f
            let mutable bottomOfFeetY = 0.0f
            let mutable waistY = 0.0f
            let mutable shouldersY = 0.0f

            //depth image map
            let mutable depthImage:Texture2D = null

            override game.Initialize() =
                graphicsDeviceManager.GraphicsProfile <- GraphicsProfile.HiDef
                graphicsDeviceManager.PreferredBackBufferWidth <- 640
                graphicsDeviceManager.PreferredBackBufferHeight <- 480
                graphicsDeviceManager.ApplyChanges() 
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
                kinect.Initialize()
                base.Initialize()

            override game.LoadContent() =
                sprite <- game.Content.Load<Texture2D>("Sprite")
                base.LoadContent()
        
            override game.Update gameTime = 
                base.Update gameTime

            override game.Draw gameTime = 
                game.GraphicsDevice.Clear(Color.CornflowerBlue)
                spriteBatch.Begin()

                //Draw the depth map display
                if not(depthImage = null) then spriteBatch.Draw(depthImage, new Rectangle(0, 0, screenWidth, screenHeight), Color.White)
                spriteBatch.End()
                base.Draw gameTime

            member game.SetDepthImage dI=
                depthImage <- dI

        and Kinect(game:XnaGame)=

            let nui = Runtime.Kinects.[0]//kinect natural user interface object
            let game = game
            let body = new BodyData.BodyMeasurements(game)

            //*****************SKELETON EVENT HANDLER************************************
            //Manipulates and processes the skeleton data once it has been recieved 
            //from the sensor
            //***************************************************************************
            let SkeletonReady (sender : obj) (args: SkeletonFrameReadyEventArgs)=
                for skeleton in args.SkeletonFrame.Skeletons do
                    if skeleton.TrackingState.Equals(SkeletonTrackingState.Tracked) then
                        let depthWidth, depthHeight = 320, 240
                        let leftShoulderJ = skeleton.Joints.[JointID.ShoulderLeft]
                        let leftShoulder = new Vector3(leftShoulderJ.GetScreenPosition(nui, 320, 240).X, leftShoulderJ.GetScreenPosition(nui, 320, 240).Y, leftShoulderJ.Position.Z )
                        let rightShoulderJ = skeleton.Joints.[JointID.ShoulderRight]
                        let rightShoulder = new Vector3(rightShoulderJ.GetScreenPosition(nui, 320, 240).X, rightShoulderJ.GetScreenPosition(nui, 320, 240).Y, rightShoulderJ.Position.Z )
                        let centerShoulderJ = skeleton.Joints.[JointID.ShoulderCenter]
                        let centerShoulder = new Vector3(centerShoulderJ.GetScreenPosition(nui, 320, 240).X, centerShoulderJ.GetScreenPosition(nui, 320, 240).Y, centerShoulderJ.Position.Z )
                        let headJ = skeleton.Joints.[JointID.Head]
                        let head = new Vector3(headJ.GetScreenPosition(nui, 320, 240).X, headJ.GetScreenPosition(nui, 320, 240).Y, headJ.Position.Z )
                        let leftHipJ = skeleton.Joints.[JointID.HipLeft]
                        let leftHip = new Vector3(leftHipJ.GetScreenPosition(nui, 320, 240).X, leftHipJ.GetScreenPosition(nui, 320, 240).Y, leftHipJ.Position.Z )
                        let rightHipJ = skeleton.Joints.[JointID.HipRight]
                        let rightHip = new Vector3(rightHipJ.GetScreenPosition(nui, 320, 240).X, rightHipJ.GetScreenPosition(nui, 320, 240).Y, rightHipJ.Position.Z )
                        let centerHipJ = skeleton.Joints.[JointID.HipCenter ]
                        let centerHip = new Vector3(centerHipJ.GetScreenPosition(nui, 320, 240).X, centerHipJ.GetScreenPosition(nui, 320, 240).Y, centerHipJ.Position.Z )
                        let leftFootJ = skeleton.Joints.[JointID.FootLeft]
                        let leftFoot = new Vector3(leftFootJ.GetScreenPosition(nui, 320, 240).X, leftFootJ.GetScreenPosition(nui, 320, 240).Y, leftFootJ.Position.Z )
                        let rightFootJ = skeleton.Joints.[JointID.FootRight]
                        let rightFoot = new Vector3(rightFootJ.GetScreenPosition(nui, 320, 240).X, rightFootJ.GetScreenPosition(nui, 320, 240).Y, rightFootJ.Position.Z )
                        let leftKneeJ = skeleton.Joints.[JointID.KneeLeft]
                        let leftKnee = new Vector3(leftKneeJ.GetScreenPosition(nui, 320, 240).X, leftKneeJ.GetScreenPosition(nui, 320, 240).Y, leftKneeJ.Position.Z )
                        let rightKneeJ = skeleton.Joints.[JointID.KneeRight]
                        let rightKnee = new Vector3(rightKneeJ.GetScreenPosition(nui, 320, 240).X, rightKneeJ.GetScreenPosition(nui, 320, 240).Y, rightKneeJ.Position.Z )

                        body.SetSkeleton(head, leftShoulder, rightShoulder, centerShoulder, leftHip, rightHip, centerHip, leftFoot, rightFoot, leftKnee, rightKnee)
            //***************************************************************************

            
            //*****************DEPTHFRAME EVENT HANDLER**********************************
            //Manipulates and processes the depth data once it has been recieved from 
            //the sensor
            //***************************************************************************
            let DepthReady (sender : obj) (args:ImageFrameReadyEventArgs)=
                        
                let maxDist = 4000
                let minDist = 850
                let distOffset = maxDist - minDist

                let pImg = args.ImageFrame.Image
                let img = new Texture2D(game.GraphicsDevice, pImg.Width, pImg.Height)
                let DepthColor = Array.create (pImg.Width*pImg.Height) (new Color(255,255,255))

                let distancesArray = Array.create (320*240) 0

                for y = 0 to pImg.Height-1 do
                    for x = 0 to pImg.Width-1 do
                        let n = (y * pImg.Width + x) * 2
                        let distance = (int pImg.Bits.[n + 0] >>>3) ||| (int pImg.Bits.[n + 1] <<< 5) //put together bit data as depth
                        let pI = int (pImg.Bits.[n] &&& 7uy) // gets the player index
                        
                        distancesArray.[y*pImg.Width + x] <- if pI > 0 then distance else 0

                        //change distance to colour
                        let intensity = (if pI > 0 then (255-(255 * Math.Max(int(distance-minDist),0)/distOffset)) else 0) //convert distance into a gray level value between 0 and 255 taking into account min and max distances of the kinect.
                        let colour = new Color(intensity, intensity, intensity)
                        DepthColor.[y * pImg.Width + x] <- colour

                img.SetData(DepthColor)

                let copy =  Array.create distancesArray.Length 0 //maybe unnessescary, will check
                Array.Copy(distancesArray, copy, distancesArray.Length)
                body.SetDepthData copy
                game.SetDepthImage img  //pass depth image to the main program  
            //****************************************************************************   
            
            member this.Initialize ()=
                try 
                    do nui.Initialize(RuntimeOptions.UseSkeletalTracking ||| RuntimeOptions.UseDepthAndPlayerIndex)
                    //do nui.SkeletonEngine.TransformSmooth <- true;
                    do nui.SkeletonFrameReady.AddHandler(new EventHandler<SkeletonFrameReadyEventArgs>(SkeletonReady))
                    do nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex)
                    do nui.DepthFrameReady.AddHandler(new EventHandler<ImageFrameReadyEventArgs>(DepthReady))
                    game.Components.Add(body)
                with
                    | :? System.InvalidOperationException -> System.Diagnostics.Debug.Write("Kinect not connected!")
                            
            member this.Update ()=
                ()

        let game = new XnaGame()
        game.Run()
