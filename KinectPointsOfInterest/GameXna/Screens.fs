namespace KinectPointsOfInterest

    open Microsoft.Xna.Framework
    open Microsoft.Xna.Framework.Input
    open Microsoft.Xna.Framework.Graphics
    open Microsoft.Xna.Framework.Audio

    open BodyData
    open MenuItems
    open VisualisationAssets

    module Instructions=
        type InstructionStep(game:Game, image_name, instruction, kinect)=
            inherit GameComponent(game)

            let userCompliedEvent = new Event<_>()

            let mutable complete = false
            let mutable background =null
            let mutable instructionImage =null
            let mutable messageLabel =null

            override this.Initialize() =
                base.Initialize()
                this.LoadContent()

            member this.LoadContent()=
                //build and add all components that make up the instruction screen to the game components
                background <- new Button(game, "UI/MeasurementInstructions/GrayBackground800x600", "none", new Vector2(112.0f, 84.0f), "", kinect)
                do background.DrawOrder <- 10
                do game.Components.Add(background)
                instructionImage <- new Image(game, image_name, new Vector2(112.0f, 84.0f))
                do instructionImage.DrawOrder <- 11
                do game.Components.Add(instructionImage)
                messageLabel <- new Label(game, instruction, new Vector2(512.0f, 650.0f))
                do messageLabel.DrawOrder <- 12
                do game.Components.Add(messageLabel)

            [<CLIEvent>]
            member this.UserComplied = userCompliedEvent.Publish

            member this.UserCompliedEvent
                with get() = userCompliedEvent
            //remove all game components associated with this screen
            member this.DestroyScene=
                do game.Components.Remove(background) |> ignore
                do game.Components.Remove(instructionImage) |> ignore
                do game.Components.Remove(messageLabel) |> ignore

            member this.Complete
                with get() = complete
                and set(c) = complete <- c

            override this.Update(gameTime)=
                base.Update(gameTime)

        type MeasureFrontInstructionStep(game:Game, image_name, instruction, kinect)=
            inherit InstructionStep(game, image_name, instruction, kinect)
            
            override this.Update(gameTime)=
                try
                    if kinect.GetPose(gameTime) = "front" then
                        base.UserCompliedEvent.Trigger()
                with
                    | NoUserTracked -> ()

        type MeasureSideInstructionStep(game:Game, image_name, instruction, kinect)=
            inherit InstructionStep(game, image_name, instruction, kinect)
            
            override this.Update(gameTime)=
                try
                    if kinect.GetPose(gameTime) = "side" then
                        base.UserCompliedEvent.Trigger()
                with
                    | NoUserTracked -> ()

    module Screens=

        type Menu(game:Game, kinect)=
            inherit DrawableGameComponent(game)

            let mutable spriteBatch = null
            let mutable cursor = new Cursor(game, new Vector2(float32 (Mouse.GetState().X), float32 (Mouse.GetState().Y)))
            let mutable background:Texture2D = null
            let mutable backgroundFooter = new Image(game, "UI/BackgroundFooter1024x116", new Vector2(0.0f, 768.0f-116.0f))
            let mutable backgroundHeader = new Image(game, "UI/BackgroundHeader1024x108", new Vector2(0.0f, 0.0f))


            let mutable kinectUI = kinect

            override this.Initialize()=
                this.Game.Components.Add(cursor)
                this.Game.Components.Add(backgroundFooter)
                this.Game.Components.Add(backgroundHeader)
                backgroundFooter.DrawOrder <- 10
                backgroundHeader.DrawOrder <- 10
                cursor.DrawOrder <- 99
                spriteBatch <- new SpriteBatch(this.Game.GraphicsDevice)
                base.Initialize()
                 
            override this.LoadContent()=
                
                background <- game.Content.Load<Texture2D>("UI/background")
                base.LoadContent()

            override this.Update(gameTime)=
                base.Update(gameTime)

            override this.Draw(gameTime)=
                spriteBatch.Begin()
                spriteBatch.Draw(background, new Rectangle(0, 0, game.GraphicsDevice.Viewport.Width, game.GraphicsDevice.Viewport.Height), Color.White)
                spriteBatch.End()
                base.Draw(gameTime)
                

            member this.InBounds (pos:Vector2, button:Button) =
                if pos.X < (float32 button.GetTextureBounds.Right + button.Position.X) && pos.X > (float32 button.GetTextureBounds.Left + button.Position.X) then
                    if pos.Y < (float32 button.GetTextureBounds.Bottom + button.Position.Y) && pos.Y > (float32 button.GetTextureBounds.Top + button.Position.Y) then
                        true
                    else
                        false
                else
                    false

            member this.KinectUI
                with get() = kinectUI
                and set(k) = kinectUI <- k

            abstract member DestroyScene: unit -> unit
            default this.DestroyScene() = 
                this.KinectUI <- null
                this.Game.Components.Remove(cursor) |> ignore
                this.Game.Components.Remove(backgroundFooter) |> ignore 
                this.Game.Components.Remove(backgroundHeader) |> ignore 

        and MeasurementScreen(game:Game, sex, e:Event<ChangeScreenEventArgs>, kinectUI)=
            inherit Menu(game, kinectUI)

            let event = e

            let mutable sprite : Texture2D = null

            let kinect = new Kinect.KinectMeasure(game)
    
            let noOfSamples = 200
            let mutable timer = 10000.0f //start timer at 10 seconds
            let mutable finished = false //has the data been captured
            let mutable frontBody:Body[] = Array.zeroCreate noOfSamples //front data
            let mutable backBody:Body[] = Array.zeroCreate noOfSamples //back data
            let mutable sideBody:Body[] = Array.zeroCreate noOfSamples //side data

            let mutable leftClick = true

            let mutable depthImage:Texture2D = null//depth image map
            let mutable clickSound:SoundEffect = null
            let mutable beepSound:SoundEffect = null

            let mutable nextButton = new TextButton(game, "nextButton", "no_shadow", "Next", new Vector2( 300.0f, 300.0f), base.KinectUI)
            
            let mutable frontReady, sideReady, backReady = false, false, false
            let mutable frontInstructions = new Instructions.MeasureFrontInstructionStep(game, "UI/MeasurementInstructions/MeasureInstructionsManFrontBack800x600", "Stand still with your arms out, facing the Kinect sensor", kinectUI)
            do frontInstructions.UserComplied.Add(fun args -> frontReady <- true)
            let mutable sideInstructions = new Instructions.MeasureSideInstructionStep(game, "UI/MeasurementInstructions/MeasureInstructionsManSide800x600", "Side", kinectUI)
            do sideInstructions.UserComplied.Add(fun args -> sideReady <- true)
            let mutable backInstructions = new Instructions.MeasureFrontInstructionStep(game, "UI/MeasurementInstructions/MeasureInstructionsManFrontBack800x600", "Back", kinectUI)
            do backInstructions.UserComplied.Add(fun args -> backReady <- true)
            
            member this.ClickOption(mouseClickPos:Vector2)=
                match mouseClickPos with
                    | x when base.InBounds(x, nextButton) -> event.Trigger(new ChangeScreenEventArgs(this, new VisualisationScreen(this.Game, sex, 0, 0, 0, 0, event, kinectUI))) //clicked on next button
                    | _ -> ()// clicked elsewhere, do nothing

            override this.Initialize()=
                game.Components.Add(kinect)
                game.Components.Add(nextButton)
                game.Components.Add(frontInstructions)
                base.Initialize()

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
                
                if backReady && backBody.[0] = null then
                    backInstructions.DestroyScene
                    game.Components.Remove(backInstructions) |> ignore

                    clickSound.Play() |> ignore
                    for i = 0 to noOfSamples-1 do
                        backBody.[i] <- kinect.CaptureBody
                    beepSound.Play() |> ignore
                    backReady <-true
                    
                else if sideReady && sideBody.[0] = null then
                    sideInstructions.DestroyScene
                    game.Components.Remove(sideInstructions) |> ignore

                    clickSound.Play() |> ignore
                    for i = 0 to noOfSamples-1 do
                        sideBody.[i] <- kinect.CaptureBody
                    beepSound.Play() |> ignore
                    
                    game.Components.Add(backInstructions)

                else if frontReady && frontBody.[0] = null then
                    frontInstructions.DestroyScene
                    game.Components.Remove(frontInstructions) |> ignore

                    clickSound.Play() |> ignore
                    for i = 0 to noOfSamples-1 do
                        frontBody.[i] <- kinect.CaptureBody
                    beepSound.Play() |> ignore
                    
                    game.Components.Add(sideInstructions)
                    
                if frontBody.[noOfSamples-1] <> null && backBody.[noOfSamples-1] <> null && sideBody.[noOfSamples-1] <> null && not finished then
                    //game.Components.Add(new BodyMeasurements(this, kinect, frontBody, sideBody, backBody))
                    //this.Game.Components.Add(new BodyMeasurementsPostProcess(this.Game, kinect, frontBody, sideBody, backBody))
                    let processor = new BodyMeasurementsPostProcess(this.Game, kinect, frontBody, sideBody, backBody)
                    let (waist, hips, height) = processor.GetMeasurements
                    printfn "WAIST=%A \nHIPS=%A \nHEIGHT=%A" waist hips height
                    finished <- true
                    event.Trigger(new ChangeScreenEventArgs(this, new VisualisationScreen(game, sex, height, 10, waist, hips, e, kinectUI)))
                base.Update gameTime

            override this.DestroyScene()=
                this.Game.Components.Remove(kinect) |> ignore
                this.Game.Components.Remove(nextButton) |> ignore
                base.DestroyScene()

        and VisualisationScreen(game:Game, sex, height, chest, waist, hips, e:Event<ChangeScreenEventArgs>, kinect)=
            inherit Menu(game, kinect)

            let event = e

            let mutable nextButton = new TextButton(game, "nextButton", "no_shadow", "Next", new Vector2( 300.0f, 500.0f), base.KinectUI)

            let model = new Visualisation.HumanModel(game, "none", 0)
            
            let mutable leftClick = true //so click form last screen is not read through

            override this.Initialize()=
                this.Game.Components.Add(model)
                //this.Game.Components.Add(nextButton)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.Update(gameTime)=
                let key = Keyboard.GetState().GetPressedKeys()
                if key.Length <> 0 then
                    match key.[0] with
                        | Keys.D1 -> model.ChangeFrame(10)
                        | Keys.D2 -> model.ChangeFrame(20)
                        | Keys.D3 -> model.ChangeFrame(30)
                        | Keys.D4 -> model.ChangeFrame(40)
                        | Keys.D5 -> model.ChangeFrame(50)
                        | Keys.D6 -> model.ChangeFrame(59)
                        | _ -> ()

                base.Update(gameTime)

            override this.DestroyScene()=
                this.Game.Components.Remove(model) |> ignore
                //this.Game.Components.Remove(nextButton) |> ignore
                base.DestroyScene()

        and MainMenu(game:Game, e:Event<ChangeScreenEventArgs>, kinect) as this=
            inherit Menu(game, kinect)

            let mutable measureButton = new Button(game, "UI/MeasureButton300x300", "no_shadow", new Vector2(90.0f,150.0f), "", base.KinectUI)
            let mutable shopButton = new Button(game, "UI/ShopButton300x300", "no_shadow", new Vector2(410.0f,150.0f), "", base.KinectUI)

            let event = e
            
            let measureButtonHandler args= event.Trigger(new ChangeScreenEventArgs(this, new GenderSelectScreen(this.Game, event, kinect)))

            let shopButtonHandler args= event.Trigger(new ChangeScreenEventArgs(this, new StoreScreen(this.Game, event, kinect)))
            
            override this.Initialize()=
                measureButton.DrawOrder <- 1
                shopButton.DrawOrder <- 1
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

        and RecentUsersScreen(game:Game, event:Event<ChangeScreenEventArgs>, kinect) as this=
            inherit Menu(game, kinect)

            let mutable recentUsers = new MenuItemList(game, kinect)
            
            let userClickedHandler (args:LoginEventArgs) =
                event.Trigger(new ChangeScreenEventArgs(this, new LoginScreen(this.Game, args.Email.ToString(), event, kinect)))

            let getPrevUsers =()

            override this.Initialize()=
                let mutable startYPos = 0
                try
                    let prevUsers = System.Xml.Linq.XElement.Load("recentUsers.xml")
                    startYPos <- (game.GraphicsDevice.Viewport.Height / 2) - ((Seq.length(prevUsers.Elements()) + 1)*110)/2
                    if startYPos < 100 then startYPos <- 100
                    for (f:System.Xml.Linq.XElement) in prevUsers.Elements() do
                        let userName = f.Element(System.Xml.Linq.XName.Get("name")) 
                        let userId = f.Element(System.Xml.Linq.XName.Get("id"))
                        let userEmail = f.Element(System.Xml.Linq.XName.Get("email"))
                        let user = new User(game, (int) userId.Value, userName.Value, userEmail.Value, new Vector2(200.0f,((float32)userId.Value * 160.0f)+float32 startYPos), base.KinectUI)
                        user.DrawOrder <- 1
                        user.Click.Add(fun args -> userClickedHandler args)
                        recentUsers.Add(user)
                with
                    | :? System.IO.FileNotFoundException as ex -> ()
                    | ex -> ()
                let notOnList = new User(game, 99, "I'm not on the List!", "", new Vector2(200.0f,((float32)(recentUsers.List.Count) * 160.0f)+float32 startYPos), base.KinectUI)
                notOnList.Click.Add(fun args -> userClickedHandler args)
                recentUsers.Add(notOnList)
                recentUsers.DrawOrder <- 1
                game.Components.Add(recentUsers)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.DestroyScene() =
                System.Diagnostics.Debug.WriteLine("recentUsers:"+ string (this.Game.Components.Remove(recentUsers)))
                base.DestroyScene()

        and LoginScreen(game:Game, email, e:Event<ChangeScreenEventArgs>, kinect) as this=
            inherit Menu(game, kinect)

            let event = e
            let deselectTextBoxEvent = new Event<_>()

            let mutable username = new TextBox(game, false, "UI/BlueButton600x150", "no", new Vector2(212.0f,159.0f), deselectTextBoxEvent, kinect)
            do username.DrawOrder <- 1
            let mutable password = new TextBox(game, true, "UI/BlueButton600x150", "no", new Vector2(212.0f,319.0f), deselectTextBoxEvent, kinect)
            do password.DrawOrder <- 1
            let mutable nextButton = new TextButton(game, "UI/BlueButton300x150", "no", "Login", new Vector2( 512.0f, 479.0f), base.KinectUI)
            do nextButton.DrawOrder <- 1
            let mutable backButton = new TextButton(game, "UI/BlueButton300x150", "no", "Back", new Vector2( 212.0f, 479.0f), base.KinectUI)
            do backButton.DrawOrder <- 1
            let mutable errorBox:ErrorBox = null
            //Event Handlers
            let deselectTextBoxHandler sender args=
                if args = "none" then
                    username.Deselect
                    password.Deselect
                else if args = "kinect" then
                    event.Trigger(new ChangeScreenEventArgs(this, new KinectTextInputScreen(game, sender, e, this, kinect))) // go back to previous screen
                    
            let BackHandler args=
                event.Trigger(new ChangeScreenEventArgs(this, new RecentUsersScreen(game, e, kinect))) // go back to previous screen
            let ErrorClickHandler args= 
                nextButton.ClicksEnable
                backButton.ClicksEnable
                game.Components.Remove(errorBox) |> ignore
            let LoginHandler args=
                let db = new Database.DatabaseAccess()
                let customer = db.getCustomer username.Text password.Text //try to login
                if customer <> null then //if login succeded
                    event.Trigger(new ChangeScreenEventArgs(this, new MainMenu(this.Game, event, kinect)))
                else //if login failed
                    //show error message
                    errorBox <- new ErrorBox(game, "Login Failed", "Incorrect Username or Password, please try again.\nIf you do not have an account visit \nkinect.fadeinfuture.net/kinectedfashion to register", kinect)
                    errorBox.DrawOrder <- 5
                    errorBox.Click.Add(ErrorClickHandler)
                    //disable the buttons below so that they cannot be clicked through the errr message
                    nextButton.ClicksDisable
                    backButton.ClicksDisable
                    username.Deselect
                    password.Deselect
                    password.Text <- ""
                    game.Components.Add(errorBox)

            [<CLIEvent>]
            member this.DeselectTextBoxes = deselectTextBoxEvent.Publish

            override this.Initialize()=
                this.DeselectTextBoxes.Add(fun (sender,args) -> deselectTextBoxHandler sender args)
                nextButton.Click.Add(fun args-> LoginHandler args)
                backButton.Click.Add(fun args -> BackHandler args)
                username.Text <- email
                this.Game.Components.Add(username)
                this.Game.Components.Add(password)
                this.Game.Components.Add(nextButton)
                this.Game.Components.Add(backButton)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.DestroyScene() =
                System.Diagnostics.Debug.WriteLine("username:"+ string (this.Game.Components.Remove(username)))
                System.Diagnostics.Debug.WriteLine("password:"+ string (this.Game.Components.Remove(password)))
                System.Diagnostics.Debug.WriteLine("next:"+ string (this.Game.Components.Remove(nextButton)))
                System.Diagnostics.Debug.WriteLine("back:"+ string (this.Game.Components.Remove(backButton))) 
                base.DestroyScene()

        and GenderSelectScreen(game:Game, e:Event<ChangeScreenEventArgs>, kinect) as this=
            inherit Menu(game, kinect)

            let maleButton = new Button(game, "UI/MaleButton300x300", "no_shadow", new Vector2(90.0f,150.0f), "", base.KinectUI)
            let femaleButton = new Button(game, "UI/FemaleButton300x300", "no_shadow", new Vector2(410.0f,150.0f), "", base.KinectUI)
            let event = e

            let genderSelectedHandler (args:ButtonClickedEventArgs)= 
                match args.Sender with
                    | x when x = maleButton -> event.Trigger(new ChangeScreenEventArgs(this, new MeasurementScreen(this.Game, "male", event, kinect)))
                    | x when x = femaleButton -> event.Trigger(new ChangeScreenEventArgs(this, new MeasurementScreen(this.Game, "female", event, kinect)))
                    | _ -> () //should never happen as there is no other buttons

            override this.Initialize()=
                maleButton.Click.Add(fun args -> genderSelectedHandler args)
                femaleButton.Click.Add(fun args -> genderSelectedHandler args)

                maleButton.DrawOrder <- 1
                femaleButton.DrawOrder <- 1
                this.Game.Components.Add(maleButton)
                this.Game.Components.Add(femaleButton)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.DestroyScene()=
                this.Game.Components.Remove(maleButton) |> ignore
                this.Game.Components.Remove(femaleButton) |> ignore
                base.DestroyScene()

        and StoreScreen(game:Game, e:Event<ChangeScreenEventArgs>, kinect) as this=
            inherit Menu(game, kinect)

            let dbAccess = new Database.DatabaseAccess()

            let mutable garmentItems = new MenuItemList(game, kinect)
            let mutable femaleButton = null
            let event = e

            let garmentClickHandler (args:GarmentItemClickedEventArgs)= 
                e.Trigger(new ChangeScreenEventArgs(this, new GarmentScreen(this.Game, args.Garment, event, this, kinect)))

            override this.Initialize()=
                let garmentList = dbAccess.getGarments null
                                  |> Seq.distinctBy (fun (x:Store.Garment)-> x.Name) //remove duplicate items (e.g. size variations)
                let startYPos = System.Math.Min(((game.GraphicsDevice.Viewport.Height / 2) - ((Seq.length(garmentList) + 1)*110)/2), 10)
                garmentItems <- new MenuItemList(game, kinect)
                let kinect = base.KinectUI
                garmentList
                |> Seq.iter (fun (x:Store.Garment) -> (let newGarment = new GarmentItem(game, x, new Vector2(100.0f,(((float32)x.ID * 110.0f) + float32 startYPos)), kinect) //make a new garment Object
                                                       newGarment.Click.Add(fun x -> garmentClickHandler x) //add it's click handler
                                                       newGarment.DrawOrder <- 1
                                                       garmentItems.Add(newGarment))) //add it to the garment items list
                garmentItems.DrawOrder <- 1
                this.Game.Components.Add(garmentItems)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.DestroyScene()=
                this.Game.Components.Remove(garmentItems) |> ignore
                base.DestroyScene()

        and GarmentScreen(game:Game, garment:Store.Garment, e:Event<ChangeScreenEventArgs>, prevScreen, kinect) as this=
            inherit Menu(game, kinect)

            let event = e
            let center = new Vector2(float32(game.GraphicsDevice.Viewport.Width / 2), float32(game.GraphicsDevice.Viewport.Height / 2))
            let mutable font:SpriteFont = null
            let mutable backButton = new TextButton(game, "backButton", "no_shadow", "Back",new Vector2(10.0f,300.0f), base.KinectUI)
            let mutable garmentNameLabel = new Label(game, garment.Name, new Vector2(center.X, 20.0f))
            let mutable garmentImage:GarmentImage = null

            let backButtonClickHandler (args) =  
                event.Trigger(new ChangeScreenEventArgs(this, prevScreen))

            let buttonClickedEvent = new Event<GarmentItemClickedEventArgs>()
            [<CLIEvent>]
            member this.Click = buttonClickedEvent.Publish

            override this.LoadContent()=
                garmentImage <- new GarmentImage(game, garment, new Vector2(600.0f, 50.0f))
                this.Game.Components.Add(garmentImage)
                font <- game.Content.Load<SpriteFont>("Font")
                base.LoadContent()

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

        and KinectTextInputScreen(game:Game, textBox:TextBox, e:Event<ChangeScreenEventArgs>, prevScreen, kinect) as this=
            inherit Menu(game, kinect)

            let charSet = "1234567890qwertyuiopasdfghjklzxcvbnm" //!@£$%^&*(),.+=-/_\"\'\\|:;[]#
            let charSetCaps = "!@£$%^&*()QWERTYUIOPASDFGHJKLZXCVBNM"
            let keys = Array.init 36 (fun x -> new TextButton(game, "UI/BlueButton100x100", "none", charSet.Substring(x, 1), new Vector2(12.0f+(match x with
                                                                                                                                        | x when x >= 29 -> float32(x-29) * 100.0f
                                                                                                                                        | x when x >= 20 -> float32(x-20) * 100.0f 
                                                                                                                                        | x when x >= 10 -> float32(x-10) * 100.0f
                                                                                                                                        | x when x >= 0 -> float32(x) * 100.0f), match x with
                                                                                                                                                                                    | x when x >= 29 -> 550.0f
                                                                                                                                                                                    | x when x >= 20 -> 450.0f 
                                                                                                                                                                                    | x when x >= 10 -> 350.0f
                                                                                                                                                                                    | x when x >= 0 -> 250.0f), kinect))
            let capsButton = new TextButton(game, "UI/BlueButton100x100", "none", "Caps", new Vector2(100.0f,700.0f), kinect)
            let okButton = new TextButton(game, "UI/BlueButton100x100", "none", "OK", new Vector2(900.0f,700.0f), kinect)
            let backspaceButton = new TextButton(game, "UI/BlueButton100x100", "none", "<--", new Vector2(900.0f,50.0f), kinect)
            let mutable caps = false

            override this.LoadContent()=
                base.LoadContent()
                let prevTextBoxPos = textBox.Position
                do textBox.Position <- (new Vector2(212.0f, 50.0f))

            override this.Initialize()=
                
                for x in keys do
                    this.Game.Components.Add(x)
                    x.DrawOrder <- 10 
                    x.Click.Add(fun x -> textBox.AddChar x.Sender.Value)
                this.Game.Components.Add(capsButton)
                capsButton.DrawOrder <- 10 
                capsButton.Click.Add(fun x -> caps <- not caps
                                              for i = 0 to charSet.Length - 1 do
                                                    keys.[i].ChangeText (if caps then charSetCaps.Substring(i,1) else charSet.Substring(i,1))
                                              ) 
                this.Game.Components.Add(okButton)
                okButton.DrawOrder <- 10 
                okButton.Click.Add(fun x -> ())
                this.Game.Components.Add(backspaceButton)
                backspaceButton.DrawOrder <- 10 
                backspaceButton.Click.Add(fun x -> textBox.Backspace)  
                this.Game.Components.Add(textBox)
                textBox.DrawOrder <- 10 
                base.Initialize()
                
            override this.Update(gameTime)=
                let leftHandX = kinect.GetState().LeftHandPosition.X
                ()

            override this.DestroyScene()=
                for x in keys do
                    this.Game.Components.Remove(x) |> ignore
                this.Game.Components.Remove(capsButton) |> ignore
                this.Game.Components.Remove(textBox) |> ignore
                base.DestroyScene()