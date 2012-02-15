namespace KinectPointsOfInterest

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

open EventControllers

open System.Collections

open System.Net
open System.IO

    module MenuItems=


        [<AllowNullLiteral>] //allow null as a proper value
        type DrawableMenuItem(game, textureName, shadowTextureName, pos)=
            inherit DrawableGameComponent(game)

            let mutable sprite:Texture2D = null
            let mutable shadow:Texture2D = null
            let mutable position:Vector2= pos
            let mutable spriteBatch = null

            override this.Initialize() =
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
                base.Initialize()
            
            override this.LoadContent() =
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

            member this.GetTextureBounds
                with get() = sprite.Bounds
            member this.Position
                with get() = position  
                and set(pos) = position <- pos
            member this.Sprite
                with get() = sprite
            member this.SpriteBatch
                with get() = spriteBatch
                
                 
        [<AllowNullLiteral>] //allow null as a proper value
        type Button(game, textureName, shadowName, pos:Vector2)=
            inherit DrawableMenuItem(game, textureName, shadowName, pos)

            let mutable leftClick = true
            let buttonClickedEvent = new Event<ButtonClickedEventArgs>()

            [<CLIEvent>]
            member this.Click = buttonClickedEvent.Publish

            override this.Update(gameTime)=
                let mouse = Mouse.GetState()
                if Mouse.GetState().LeftButton = ButtonState.Pressed && not leftClick then //if left button clicked
                    if mouse.X < (base.Sprite.Bounds.Right + (int base.Position.X)) 
                    && mouse.X > (base.Sprite.Bounds.Left  + (int base.Position.X)) then 
                        if mouse.Y < (base.Sprite.Bounds.Bottom  + (int base.Position.Y))
                        && mouse.Y > (base.Sprite.Bounds.Top + (int base.Position.Y)) then 
                            buttonClickedEvent.Trigger(new ButtonClickedEventArgs(this))
                if Mouse.GetState().LeftButton = ButtonState.Released then
                    leftClick <- false
                base.Update(gameTime)

        and ButtonClickedEventArgs(sender)=
            inherit System.EventArgs()
            member this.Sender = sender          
        
        [<AllowNullLiteral>] //allow null as a proper value
        type TextButton(game, textureName, shadowTextureName, label:string, pos:Vector2)=
            inherit Button(game, textureName, shadowTextureName, pos)

            let mutable font:SpriteFont = null
            

            let mutable textPosOffset = Vector2.Zero //the label's origin
            let mutable spriteCenter = Vector2.Zero

            override this.LoadContent()=
                font <- game.Content.Load<SpriteFont>("Font")
                textPosOffset <- Vector2.Divide(font.MeasureString(label), 2.0f)
                
                base.LoadContent()
                spriteCenter <- new Vector2(float32 base.Sprite.Bounds.Center.X, float32 base.Sprite.Bounds.Center.Y)

            override this.Draw(gameTime)=
               base.Draw(gameTime)
               base.SpriteBatch.Begin()
               base.SpriteBatch.DrawString(font, label, base.Position + spriteCenter, Color.White, 0.0f, textPosOffset, 1.0f, SpriteEffects.None, 0.5f)
               base.SpriteBatch.End()

        [<AllowNullLiteral>] //allow null as a proper value
        type Cursor(game, pos:Vector2)=
            inherit DrawableMenuItem(game, "cursor", "no_shadow", pos)

            override this.Update(gameTime) =
                let mouseState = Mouse.GetState()
                base.Position <- new Vector2(float32 mouseState.X, float32 mouseState.Y)
                base.Update(gameTime)

        [<AllowNullLiteral>] //allow null as a proper value
        type TextBox(game, textureName, shadowTextureName, pos:Vector2, event)=
            inherit DrawableMenuItem(game, textureName, shadowTextureName, pos)
            
            let mutable selected = false
            let mutable text = ""
            let mutable leftClick = false

            let mutable selectedTex = null
            let mutable font:SpriteFont = null

            let textPosOffset = new Vector2(20.0f, 20.0f)

            let deselectEvent:Event<_> = event
            
            let OnKeyPress (args:Keyboard.KeyboardArgs) =
                if selected then
                    let k = args.Key
                    match k with
                        | x when x.ToString().Equals("Space") -> text <- text + " "
                        | _ -> text <- text + args.Key.ToString()
            
            do game.Components.Add(new EventControllerComponent(game))
            let keyboard = ControllerFactory.GetKeyboard().WithAllKeys()
            do keyboard.OnClick.Add(fun args -> OnKeyPress args)

            member this.Deselect =
                selected <- false
            
            override this.LoadContent()=
                selectedTex <- game.Content.Load<Texture2D>("Sprite")
                font <- game.Content.Load<SpriteFont>("textBoxFont")
                base.LoadContent()

            override this.Update(gameTime)=
                let mouse = Mouse.GetState()
                if mouse.LeftButton = ButtonState.Pressed && not leftClick then
                    leftClick <- true
                    if mouse.X < int base.Position.X + base.Sprite.Bounds.Right && mouse.X > int base.Position.X + base.Sprite.Bounds.Left
                    && mouse.Y > int base.Position.Y + base.Sprite.Bounds.Top && mouse.Y < int base.Position.Y + base.Sprite.Bounds.Bottom then
                            deselectEvent.Trigger("none")
                            selected <- true
                if mouse.LeftButton = ButtonState.Released then
                    leftClick <- false

            override this.Draw(gameTime)=
               base.Draw(gameTime)
               base.SpriteBatch.Begin()
               if selected then
                   base.SpriteBatch.Draw(selectedTex, base.Position, Color.White)
               base.SpriteBatch.DrawString(font, text, base.Position + textPosOffset, Color.White)
               base.SpriteBatch.End()

        [<AllowNullLiteral>] //allow null as a proper value
        type Label(game, label:string, pos:Vector2)=
            inherit DrawableMenuItem(game, "no_texture", "no_shadow", pos)

            let mutable labelOrigin = Vector2.Zero
            let mutable font:SpriteFont = null

            override this.Initialize()=
                
                base.Initialize()

            override this.LoadContent()=
                font <- game.Content.Load<SpriteFont>("Font")
                labelOrigin <- Vector2.Divide(font.MeasureString(label), 2.0f)
                base.LoadContent()

            override this.Draw(gameTime)=
                base.Draw(gameTime)
                base.SpriteBatch.Begin()
                base.SpriteBatch.DrawString(font, label, base.Position, Color.White, 0.0f, labelOrigin, 1.0f, SpriteEffects.None, 0.5f)
                base.SpriteBatch.End()

        [<AllowNullLiteral>] //allow null as a proper value
        type GarmentItem(game:Game, garment:Store.Garment, pos)=
            inherit Button(game, "textbox", "textbox_shaddow", pos)

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
                webTex <- game.Content.Load<Texture2D>("GarmentThumbLoading")
                font <- game.Content.Load<SpriteFont>("garmentItemFont")
                textPosOffset <- Vector2.Divide(font.MeasureString(garment.Name), 2.0f)
                textPosOffset <- textPosOffset - new Vector2((Vector2.Divide(font.MeasureString(garment.Name), 2.0f)).X, 0.0f)
                base.LoadContent()
                spriteCenter <- new Vector2(120.0f, float32 base.Sprite.Bounds.Center.Y)
                let webClient = new WebClient()
                let URI = new System.Uri("http://localhost/garment_images/"+garment.ID.ToString()+"_thumb.jpg")
                webClient.DownloadDataCompleted.Add(WebImageDownloadComplete)
                async{
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
        
        type GarmentList(game:Game)= //A simple container to hold any garment items that need drawn as a list
            inherit DrawableGameComponent(game)
            
            //let mutable list:List<GarmentItem> = []
            let list = new System.Collections.Generic.List<GarmentItem>()
            
            let mutable position = Vector2.Zero
            let mutable lastMouseWheel = Mouse.GetState().ScrollWheelValue

            member this.List
                with get() = list

            member this.Add garment=
                //list <- list @ [garment]
                list.Add garment

            override this.Initialize()=
                for x in list do
                    x.Initialize()
               //List.iter (fun (x:GarmentItem) -> x.Initialize()) list

            override this.Update(gameTime)=
                position <- new Vector2(0.0f, (float32 (Mouse.GetState().ScrollWheelValue - lastMouseWheel))/10.0f)
                for x in list do
                    x.Position <- x.Position + position
                    x.Update gameTime
                base.Update(gameTime)
                lastMouseWheel <- Mouse.GetState().ScrollWheelValue

            override this.Draw(gameTime)=
                for x in list do
                    x.Draw(gameTime)

        [<AllowNullLiteral>] //allow null as a proper value
        type Image(game:Game, garment:Store.Garment, pos:Vector2)=
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
                base.LoadContent()
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
        type User(game, id:int, name:string, email, pos)=
            inherit Button(game, "textbox", "textbox_shaddow", pos)

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

            let buttonClickedEvent = new Event<LoginEventArgs>()
            [<CLIEvent>]
            member this.Click = buttonClickedEvent.Publish

            override this.LoadContent()=
                webTex <- game.Content.Load<Texture2D>("GarmentThumbLoading")
                font <- game.Content.Load<SpriteFont>("garmentItemFont")
                textPosOffset <- Vector2.Divide(font.MeasureString(name), 2.0f)
                textPosOffset <- textPosOffset - new Vector2((Vector2.Divide(font.MeasureString(name), 2.0f)).X, 0.0f)
                base.LoadContent()
                spriteCenter <- new Vector2(120.0f, float32 base.Sprite.Bounds.Center.Y)
                let webClient = new WebClient()
                let URI = new System.Uri("http://localhost/garment_images/"+id.ToString()+"_thumb.jpg")
                webClient.DownloadDataCompleted.Add(WebImageDownloadComplete)
                async{
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
                            buttonClickedEvent.Trigger(new LoginEventArgs(this, email))
                if Mouse.GetState().LeftButton = ButtonState.Released then
                    leftClick <- false

            override this.Draw(gameTime)=
                base.Draw(gameTime)
                base.SpriteBatch.Begin()
                base.SpriteBatch.Draw(webTex, base.Position, Color.White)
                base.SpriteBatch.DrawString(font, name, base.Position + spriteCenter, Color.White, 0.0f, textPosOffset, 1.0f, SpriteEffects.None, 0.5f)
                base.SpriteBatch.End()

            member this.Position
                with get() = base.Position
                and set(p) = base.Position <- p

        and LoginEventArgs(sender, email)=
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
                
                

