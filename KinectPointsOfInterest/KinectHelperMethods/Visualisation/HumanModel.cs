#region File Description
//-----------------------------------------------------------------------------
// SkinningSample.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkinnedModel;
#endregion

namespace Visualisation
{
    /// <summary>
    /// Sample game showing how to display skinned character animation.
    /// </summary>
    public class HumanModel : DrawableGameComponent
    {
        #region Fields

        Model currentModel;
        AnimationPlayer animationPlayer;

        float cameraArc = 0;
        float cameraRotation = 0;
        float cameraDistance = 100;

        Game game;
        String modelname;
        int startFrame;

        float rotation = 0.0f;

        #endregion

        #region Initialization


        public HumanModel(Game game, String modelname, int startFrame)
            :base(game)
        {
            this.game = game;
            this.modelname = modelname;
            this.startFrame = startFrame;
        }


        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load the model.
            currentModel = game.Content.Load<Model>("VisualisationModels/manrigged");

            // Look up our custom skinning information.
            SkinningData skinningData = currentModel.Tag as SkinningData;

            if (skinningData == null)
                throw new InvalidOperationException
                    ("This model does not contain a SkinningData tag.");

            // Create an animation player, and start decoding an animation clip.
            animationPlayer = new AnimationPlayer(skinningData);

            AnimationClip clip = skinningData.AnimationClips["Take 001"];

            animationPlayer.StartClip(clip, startFrame);
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Allows the game to run logic.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Left))//left and right arow keys rotate the model
            {
                rotation += (float) gameTime.ElapsedGameTime.TotalSeconds;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                rotation -= (float) gameTime.ElapsedGameTime.TotalSeconds;
            }

            //UpdateCamera(gameTime);
            animationPlayer.Update(Matrix.Identity, startFrame);
            base.Update(gameTime);
        }

        public void ChangeFrame(int frame)
        {
            startFrame = frame;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice device = game.GraphicsDevice;

            //device.Clear(Color.CornflowerBlue);

            Matrix[] bones = animationPlayer.GetSkinTransforms();

            // Compute camera matrices.
            Matrix view = Matrix.CreateTranslation(20, 0, -20) * 
                          Matrix.CreateRotationY(MathHelper.ToRadians(cameraRotation)) *
                          Matrix.CreateRotationX(MathHelper.ToRadians(cameraArc)) *
                          Matrix.CreateLookAt(new Vector3(0, 0, cameraDistance), 
                                              new Vector3(0, 0, 0), Vector3.Up);

            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                                                                    device.Viewport.AspectRatio,
                                                                    1,
                                                                    10000);
            this.Game.GraphicsDevice.BlendState = BlendState.Opaque;
            this.Game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            // Render the skinned mesh.
            foreach (ModelMesh mesh in currentModel.Meshes)
            {
                foreach (SkinnedEffect effect in mesh.Effects)
                {
                    effect.SetBoneTransforms(bones);
                    effect.World = Matrix.CreateRotationY(rotation);
                    
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();

                    effect.SpecularColor = new Vector3(0.25f);
                    effect.SpecularPower = 16;
                }

                mesh.Draw();
            }

            base.Draw(gameTime);
        }

        
        #endregion

        #region Handle Input


        /// <summary>
        /// Handles input for quitting the game.
        /// </summary>
        private void HandleInput()
        {
            //currentKeyboardState = Keyboard.GetState();
            //currentGamePadState = GamePad.GetState(PlayerIndex.One);

            //// Check for exit.
            //if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
            //    currentGamePadState.Buttons.Back == ButtonState.Pressed)
            //{
            //    Exit();
            //}
        }


        /// <summary>
        /// Handles camera input.
        /// </summary>
        private void UpdateCamera(GameTime gameTime)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            // Check for input to rotate the camera up and down around the model.
            //if (currentKeyboardState.IsKeyDown(Keys.Up) ||
            //    currentKeyboardState.IsKeyDown(Keys.W))
            //{
            //    cameraArc += time * 0.1f;
            //}
            
            //if (currentKeyboardState.IsKeyDown(Keys.Down) ||
            //    currentKeyboardState.IsKeyDown(Keys.S))
            //{
            //    cameraArc -= time * 0.1f;
            //}

            //cameraArc += currentGamePadState.ThumbSticks.Right.Y * time * 0.25f;

            //// Limit the arc movement.
            //if (cameraArc > 90.0f)
            //    cameraArc = 90.0f;
            //else if (cameraArc < -90.0f)
            //    cameraArc = -90.0f;

            //// Check for input to rotate the camera around the model.
            //if (currentKeyboardState.IsKeyDown(Keys.Right) ||
            //    currentKeyboardState.IsKeyDown(Keys.D))
            //{
            //    cameraRotation += time * 0.1f;
            //}

            //if (currentKeyboardState.IsKeyDown(Keys.Left) ||
            //    currentKeyboardState.IsKeyDown(Keys.A))
            //{
            //    cameraRotation -= time * 0.1f;
            //}

            //cameraRotation += currentGamePadState.ThumbSticks.Right.X * time * 0.25f;

            //// Check for input to zoom camera in and out.
            //if (currentKeyboardState.IsKeyDown(Keys.Z))
            //    cameraDistance += time * 0.25f;

            //if (currentKeyboardState.IsKeyDown(Keys.X))
            //    cameraDistance -= time * 0.25f;

            //cameraDistance += currentGamePadState.Triggers.Left * time * 0.5f;
            //cameraDistance -= currentGamePadState.Triggers.Right * time * 0.5f;

            //// Limit the camera distance.
            //if (cameraDistance > 500.0f)
            //    cameraDistance = 500.0f;
            //else if (cameraDistance < 10.0f)
            //    cameraDistance = 10.0f;

            //if (currentGamePadState.Buttons.RightStick == ButtonState.Pressed ||
            //    currentKeyboardState.IsKeyDown(Keys.R))
            //{
            //    cameraArc = 0;
            //    cameraRotation = 0;
            //    cameraDistance = 100;
            //}
        }


        #endregion
    }
}
