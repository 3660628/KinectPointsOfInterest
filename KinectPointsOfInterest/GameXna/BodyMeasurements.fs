namespace BodyData

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open System
    
    //*************************************************************
    // Methods to measure the body from 3 views, front side and back
    //
    // parameters:
    //      game:Game - the Game object that is using this class
    //      frontBody:Body - the front body view with joints and depth data
    //      leftSideBody:Body - the body view of the user's left side with joints and depth data
    //      backBody:Body - the back body view with joints and depth data
    //*************************************************************
    type BodyMeasurements(game, frontBody:Body, leftSideBody:Body, backBody:Body)=
        inherit DrawableGameComponent(game)
        
        let phi = 1.61803399 //golden ratio
        
        //points of interest
        let mutable topOfHead = 0.0f
        let mutable bottomOfFeet = 0.0f
        let mutable waist = 0.0f
        let mutable height = 0.0f
        let mutable shoulders = 0.0f
        let mutable hips = 0.0f
        let mutable knees = 0.0f

        let mutable pointsFound = false

        let mutable pointOfInterestLine:Texture2D = null
        let mutable frontBodyView:Texture2D = null
        let mutable sideBodyView:Texture2D = null
        
        let game = game
        let mutable spriteBatch = null

        //pixel resolution formula, obtained empirically
        let horizontalPixelResolution depth =
            374.0 / 80096.0 * Math.Pow(depth, -1.0)
        
        let measureSurfaceDistance (points:int[]) =
            let mutable measurement = 0.0
            let mutable pixelWidth=0.0
            let mutable lastPixelDepth =0
            let mutable i=0
            while lastPixelDepth = 0 && i<points.Length-1 do
                lastPixelDepth <- points.[i]
                i<-i+1
            while i < points.Length-1 do
                pixelWidth <- 374.0 / (80096.0 * Math.Pow(float(points.[i]), -0.953))
                if points.[i] >0 then
                    let currentPixelDepthChange = Math.Sqrt(Math.Pow(float(points.[i] - lastPixelDepth),2.0))
                    //By pythagoras
                    let diagonalWH = Math.Sqrt(Math.Pow(currentPixelDepthChange,2.0) + Math.Pow(pixelWidth, 2.0))
                    measurement <- measurement + diagonalWH
                    lastPixelDepth <- points.[i]
                i<-i+1
            measurement

        //These members find the top and bottom most points of the depth image
        //The values they return are based on the 2D visualisation space i.e. in the range x=0-240, y=0-320
        member this.GetTopOfHead =
            let depthImage = frontBody.DepthImg
            let head = frontBody.GetJoint("head")
            let mutable TOH = Unchecked.defaultof<Vector3>
            let mutable y = 0
            while y < 239 && TOH.Equals(Unchecked.defaultof<Vector3>) do
                let mutable x = 0
                while x < 319 && TOH.Equals(Unchecked.defaultof<Vector3>) do
                    let arrayPosition = y * 320 + x  
                    let depth = depthImage.[arrayPosition]
                    if depth > 0 then
                        let coordinates = new Vector3(float32 x, float32 y, float32 depth)
                        //check it is not a hand raised above the head
                        let closeEnoughToHead = 
                            let euclidDist = Vector2.Distance(new Vector2(head.X, head.Y), new Vector2(coordinates.X, coordinates.Y))
                            if euclidDist < 50.0f then
                                true
                            else
                                false
                        if closeEnoughToHead then
                            TOH <- coordinates
                            System.Diagnostics.Debug.WriteLine("TopOfHead=" + TOH.ToString())
                    x <- x + 1
                y <- y + 1
            topOfHead <- TOH.Y
            //TOH

        member this.GetBottomOfFeet =
            let depthImage = frontBody.DepthImg
            let mutable BOF = Unchecked.defaultof<Vector3>
            let mutable y = 0
            while y < 239 && BOF.Equals(Unchecked.defaultof<Vector3>) do
                let mutable x = 0
                while x < 319 && BOF.Equals(Unchecked.defaultof<Vector3>) do
                    let arrayPosition = 76799 - (y * 320 + x)  
                    let depth = depthImage.[arrayPosition]
                    if depth > 0 then
                        let coordinates = new Vector3(320.0f - float32 x, 240.0f - float32 y, float32 depth)
                        BOF <- coordinates
                        System.Diagnostics.Debug.WriteLine("BottomOfFeet=" + BOF.ToString())
                    x <- x + 1
                y <- y + 1
            bottomOfFeet <- BOF.Y
            //BOF

        member this.GetHips =
            let kneeL = backBody.GetJoint("leftKnee")
            let depthImage = backBody.DepthImg
            let mutable h = 0
            let mutable hipWidth = 0.0
            
            let mutable y = int (backBody.GetJoint("centerHip").Y) //start at hip bone as hips are below this
            while y < (int kneeL.Y) do //finish at knee as hips are above knee
                let pointsOnLine =  depthImage.[(y * 240)..((y * 240)+320)]
                let currentFoundWidth = measureSurfaceDistance pointsOnLine
                if currentFoundWidth > hipWidth then
                    hipWidth <- currentFoundWidth
                    h <- y
                y <- y + 1
            hips <-  float32 h

        member this.GetHipsOld =
            let kneeL = leftSideBody.GetJoint("leftKnee")
            let footL = leftSideBody.GetJoint("leftFoot")
            let depthImage = leftSideBody.DepthImg
            let mutable h = 0
            let mutable hipWidth = 0
            let mutable y = int (leftSideBody.GetJoint("centerHip").Y) //start at waist as hips are below waist
            while y < (int kneeL.Y) do //finish at knee as hips are below knee
                let mutable x = 0
                let mutable currentFoundWidth = 0
                while x < int footL.X do
                    let arrayPosition = (y * 320 + x)  
                    let depth = depthImage.[arrayPosition]
                    if depth > 0 then
                        currentFoundWidth <- currentFoundWidth + 1
                    x <- x + 1
                if currentFoundWidth > hipWidth then
                    hipWidth <- currentFoundWidth
                    h <- y
                y <- y + 1
            hips <-  float32 h

        //*******************************
        //Measurement members. Used to find points at which measurements should be taken
        //*******************************

        //Height measurement
        member this.MeasureHeightVis=
            height <- bottomOfFeet - topOfHead
            //height
        
        member this.MeasureHeightWorld=
            bottomOfFeet * 5.0f - topOfHead * 5.0f 
        
        //Ceiling to waist measurement
        member this.MeasureToWaistVis=
            let w = -(height / float32 phi) + topOfHead + height
            waist <- w

        member this.MeasureToWaistWorld=
            this.MeasureHeightWorld / float32 phi

        //top of screen to shoulders
        member this.MeasureToShoulders=
            shoulders <- frontBody.GetJoint("centerShoulder").Y
            //shoulders
        
        member this.MeasureToKnees=
            knees <- frontBody.GetJoint("leftKnee").Y

        member this.MeasureToHips=
            hips <- frontBody.GetJoint("centerHip").Y

        override this.Initialize()=
            spriteBatch <- new SpriteBatch(game.GraphicsDevice)
            pointOfInterestLine <- game.Content.Load<Texture2D>("whiteLine")

        override this.LoadContent()=
            pointOfInterestLine <- game.Content.Load<Texture2D>("whiteLine")

        override this.Update(gameTime)=
            if frontBody.CompleteBody && leftSideBody.CompleteBody && not pointsFound then
                this.GetTopOfHead
                this.GetBottomOfFeet
                this.MeasureHeightVis
                this.MeasureToWaistVis
                this.MeasureToShoulders
                this.MeasureToKnees
                this.GetHipsOld
                frontBodyView <- this.ConvertDepthToTexture frontBody
                sideBodyView <- this.ConvertDepthToTexture leftSideBody
                pointsFound <- true

        member this.ConvertDepthToTexture (b:Body)=
            let img = new Texture2D(game.GraphicsDevice, 320, 240)
            let DepthColor = Array.create (320 * 240) (new Color(255,255,255))

            let maxDist = 4000
            let minDist = 850
            let distOffset = maxDist - minDist

            for y = 0 to 239 do
                for x = 0 to 319 do
                    let n = (y * 320 + x)
                    let distance = b.DepthImg.[n]
                    //change distance to colour
                    let intensity = ((255 * Math.Max(int(distance-minDist),0)/distOffset)) //convert distance into a gray level value between 0 and 255 taking into account min and max distances of the kinect.
                    let colour = new Color(intensity, intensity, intensity)
                    DepthColor.[y * 320 + x] <- colour
            img.SetData(DepthColor)
            img


        override this.Draw(gameTime)=
            spriteBatch.Begin()
            //Draw the points of interest lines
            if frontBodyView <> null then
                spriteBatch.Draw(frontBodyView, new Vector2(320.0f, 0.0f), Color.White)//front view
            if sideBodyView <> null then
                spriteBatch.Draw(sideBodyView, new Vector2(640.0f, 0.0f), Color.White)//left side view
            spriteBatch.Draw(pointOfInterestLine, new Vector2(320.0f, waist), Color.White)//Waist
            spriteBatch.Draw(pointOfInterestLine, new Vector2(320.0f, topOfHead), Color.White)//Top of Head
            spriteBatch.Draw(pointOfInterestLine, new Vector2(320.0f, bottomOfFeet), Color.White)//Bottom of feet
            spriteBatch.Draw(pointOfInterestLine, new Vector2(320.0f, shoulders), Color.White)//Shoulders
            spriteBatch.Draw(pointOfInterestLine, new Vector2(320.0f, hips), Color.White)//Hips
            spriteBatch.Draw(pointOfInterestLine, new Vector2(320.0f, knees), Color.White)//Knees
            spriteBatch.End()
        
