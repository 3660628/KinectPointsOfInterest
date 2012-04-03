namespace KinectPointsOfInterest
    open Microsoft.Xna.Framework
    open Microsoft.Xna.Framework.Audio
    open Microsoft.Xna.Framework.Graphics
    open Microsoft.Kinect

    //open KinectHelperMethods

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
            || var1 = var2
            
            then
                returnval <- true
           
            returnval

        let fuzzyEqualsVector (var1:Vector3) (var2:Vector3) disparity=
            let mutable returnval = false
            if fuzzyEquals var1.X var2.X disparity
            && fuzzyEquals var1.Y var2.Y disparity then
                returnval <- true
           
            returnval

        let processJoint (joint:Joint, nui:KinectSensor) = //process the joint and translates it from depth space to screen space for a given resolution
                let DIPoint = nui.MapSkeletonPointToDepth(joint.Position, DepthImageFormat.Resolution320x240Fps30)
                new Vector3(float32 DIPoint.X * 4.0f - 256.0f , float32 DIPoint.Y* 4.0f - 256.0f, joint.Position.Z )
        
        let processJointRel (joint:Joint, nui:KinectSensor, referenceJoint:Joint) = //process the joint and translates it from depth space to screen space for a given resolution
                let DIPoint = nui.MapSkeletonPointToDepth(joint.Position, DepthImageFormat.Resolution320x240Fps30)
                let DIRef = nui.MapSkeletonPointToDepth(referenceJoint.Position, DepthImageFormat.Resolution320x240Fps30)
                new Vector3(float32 ((DIPoint.X*6)-DIRef.X), float32 ((DIPoint.Y*6)-DIRef.Y), joint.Position.Z )

        type KinectMeasure(game:Game)=
            inherit DrawableGameComponent(game)


            let nui = KinectSensor.KinectSensors.[0]//kinect natural user interface object
            let body = new BodyData.Body()

            let maxDist = 4000
            let minDist = 850
            let distOffset = maxDist - minDist

            let mutable liveDepthData:int[]=Array.zeroCreate (320 * 240)

            let mutable liveDepthView:Texture2D = null
            let mutable spriteBatch = null

            override this.Initialize ()=
                try 
                    do nui.Start() 
                    do nui.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30)
                with
                    | :? System.InvalidOperationException -> System.Diagnostics.Debug.Write("Kinect not connected!")
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
            
            override this.LoadContent()=
                liveDepthView <- new Texture2D(game.GraphicsDevice, 320, 240)
            
            override this.Update gameTime=
                let args = nui.DepthStream.OpenNextFrame 0

                if args <> null then
                    let depthPixelData = Array.init args.PixelDataLength (fun x-> int16 0)
                    args.CopyPixelDataTo depthPixelData
                    let img = new Texture2D(game.GraphicsDevice, args.Width, args.Height)
                    let DepthColor = Array.create (depthPixelData.Length) (new Color(255,255,255))

                    for n = 0 to depthPixelData.Length-1 do
                        
                        let distance = (int depthPixelData.[n] >>> DepthImageFrame.PlayerIndexBitmaskWidth ) //put together bit data as depth
                        let pI = (int depthPixelData.[n] &&& DepthImageFrame.PlayerIndexBitmask) // gets the player index
                        liveDepthData.[n] <- if pI > 0 then distance else 0
                        let intensity = (if pI > 0 then (255-(255 * Math.Max(int(distance-minDist),0)/distOffset)) else 0) //convert distance into a gray level value between 0 and 255 taking into account min and max distances of the kinect.
                        let colour = new Color(intensity, intensity, intensity)
                        DepthColor.[n] <- colour
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
                let skeletonFrame = nui.SkeletonStream.OpenNextFrame 100
                let skeletons:Skeleton[] = Array.init skeletonFrame.SkeletonArrayLength (fun x -> null)
                skeletonFrame.CopySkeletonDataTo skeletons
                for skeleton in skeletons do
                    if skeleton.TrackingState.Equals(SkeletonTrackingState.Tracked) then
                        let depthWidth, depthHeight = 320, 240
                        let leftShoulder= processJoint(skeleton.Joints.[JointType.ShoulderLeft], nui)
                        let rightShoulder = processJoint(skeleton.Joints.[JointType.ShoulderRight], nui)
                        let centerShoulder = processJoint(skeleton.Joints.[JointType.ShoulderCenter], nui)
                        let head = processJoint(skeleton.Joints.[JointType.Head], nui)
                        let leftHip = processJoint(skeleton.Joints.[JointType.HipLeft], nui)
                        let rightHip = processJoint(skeleton.Joints.[JointType.HipRight], nui)
                        let centerHip = processJoint(skeleton.Joints.[JointType.HipCenter ], nui)
                        let leftFoot = processJoint(skeleton.Joints.[JointType.FootLeft], nui)
                        let rightFoot = processJoint(skeleton.Joints.[JointType.FootRight], nui)
                        let leftKnee = processJoint(skeleton.Joints.[JointType.KneeLeft], nui)
                        let rightKnee = processJoint(skeleton.Joints.[JointType.KneeRight], nui)
                        
                        body.SetSkeleton(head, leftShoulder, rightShoulder, centerShoulder, leftHip, rightHip, centerHip, leftFoot,rightFoot, leftKnee, rightKnee)
                skeletonFrame.Dispose()

                let depthFrame = nui.DepthStream.OpenNextFrame 100

                let distancesArray = Array.create (320*240) 0

                let depthPixelData = Array.create depthFrame.PixelDataLength (int16 0)
                depthFrame.CopyPixelDataTo depthPixelData
                let img = new Texture2D(game.GraphicsDevice, depthFrame.Width, depthFrame.Height)
                let DepthColor = Array.create (depthPixelData.Length) (new Color(255,255,255))

                //for y = 0 to pImg.Height-1 do
                for n = 0 to depthPixelData.Length-1 do
                    //let n = (y * pImg.Width + x) * 2
                    let distance = (int depthPixelData.[n] >>> DepthImageFrame.PlayerIndexBitmaskWidth ) //put together bit data as depth
                    let pI = int (int depthPixelData.[n] &&& DepthImageFrame.PlayerIndexBitmask) // gets the player index
                    liveDepthData.[n] <- if pI > 0 then distance else 0
                    //change distance to colour
                    let intensity = (if pI > 0 then (255-(255 * Math.Max(int(distance-minDist),0)/distOffset)) else 0) //convert distance into a gray level value between 0 and 255 taking into account min and max distances of the kinect.
                    let colour = new Color(intensity, intensity, intensity)
                    DepthColor.[n] <- colour
                body.DepthImg <- distancesArray
                body

        exception NoUserTracked

        [<AllowNullLiteral>] //allow null as a proper value
        type KinectCursor(game:Game)=
            inherit DrawableGameComponent(game)
        
            let CLICKSENSITIVITY = 5 //lower is more sensitive
            let CLICKTIME = 1.5
        
            let mutable nui = null
            
            let mutable poseTime = 0
            let mutable lastPoseTime =0
            let prevPoseStates = Array.init 6 (fun x -> new KinectPoseState(game, nui))

            let maxDist = 4000
            let minDist = 850
            let distOffset = maxDist - minDist

            let mutable skeletonFrame = null
            let mutable lastSkeletonFrame = null

            let mutable leftHand = Vector3.Zero
            let mutable leftShoulder = Vector3.Zero
            let mutable rightHand = Vector3.Zero
            let mutable centerHip = Vector3.Zero //central hip position for reference with the hand
            let mutable rightElbow = Vector3.Zero
            let mutable rightShoulder = Vector3.Zero
            let mutable leftElbow = Vector3.Zero
            let mutable rightFoot = Vector3.Zero
            let mutable leftFoot = Vector3.Zero
            let mutable centerShoulder = Vector3.Zero

            let mutable rightHandClick = false
            let mutable leftHandClick = false

            let mutable rightClickTime = 0.0
            let mutable leftClickTime = 0.0
            let mutable lastRightHandPos = Vector3.Zero

            let mutable rightHandSprite:Texture2D = null //hand cursor texture
            let mutable leftHandSprite:Texture2D = null
            let mutable spriteBatch = null //for drawing the hand cursor
            let mutable jointSprite:Texture2D = null

            let mutable clickSound:SoundEffect = null
            let mutable countClicks = 0

            let kinectInitalize =
                try 
                    nui <- KinectSensor.KinectSensors.[0]//kinect natural user interface object
                    nui.Start() //(RuntimeOptions.UseSkeletalTracking ||| RuntimeOptions.UseDepthAndPlayerIndex)
                    do nui.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30)
                    let mutable parameters = new TransformSmoothParameters() // smooth out skeletal jiter
                    parameters.Smoothing <- 0.5f
                    parameters.Correction <- 0.3f
                    parameters.Prediction <- 0.3f
                    parameters.JitterRadius <- 1.0f
                    parameters.MaxDeviationRadius <- 0.3f
                    nui.SkeletonStream.Enable(parameters)
                with
                    | :? System.InvalidOperationException -> System.Diagnostics.Debug.Write("Kinect not connected!")
                    | :? System.ArgumentOutOfRangeException -> System.Diagnostics.Debug.Write("Kinect not connected!")
        
            let depthWidth, depthHeight = 1024, 768
            
       
            override this.Initialize ()=
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
                clickSound <- game.Content.Load<SoundEffect>("click_1")
                this.LoadContent()
            
            override this.LoadContent()=
                rightHandSprite <- game.Content.Load<Texture2D>("UI/HandRight70x81Animated")
                leftHandSprite <- game.Content.Load<Texture2D>("UI/HandLeft70x81")
                jointSprite <- game.Content.Load<Texture2D>("Sprite")
            
            override this.Update gameTime=
                if nui <> null then //only update possitions and get hand positions if kinect connected
                    skeletonFrame <- nui.SkeletonStream.OpenNextFrame 0
                    
                    if skeletonFrame <> null then
                        let skeletons = Array.init (skeletonFrame.SkeletonArrayLength) (fun x -> null)
                        skeletonFrame.CopySkeletonDataTo skeletons
                        for skeleton in skeletons do
                            if skeleton.TrackingState.Equals(SkeletonTrackingState.Tracked) then
                                //let depthWidth, depthHeight = 320, 240
                                leftHand <- processJoint( skeleton.Joints.[JointType.HandLeft], nui)
                                leftShoulder <- processJoint( skeleton.Joints.[JointType.ShoulderLeft], nui)
                                leftElbow <- processJoint(skeleton.Joints.[JointType.ElbowLeft], nui)
                                rightHand <- processJoint(skeleton.Joints.[JointType.HandRight], nui)
                                centerHip <- processJoint(skeleton.Joints.[JointType.HipCenter], nui)
                                rightElbow <- processJoint(skeleton.Joints.[JointType.ElbowRight], nui)
                                rightShoulder <- processJoint(skeleton.Joints.[JointType.ShoulderRight], nui)
                                rightFoot <- processJoint(skeleton.Joints.[JointType.FootRight], nui)
                                leftFoot <- processJoint(skeleton.Joints.[JointType.FootRight], nui)
                                centerShoulder <- processJoint(skeleton.Joints.[JointType.ShoulderCenter], nui)
                        //lastSkeletonFrame <- skeletonFrame
                        skeletonFrame.Dispose()

//                    let RhesDist = Vector3.Distance(rightShoulder, rightElbow) + Vector3.Distance(rightElbow, rightHand)
//                    let RhsDist = Vector3.Distance(rightShoulder, rightHand)
//                    let rhClick = fuzzyEquals RhesDist RhsDist 6
//                    let LhesDist = Vector3.Distance(leftShoulder, leftElbow) + Vector3.Distance(leftElbow, leftHand)
//                    let LhsDist = Vector3.Distance(leftShoulder, leftHand)
//                    let lhClick = fuzzyEquals RhesDist RhsDist 6
//
//                    //System.Diagnostics.Debug.WriteLine("RIGHT HAND CLICK:"+(RhesDist-RhsDist).ToString())
//                    if rhClick && rightHandColor.Equals(Color.White) then //a click with right hand
//                        System.Diagnostics.Debug.WriteLine(">>>>>>>>>>>kinectClick @ " + string gameTime.TotalGameTime.Seconds + " seconds<<<<<<<<<<<<")
//                        countClicks <- countClicks + 1
//                        rightHandColor <- Color.Red 
//                        //clickSound.Play() |> ignore
//                        System.Diagnostics.Debug.WriteLine(">>>>>>>>>>>end of kinect click<<<<<<<<<<<<")
//                
//                    else if not rhClick then //release right hand click
//                        rightHandColor <- Color.White

//                    if (Vector3.Distance(leftShoulder, leftHand)) >= CLICKSENSITIVITY && leftHandColor.Equals(Color.White) then //a click with left hand
//                        leftHandColor <- Color.Red 
//                        clickSound.Play() |> ignore
//                    else if (Vector3.Distance(leftShoulder, leftHand)) < CLICKSENSITIVITY then //relese left hand click
//                        leftHandColor <- Color.White
                        if (fuzzyEqualsVector lastRightHandPos rightHand CLICKSENSITIVITY) then
                            rightClickTime <- rightClickTime + gameTime.ElapsedGameTime.TotalSeconds
                        else
                            rightHandClick <- false
                            rightClickTime <- 0.0
                    
                        if rightClickTime >= CLICKTIME && not rightHandClick then
                            System.Diagnostics.Debug.WriteLine(">>>>>>>>>>>kinectClick @ " + string gameTime.TotalGameTime.Seconds + " seconds<<<<<<<<<<<<")
                            countClicks <- countClicks + 1
                            rightHandClick <- true
                            clickSound.Play() |> ignore
                            System.Diagnostics.Debug.WriteLine(">>>>>>>>>>>end of kinect click<<<<<<<<<<<<")
                        if rightClickTime >= CLICKTIME + 0.5 then
                            rightHandClick <- false
                            rightClickTime <- 0.0
                    lastRightHandPos <- rightHand //update last right hand position

            override this.Draw gameTime=
                if nui <> null then //only draw the hand cursor if a kinect is connected
                    let rightHandAnimationFrame = int (Math.Min((Math.Round((rightClickTime * (10.0/CLICKTIME)), 0)), 10.0))
                    System.Diagnostics.Debug.WriteLine("aniFrame:" + rightHandAnimationFrame.ToString())
                    spriteBatch.Begin()
                    spriteBatch.Draw(rightHandSprite, new Vector2(rightHand.X, rightHand.Y), Nullable<_> (new Rectangle (rightHandAnimationFrame*70,0, 70,81)), Color.White) //draw right hand cursor
                    spriteBatch.Draw(leftHandSprite, new Vector2(leftHand.X, leftHand.Y), Color.White) //draw left hand cursor
                    spriteBatch.Draw(jointSprite, new Vector2(rightShoulder.X, rightShoulder.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(leftShoulder.X, leftShoulder.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(leftElbow.X, leftElbow.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(rightElbow.X, rightElbow.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(leftFoot.X, leftFoot.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(rightFoot.X, rightFoot.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(centerShoulder.X, centerShoulder.Y), Color.White)
                    spriteBatch.Draw(jointSprite, new Vector2(centerHip.X, centerHip.Y), Color.White)
                    spriteBatch.End()

            member this.GetState() = new KinectCursorState(leftHand, (if not leftHandClick then Microsoft.Xna.Framework.Input.ButtonState.Released else Microsoft.Xna.Framework.Input.ButtonState.Pressed), rightHand, (if not rightHandClick then Microsoft.Xna.Framework.Input.ButtonState.Released else Microsoft.Xna.Framework.Input.ButtonState.Pressed), centerHip)
        
        
            member this.PoseStable (poseArray:KinectPoseState[])=
                let mutable stable = true
                for x = 0 to 4 do
                    if stable then
                        stable <- poseArray.[x].Close poseArray.[x+1]
                stable

            member this.PoseType (poseArray:KinectPoseState[])=
                let mutable poseType = 0
                for x in poseArray do
                    if x.FrontMeasurePose then
                         poseType <- poseType + 1
                         System.Console.WriteLine "frontPose Detected" 
                    else if x.SideMeasurePose then
                         poseType <- poseType + 2
                         System.Console.WriteLine "sidePose Detected" 

                if poseType = 6 then
                    "Front"
                else if poseType = 12 then
                    "Side"
                else
                    "None"
            member this.GetPose(gameTime:GameTime) = 
                lastPoseTime <- lastPoseTime + gameTime.ElapsedGameTime.Milliseconds
                if lastPoseTime >= 500 then
                    if skeletonFrame <> null then 
                        for x = 0 to 4 do
                            prevPoseStates.[x] <- prevPoseStates.[x+1]
                        prevPoseStates.[5] <- new KinectPoseState(game, nui, leftHand,rightHand,leftFoot,rightFoot,leftShoulder,rightShoulder,centerShoulder, centerHip ) 
                        System.Console.WriteLine "NEW POSE ADDED" 
                    else 
                        raise NoUserTracked
                    lastPoseTime <- 0
                if this.PoseStable prevPoseStates then
                    if this.PoseType prevPoseStates = "Front" then
                        "front"
                    else if this.PoseType prevPoseStates = "Side" then
                        "side"
                    else
                        "none"
                else
                    "none"

        //****************************************************************       
        and KinectCursorState(leftHandPos, leftButton, rightHandPos, rightButton, centerHips)=

            member this.LeftHandPosition
                with get() = leftHandPos
        
            member this.RightHandPosition
                with get() = rightHandPos

            member this.LeftButton
                with get() = leftButton

            member this.RightButton
                with get() = rightButton

            member this.CenterHipReference
                with get() = centerHips

        and KinectPoseState(game:Game, nui:KinectSensor, leftHand,rightHand,leftFoot,rightFoot,leftShoulder,rightShoulder,centerShoulder, centerHips )=
            let depthWidth, depthHeight = 1024, 768
            new(game:Game, nui:KinectSensor) = KinectPoseState(game, nui, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero)

            //detects if the user in standing in the correct pos to be measured from the front
            member this.FrontMeasurePose=
                let mutable result = false
                let disparity = 20
                if fuzzyEquals leftHand.Y rightHand.Y disparity && not (fuzzyEquals leftHand.X rightHand.X 5)//left and right hand are level
                && fuzzyEquals leftShoulder.Y rightShoulder.Y disparity && not (fuzzyEquals leftShoulder.X rightHand.X disparity) // left and right shoulder are straight
                && fuzzyEquals leftHand.Y centerShoulder.Y disparity //left hand is level with shoulders
                && fuzzyEquals centerShoulder.X centerHips.X disparity //back is straight
                && fuzzyEquals leftFoot.X rightFoot.X 100 //feet are not too far apart
                then
                    result <- true
                result

            //detects if the user in standing in the correct pos to be measured from the front
            member this.SideMeasurePose=
                let mutable result = false
                let disparity = 20
                if fuzzyEquals leftHand.Y rightHand.Y disparity && fuzzyEquals leftHand.X rightHand.X disparity//left and right hand are level//left and right hand are level
                && fuzzyEquals leftShoulder.Y rightShoulder.Y disparity && fuzzyEquals leftShoulder.X rightShoulder.X disparity // left and right shoulder are straight
                && fuzzyEquals leftHand.Y centerShoulder.Y disparity && fuzzyEquals leftHand.X centerShoulder.X disparity //left hand is level with shoulders
                && fuzzyEquals centerShoulder.X centerHips.X disparity //back is straight
                && not(fuzzyEquals centerShoulder.Y leftFoot.Y 10)
                //&& fuzzyEquals leftFoot.X rightFoot.X 10 && fuzzyEquals leftFoot.Y rightFoot.Y 10//feet are together
                then
                    result <- true
                result

            member this.Close (kps:KinectPoseState)=
                let disparity = 10
                fuzzyEquals this.LeftHand.Y kps.LeftHand.Y disparity
                && fuzzyEquals this.LeftHand.X kps.LeftHand.X disparity
                && fuzzyEquals this.LeftFoot.Y kps.LeftFoot.Y disparity
                && fuzzyEquals this.LeftFoot.X kps.LeftFoot.X disparity
                && fuzzyEquals this.RightHand.Y kps.RightHand.Y disparity
                && fuzzyEquals this.RightHand.X kps.RightHand.X disparity
                && fuzzyEquals this.RightFoot.Y kps.RightFoot.Y disparity
                && fuzzyEquals this.RightFoot.X kps.RightFoot.X disparity
                && fuzzyEquals this.RightShoulder.Y kps.RightShoulder.Y disparity
                && fuzzyEquals this.RightShoulder.X kps.RightShoulder.X disparity
                && fuzzyEquals this.LeftShoulder.Y kps.LeftShoulder.Y disparity
                && fuzzyEquals this.LeftShoulder.X kps.LeftShoulder.X disparity
                && fuzzyEquals this.CenterHips.Y kps.CenterHips.Y disparity
                && fuzzyEquals this.CenterHips.X kps.CenterHips.X disparity
                && fuzzyEquals this.CenterShoulders.Y kps.CenterShoulders.Y disparity 
                && fuzzyEquals this.CenterShoulders.X kps.CenterShoulders.X disparity

            member this.LeftHand
                with get():Vector3 = leftHand    
            member this.RightHand
                with get():Vector3 = rightHand 
            member this.LeftShoulder
                with get():Vector3 = leftShoulder
            member this.RightShoulder
                with get():Vector3 = rightShoulder
            member this.LeftFoot
                with get():Vector3 = leftFoot
            member this.RightFoot
                with get():Vector3 = rightFoot
            member this.CenterShoulders
                with get():Vector3 = centerShoulder
            member this.CenterHips
                with get():Vector3 = centerHips