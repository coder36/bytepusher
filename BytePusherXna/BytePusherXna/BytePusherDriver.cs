using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BytePusher;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using SD = System.Drawing;
using SDI = System.Drawing.Imaging;


/**
 * BytePusher drivers
 * Author: Mark Middleton
 */
namespace BytePusher
{
    namespace Driver
    {
        /**
          * XNA implementation of BytePusherIODriver  
          */
        public class XnaBytePusherIODriver : BytePusherIODriver
        {
            public Texture2D texture { get; set; }
            GraphicsDevice gd;
            DynamicSoundEffectInstance soundEffect;

            public XnaBytePusherIODriver(GraphicsDevice gd)
            {
                this.gd = gd;
                // start audio device
                soundEffect = new DynamicSoundEffectInstance(15360, AudioChannels.Mono);
                soundEffect.Play();
            }

            public void renderAudioFrame(byte[] data)
            {
                // convert to 16bit data
                var audio = new byte[data.Length * 2];
                var z = 0;
                foreach (var d in data)
                {
                    audio[z++] = 0;
                    audio[z++] = d;
                }
                // submit audio buffer
                soundEffect.SubmitBuffer(audio);
            }

            public void renderDisplayFrame(byte[] data)
            {
                // convert 8 bit data to 24 bit RGB            
                var rgbuffer = new byte[256 * 256 * 3];
                var z = 0;

                foreach (var c in data)
                {
                    if (c > 215)
                    {
                        z = z + 3;
                    }
                    else
                    {
                        int blue = c % 6;
                        int green = ((c - blue) / 6) % 6;
                        int red = ((c - blue - (6 * green)) / 36) % 6;

                        rgbuffer[z++] = (byte)(blue * 0x33);
                        rgbuffer[z++] = (byte)(green * 0x33);
                        rgbuffer[z++] = (byte)(red * 0x33);
                    }
                }

                // Gerate bitmap.
                var b = new SD.Bitmap(256, 256, SDI.PixelFormat.Format24bppRgb);
                var rect = new SD.Rectangle(0, 0, 256, 256);
                var bitmapdata = b.LockBits(rect, SDI.ImageLockMode.WriteOnly, b.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(rgbuffer, 0, bitmapdata.Scan0, 256 * 256 * 3);
                b.UnlockBits(bitmapdata);

                // convert to png (which xna can use)
                using (var ms = new MemoryStream())
                {
                    b.Save(ms, SDI.ImageFormat.Png);
                    // create texture object which can then be rendered in the next Draw() call
                    texture = Texture2D.FromStream(gd, new MemoryStream(ms.GetBuffer()));
                }
            }

            public ushort getKeyPress()
            {
                ushort key = 0;
                foreach (var k in Keyboard.GetState().GetPressedKeys())
                {
                    switch (k)
                    {
                        case Keys.D0: key += 1; break;
                        case Keys.D1: key += 2; break;
                        case Keys.D2: key += 4; break;
                        case Keys.D3: key += 8; break;
                        case Keys.D4: key += 16; break;
                        case Keys.D5: key += 32; break;
                        case Keys.D6: key += 64; break;
                        case Keys.D7: key += 128; break;
                        case Keys.D8: key += 256; break;
                        case Keys.D9: key += 512; break;
                        case Keys.A: key += 1024; break;
                        case Keys.B: key += 2048; break;
                        case Keys.C: key += 4096; break;
                        case Keys.D: key += 8192; break;
                        case Keys.E: key += 16384; break;
                        case Keys.F: key += 32768; break;
                    }
                }

                return key;
            }

        }
    }
}
