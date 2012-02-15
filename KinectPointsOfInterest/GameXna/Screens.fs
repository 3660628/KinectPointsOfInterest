namespace KinectPointsOfInterest

    open Microsoft.Xna.Framework
    open Microsoft.Xna.Framework.Input
    open Microsoft.Xna.Framework.Graphics
    open Microsoft.Xna.Framework.Audio

    open BodyData
    open MenuItems
    open VisualisationAssets

    module Screens=

        type Menu(game:Game)=
            inherit DrawableGameComponent(game)

            let mutable spriteBatch = null
            let mutable cursor = new Cursor(game, new Vector2(0.0f,0.0f))

            override this.Initialize()=
                this.Game.Components.Add(cursor)
                cursor.DrawOrder = 99 |> ignore
                base.Initialize()

            override this.LoadContent()=
                spriteBatch <- new SpriteBatch(this.Game.GraphicsDevice)
                base.LoadContent()

            override this.Update(gameTime)=
                base.Update(gameTime)

            override this.Draw(gameTime)=
                base.Draw(gameTime)

            member this.InBounds (pos:Vector2, button:Button) =
                if pos.X < (float32 button.GetTextureBounds.Right + button.Position.X) && pos.X > (float32 button.GetTextureBounds.Left + button.Position.X) then
                    if pos.Y < (float32 button.GetTextureBounds.Bottom + button.Position.Y) && pos.Y > (float32 button.GetTextureBounds.Top + button.Position.Y) then
                        true
                    else
                        false
                else
                    false

            abstract member DestroyScene: unit -> unit
            default this.DestroyScene() = 
                this.Game.Components.Remove(cursor) |> ignore

        and MeasurementScreen(game:Game, sex, e:Event<ChangeScreenEventArgs>)=
            inherit Menu(game)

            let event = e

            let mutable sprite : Texture2D = null

            let kinect = new Kinect(game)
    
            let noOfSamples = 200
            let mutable timer = 10000.0f //start timer at 10 seconds
            let mutable finished = false //has the data been captured
            let mutable frontBody:Body[] = Array.zeroCreate noOfSamples //front data
            let mutable backBody:Body[] = Array.zeroCreate noOfSamples //back data
            let mutable sideBody:Body[] = Array.zeroCreate noOfSamples //side data

            let mutable leftClick = true

            //depth image map
            let mutable depthImage:Texture2D = null
            let mutable clickSound:SoundEffect = null
            let mutable beepSound:SoundEffect = null

            let mutable nextButton = new TextButton(game, "nextButton", "no_shadow", "Next", new Vector2( 300.0f, 300.0f))

            member this.ClickOption(mouseClickPos:Vector2)=
                match mouseClickPos with
                    | x when base.InBounds(x, nextButton) -> event.Trigger(new ChangeScreenEventArgs(this, new VisualisationScreen(this.Game, sex, 0, 0, 0, 0, event))) //clicked on next button
                    | _ -> ()// clicked elsewhere, do nothing

            override this.Initialize()=
                game.Components.Add(kinect)
                game.Components.Add(nextButton)

            override this.LoadContent() =
                sprite <- this.Game.Content.Load<Texture2D>("Sprite")
                clickSound <- this.Game.Content.Load<SoundEffect>("click_1")
                beepSound <- this.Game.Content.Load<SoundEffect>("BEEP1A")
                base.LoadContent()

            override this.Update gameTime = 
                
                if Mouse.GetState().LeftButton = ButtonState.Pressed && not leftClick then
                    leftClick <- true
                    this.ClickOption (new Vector2(float32(Mouse.GetState().X), float32( Mouse.GetState().Y)))
                if Mouse.GetState().LeftButton = ButtonState.Released then
                    leftClick <- false

                //KINECT TAKING THE MEASUREMENTS
                timer <- timer - float32 gameTime.ElapsedGameTime.TotalMilliseconds
                
                if timer <= -10000.0f && backBody.[0] = null then
                    clickSound.Play() |> ignore
                    for i = 0 to noOfSamples-1 do
                        backBody.[i] <- kinect.CaptureBody
                    beepSound.Play() |> ignore
                else if timer <= -5000.0f && sideBody.[0] = null then
                    clickSound.Play() |> ignore
                    for i = 0 to noOfSamples-1 do
                        sideBody.[i] <- kinect.CaptureBody
                    beepSound.Play() |> ignore
                else if timer <= 0.0f && frontBody.[0] = null then
                    clickSound.Play() |> ignore
                    for i = 0 to noOfSamples-1 do
                        frontBody.[i] <- kinect.CaptureBody
                    beepSound.Play() |> ignore
                    
                if frontBody.[noOfSamples-1] <> null && backBody.[noOfSamples-1] <> null && sideBody.[noOfSamples-1] <> null && not finished then
                    //game.Components.Add(new BodyMeasurements(this, kinect, frontBody, sideBody, backBody))
                    this.Game.Components.Add(new BodyMeasurementsPostProcess(this.Game, kinect, frontBody, sideBody, backBody))
                    finished <- true
                base.Update gameTime

            override this.DestroyScene()=
                this.Game.Components.Remove(kinect) |> ignore
                this.Game.Components.Remove(nextButton) |> ignore
                base.DestroyScene()

        and VisualisationScreen(game:Game, sex, height, chest, waist, hips, e:Event<ChangeScreenEventArgs>)=
            inherit Menu(game)

            let event = e

            let mutable nextButton = new TextButton(game, "nextButton", "no_shadow", "Next", new Vector2( 300.0f, 500.0f))

            let model = new PersonModel(game, sex, height, chest, waist, hips)
            
            let mutable leftClick = true //so click form last screen is not read through

            override this.Initialize()=
                this.Game.Components.Add(model)
                //this.Game.Components.Add(nextButton)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.Update(gameTime)=
                if Mouse.GetState().LeftButton = ButtonState.Pressed && not leftClick then
                    leftClick <- true
                    this.ClickOption (new Vector2(float32(Mouse.GetState().X), float32( Mouse.GetState().Y)))
                if Mouse.GetState().LeftButton = ButtonState.Released then
                    leftClick <- false
                base.Update(gameTime)

            member this.ClickOption(mouseClickPos:Vector2)=
                match mouseClickPos with
                    | x when base.InBounds(x, nextButton) -> event.Trigger(new ChangeScreenEventArgs(this, new GenderSelectScreen(this.Game, event))) //clicked on measure button
                    | _ -> ()// clicked elsewhere, do nothing

            override this.DestroyScene()=
                this.Game.Components.Remove(model) |> ignore
                //this.Game.Components.Remove(nextButton) |> ignore
                base.DestroyScene()

        and MainMenu(game:Game, e:Event<ChangeScreenEventArgs>) as this=
            inherit Menu(game)

            let mutable measureButton = new Button(game, "measureButton", "no_shadow", new Vector2(0.0f,0.0f))
            let mutable shopButton = new Button(game, "shopButton", "no_shadow", new Vector2(400.0f,0.0f))
            let event = e
            
            let measureButtonHandler args= event.Trigger(new ChangeScreenEventArgs(this, new GenderSelectScreen(this.Game, event)))

            let shopButtonHandler args= event.Trigger(new ChangeScreenEventArgs(this, new StoreScreen(this.Game, event)))
            
            override this.Initialize()=
                measureButton.Click.Add(fun args -> measureButtonHandler args)
                shopButton.Click.Add(fun args -> shopButtonHandler args)
                this.Game.Components.Add(measureButton)
                this.Game.Components.Add(shopButton)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.DestroyScene()=
                this.Game.Components.Remove(measureButton) |> ignore
                this.Game.Components.Remove(shopButton) |> ignore
                base.DestroyScene()

        and LoginScreen(game:Game, e:Event<ChangeScreenEventArgs>) as this=
            inherit Menu(game)
            let recentUsers = []
            let printElements =
                try
                    let prevUsers = System.Xml.Linq.XElement.Load("recentUsers.xml")
                    for (f:System.Xml.Linq.XElement) in prevUsers.Elements() do
                        let userName = f.Element(System.Xml.Linq.XName.Get("name"))
                        let userId = f.Element(System.Xml.Linq.XName.Get("id"))
                        let userEmail = f.Element(System.Xml.Linq.XName.Get("email"))
                        game.Components.Add(new User(game, (int) userId.Value, userName.Value, userEmail.Value, new Vector2(100.0f,((float32)userId.Value * 110.0f))))
                with
                    | :? System.IO.FileNotFoundException as ex -> ()
            let event = e
            let deselectTextBoxEvent = new Event<_>()

            let mutable username = new TextBox(game, "textbox", "textbox_shadow", new Vector2(0.0f,100.0f), deselectTextBoxEvent)
            let mutable password = new TextBox(game, "textbox", "textbox_shadow", new Vector2(0.0f,250.0f), deselectTextBoxEvent)

            let mutable nextButton = new TextButton(game, "nextButton", "no_shadow", "Login", new Vector2( 400.0f, 300.0f))

            
            
            //Event Handlers
            let deselectTextBoxHandler args=
                username.Deselect
                password.Deselect
            let LoginHandler args= 
                event.Trigger(new ChangeScreenEventArgs(this, new MainMenu(this.Game, event)))

            [<CLIEvent>]
            member this.DeselectTextBoxes = deselectTextBoxEvent.Publish

            override this.Initialize()=
                this.DeselectTextBoxes.Add(fun args -> deselectTextBoxHandler args)
                nextButton.Click.Add(fun args-> LoginHandler args)
                //this.Game.Components.Add(username)
                //this.Game.Components.Add(password)
                this.Game.Components.Add(nextButton)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.DestroyScene() =
                this.Game.Components.Remove(username) |> ignore
                this.Game.Components.Remove(password) |> ignore
                this.Game.Components.Remove(nextButton) |> ignore
                base.DestroyScene()

        and GenderSelectScreen(game:Game, e:Event<ChangeScreenEventArgs>) as this=
            inherit Menu(game)

            let maleButton = new Button(game, "maleButton", "no_shadow", new Vector2(0.0f,0.0f))
            let femaleButton = new Button(game, "femaleButton", "no_shadow", new Vector2(400.0f,0.0f))
            let event = e

            let genderSelectedHandler (args:ButtonClickedEventArgs)= 
                match args.Sender with
                    | x when x = maleButton -> event.Trigger(new ChangeScreenEventArgs(this, new MeasurementScreen(this.Game, "male", event)))
                    | x when x = femaleButton -> event.Trigger(new ChangeScreenEventArgs(this, new MeasurementScreen(this.Game, "female", event)))
                    | _ -> () //should never happen as there is no other buttons

            override this.Initialize()=
                maleButton.Click.Add(fun args -> genderSelectedHandler args)
                femaleButton.Click.Add(fun args -> genderSelectedHandler args)
                this.Game.Components.Add(maleButton)
                this.Game.Components.Add(femaleButton)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.DestroyScene()=
                this.Game.Components.Remove(maleButton) |> ignore
                this.Game.Components.Remove(femaleButton) |> ignore
                base.DestroyScene()

        and StoreScreen(game:Game, e:Event<ChangeScreenEventArgs>) as this=
            inherit Menu(game)

            let dbAccess = new Database.DatabaseAccess()

            let mutable garmentItems = new GarmentList(game)
            let mutable femaleButton = null
            let event = e

            let garmentClickHandler (args:GarmentItemClickedEventArgs)= 
                e.Trigger(new ChangeScreenEventArgs(this, new GarmentScreen(this.Game, args.Garment, event, this)))

            override this.Initialize()=
                garmentItems <- new GarmentList(game)
                dbAccess.getGarments null
                |> Seq.distinctBy (fun (x:Store.Garment)-> x.Name) //remove duplicate items (e.g. size variations)
                |> Seq.iter (fun (x:Store.Garment) -> (let newGarment = new GarmentItem(game, x, new Vector2(100.0f,((float32)x.ID * 110.0f))) //make a new garment Object
                                                       newGarment.Click.Add(fun x -> garmentClickHandler x) //add it's click handler
                                                       garmentItems.Add(newGarment))) //add it to the garment items list
                this.Game.Components.Add(garmentItems)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.DestroyScene()=
                this.Game.Components.Remove(garmentItems) |> ignore
                base.DestroyScene()

        and GarmentScreen(game:Game, garment:Store.Garment, e:Event<ChangeScreenEventArgs>, prevScreen) as this=
            inherit Menu(game)

            let event = e
            let center = new Vector2(float32(game.GraphicsDevice.Viewport.Width / 2), float32(game.GraphicsDevice.Viewport.Height / 2))
            let mutable font:SpriteFont = null
            let mutable backButton = new TextButton(game, "backButton", "no_shadow", "Back",new Vector2(10.0f,300.0f))
            let mutable garmentNameLabel = new Label(game, garment.Name, new Vector2(center.X, 20.0f))
            let mutable garmentImage:Image = null

            let backButtonClickHandler (args) =  
                event.Trigger(new ChangeScreenEventArgs(this, prevScreen))

            let buttonClickedEvent = new Event<GarmentItemClickedEventArgs>()
            [<CLIEvent>]
            member this.Click = buttonClickedEvent.Publish

            override this.LoadContent()=
                garmentImage <- new Image(game, garment, new Vector2(600.0f, 50.0f))
                this.Game.Components.Add(garmentImage)
                font <- game.Content.Load<SpriteFont>("Font")

            override this.Initialize()=
                game.Components.Add(backButton)
                game.Components.Add(garmentNameLabel)
                backButton.Click.Add(fun (args) -> backButtonClickHandler (args))
                base.Initialize()  
            
            override this.DestroyScene()=
                this.Game.Components.Remove(backButton) |> ignore
                this.Game.Components.Remove(garmentNameLabel) |> ignore
                this.Game.Components.Remove(garmentImage) |> ignore
                base.DestroyScene()

        and ChangeScreenEventArgs(oldScreen:Menu, newScreen:Menu)=
            inherit System.EventArgs()
            member this.OldScreen = oldScreen
            member this.NewScreen = newScreen