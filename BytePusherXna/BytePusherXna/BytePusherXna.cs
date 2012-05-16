using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using BytePusher;
using BytePusher.Driver;
using SD = System.Drawing;
using SDI = System.Drawing.Imaging;

/**
 * Author: Mark Middleton
 * XNA implementation of the BytePusherVM.  See http://esolangs.org/wiki/BytePusher
 * 
 * The BytePusher CPU emulation is done by the classes BytePusher.BytePusherVM.  The BytePusher IO is decoupled from the hardware 
 * by BytePusher.BytePusherIODriver The XNA IO implementation is provided by BytePusher.Driver.XnaBytePusherIODriver
 */
namespace BytePusherXna
{

    public class BytePusherXna : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        int screenWidth;
        int screenHeight;
        BytePusherVM vm;
        XnaBytePusherIODriver bytePusherIODriver;
        SpriteFont spriteFont;
        FrameRate fr = new FrameRate();
        string rom;
        bool hideInfo;
        bool hideHelp;
        int fileIndex = 0;
        Keys currentkey;
        bool keyPressed;
        bool paused;
        float freq = 60;

        public BytePusherXna()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = true;
            graphics.PreferredBackBufferHeight = 768;
            graphics.PreferredBackBufferWidth = 1024;
            Content.RootDirectory = "Content";
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / freq);
        }

        protected override void LoadContent()
        {
            // initialise BytePusher VN
            bytePusherIODriver = new XnaBytePusherIODriver(GraphicsDevice);
            vm = new BytePusherVM(bytePusherIODriver);

            // load first rom in <cwd>/roms
            var files = Directory.Exists("\\bytepusher\\roms") ? Directory.GetFiles("\\bytepusher\\roms", "*.BytePusher") : new String[0];
            Array.Sort(files);
            if (files.Length != 0)
            {
                rom = files[0];
                vm.load(rom);
            } 

            // Setup fonts and screen dimensions            
            spriteBatch = new SpriteBatch(GraphicsDevice);
            screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
            spriteFont = Content.Load<SpriteFont>("SpriteFont1");
        }

        /**
         * Update called every 60th second.  This seems to work fine up to 60fps.  
         */
        protected override void Update(GameTime gameTime)
        {
            fr.update("cpu", gameTime);  // update frame counters

            if (!paused)
            {
                vm.run();
            }
            handleSpecialKeys();            
            base.Update(gameTime);
        }

        /**
         * Called once per update(), however calls may be dropped.  
         */
        protected override void Draw(GameTime gameTime)
        {

            fr.update("gpu", gameTime);  // update frame counters
            graphics.GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            // render BytePusherVM disaply
            spriteBatch.Draw(bytePusherIODriver.texture, new Rectangle(0, 0, 256*3, 256*3), Color.White);
            // render frame info
            drawInfo();
            drawHelp();
            spriteBatch.End();

            base.Draw(gameTime);
        }

        // handle key presses
        void handleSpecialKeys()
        {
            if (keyPressed)
            {
                // if in here, then on the last update a key was pressed.  Need to wait until it released
                // before processing the next key press
                if ( Keyboard.GetState().IsKeyUp(currentkey))
                {
                    keyPressed = false;
                }
                return;
            }

            foreach (var k in Keyboard.GetState().GetPressedKeys())
            {
                // wait for key release
                currentkey = k;
                keyPressed = true;
                var files = Directory.Exists("\\bytepusher\\roms") ? Directory.GetFiles("\\bytepusher\\roms", "*.BytePusher") : new String[0];
                Array.Sort(files);

                switch( k )
                {
                    // get next ROM
                    case Keys.Right:
                        if ( files.Length != 0 )
                        {
                            if (fileIndex < files.Length - 1)
                            {
                                fileIndex++;
                            }
                            rom = files[fileIndex];
                            vm.load(rom);
                        }
                        break;

                    // get Previous rom
                    case Keys.Left:
                        if ( files.Length != 0)
                        {
                            if (fileIndex != 0)
                            {
                                fileIndex--;
                            }
                            rom = files[fileIndex]; 
                            vm.load(rom);
                        }
                        break;

                    // Hide info
                    case Keys.I:
                        hideInfo = hideInfo ? false : true;
                        break;

                    // toggle window mode
                    case Keys.W:
                        graphics.ToggleFullScreen();
                        if (!graphics.IsFullScreen)
                        {
                            graphics.PreferredBackBufferHeight = 256 * 3;
                            graphics.PreferredBackBufferWidth = 256 * 3;
                            graphics.ApplyChanges();
                        }
                        else
                        {
                            graphics.PreferredBackBufferHeight = 768;
                            graphics.PreferredBackBufferWidth = 1024;
                            graphics.ApplyChanges();
                        }
                        break;
                    // Pause
                    case Keys.P:
                        paused = paused ? false : true;
                        break;
                    // Increase frequency
                    case Keys.PageUp:
                        if (freq < 60)
                        {
                            freq++;
                        }
                        
                        this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / freq);
                        break;

                    // Decrease frequency
                    case Keys.PageDown:
                        if (freq != 1)
                        {
                            freq--;                            
                        }
                        this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / freq);
                        break;

                    // Reset frequency
                    case Keys.R:
                        freq = 60;
                        this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / freq);
                        break;
                    case Keys.H:
                        hideHelp = hideHelp ? false : true;
                        break;
                    // Exit
                    case Keys.Q: this.Exit(); 
                        break;
                }
            }
        }

        // display rom name, cpu and gpu frames per second
        void drawInfo()
        {
            if (hideInfo) return;
            string info = string.Format("rom: {0} \ncpu: {1} fps / {2} ips\ngpu: {3} fps\nfre: {4} fps", 
                rom, 
                fr.d["cpu"].frameRate, 65536 * fr.d["cpu"].frameRate,
                fr.d["gpu"].frameRate,
                freq
                );
            spriteBatch.DrawString(spriteFont, info, new Vector2(3, 3), Color.White);
        }

        void drawHelp()
        {
            if (hideHelp) return;
            string help = "BytePusher XNA by Mark Middleton (http://coder36.blogspot.co.uk)\n\n" +
                          "Left Arrow:  Load Previous Rom\n" +
                          "Right Arrow: Load Next Rom\n" +
                          "P:           Pause\n" +
                          "H:           Toggle Help\n" +
                          "I:           Toggle Info\n" +
                          "W:           Toggle Windowed mode\n" +
                          "Page Up:     Increase Frequency\n" +
                          "Page Down:   Decrease Frequency\n" +
                          "R:           Reset Frequency\n" +
                          "Q:           Quit\n" +
                          "ROMS folder: \\bytepusher\\roms\n\n" + 
                          "Full virtual machine specs can be found at http://esolangs.org/wiki/BytePusher";

            spriteBatch.DrawString(spriteFont, help, new Vector2(3, 90), Color.White);
        }
    }

    /**
     * Crude frame rate counter
     */
    public class FrameRate
    {
        public class FrameCounter
        {
            public TimeSpan elapsedTime = TimeSpan.Zero;
            public int frameCounter;
            public int frameRate;
        }

        public Dictionary<String, FrameCounter> d = new Dictionary<String, FrameCounter>();

        public void registerCounter(String name)
        {
            d.Add(name, new FrameCounter());
        }

        public void update(String name, GameTime gameTime)
        {
            if (!d.ContainsKey(name))
            {
                d[name] = new FrameCounter();
            }

            FrameCounter fc = d[name]; 
            fc.elapsedTime += gameTime.ElapsedGameTime;

            if (fc.elapsedTime > TimeSpan.FromSeconds(1))
            {
                fc.elapsedTime -= TimeSpan.FromSeconds(1);
                fc.frameRate = fc.frameCounter;
                fc.frameCounter = 1;
            }
            fc.frameCounter++;   
        }
    }

}
