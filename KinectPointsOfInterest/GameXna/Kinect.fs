namespace KinectPointsOfInterest
    open Microsoft.Xna.Framework
    open Microsoft.Xna.Framework.Audio
    open Microsoft.Xna.Framework.Graphics
    open Microsoft.Research.Kinect.Nui

    open KinectHelperMethods

    open System
    open System.Windows.Forms
    
    module Kinect=
        //used to check if values are close to each other.
        //the two var1 parameters are the values being compared
        //disparity sets the strictness of the function
        let fuzzyEquals var1 var2 disparity=
            let mutable returnval = false
            if ((var1 + float32 disparity  >= var2 //case: var1 is less than var2 but within the range
            && var1 < var2))
            || ((var1 - float32 disparity  <= var2 //case: var1 is less than var2 but within the range
            && var1 > var2))
            
            then
                returnval <- true
           
            returnval


        type KinectMeasure(game:Game)=
            inherit DrawableGameComponent(game)


            let nui = Runtime.Kinects.[0]//kinect natural user interface object
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

        exception NoUserTracked

        [<AllowNullLiteral>] //allow null as a proper value
        type KinectCursor(game:Game)=
            inherit DrawableGameComponent(game)
        
            let CLICKSENSITIVITY = 0.7f //lower is more sensitive
        
            let mutable nui = null
        
            let maxDist = 4000
            let minDist = 850
            let distOffset = maxDist - minDist

            let mutable skeletonFrame = null

            let mutable leftHand = Vector3.Zero
            let mutable leftShoulder = Vector3.Zero
            let mutable rightHand = Vector3.Zero
            let mutable centerHip = Vector3.Zero //central hip position for reference with the hand
            let mutable rightElbow = Vector3.Zero
            let mutable rightShoulder = Vector3.Zero
            let mutable leftElbow = Vector3.Zero

            let mutable rightHandColor = Color.White
            let mutable leftHandColor = Color.White

            let mutable rightHandSprite:Texture2D = null //hand cursor texture
            let mutable leftHandSprite:Texture2D = null
            let mutable spriteBatch = null //for drawing the hand cursor
            let mutable jointSprite:Texture2D = null

            let mutable clickSound:SoundEffect = null
            let mutable countClicks = 0

            let kinectInitalize =
                try 
                    nui <- Runtime.Kinects.[0]//kinect natural user interface object
                    do nui.Initialize(RuntimeOptions.UseSkeletalTracking ||| RuntimeOptions.UseDepthAndPlayerIndex)
                    do nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex)
                    nui.SkeletonEngine.TransformSmooth <- true
                    let mutable parameters = new TransformSmoothParameters() // smooth out skeletal jiter
                    parameters.Smoothing <- 0.5f
                    parameters.Correction <- 0.3f
                    parameters.Prediction <- 0.3f
                    parameters.JitterRadius <- 1.0f
                    parameters.MaxDeviationRadius <- 0.3f
                    nui.SkeletonEngine.SmoothParameters <- parameters
                with
                    | :? System.InvalidOperationException -> System.Diagnostics.Debug.Write("Kinect not connected!")
                    | :? System.ArgumentOutOfRangeException -> System.Diagnostics.Debug.Write("Kinect not connected!")
        
            let depthWidth, depthHeight = 1024, 768
            let processJoint (joint:Joint) = //process the joint and translates it from depth space to screen space for a given resolution
                new Vector3(joint.GetScreenPosition(nui, depthWidth, depthHeight).X, joint.GetScreenPosition(nui, depthWidth, depthHeight).Y, joint.Position.Z )

       
            override this.Initialize ()=
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
                clickSound <- game.Content.Load<SoundEffect>("click_1")
                this.LoadContent()
            
            override this.LoadContent()=
                rightHandSprite <- game.Content.Load<Texture2D>("UI/HandRight70x81")
                leftHandSprite <- game.Content.Load<Texture2D>("UI/HandLeft70x81")
                jointSprite <- game.Content.Load<Texture2D>("Sprite")
            
            override this.Update gameTime=
                if nui <> null then //only update possitions and get hand positions if kinect connected
                    skeletonFrame <- nui.SkeletonEngine.GetNextFrame 0
                    if skeletonFrame <> null then
                        for skeleton in skeletonFrame.Skeletons do
                            if skeleton.TrackingState.Equals(SkeletonTrackingState.Tracked) then
                                //let depthWidth, depthHeight = 320, 240
                                leftHand <- processJoint skeleton.Joints.[JointID.HandLeft]
                                leftShoulder <- processJoint skeleton.Joints.[JointID.ShoulderLeft]
                                leftElbow <- processJoint skeleton.Joints.[JointID.ElbowLeft]
                                rightHand <- processJoint skeleton.Joints.[JointID.HandRight]
                                centerHip <- processJoint skeleton.Joints.[JointID.HipCenter]
                                rightElbow <- processJoint skeleton.Joints.[JointID.ElbowRight]
                                rightShoulder <- processJoint skeleton.Joints.[JointID.ShoulderRight]

    //                    let hand2elbow = ()
    //                    let crossHandShoulder = Vector3.Cross(rightHand, rightShoulder)
    //                    let crossHandElbow = Vector3.Cross(rightHand, rightElbow)
    //                    if Vector3.Distance(crossHandShoulder, Vector3.Zero) < 10.0f && Vector3.Distance(crossHandElbow, Vector3.Zero) < 10.0f then
    //                        System.Diagnostics.Debug.WriteLine("\n*******ARM IS STRAIGHT*********\n")
    //
    //                    System.Diagnostics.Debug.WriteLine("crossHandShoulder:"+ Vector3.Distance(crossHandShoulder, Vector3.Zero).ToString())

                    let RhesDist = Vector3.Distance(rightShoulder, rightElbow) + Vector3.Distance(rightElbow, rightHand)
                    let RhsDist = Vector3.Distance(rightShoulder, rightHand)
                    let rhClick = fuzzyEquals RhesDist RhsDist 6
                    let LhesDist = Vector3.Distance(leftShoulder, leftElbow) + Vector3.Distance(leftElbow, leftHand)
                    let LhsDist = Vector3.Distance(leftShoulder, leftHand)
                    let lhClick = fuzzyEquals RhesDist RhsDist 6

                    System.Diagnostics.Debug.WriteLine("RIGHT HAND CLICK:"+(RhesDist-RhsDist).ToString())
                    if rhClick && rightHandColor.Equals(Color.White) then //a click with right hand
                        System.Diagnostics.Debug.WriteLine(">>>>>>>>>>>kinectClick @ " + string gameTime.TotalGameTime.Seconds + " seconds<<<<<<<<<<<<")
                        countClicks <- countClicks + 1
                        rightHandColor <- Color.Red 
                        clickSound.Play() |> ignore
                        System.Diagnostics.Debug.WriteLine(">>>>>>>>>>>end of kinect click<<<<<<<<<<<<")
                
                    else if not rhClick then //release right hand click
                        rightHandColor <- Color.White

//                    if (Vector3.Distance(leftShoulder, leftHand)) >= CLICKSENSITIVITY && leftHandColor.Equals(Color.White) then //a click with left hand
//                        leftHandColor <- Color.Red 
//                        clickSound.Play() |> ignore
//                    else if (Vector3.Distance(leftShoulder, leftHand)) < CLICKSENSITIVITY then //relese left hand click
//                        leftHandColor <- Color.White
            

            override this.Draw gameTime=
                if nui <> null then //only draw the hand cursor if a kinect is connected
                    spriteBatch.Begin()
                    spriteBatch.Draw(rightHandSprite, new Vector2(rightHand.X, rightHand.Y), rightHandColor) //draw right hand cursor
                    spriteBatch.Draw(leftHandSprite, new Vector2(leftHand.X, leftHand.Y), leftHandColor) //draw left hand cursor
                    spriteBatch.Draw(jointSprite, new Vector2(rightShoulder.X, rightShoulder.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(leftShoulder.X, leftShoulder.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(leftElbow.X, leftElbow.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(rightElbow.X, rightElbow.Y), Color.White)
                    spriteBatch.End()

            member this.GetState() = new KinectCursorState(leftHand, (if leftHandColor = Color.White then Microsoft.Xna.Framework.Input.ButtonState.Released else Microsoft.Xna.Framework.Input.ButtonState.Pressed), rightHand, (if rightHandColor = Color.White then Microsoft.Xna.Framework.Input.ButtonState.Released else Microsoft.Xna.Framework.Input.ButtonState.Pressed))
        
        

            member this.GetPose() = if skeletonFrame <> null then new KinectPoseState(game, nui, skeletonFrame) else raise NoUserTracked
                

        //****************************************************************       
        and KinectCursorState(leftHandPos, leftButton, rightHandPos, rightButton)=

            member this.LeftHandPosition
                with get() = leftHandPos
        
            member this.RightHandPosition
                with get() = rightHandPos

            member this.LeftButton
                with get() = leftButton

            member this.RightButton
                with get() = rightButton

        and KinectPoseState(game:Game, nui:Runtime, skeletonFrame:SkeletonFrame)=
            let depthWidth, depthHeight = game.GraphicsDevice.Viewport.Width, game.GraphicsDevice.Viewport.Height

            let mutable leftHand, rightHand, centerHips, centerShoulder, leftShoulder, rightShoulder, leftFoot, rightFoot = 
                Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero

            let processJoint (joint:Joint) = //process the joint and translates it from depth space to screen space for a given resolution
                new Vector3(joint.GetScreenPosition(nui, depthWidth, depthHeight).X, joint.GetScreenPosition(nui, depthWidth, depthHeight).Y, joint.Position.Z )
                
            do for skeleton in skeletonFrame.Skeletons do
                do if skeleton.TrackingState.Equals(SkeletonTrackingState.Tracked) then
                
                    leftHand <- processJoint skeleton.Joints.[JointID.ElbowLeft]
                    rightHand <- processJoint skeleton.Joints.[JointID.ElbowRight]
                    centerHips <- processJoint skeleton.Joints.[JointID.HipCenter]
                    centerShoulder <- processJoint skeleton.Joints.[JointID.ShoulderCenter]
                    leftShoulder <- processJoint skeleton.Joints.[JointID.ShoulderLeft]
                    rightShoulder <- processJoint skeleton.Joints.[JointID.ShoulderRight]
                    leftFoot <- processJoint skeleton.Joints.[JointID.FootLeft]
                    rightFoot <- processJoint skeleton.Joints.[JointID.FootRight]          
                
        
        

            //detects if the user in standing in the correct pos to be measured from the front
            member this.FrontMeasurePose=
                let mutable result = false
                let disparity = 20
                if fuzzyEquals leftHand.Y rightHand.Y disparity //left and right hand are level
                && fuzzyEquals leftShoulder.Y rightShoulder.Y disparity // left and right shoulder are straight
                && fuzzyEquals leftHand.Y centerShoulder.Y disparity //left hand is level with shoulders
                && fuzzyEquals centerShoulder.X centerHips.X disparity //back is straight
                && fuzzyEquals leftFoot.X rightFoot.X 100 //feet are not too far apart
                then
                    result <- true
                result

            //detects if the user in standing in the correct pos to be measured from the front
            member this.SideMeasurePose=
                let mutable result = false
                let disparity = 10
                if fuzzyEquals leftHand.Y rightHand.Y disparity && fuzzyEquals leftHand.X rightHand.X disparity //left and right hand are level//left and right hand are level
                && fuzzyEquals leftShoulder.Y rightShoulder.Y disparity && fuzzyEquals leftShoulder.X rightShoulder.X disparity// left and right shoulder are straight
                && fuzzyEquals leftHand.Y centerShoulder.Y disparity && fuzzyEquals leftHand.X centerShoulder.X disparity//left hand is level with shoulders
                && fuzzyEquals centerShoulder.X centerHips.X disparity //back is straight
                && fuzzyEquals leftFoot.X rightFoot.X 10 && fuzzyEquals leftFoot.Y rightFoot.Y 10//feet are together
                then
                    result <- true
                result