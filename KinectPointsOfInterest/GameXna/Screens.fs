namespace KinectPointsOfInterest

    open Microsoft.Xna.Framework
    open Microsoft.Xna.Framework.Input
    open Microsoft.Xna.Framework.Graphics
    open Microsoft.Xna.Framework.Audio

    open BodyData
    open MenuItems

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
            default this.DestroyScene() = ()

        and MeasurementScreen(game:Game, e:Event<ChangeScreenEventArgs>)=
            inherit Menu(game)

            let mutable sprite : Texture2D = null

            let kinect = new Kinect(game)
    
            let noOfSamples = 200
            let mutable timer = 10000.0f //start timer at 10 seconds
            let mutable finished = false //has the data been captured
            let mutable frontBody:Body[] = Array.zeroCreate noOfSamples //front data
            let mutable backBody:Body[] = Array.zeroCreate noOfSamples //back data
            let mutable sideBody:Body[] = Array.zeroCreate noOfSamples //side data

            //vertical points of interest. all dimensions are in visualisation space
            let mutable topOfHeadY = 0.0f
            let mutable bottomOfFeetY = 0.0f
            let mutable waistY = 0.0f
            let mutable shouldersY = 0.0f

            //depth image map
            let mutable depthImage:Texture2D = null
            let mutable clickSound:SoundEffect = null
            let mutable beepSound:SoundEffect = null

            override this.Initialize()=
                game.Components.Add(kinect)

            override this.LoadContent() =
                sprite <- this.Game.Content.Load<Texture2D>("Sprite")
                clickSound <- this.Game.Content.Load<SoundEffect>("click_1")
                beepSound <- this.Game.Content.Load<SoundEffect>("BEEP1A")
                base.LoadContent()

            override this.Update gameTime = 

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
        
        and MainMenu(game:Game, e:Event<ChangeScreenEventArgs>)=
            inherit Menu(game)

            let mutable measureButton = null
            let mutable shopButton = null
            let event = e
            
            let mutable leftClick = false

            override this.Initialize()=
                measureButton <- new Button(game, "measureButton", new Vector2(0.0f,0.0f))
                shopButton <- new Button(game, "shopButton", new Vector2(400.0f,0.0f))
                measureButton.DrawOrder = 1 |> ignore
                shopButton.DrawOrder = 1 |> ignore
                this.Game.Components.Add(measureButton)
                this.Game.Components.Add(shopButton)
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
                    | x when base.InBounds(x, measureButton) -> event.Trigger(new ChangeScreenEventArgs(this, new MeasurementScreen(this.Game, event))) //clicked on measure button
                    | x when base.InBounds(x, shopButton) -> event.Trigger(new ChangeScreenEventArgs(this, new LoginScreen(this.Game, event))) //clicked on shop button
                    | _ -> ()// clicked elsewhere, do nothing

            override this.DestroyScene()=
                this.Game.Components.Remove(measureButton) |> ignore
                this.Game.Components.Remove(shopButton) |> ignore

        and LoginScreen(game:Game, e:Event<ChangeScreenEventArgs>)=
            inherit Menu(game)
            
            let deselectTextBoxEvent = new Event<_>()

            let mutable username = new TextBox(game, "textbox", new Vector2(0.0f,100.0f), deselectTextBoxEvent)
            let mutable password = new TextBox(game, "textbox", new Vector2(0.0f,250.0f), deselectTextBoxEvent)

            

            let deselectTextBoxHandler args=
                username.Deselect
                password.Deselect

            [<CLIEvent>]
            member this.DeselectTextBoxes = deselectTextBoxEvent.Publish

            override this.Initialize()=
                
                this.DeselectTextBoxes.Add(fun args -> deselectTextBoxHandler args)
                this.Game.Components.Add(username)
                this.Game.Components.Add(password)
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.Update(gameTime)=
                base.Update(gameTime)

            override this.DestroyScene() =
                this.Game.Components.Remove(username) |> ignore
                this.Game.Components.Remove(password) |> ignore

        and Screen(game:Game)=
            inherit Menu(game)

            override this.Initialize()=
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()

            override this.Update(gameTime)=
                base.Update(gameTime)

            override this.DestroyScene() =
                ()

        and ChangeScreenEventArgs(oldScreen:Menu, newScreen:Menu)=
            inherit System.EventArgs()
            member this.OldScreen = oldScreen
            member this.NewScreen = newScreen