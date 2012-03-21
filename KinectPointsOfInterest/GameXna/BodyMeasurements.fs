namespace BodyData

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

open System
open System.IO
    
    //*************************************************************
    // Methods to measure the body from 3 views, front side and back
    //
    // parameters:
    //      game:Game - the Game object that is using this class
    //      frontBody:Body - the front body view with joints and depth data
    //      leftSideBody:Body - the body view of the user's left side with joints and depth data
    //      backBody:Body - the back body view with joints and depth data
    //*************************************************************
    type BodyMeasurements(game, kinect:KinectPointsOfInterest.Kinect.KinectMeasure, frontBodys:Body[], leftSideBodys:Body[], backBodys:Body[])=
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

        //measurements
        let mutable waistMeasurement = 0.0
        
        let smooth (avg:int[])=
            for y = 0 to 238 do
                let row = avg.[y*320..y*320+320]    
                for e=1 to row.Length-2 do
                    let range = Math.Sqrt(Math.Pow(float(row.[e-1] - row.[e+1]), 2.0))
                    match row.[e] with
                    | x when x = 0 -> ()
                    | x when row.[e-1] =0 -> ()
                    | x when row.[e+1] =0 -> ()
                    | x when x > row.[e-1] + int range -> (row.[e] <- row.[e-1] + (int range)/2)
                    | x when x < row.[e-1] - int range -> (row.[e] <- row.[e-1] - (int range)/2)
                    | _ -> () //if the point is in the correct range then do nothing to it.
                avg.[y*320..y*320+320] <- row
            avg

        let movingAvg (avg:int[]) n=
            for y=0 to 238 do
                let row = avg.[y*320..(y*320)+319]
                for i = n to (row.Length - n) do
                    if row.[i] <> 0 && row.[i-1] <> 0 && row.[i+1] <> 0 then
                        for d = 1 to n do 
                            row.[i] <- row.[i] + row.[i-d] + row.[i+d]         
                        row.[i] <- int ((float row.[i]) / float (n * 2 + 1))
                avg.[y*320..y*320+319] <- row
            avg

        //std devaition per row.
        let stdDeviation (avg:int[]) =
            for i = 0 to 239 do
                let mutable row = avg.[i*320..(i*320)+319]
                let rowWithout0s = row |> Array.filter (fun elem -> if elem = 0 then false else true) //filter out the 0s
                let mutable averageRawDepth = 0.0
                if rowWithout0s.Length>0 then 
                    averageRawDepth <- rowWithout0s |> Array.averageBy(fun a-> float a) 
                //gets the average of the depths in the array
                
                let rangeRawDepth = Array.max row - if rowWithout0s.Length>0 then Array.min(rowWithout0s) else 0
                let stdDeviationRange = float rangeRawDepth * 0.4 // 60% of the range
                let stdDeviationMax = averageRawDepth + (stdDeviationRange / 2.0)
                let stdDeviationMin = averageRawDepth - (stdDeviationRange / 2.0)
                row <- row |> Array.map(fun a -> (if a > int stdDeviationMin && a < int stdDeviationMax then a else 0)) //sould average and threshhold values.
                avg.[i*320..(i*320)+319] <- row
            avg
            

        let removeSinglePointOutliers (avg:int[])=
            for y=0 to 238 do
                let row = avg.[y*320..(y*320)+319]
                for i = 1 to (row.Length - 2) do
                    if row.[i-1] = 0 && row.[i+1] =0 then
                      row.[i] <- 0         
                avg.[y*320..y*320+319] <- row
            avg


        //averages the body from a set of body objects
        let avgBody (bodies:Body[]) =
            let mutable avg = Array.zeroCreate (320*240)
            for b in bodies do
                for y = 0 to 239 do
                    for x = 0 to 319 do
                         let n = y * 320 + x
                         avg.[n] <- avg.[n] + b.DepthImg.[n]
            avg <- avg |> Array.map(fun a -> (a / bodies.Length)) //average each pixel by dividing by the number of samples taken
            avg <- removeSinglePointOutliers avg
            avg <- stdDeviation avg
            
            //avg <- movingAvg avg 1
            
            //avg <- smooth avg

            let mAvg = Array.max avg
            let theAvgBody = new Body()
            theAvgBody.DepthImg <- avg
            theAvgBody.SetSkeleton(bodies.[0].GetSkeleton)
            theAvgBody

            
        
        //diagnostics
        let mutable frontMeasurement = 0.0
        let mutable flatFront = 0
        let mutable waistMax = 0.0
        let mutable waistMin = Double.MaxValue
        let waistContour:int[] = Array.zeroCreate 320
        let fn:string = "frontWaist.cvs"
        let strm = new StreamWriter( fn,  false)
        let mutable measurementCount = 0
       
        

        let mutable pointOfInterestLine:Texture2D = null
        let mutable frontBodyView:Texture2D = null
        let mutable sideBodyView:Texture2D = null
        let mutable dot:Texture2D = null
        let mutable measurementFont:SpriteFont =null
        
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
                    if diagonalWH <= 30.0 then
                        measurement <- measurement + diagonalWH
                    else //outlier
                        measurement <- measurement + 30.0
                    lastPixelDepth <- points.[i]
                i<-i+1
            measurement

        //These members find the top and bottom most points of the depth image
        //The values they return are based on the 2D visualisation space i.e. in the range x=0-240, y=0-320
        member this.GetTopOfHead =
            let frontBody = avgBody frontBodys
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
            let frontBody = avgBody frontBodys
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

        member this.GetHipsOld =
            let backBody = avgBody backBodys
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

        member this.GetHips =
            let leftSideBody = avgBody leftSideBodys
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

        member this.GetChest =
            let frontBody = avgBody frontBodys
            let shoulderC = frontBody.GetJoint("centerShoulder").Y
            let depthImage = frontBody.DepthImg
            let mutable lastWidth = 0
            let mutable y = (int shoulderC)
            while y <  240 do //finish at knee as hips are below knee
                    let mutable x = 0
                    let mutable currentFoundWidth = 0
                    while x < 320 do
                        let arrayPosition = (y * 320 + x)  
                        let depth = depthImage.[arrayPosition]
                        if depth > 0 then
                            currentFoundWidth <- currentFoundWidth + 1
                        x <- x + 1
                    //if currentFoundWidth > hipWidth then
                        //hipWidth <- currentFoundWidth
                        //h <- y
                    y <- y + 1
                ///hips <-  float32 h

        member this.GetWaist=
            let w = -((bottomOfFeet - topOfHead) / float32 phi) + topOfHead + (bottomOfFeet - topOfHead)
            waist <- w

        member this.GetShoulders=
            shoulders <- (avgBody frontBodys).GetJoint("centerShoulder").Y



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
        

        member this.MeasureWaist=
            let frontBody = avgBody frontBodys
            let backBody = avgBody backBodys
            let waistStart = int waist * 320
            let waistEnd = waistStart + 320
            let frontRow = frontBody.DepthImg.[waistStart..waistEnd]
            let backRow = backBody.DepthImg.[waistStart..waistEnd]
            waistMeasurement <- measureSurfaceDistance frontRow
            waistMeasurement <- waistMeasurement + (measureSurfaceDistance backRow)

        //top of screen to shoulders
        member this.MeasureToShoulders=
            shoulders <- frontBodys.[0].GetJoint("centerShoulder").Y
            //shoulders
        
        member this.MeasureToKnees=
            knees <- frontBodys.[0].GetJoint("leftKnee").Y

        member this.MeasureToHips=
            hips <- frontBodys.[0].GetJoint("centerHip").Y

        override this.Initialize()=
            spriteBatch <- new SpriteBatch(game.GraphicsDevice)
            pointOfInterestLine <- game.Content.Load<Texture2D>("whiteLine")
            dot <- game.Content.Load<Texture2D>("dot")
            measurementFont <- game.Content.Load<SpriteFont>("Font")


        
        override this.Update(gameTime)=
            if not pointsFound then
                this.GetTopOfHead
                this.GetBottomOfFeet
                this.GetWaist
                this.MeasureToShoulders
                this.MeasureToKnees
                this.GetHips
                frontBodyView <- this.ConvertDepthToTexture (avgBody frontBodys)
                sideBodyView <- this.ConvertDepthToTexture (avgBody leftSideBodys)
                this.MeasureWaist
                pointsFound <- true
                let waistRow = (avgBody frontBodys).DepthImg.[(int waist * 320)..((int waist * 320)+320)] //kinect.LiveDepthData.[(int waist * 320)..((int waist * 320)+320)]
                frontMeasurement <- measureSurfaceDistance waistRow
            if waist > 0.0f && pointsFound then
                let waistRow = kinect.LiveDepthData.[(int waist * 320)..((int waist * 320)+320)]
//                let flatFrontArray = Array.map (fun a ->
//                                                match a with
//                                                | 0 -> None
//                                                | _ -> Some a) waistRow
//                
//                flatFront <- flatFrontArray.Length
                let frontMeasurement2 = measureSurfaceDistance waistRow
                //try
                  //  strm.Write (frontMeasurement.ToString() + "\r\n")
                //with 
                //    | :? System.ObjectDisposedException -> System.Diagnostics.Debug.Write("finished")
                //measurementCount <- measurementCount + 1
                
                //if measurementCount = 1000 then
                //    strm.Close()

                if frontMeasurement2 > waistMax then
                    waistMax <- frontMeasurement
                if frontMeasurement2 < waistMin then
                    waistMin <- frontMeasurement
                
                if Array.sum waistRow > 0 then
                    let range = Array.max waistRow - Array.min(Array.filter (fun elem -> if elem = 0 then false else true) waistRow)
                    for i = 0 to 319 do  
                        waistContour.[i] <- waistRow.[i] - (Array.max waistRow - range)

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

            spriteBatch.DrawString(measurementFont, "Waist:"+waistMeasurement.ToString(), new Vector2(0.0f, 320.0f), Color.White);
            spriteBatch.DrawString(measurementFont, "Front@Waist:"+frontMeasurement.ToString(), new Vector2(0.0f, 340.0f), Color.White);
            spriteBatch.DrawString(measurementFont, "Front@Waist(Flat):"+flatFront.ToString(), new Vector2(0.0f, 360.0f), Color.White);
            spriteBatch.DrawString(measurementFont, "MaxFront@Waist:"+waistMax.ToString(), new Vector2(0.0f, 380.0f), Color.White);
            spriteBatch.DrawString(measurementFont, "MinFront@Waist:"+waistMin.ToString(), new Vector2(0.0f, 400.0f), Color.White);

            let visOffset = new Vector2(400.0f, 240.0f)
            for i = 0 to 319 do
                let point = waistContour.[i]
                if point > 0 then
                     spriteBatch.Draw(dot, visOffset+ new Vector2(float32 i, float32 point), Color.White)//Knees

            spriteBatch.End()
        
     //*************************************************************
    // Methods to measure the body from 3 views, front side and back
    //
    // parameters:
    //      game:Game - the Game object that is using this class
    //      frontBody:Body - the front body view with joints and depth data
    //      leftSideBody:Body - the body view of the user's left side with joints and depth data
    //      backBody:Body - the back body view with joints and depth data
    //*************************************************************
    type BodyMeasurementsPostProcess(game:Game, kinect:KinectPointsOfInterest.Kinect.KinectMeasure, frontBodys:Body[], leftSideBodys:Body[], backBodys:Body[])=
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

        //measurements
        let mutable waistMeasurement = 0.0
        
        let smooth (avg:int[])=
            for y = 0 to 238 do
                let row = avg.[y*320..y*320+320]    
                for e=1 to row.Length-2 do
                    let range = Math.Sqrt(Math.Pow(float(row.[e-1] - row.[e+1]), 2.0))
                    match row.[e] with
                    | x when x = 0 -> ()
                    | x when row.[e-1] =0 -> ()
                    | x when row.[e+1] =0 -> ()
                    | x when x > row.[e-1] + int range -> (row.[e] <- row.[e-1] + (int range)/2)
                    | x when x < row.[e-1] - int range -> (row.[e] <- row.[e-1] - (int range)/2)
                    | _ -> () //if the point is in the correct range then do nothing to it.
                avg.[y*320..y*320+320] <- row
            avg

        let movingAvg (avg:int[]) n=
            for y=0 to 238 do
                let row = avg.[y*320..(y*320)+319]
                for i = n to (row.Length - n) do
                    if row.[i] <> 0 && row.[i-1] <> 0 && row.[i+1] <> 0 then
                        for d = 1 to n do 
                            row.[i] <- row.[i] + row.[i-d] + row.[i+d]         
                        row.[i] <- int ((float row.[i]) / float (n * 2 + 1))
                avg.[y*320..y*320+319] <- row
            avg

        //std devaition per row.
        let stdDeviation (avg:int[]) =
            for i = 0 to 239 do
                let mutable row = avg.[i*320..(i*320)+319]
                let rowWithout0s = row |> Array.filter (fun elem -> if elem = 0 then false else true) //filter out the 0s
                let mutable averageRawDepth = 0.0
                if rowWithout0s.Length>0 then 
                    averageRawDepth <- rowWithout0s |> Array.averageBy(fun a-> float a) 
                //gets the average of the depths in the array
                
                let rangeRawDepth = Array.max row - if rowWithout0s.Length>0 then Array.min(rowWithout0s) else 0
                let stdDeviationRange = float rangeRawDepth * 0.4 // 60% of the range
                let stdDeviationMax = averageRawDepth + (stdDeviationRange / 2.0)
                let stdDeviationMin = averageRawDepth - (stdDeviationRange / 2.0)
                row <- row |> Array.map(fun a -> (if a > int stdDeviationMin && a < int stdDeviationMax then a else 0)) //sould average and threshhold values.
                avg.[i*320..(i*320)+319] <- row
            avg
            

        let removeSinglePointOutliers (avg:int[])=
            for y=0 to 238 do
                let row = avg.[y*320..(y*320)+319]
                for i = 1 to (row.Length - 2) do
                    if row.[i-1] = 0 && row.[i+1] =0 then
                      row.[i] <- 0         
                avg.[y*320..y*320+319] <- row
            avg


        //averages the body from a set of body objects
        let avgBody (bodies:Body[]) =
            let mutable avg = Array.zeroCreate (320*240)
            for b in bodies do
                for y = 0 to 239 do
                    for x = 0 to 319 do
                         let n = y * 320 + x
                         avg.[n] <- avg.[n] + b.DepthImg.[n]
            avg <- avg |> Array.map(fun a -> (a / bodies.Length)) //average each pixel by dividing by the number of samples taken
            avg <- removeSinglePointOutliers avg
            avg <- stdDeviation avg
            
            //avg <- movingAvg avg 1
            
            //avg <- smooth avg

            let mAvg = Array.max avg
            let theAvgBody = new Body()
            theAvgBody.DepthImg <- avg
            theAvgBody.SetSkeleton(bodies.[0].GetSkeleton)
            theAvgBody
        
        let freqCount (measurements:float[])=
            let range = int (Array.max(measurements) - Array.min(measurements))
            let min = int (Array.min(measurements))
            let bins = 10
            let nBins = range / 10
            let count = Array.zeroCreate nBins
            let binLables = Array.zeroCreate nBins
            for i = 0 to measurements.Length-1 do
                let m = int measurements.[i]
                for j = 1 to nBins do
                    if m < (min + j*bins) && int measurements.[i] >= (min + (j-1)*bins) then
                        count.[j-1] <- count.[j-1] + 1
            for i=1 to nBins do
                binLables.[i-1] <- ((min+(i-1)*bins) + (min + i*bins))/2
            binLables.[Array.max count]
            
        
        //diagnostics
        let mutable frontMeasurement = 0.0
        let mutable flatFront = 0
        let mutable waistMax = 0.0
        let mutable waistMin = Double.MaxValue
        let waistContour:int[] = Array.zeroCreate 320
        let fn:string = "frontWaist.cvs"
        let strm = new StreamWriter( fn,  false)
        let mutable measurementCount = 0
       
        
        //Texture Assets
        let mutable pointOfInterestLine:Texture2D = null
        let mutable frontBodyView:Texture2D = null
        let mutable sideBodyView:Texture2D = null
        let mutable dot:Texture2D = null
        let mutable measurementFont:SpriteFont =null
        
        let game = game
        let mutable spriteBatch = null

        //pixel resolution formula, obtained empirically
        let horizontalPixelResolution depth =
            374.0 / 80096.0 * Math.Pow(depth, -1.0)
        
        let measureSurfaceDistance (points:int[]) =
            
            let mutable measurement = 0.0
            //let mutable pixelWidth=0.0
            let mutable lastPixelDepth =0
            let mutable i=0
            while lastPixelDepth = 0 && i<points.Length-1 do
                lastPixelDepth <- points.[i]
                i<-i+1
            while i < points.Length-1 do
                let mutable leftPixelWidths = 0.0
                let pixelWidth = 374.0 / (80096.0 * Math.Pow(float(points.[i]), -0.953)) + leftPixelWidths
                if points.[i] >0 then
                    let currentPixelDepthChange = Math.Sqrt(Math.Pow(float(points.[i] - lastPixelDepth),2.0))
                    //By pythagoras
                    let diagonalWH = Math.Sqrt(Math.Pow(currentPixelDepthChange,2.0) + Math.Pow(pixelWidth, 2.0))
                    if diagonalWH <= 30.0 then
                        measurement <- measurement + diagonalWH
                        lastPixelDepth <- points.[i]
                        leftPixelWidths <- 0.0
                    else //outlier
                        leftPixelWidths <- leftPixelWidths+pixelWidth
                    
                i<-i+1
            measurement
        
        //These members find the top and bottom most points of the depth image
        //The values they return are based on the 2D visualisation space i.e. in the range x=0-240, y=0-320
        member this.GetTopOfHead (body:Body) =
            let frontBody = body
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

        member this.GetBottomOfFeet (body:Body)=
            let frontBody = body
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

        member this.GetHipsOld =
            let backBody = avgBody backBodys
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

        member this.GetHips (body:Body)=
            let leftSideBody = body
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

        member this.GetChest (body:Body)=
            let frontBody = body
            let shoulderC = frontBody.GetJoint("centerShoulder").Y
            let depthImage = frontBody.DepthImg
            let mutable lastWidth = 0
            let mutable y = (int shoulderC)
            while y <  240 do //finish at knee as hips are below knee
                    let mutable x = 0
                    let mutable currentFoundWidth = 0
                    while x < 320 do
                        let arrayPosition = (y * 320 + x)  
                        let depth = depthImage.[arrayPosition]
                        if depth > 0 then
                            currentFoundWidth <- currentFoundWidth + 1
                        x <- x + 1
                    //if currentFoundWidth > hipWidth then
                        //hipWidth <- currentFoundWidth
                        //h <- y
                    y <- y + 1
                ///hips <-  float32 h

        member this.GetWaist=
            let w = -((bottomOfFeet - topOfHead) / float32 phi) + topOfHead + (bottomOfFeet - topOfHead)
            waist <- w

        member this.GetShoulders=
            shoulders <- (avgBody frontBodys).GetJoint("centerShoulder").Y



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
        

        member this.MeasureWaist (body:Body, backBody:Body)=
            let mutable waistMeasurement =0.0
            let frontBody = body
            let backBody = backBody
            let waistStart = int waist * 320
            let waistEnd = waistStart + 320
            let frontRow = frontBody.DepthImg.[waistStart..waistEnd]
            let backRow = backBody.DepthImg.[waistStart..waistEnd]
            waistMeasurement <- measureSurfaceDistance frontRow
            waistMeasurement <- waistMeasurement + (measureSurfaceDistance backRow)
            waistMeasurement

        member this.MeasureHips (body:Body, backBody:Body)=
            let mutable hipsMeasurement =0.0
            let frontBody = body
            let backBody = backBody
            let hipsStart = int hips * 320
            let hipsEnd = hipsStart + 320
            let frontRow = frontBody.DepthImg.[hipsStart..hipsEnd]
            let backRow = backBody.DepthImg.[hipsStart..hipsEnd]
            hipsMeasurement <- measureSurfaceDistance frontRow
            hipsMeasurement <- hipsMeasurement + (measureSurfaceDistance backRow)
            hipsMeasurement

        //top of screen to shoulders
        member this.MeasureToShoulders=
            shoulders <- frontBodys.[0].GetJoint("centerShoulder").Y
            //shoulders
        
        member this.MeasureToKnees=
            knees <- frontBodys.[0].GetJoint("leftKnee").Y

        member this.MeasureToHips=
            hips <- frontBodys.[0].GetJoint("centerHip").Y

        override this.Initialize()=
            spriteBatch <- new SpriteBatch(game.GraphicsDevice)
            pointOfInterestLine <- game.Content.Load<Texture2D>("whiteLine")
            dot <- game.Content.Load<Texture2D>("dot")
            measurementFont <- game.Content.Load<SpriteFont>("Font")


        member this.GetMeasurements =
            let waistMeasures = Array.zeroCreate frontBodys.Length
            let hipsMeasures = Array.zeroCreate frontBodys.Length
            for i = 0 to frontBodys.Length-1 do
                    this.GetTopOfHead frontBodys.[i] 
                    this.GetBottomOfFeet frontBodys.[i] 
                    this.GetWaist
                    this.MeasureToShoulders
                    this.MeasureToKnees
                    this.GetHips leftSideBodys.[0]
                    frontBodyView <- this.ConvertDepthToTexture (frontBodys.[0])
                    sideBodyView <- this.ConvertDepthToTexture (leftSideBodys.[0])
                    
                    waistMeasures.[i] <- this.MeasureWaist (frontBodys.[i], backBodys.[i])
                    hipsMeasures.[i] <- this.MeasureHips (frontBodys.[i], backBodys.[i])
                    //let waistRow = (avgBody frontBodys).DepthImg.[(int waist * 320)..((int waist * 320)+320)] //kinect.LiveDepthData.[(int waist * 320)..((int waist * 320)+320)]
                    //frontMeasurement <- measureSurfaceDistance frontBodys.[i]
            (freqCount waistMeasures, freqCount hipsMeasures)
            //(waistMeasures, hipsMeasures)

        //member this.GetFinalMeasurements =
            
        //override this.Update(gameTime)=
//            let mutable waistMeasures = [||]
//            let mutable hipsMeasures = [||]
//            if not pointsFound then
//                let allMeasures = this.GetMeasurements
//                waistMeasures <- fst allMeasures
//                hipsMeasures <- snd allMeasures
//                pointsFound <- true
//            if waist > 0.0f && pointsFound then
                //let waistRow = kinect.LiveDepthData.[(int waist * 320)..((int waist * 320)+320)]
                //try
                  //  strm.Write (frontMeasurement.ToString() + "\r\n")
                //with 
                //    | :? System.ObjectDisposedException -> System.Diagnostics.Debug.Write("finished")
                //measurementCount <- measurementCount + 1
                
                //if measurementCount = 1000 then
                //    strm.Close()
               
//                waistMax <- Array.max waistMeasures
//                waistMin <- Array.min waistMeasures
//                freqCount waistMeasures
//                if Array.sum waistRow > 0 then
//                    let range = Array.max waistRow - Array.min(Array.filter (fun elem -> if elem = 0 then false else true) waistRow)
//                    for i = 0 to 319 do  
//                        waistContour.[i] <- waistRow.[i] - (Array.max waistRow - range)

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

            spriteBatch.DrawString(measurementFont, "Waist:"+waistMeasurement.ToString(), new Vector2(0.0f, 320.0f), Color.White);
            spriteBatch.DrawString(measurementFont, "Front@Waist:"+frontMeasurement.ToString(), new Vector2(0.0f, 340.0f), Color.White);
            spriteBatch.DrawString(measurementFont, "Front@Waist(Flat):"+flatFront.ToString(), new Vector2(0.0f, 360.0f), Color.White);
            spriteBatch.DrawString(measurementFont, "Max@Waist:"+waistMax.ToString(), new Vector2(0.0f, 380.0f), Color.White);
            spriteBatch.DrawString(measurementFont, "Min@Waist:"+waistMin.ToString(), new Vector2(0.0f, 400.0f), Color.White);

            let visOffset = new Vector2(400.0f, 240.0f)
            for i = 0 to 319 do
                let point = waistContour.[i]
                if point > 0 then
                     spriteBatch.Draw(dot, visOffset+ new Vector2(float32 i, float32 point), Color.White)//Knees

            spriteBatch.End()