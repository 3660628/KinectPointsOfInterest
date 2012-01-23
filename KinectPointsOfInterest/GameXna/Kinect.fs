namespace KinectPointsOfInterest
    open Microsoft.Xna.Framework
    open Microsoft.Xna.Framework.Graphics
    open Microsoft.Research.Kinect.Nui

    

    open KinectHelperMethods

    open System
    
    type Kinect(game:Game)=
        inherit DrawableGameComponent(game)

        let nui = Runtime.Kinects.[0]//kinect natural user interface object
        let game = game
        let body = new BodyData.Body()

        let maxDist = 4000
        let minDist = 850
        let distOffset = maxDist - minDist

        let mutable liveDepthData:int[]=Array.zeroCreate (320 * 240)

        let mutable liveDepthView:Texture2D = null
        let mutable spriteBatch = null

        override this.Initialize ()=
            try 
                do nui.Initialize(RuntimeOptions.UseSkeletalTracking ||| RuntimeOptions.UseDepthAndPlayerIndex)
                do nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex)
            with
                | :? System.InvalidOperationException -> System.Diagnostics.Debug.Write("Kinect not connected!")
            spriteBatch <- new SpriteBatch(game.GraphicsDevice)
            
        override this.LoadContent()=
            liveDepthView <- new Texture2D(game.GraphicsDevice, 320, 240)
            
        override this.Update gameTime=
            let args = nui.DepthStream.GetNextFrame 0

            if args <> null then
                let pImg = args.Image
                let img = new Texture2D(game.GraphicsDevice, pImg.Width, pImg.Height)
                let DepthColor = Array.create (pImg.Width*pImg.Height) (new Color(255,255,255))

                for y = 0 to pImg.Height-1 do
                    for x = 0 to pImg.Width-1 do
                        let n = (y * pImg.Width + x) * 2
                        let distance = (int pImg.Bits.[n + 0] >>>3) ||| (int pImg.Bits.[n + 1] <<< 5) //put together bit data as depth
                        let pI = int (pImg.Bits.[n] &&& 7uy) // gets the player index
                        liveDepthData.[y * pImg.Width + x] <- if pI > 0 then distance else 0
                        //change distance to colour
                        let intensity = (if pI > 0 then (255-(255 * Math.Max(int(distance-minDist),0)/distOffset)) else 0) //convert distance into a gray level value between 0 and 255 taking into account min and max distances of the kinect.
                        let colour = new Color(intensity, intensity, intensity)
                        DepthColor.[y * pImg.Width + x] <- colour
                img.SetData(DepthColor)
                liveDepthView <- img

        override this.Draw gameTime=
            spriteBatch.Begin()
            if liveDepthView <> null then 
                spriteBatch.Draw(liveDepthView, new Vector2(0.0f, 0.0f), Color.White)
            spriteBatch.End()
         
        member this.LiveDepthData
            with get() = liveDepthData   
                            
        member this.CaptureBody =
            let body = new BodyData.Body()
            let skeletonFrame = nui.SkeletonEngine.GetNextFrame 100
            for skeleton in skeletonFrame.Skeletons do
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
                        
                    body.SetSkeleton(head, leftShoulder, rightShoulder, centerShoulder, leftHip, rightHip, centerHip, leftFoot,rightFoot, leftKnee, rightKnee)

            let depthFrame = nui.DepthStream.GetNextFrame 100
            let pImg = depthFrame.Image

            let distancesArray = Array.create (320*240) 0

            for y = 0 to pImg.Height-1 do
                for x = 0 to pImg.Width-1 do
                    let n = (y * pImg.Width + x) * 2
                    let distance = (int pImg.Bits.[n + 0] >>>3) ||| (int pImg.Bits.[n + 1] <<< 5) //put together bit data as depth
                    let pI = int (pImg.Bits.[n] &&& 7uy) // gets the player index
                        
                    distancesArray.[y*pImg.Width + x] <- if pI > 0 then distance else 0

            body.DepthImg <- distancesArray
            body

