namespace BodyData

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

    type BodyMeasurements(game, hea, shL, shR, shC, hiL, hiR, hiC, foL, foR)=
        inherit DrawableGameComponent(game)
        
        let phi = 1.61803399 //golden ratio
        
        //the depth image of the player.  it should only contain one player and each depth should be an int from  850 - 4000 (the valid depths for the depths)  
        let mutable depthImage:int[] = Array.create 76800 0
        
        //joints
        let mutable head = hea
        let mutable shoulderL = shL
        let mutable shoulderR = shR
        let mutable shoulderC = shC
        let mutable hipL = hiL
        let mutable hipR = hiR
        let mutable hipC = hiC
        let mutable footL = foL
        let mutable footR = foR
        let mutable kneeL = new Vector3(0.0f, 0.0f, 0.0f)

        //points of interest
        let mutable topOfHead = 0.0f
        let mutable bottomOfFeet = 0.0f
        let mutable waist = 0.0f
        let mutable height = 0.0f
        let mutable shoulders = 0.0f
        let mutable hips = 0.0f
        let mutable knees = 0.0f

        let mutable pointOfInterestLine:Texture2D = null
        
        let game = game
        let mutable spriteBatch = null

        new(game) = new BodyMeasurements(game, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f))
        
        member this.completeBody =  //complete body if has joints and depth data
            if Array.max(depthImage) > 0 then
                if not(head.Equals(new Vector3(0.0f,0.0f,0.0f))) then
                    true
                else
                    false
            else
                false

        
        //These members find the top and bottom most points of the depth image
        //The values they return are based on the 2D visualisation space i.e. in the range x=0-240, y=0-320
        member this.GetTopOfHead =
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
            let mutable BOF = Unchecked.defaultof<Vector3>
            let mutable y = 0
            while y < 239 && BOF.Equals(Unchecked.defaultof<Vector3>) do
                let mutable x = 0
                while x < 319 && BOF.Equals(Unchecked.defaultof<Vector3>) do
                    let arrayPosition = 76799 - (y * 320 + x)  
                    let depth = depthImage.[arrayPosition]
                    if depth > 0 then
                        let coordinates = new Vector3(320.0f - float32 x, 240.0f - float32 y, float32 depth)
                        let closeEnoughToHead = 
                            let euclidDist = Vector2.Distance(new Vector2(head.X, head.Y), new Vector2(coordinates.X, coordinates.Y))
                            if euclidDist < 100.0f then
                                true
                            else
                                false
                        //if closeEnoughToHead then
                        BOF <- coordinates
                        System.Diagnostics.Debug.WriteLine("BottomOfFeet=" + BOF.ToString())
                    x <- x + 1
                y <- y + 1
            bottomOfFeet <- BOF.Y
            //BOF

        

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
            shoulders <- shoulderC.Y
            //shoulders
        
        member this.MeasureToKnees=
            knees <- kneeL.Y

        member this.MeasureToHips=
            hips <- hipC.Y

        member this.SetDepthData dI =
            depthImage <- dI;

        member this.SetSkeleton (hea, shL, shR, shC, hiL, hiR, hiC, foL, foR, knL, knR) =
            head <- hea
            shoulderL <- shL
            shoulderR <- shR
            shoulderC <- shC
            hipL <- hiL
            hipR <- hiR
            hipC <- hiC
            footL <- foL
            footR <- foR
            kneeL <- knL

        override this.Initialize()=
            spriteBatch <- new SpriteBatch(game.GraphicsDevice)
            pointOfInterestLine <- game.Content.Load<Texture2D>("whiteLine")

        override this.LoadContent()=
            pointOfInterestLine <- game.Content.Load<Texture2D>("whiteLine")

        override this.Update(gameTime)=
            if this.completeBody then
                this.GetTopOfHead
                this.GetBottomOfFeet
                this.MeasureHeightVis
                this.MeasureToWaistVis
                this.MeasureToShoulders
                this.MeasureToKnees
                this.MeasureToHips


        override this.Draw(gameTime)=
            spriteBatch.Begin()
            //Draw the points of interest lines
            spriteBatch.Draw(pointOfInterestLine, new Vector2(0.0f, waist * 2.0f), Color.White)//Waist
            spriteBatch.Draw(pointOfInterestLine, new Vector2(0.0f, topOfHead * 2.0f), Color.White)//Top of Head
            spriteBatch.Draw(pointOfInterestLine, new Vector2(0.0f, bottomOfFeet * 2.0f), Color.White)//Bottom of feet
            spriteBatch.Draw(pointOfInterestLine, new Vector2(0.0f, shoulders * 2.0f), Color.White)//Shoulders
            //spriteBatch.Draw(pointOfInterestLine, new Vector2(0.0f, hips * 2.0f), Color.White)//Hips
            spriteBatch.Draw(pointOfInterestLine, new Vector2(0.0f, knees * 2.0f), Color.White)//Knees
            spriteBatch.End()
        
