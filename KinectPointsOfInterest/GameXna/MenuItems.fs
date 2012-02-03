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
                    text <- text + args.Key.ToString()
            
            do game.Components.Add(new EventControllerComponent(game))
            let keyboard = ControllerFactory.GetKeyboard().WithCommonKeys()
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
                

