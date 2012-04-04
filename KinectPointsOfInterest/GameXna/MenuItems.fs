namespace KinectPointsOfInterest

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

open EventControllers

open System.Collections
open KinectHelperMethods

open System.Net
open System.IO

open Kinect

    module MenuItems=


        [<AllowNullLiteral>] //allow null as a proper value
        type DrawableMenuItem(game, textureName, shadowTextureName, pos)=
            inherit DrawableGameComponent(game)

            let mutable sprite:Texture2D = null
            let mutable shadow:Texture2D = null
            let mutable position:Vector2= pos
            let mutable spriteBatch = null

            override this.Initialize() =
                base.Initialize()
            
            override this.LoadContent() =
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
                try
                    shadow <- this.Game.Content.Load<Texture2D>(shadowTextureName)
                with
                    | ex -> ()
                try
                    sprite <- this.Game.Content.Load<Texture2D>(textureName)
                with
                    | ex -> ()
                base.LoadContent()

            override this.Draw(gameTime)=
                spriteBatch.Begin()
                if shadow <> null then
                    spriteBatch.Draw(shadow, position, Color.White)
                if sprite <> null then
                    spriteBatch.Draw(sprite, position, Color.White)
                spriteBatch.End()
                base.Draw(gameTime)

            override this.UnloadContent()=
                if sprite <> null then
                    sprite.Dispose()
                if shadow <> null then
                    shadow.Dispose()

                base.UnloadContent()

            member this.GetTextureBounds
                with get() = sprite.Bounds
            member this.Position
                with get() = position  
                and set(pos) = position <- pos
            member this.Sprite
                with get() = sprite
            member this.SwapShadowAndSprite=
                let temp = sprite
                sprite <- shadow
                shadow <- temp
            member this.SpriteBatch
                with get() = spriteBatch
                
                 
        [<AllowNullLiteral>] //allow null as a proper value
        type Button(game, textureName, shadowName, pos:Vector2, kinect:KinectCursor)=
            inherit DrawableMenuItem(game, textureName, shadowName, pos)

            let mutable leftClick = true
            let mutable kinectRightClick = true

            let mutable enabled = true

            let buttonClickedEvent = new Event<ButtonClickedEventArgs>()
            let kinectButtonClickedEvent = new Event<ButtonClickedEventArgs>()

            [<CLIEvent>]
            member this.Click = buttonClickedEvent.Publish
            [<CLIEvent>]
            member this.KinectClick = kinectButtonClickedEvent.Publish

            member this.getBasePosition:Vector2 =
                base.Position
            member this.getBaseSprite:Texture2D =
                base.Sprite
            member this.ClicksEnable=
                enabled <- true
                leftClick <- true
            member this.ClicksDisable=
                enabled <- false

            override this.Initialize()=
                base.Initialize()
                

            override this.Update(gameTime)=
                if enabled then
                    let mouse = Mouse.GetState()
                    if Mouse.GetState().LeftButton = ButtonState.Pressed && not leftClick then //if left button clicked
                        leftClick <- true
                        if mouse.X < (base.Sprite.Bounds.Right + (int base.Position.X)) 
                        && mouse.X > (base.Sprite.Bounds.Left  + (int base.Position.X)) then 
                            if mouse.Y < (base.Sprite.Bounds.Bottom  + (int base.Position.Y))
                            && mouse.Y > (base.Sprite.Bounds.Top + (int base.Position.Y)) then 
                                buttonClickedEvent.Trigger(new ButtonClickedEventArgs(this))
                    if kinect.GetState().RightButton = ButtonState.Pressed && not kinectRightClick && not (kinect.GetState().LeftButton = ButtonState.Pressed) then
                        kinectRightClick <- true
                        if kinect.GetState().RightHandPosition.X < float32 base.Sprite.Bounds.Right + (base.Position.X) 
                        && kinect.GetState().RightHandPosition.X > float32 base.Sprite.Bounds.Left  + (base.Position.X) then 
                            if kinect.GetState().RightHandPosition.Y < float32 base.Sprite.Bounds.Bottom  + (base.Position.Y)
                            && kinect.GetState().RightHandPosition.Y > float32 base.Sprite.Bounds.Top + (base.Position.Y) then 
                                kinectButtonClickedEvent.Trigger(new ButtonClickedEventArgs(this))
                                
                    if Mouse.GetState().LeftButton = ButtonState.Released then
                        leftClick <- false
                    if kinect.GetState().RightButton = ButtonState.Released then
                        kinectRightClick <- false
                base.Update(gameTime)

        and ButtonClickedEventArgs(sender)=
            inherit System.EventArgs()
            member this.Sender = sender          
        
        [<AllowNullLiteral>] //allow null as a proper value
        type TextButton(game, textureName, shadowTextureName, label:string, pos:Vector2, kinect)=
            inherit Button(game, textureName, shadowTextureName, pos, kinect)

            let mutable font:SpriteFont = null
            let mutable label = label

            let mutable textPosOffset = Vector2.Zero //the label's origin
            let mutable spriteCenter = Vector2.Zero

            override this.LoadContent()=
                font <- game.Content.Load<SpriteFont>("textBoxFont")
                textPosOffset <- Vector2.Divide(font.MeasureString(label), 2.0f)
                
                base.LoadContent()
                spriteCenter <- new Vector2(float32 base.Sprite.Bounds.Center.X, float32 base.Sprite.Bounds.Center.Y)

            override this.Draw(gameTime)=
               base.Draw(gameTime)
               base.SpriteBatch.Begin()
               base.SpriteBatch.DrawString(font, label, base.Position + spriteCenter, Color.White, 0.0f, textPosOffset, 1.0f, SpriteEffects.None, 0.5f)
               base.SpriteBatch.End()

            member this.ChangeText t=
                label <- t

            member this.Label
                with get() = label

        [<AllowNullLiteral>] //allow null as a proper value
        type TextToggleButton(game, textureName, toggledTextureName, label:string, pos:Vector2, kinect)=
            inherit TextButton(game, textureName, toggledTextureName, label, pos, kinect)

            let mutable toggled = false

            member this.Toggle = 
                toggled <- not toggled
                base.SwapShadowAndSprite

        [<AllowNullLiteral>] //allow null as a proper value
        type ToggleButton(game, textureName, toggledTextureName, pos:Vector2, kinect)=
            inherit Button(game, textureName, toggledTextureName, pos, kinect)

            let mutable toggled = false

            member this.Toggle = 
                toggled <- not toggled
                base.SwapShadowAndSprite


        [<AllowNullLiteral>] //allow null as a proper value
        type ErrorBox(game, heading:string, message:string, kinect)=
            inherit Button(game, "UI/Error800x600", "no_shadow", new Vector2(112.0f , 84.0f) , kinect)

            let mutable font:SpriteFont = null
            let messageLines = message.Split('\n')

            let mutable textPosOffset = Vector2.Zero //the label's origin
            let mutable spriteCenter = Vector2.Zero

            override this.LoadContent()=
                font <- game.Content.Load<SpriteFont>("textBoxFont")
                textPosOffset <- Vector2.Divide(font.MeasureString(heading), 2.0f)
                
                base.LoadContent()
                spriteCenter <- new Vector2(float32 base.Sprite.Bounds.Center.X, float32 base.Sprite.Bounds.Center.Y)

            override this.Draw(gameTime)=
                base.Draw(gameTime)
                base.SpriteBatch.Begin()
                base.SpriteBatch.DrawString(font, heading, base.Position + new Vector2(spriteCenter.X, 50.0f), Color.White, 0.0f, textPosOffset, 1.0f, SpriteEffects.None, 0.5f)
                for line = 0 to messageLines.Length - 1 do
                    let lineCenter = Vector2.Divide(font.MeasureString(messageLines.[line]), 2.0f)
                    base.SpriteBatch.DrawString(font, messageLines.[line], base.Position + new Vector2(spriteCenter.X, spriteCenter.Y - float32(messageLines.Length * 15) + float32(line * 30)), Color.White, 0.0f, lineCenter, 1.0f, SpriteEffects.None, 0.5f)
                base.SpriteBatch.End()

        [<AllowNullLiteral>] //allow null as a proper value
        type Cursor(game, pos:Vector2)=
            inherit DrawableMenuItem(game, "cursor", "no_shadow", pos)

            override this.Update(gameTime) =
                let mouseState = Mouse.GetState()
                base.Position <- new Vector2(float32 mouseState.X, float32 mouseState.Y)
                base.Update(gameTime)

        [<AllowNullLiteral>] //allow null as a proper value
        type TextBox(game, passwordBox, textureName, shadowTextureName, pos:Vector2, kinect:KinectCursor)=
            inherit Button(game, textureName, shadowTextureName, pos, kinect)
            
            let createdTime = System.DateTime.Now.ToString()

            let MAXCHARS = 35
            let mutable security = passwordBox //should the input be obscured?
            let mutable selected = false
            let mutable text = ""

            let mutable textMask = "" //used to mask password text as it is typed in

            let mutable leftClick = false
            let mutable kinectRightClick = false
            let mutable carrotPosition = 0

            let mutable font:SpriteFont = null

            let mutable textCenterY = 0.0f
            let mutable spriteCenterY = 0.0f
            let mutable textPosOffset = new Vector2(20.0f, 20.0f)

            let maskText (txt:string)=
                let mutable mask = ""
                for i = 1 to txt.Length do
                    mask <- mask + "*"
                mask
            
            member this.CreatedTime 
                with get() = createdTime

            member this.Backspace=
                if text.Length > 0 then
                    carrotPosition <- System.Math.Max(carrotPosition - 1, 0)
                    text <- text.Remove(carrotPosition,1)

            member this.Deselect =
                selected <- false
            member this.Select =
                selected <- true

            member this.AddChar char=
                if text.Length < MAXCHARS then
                    (text <- text.Substring(0,carrotPosition) + char + text.Substring(carrotPosition, text.Length - carrotPosition))
                    carrotPosition <- carrotPosition + 1

            override this.Initialize()=
                KinectHelperMethods.KeyGrabber.add_InboundCharEvent (fun x -> 
                                                                            if selected then 
                                                                                if x <> '\b' then //not the backspace key
                                                                                    this.AddChar (x.ToString())
                                                                                else if x <> '\n' then //is the backspace key
                                                                                    this.Backspace
                                                                                )
                base.Initialize()

            override this.LoadContent()=
                base.LoadContent()
                font <- game.Content.Load<SpriteFont>("textBoxFont")
                let textCenterY = Vector2.Divide(font.MeasureString("Test"), 2.0f).Y
                let spriteCenterY = float32 base.Sprite.Bounds.Center.Y
                textPosOffset.Y <- spriteCenterY - textCenterY
                
            override this.Update(gameTime)=
                base.Update(gameTime)
                textMask <- maskText text

            override this.Draw(gameTime)=
                base.Draw(gameTime)
                base.SpriteBatch.Begin()
                let mutable carrot = ""
                let mutable realCarrotPosition = if passwordBox then font.MeasureString(textMask.Substring(0, carrotPosition)) else font.MeasureString(text.Substring(0, carrotPosition))
                realCarrotPosition.Y <- -4.0f
                realCarrotPosition.X <- realCarrotPosition.X - 5.0f
                if selected then
                   base.SpriteBatch.Draw(base.Sprite, base.Position, Color.Gold *0.2f)
                   if gameTime.TotalGameTime.Seconds % 2 = 0 then //blink the carrot
                        carrot <- "|"
                if passwordBox then
                    base.SpriteBatch.DrawString(font, textMask, base.Position + textPosOffset, Color.White)
                else
                    base.SpriteBatch.DrawString(font, text, base.Position + textPosOffset, Color.White)
                base.SpriteBatch.DrawString(font, carrot, base.Position + textPosOffset + realCarrotPosition, Color.White)
                base.SpriteBatch.End()

            member this.Text
                with get() = text
                and set(t) = text <- t
                             carrotPosition <- String.length text
                             textMask <- maskText text
            member this.Security
                with set(s) = security <- s
                and get() = security

        [<AllowNullLiteral>] //allow null as a proper value
        type Label(game, l:string, pos:Vector2, fontName)=
            inherit DrawableMenuItem(game, "no_texture", "no_shadow", pos)

            let mutable label = l
            let mutable labelOrigin = Vector2.Zero
            let mutable font:SpriteFont = null

            new (game, l:string, pos:Vector2) = new Label(game, l, pos, "Font")

            override this.Initialize()=
                
                base.Initialize()

            override this.LoadContent()=
                font <- game.Content.Load<SpriteFont>(fontName)
                labelOrigin <- Vector2.Divide(font.MeasureString(label), 2.0f)
                base.LoadContent()

            override this.Draw(gameTime)=
                base.Draw(gameTime)
                base.SpriteBatch.Begin()
                base.SpriteBatch.DrawString(font, label, base.Position, Color.White, 0.0f, labelOrigin, 1.0f, SpriteEffects.None, 0.5f)
                base.SpriteBatch.End()

            member this.ChangeText t=
                label <- t

            member this.Font
                with set(f) = font <- f


        [<AllowNullLiteral>] //allow null as a proper value
        type GarmentItem(game:Game, garment:Store.Garment, pos, kinect)=
            inherit Button(game, "UI/BlueButton400x100", "none", pos, kinect)

            let mutable leftClick = true

            let mutable font:SpriteFont = null
            let mutable webTex:Texture2D = null

            let mutable textPosOffset= Vector2.Zero
            let mutable spriteCenter = Vector2.Zero

            //called when the Async image download is complete
            let WebImageDownloadComplete (args:DownloadDataCompletedEventArgs)= 
                try
                    let stream = new MemoryStream(args.Result)
                    webTex <- Texture2D.FromStream(game.GraphicsDevice, stream)
                with //if the image file doesn't exist on the server, use the 'no photo' image
                    | :? System.Reflection.TargetInvocationException as ex -> webTex <- game.Content.Load<Texture2D>("GarmentThumbNotFound")
                    | ex -> ()

            let buttonClickedEvent = new Event<GarmentItemClickedEventArgs>()
            [<CLIEvent>]
            member this.Click = buttonClickedEvent.Publish

            override this.LoadContent()=
                base.LoadContent()
                webTex <- game.Content.Load<Texture2D>("GarmentThumbLoading")
                font <- game.Content.Load<SpriteFont>("garmentItemFont")
                textPosOffset <- Vector2.Divide(font.MeasureString(garment.Name), 2.0f)
                textPosOffset <- textPosOffset - new Vector2((Vector2.Divide(font.MeasureString(garment.Name), 2.0f)).X, 0.0f)
                
                spriteCenter <- new Vector2(120.0f, float32 base.Sprite.Bounds.Center.Y)
                let webClient = new WebClient()
                let URI = new System.Uri("http://localhost/garment_images/"+garment.ID.ToString()+"_thumb.jpg")
                webClient.DownloadDataCompleted.Add(WebImageDownloadComplete)
                async{ //download image from webserver
                    try
                        webClient.DownloadDataAsync(URI)
                    with
                        | ex -> printfn "%s" (ex.Message)
                    } |> Async.RunSynchronously

            override this.Update(gameTime)=
                if Mouse.GetState().LeftButton = ButtonState.Pressed && not leftClick then
                    if Mouse.GetState().X < base.Sprite.Bounds.Right + (int base.Position.X) 
                    && Mouse.GetState().X > base.Sprite.Bounds.Left  + (int base.Position.X) then 
                        if Mouse.GetState().Y < base.Sprite.Bounds.Bottom  + (int base.Position.Y)
                        && Mouse.GetState().Y > base.Sprite.Bounds.Top + (int base.Position.Y) then 
                            buttonClickedEvent.Trigger(new GarmentItemClickedEventArgs(this, garment))
                if Mouse.GetState().LeftButton = ButtonState.Released then
                    leftClick <- false

            override this.Draw(gameTime)=
                base.Draw(gameTime)
                base.SpriteBatch.Begin()
                base.SpriteBatch.Draw(webTex, base.Position, Color.White)
                base.SpriteBatch.DrawString(font, garment.Name, base.Position + spriteCenter, Color.White, 0.0f, textPosOffset, 1.0f, SpriteEffects.None, 0.5f)
                base.SpriteBatch.End()

            member this.Garment
                with get() = garment

            member this.Position
                with get() = base.Position
                and set(p) = base.Position <- p

        and GarmentItemClickedEventArgs(sender, garment)=
            inherit System.EventArgs()
            member this.Sender = sender
            member this.Garment = garment  
        
        type MenuItemList(game:Game, kinect:KinectCursor)= //A simple container to hold a list of items that need drawn
            inherit DrawableGameComponent(game)
            
            //let mutable list:List<GarmentItem> = []
            let list = new System.Collections.Generic.List<DrawableMenuItem>()
            
            let mutable position = Vector2.Zero
            let mutable lastMouseWheel = Mouse.GetState().ScrollWheelValue
            let mutable lastLeftHandPosition = kinect.GetState().RightHandPosition.Y

            member this.List
                with get() = list

            member this.Add garment=
                //list <- list @ [garment]
                list.Add garment

            override this.Initialize()=
                for x in list do
                    x.Initialize()
               //List.iter (fun (x:GarmentItem) -> x.Initialize()) list

            member this.Height
                with get() = float32((list.Count) * ((list.Item 0).Sprite.Height + 10))

            override this.Update(gameTime)=
                let dScrollWheel = (float32 (Mouse.GetState().ScrollWheelValue - lastMouseWheel))/10.0f
                let dKinectScroll = if kinect.GetState().LeftButton = ButtonState.Pressed then (kinect.GetState().LeftHandPosition.Y - lastLeftHandPosition) /10.0f else 0.0f
                let dPosition = new Vector2(0.0f, dScrollWheel+dKinectScroll)
                
                if (position.Y + dPosition.Y + this.Height >= float32(game.GraphicsDevice.Viewport.Height - 200) && (dScrollWheel < 0.0f || dKinectScroll < 0.0f)) || (position.Y + dPosition.Y <= 0.0f && (dScrollWheel > 0.0f || dKinectScroll > 0.0f)) then //check if the list can scroll any further 
                    position <- position + dPosition
                    for x in list do
                        x.Position <- x.Position + dPosition

                for x in list do
                    x.Update gameTime //always x.Update to capture clicks    
                base.Update(gameTime)
                lastMouseWheel <- Mouse.GetState().ScrollWheelValue
                lastLeftHandPosition <- kinect.GetState().RightHandPosition.Y

            override this.Draw(gameTime)=
                for x in list do
                    x.Draw(gameTime)

        [<AllowNullLiteral>] //allow null as a proper value
        type Image(game:Game, spriteName:string, pos:Vector2)=
            inherit DrawableGameComponent(game)

            let mutable spriteBatch = null
            let mutable sprite:Texture2D = null

            override this.LoadContent()=
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
                sprite <- game.Content.Load<Texture2D>(spriteName)
                base.LoadContent()
                
            override this.Draw(gameTime)=
                spriteBatch.Begin()
                spriteBatch.Draw(sprite, pos, Color.White)
                spriteBatch.End()
                base.Draw(gameTime)

        [<AllowNullLiteral>] //allow null as a proper value
        type GarmentImage(game:Game, garment:Store.Garment, pos:Vector2)=
            inherit DrawableGameComponent(game)

            let mutable spriteBatch = null
            let mutable sprite:Texture2D = null

            //called when the Async image download is complete
            let WebImageDownloadComplete (args:System.Net.DownloadDataCompletedEventArgs)= 
                try
                    let stream = new System.IO.MemoryStream(args.Result)
                    sprite <- Texture2D.FromStream(game.GraphicsDevice, stream)
                    //garmentImage.ChangeSprite(garmentLargeImageTex)
                with //if the image file doesn't exist on the server, use the 'no photo' image
                    | :? System.Reflection.TargetInvocationException as ex -> sprite <- game.Content.Load<Texture2D>("GarmentImageNotFound")
                    | ex -> ()

            override this.LoadContent()=
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
                base.LoadContent()
                sprite <- game.Content.Load<Texture2D>("GarmentImageLoading")
                let webClient = new System.Net.WebClient()
                let URI = new System.Uri("http://localhost/garment_images/"+garment.ID.ToString()+".jpg")
                webClient.DownloadDataCompleted.Add(WebImageDownloadComplete)
                async{
                    try
                        webClient.DownloadDataAsync(URI)
                    with
                        | ex -> printfn "%s" (ex.Message)
                    } |> Async.RunSynchronously

            override this.Draw(gameTime)=
                spriteBatch.Begin()
                spriteBatch.Draw(sprite, pos, Color.White)
                spriteBatch.End()
                base.Draw(gameTime)
        
        
        //A drawable button for easy access to previous users.
        //When clicked they should         
        type User(game, id:int, name:string, email, pos, kinect) as this=
            inherit Button(game, "UI/BlueButton600x150", "none", pos, kinect)

            let mutable leftClick = true
            let mutable kinectRightClick = true

            let mutable font:SpriteFont = null
            let mutable webTex:Texture2D = null

            let mutable textPosOffset= Vector2.Zero
            let mutable spriteCenter = Vector2.Zero

            let buttonClickedEvent = new Event<LoginEventArgs>()
            
//            let kinectClickHandler (args: System.Windows.Forms.MouseEventArgs) =
//                if args.X < (this.getBaseSprite.Bounds.Right + (int this.getBasePosition.X)) 
//                && args.X > (this.getBaseSprite.Bounds.Left  + (int this.getBasePosition.X)) then 
//                    if args.Y < (this.getBaseSprite.Bounds.Bottom  + (int this.getBasePosition.Y))
//                    && args.Y > (this.getBaseSprite.Bounds.Top + (int this.getBasePosition.Y)) then 
//                        buttonClickedEvent.Trigger(new LoginEventArgs(this, email))

            [<CLIEvent>]
            member this.Click = buttonClickedEvent.Publish

            member this.getBasePosition:Vector2 =
                base.Position
            member this.getBaseSprite:Texture2D =
                base.Sprite

            override this.Initialize()=
                //kinect.Click.Add(kinectClickHandler)
                this.LoadContent()

            override this.LoadContent()=
                webTex <-game.Content.Load<Texture2D>("noUserImage")
                try
                    let stream = new FileStream("userImages\\" + id.ToString() + ".jpg", FileMode.Open)
                    webTex <- Texture2D.FromStream(game.GraphicsDevice, stream)
                with
                    | :? System.IO.FileNotFoundException as ex ->  ()
                    | ex -> ()
                font <- game.Content.Load<SpriteFont>("garmentItemFont")
                textPosOffset <- Vector2.Divide(font.MeasureString(name), 2.0f)
                textPosOffset <- textPosOffset - new Vector2((Vector2.Divide(font.MeasureString(name), 2.0f)).X, 0.0f)
                base.LoadContent()
                spriteCenter <- new Vector2(120.0f, float32 base.Sprite.Bounds.Center.Y)

            override this.Update(gameTime)=
                if Mouse.GetState().LeftButton = ButtonState.Pressed && not leftClick then
                    if Mouse.GetState().X < base.Sprite.Bounds.Right + (int base.Position.X) 
                    && Mouse.GetState().X > base.Sprite.Bounds.Left  + (int base.Position.X) then 
                        if Mouse.GetState().Y < base.Sprite.Bounds.Bottom  + (int base.Position.Y)
                        && Mouse.GetState().Y > base.Sprite.Bounds.Top + (int base.Position.Y) then 
                            buttonClickedEvent.Trigger(new LoginEventArgs(this, email))
                if kinect.GetState().RightButton = ButtonState.Pressed && not kinectRightClick && not (kinect.GetState().LeftButton = ButtonState.Pressed) then
                    if kinect.GetState().RightHandPosition.X < float32 base.Sprite.Bounds.Right + (base.Position.X) 
                    && kinect.GetState().RightHandPosition.X > float32 base.Sprite.Bounds.Left  + (base.Position.X) then 
                        if kinect.GetState().RightHandPosition.Y < float32 base.Sprite.Bounds.Bottom  + (base.Position.Y)
                        && kinect.GetState().RightHandPosition.Y > float32 base.Sprite.Bounds.Top + (base.Position.Y) then 
                            buttonClickedEvent.Trigger(new LoginEventArgs(this, email))
                if Mouse.GetState().LeftButton = ButtonState.Released then
                    leftClick <- false
                if kinect.GetState().RightButton = ButtonState.Released then
                    kinectRightClick <- false

            override this.Draw(gameTime)=
                base.Draw(gameTime)
                base.SpriteBatch.Begin()
                base.SpriteBatch.Draw(webTex, base.Position, Color.White)
                base.SpriteBatch.DrawString(font, name, base.Position + spriteCenter, Color.White, 0.0f, textPosOffset, 1.0f, SpriteEffects.None, 0.5f)
                base.SpriteBatch.End()

            member this.Position
                with get() = base.Position
                and set(p) = base.Position <- p

        and LoginEventArgs(sender, email)= //arguments for loging in
            inherit System.EventArgs()
            member this.Sender = sender
            member this.Email = email

    module VisualisationAssets=
        
        type PersonModel(game, sex, height, chest, waist, hips)=
            inherit DrawableGameComponent(game)

            let mutable model:Model = null

            let mutable modelRotation = 0.0f
            let modelPosition = Vector3.Zero
            let focusPoint = modelPosition + new Vector3(0.0f, 10.0f, 0.0f)

            let mutable cameraPosition = new Vector3(0.0f, -20.0f, 10.0f)

            let mutable aspectRatio = 0.0f
            override this.Initialize()=
                base.Initialize()

            override this.LoadContent()=
                aspectRatio <- this.Game.GraphicsDevice.Viewport.AspectRatio
                model <- game.Content.Load<Model>("male")
                base.LoadContent()

            override this.Update(gameTime)=
                if Keyboard.GetState().IsKeyDown(Keys.Left)  then
                    modelRotation <- modelRotation + 0.1f
                if Keyboard.GetState().IsKeyDown(Keys.Right)  then
                    modelRotation <- modelRotation - 0.1f
                if Keyboard.GetState().IsKeyDown(Keys.Down)  then
                    cameraPosition.Y <- cameraPosition.Y - 0.1f
                if Keyboard.GetState().IsKeyDown(Keys.Up)  then
                    cameraPosition.Y <- cameraPosition.Y + 0.1f

            override this.Draw(gameTime)=
                // Copy any parent transforms.
                let (transforms:Matrix array) = Array.zeroCreate model.Bones.Count
                model.CopyAbsoluteBoneTransformsTo(transforms);
                this.Game.GraphicsDevice.BlendState <- BlendState.Opaque
                this.Game.GraphicsDevice.DepthStencilState <- DepthStencilState.Default
                // Draw the model. A model can have multiple meshes, so loop.
                for mesh in model.Meshes do
                    // This is where the mesh orientation is set, as well 
                    // as our camera and projection.
                    for e:Effect in mesh.Effects do
                        let effect = e :?> BasicEffect
                        effect.EnableDefaultLighting()
                        effect.World <- mesh.ParentBone.Transform *
                            Matrix.CreateRotationZ(modelRotation)
                            * Matrix.CreateTranslation(modelPosition)
                        effect.View <- Matrix.CreateLookAt(cameraPosition, 
                            focusPoint, Vector3.Up)
                        effect.Projection <- Matrix.CreatePerspectiveFieldOfView(
                            MathHelper.ToRadians(45.0f), aspectRatio, 
                            1.0f, 10000.0f)
                    // Draw the mesh, using the effects set above.
                    mesh.Draw();
                base.Draw(gameTime);
                
                

