﻿using System;
using System.Collections.Generic;

using LDtk;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Examples
{
    public class LDtkExample : BaseExample
    {
        private const string LDTK_FILE = "Assets/LDtkMonogameExample.ldtk";

        // LDtk stuff
        private World world;
        private LevelManager levelManager;
        private readonly List<Entity> drawableEntities = new List<Entity>();
        private KeyboardState oldKeyboard;
        private MouseState oldMouse;

        // Entities
        Texture2D pixelTexture;
        Door[] doors;
        Crate[] crates;
        Player player;

        // Debug
        bool showCollisions = false;

        public LDtkExample() : base()
        {
            freeCam = false;
        }

        protected override void Initialize()
        {
            base.Initialize();

            world = new World(spriteBatch, LDTK_FILE);
            levelManager = new LevelManager(world);
            levelManager.SetStarterLevel("Level1");
            doors = levelManager.CurrentLevel.GetEntities<Door>();

            for (int i = 0; i < doors.Length; i++)
            {
                doors[i].trigger = new Rect(doors[i].position.X - 16, doors[i].position.Y - 32, 32, 32);
            }

            drawableEntities.AddRange(doors);

            crates = levelManager.CurrentLevel.GetEntities<Crate>();
            player = levelManager.CurrentLevel.GetEntity<Player>();

            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        }

        public override void OnWindowResized()
        {
            cameraOrigin = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
            cameraZoom = Math.Max(GraphicsDevice.Viewport.Height / 250, 1);
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();

            levelManager.SetCenterPoint(player.position);
            levelManager.Update(deltaTime);

            player.Update(keyboard, oldKeyboard, mouse, oldMouse, levelManager.CurrentLevel, deltaTime);
            player.inDoor = false;
            for (int i = 0; i < doors.Length; i++)
            {
                if (player.collider.Contains(doors[i].trigger))
                {
                    player.inDoor = true;
                    break;
                }
            }

            if (player.inDoor == false && player.doorInteraction)
            {
                player.doorInteraction = false;
                Console.WriteLine("doorInteraction");
            }

            if (keyboard.IsKeyDown(Keys.F1) && oldKeyboard.IsKeyDown(Keys.F1) == false)
            {
                showCollisions = !showCollisions;
            }

            if (freeCam == false)
            {
                cameraPosition = -new Vector2(player.position.X, player.position.Y - 30);
            }

            oldKeyboard = keyboard;
            oldMouse = mouse;

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            levelManager.Clear(GraphicsDevice);

            Matrix camera = Matrix.CreateTranslation(cameraPosition.X, cameraPosition.Y, 0) * Matrix.CreateScale(cameraZoom) * Matrix.CreateTranslation(cameraOrigin.X, cameraOrigin.Y, 0);

            spriteBatch.Begin(blendState: BlendState.NonPremultiplied, samplerState: SamplerState.PointClamp, transformMatrix: camera);
            {
                levelManager.Draw(spriteBatch);

                for (int i = 0; i < drawableEntities.Count; i++)
                {
                    spriteBatch.Draw(drawableEntities[i].texture,
                        drawableEntities[i].position,
                        drawableEntities[i].frame.Width > 0 ? drawableEntities[i].frame : new Rectangle(0, 0, (int)drawableEntities[i].size.X, (int)drawableEntities[i].size.Y),
                        Color.White,
                        0,
                        drawableEntities[i].pivot * drawableEntities[i].size,
                        1,
                        SpriteEffects.None,
                        0);
                }

                // Debugging
                if (showCollisions)
                {
                    for (int i = 0; i < player.tiles.Count; i++)
                    {
                        spriteBatch.DrawRect(player.tiles[i].rect, new Color(255, 0, 255, 128));
                    }
                }

                spriteBatch.Draw(player.texture,
                    player.position,
                    player.frame,
                    Color.White, 0,
                    (player.pivot * player.size) + new Vector2(player.fliped ? -8 : 8, -14), 1,
                    player.fliped ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0.1f);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}