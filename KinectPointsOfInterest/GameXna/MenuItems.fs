namespace KinectPointsOfInterest

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

open EventControllers

    module MenuItems=
        [<AllowNullLiteral>] //allow null as a proper value
        type DrawableMenuItem(game, textureName, pos)=
            inherit DrawableGameComponent(game)

            let mutable sprite:Texture2D = null
            let mutable position:Vector2= pos
            let mutable spriteBatch = null

            override this.Initialize() =
                spriteBatch <- new SpriteBatch(game.GraphicsDevice)
                base.Initialize()
            
            override this.LoadContent() =
                sprite <- this.Game.Content.Load<Texture2D>(textureName)
                base.LoadContent()

            override this.Draw(gameTime)=
                spriteBatch.Begin()
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
        type Button(game, textureName, pos:Vector2)=
            inherit DrawableMenuItem(game, textureName, pos)


        [<AllowNullLiteral>] //allow null as a proper value
        type Cursor(game, pos:Vector2)=
            inherit DrawableMenuItem(game, "cursor", pos)


            override this.Update(gameTime) =
                let mouseState = Mouse.GetState()
                base.Position <- new Vector2(float32 mouseState.X, float32 mouseState.Y)
                base.Update(gameTime)

        [<AllowNullLiteral>] //allow null as a proper value
        type TextBox(game, textureName, pos:Vector2, event)=
            inherit DrawableMenuItem(game, textureName, pos)
            
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

        type GarmentItem(game:Game, name:string, pic, pos)=
            inherit DrawableMenuItem(game, "garment_bg", pos)

            let mutable font:SpriteFont = null

            override this.LoadContent()=
                font <- game.Content.Load<SpriteFont>("garmentItemFont")
                base.LoadContent()

            override this.Draw(gameTime)=
               base.Draw(gameTime)
               base.SpriteBatch.Begin()
               base.SpriteBatch.DrawString(font, name, base.Position, Color.White)
               base.SpriteBatch.End()

        and GarmentList(game:Game)= //A simple container to hold any garment items that need drawn as a list
            inherit DrawableGameComponent(game)

            let mutable list:List<GarmentItem> = []

            member this.Add garment=
                list <- list @ [garment]

            override this.Initialize()=
               List.iter (fun (x:GarmentItem) -> x.Initialize()) list

            //override this.LoadContent()=
               //List.iter (fun (x:GarmentItem) -> x. .LoadContent) list

            override this.Draw(gameTime)=
               List.iter (fun (x:GarmentItem) -> x.Draw(gameTime)) list


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
                //let transforms:Matrix[] = new Array(
                //model.CopyAbsoluteBoneTransformsTo(transforms);
                this.Game.GraphicsDevice.BlendState <- BlendState.Opaque
                this.Game.GraphicsDevice.DepthStencilState <- DepthStencilState.Default
                // Draw the model. A model can have multiple meshes, so loop.
                for mesh in model.Meshes do
                    // This is where the mesh orientation is set, as well 
                    // as our camera and projection.
                    for e:Effect in mesh.Effects do
                        let effect = e :?> BasicEffect
                        //effect.EnableDefaultLighting()
                        effect.World <-
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
                
                

