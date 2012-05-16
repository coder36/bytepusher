using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

/**
 * See: http://esolangs.org/wiki/BytePusher
 * 
 * BytePusher is a minimalist vitual machine:
 * Framerate: 60 frmaes per second
 * CPU:         ByteByteJunp with 3 byte addresses
 * CPU Speed:   65536 instructions per frame (3932160 instructions per second. ~3.93 MHz)
 * Memory:      16Mb RAM
 * Graphics:    256*256 pixels, 1 byte per pixel, 216 fixed colors
 * Sound:       8-bit mono, signed values, 256 samples per frame (15360 samples per second)
 *
 * Usage:
 * var ioDrvier = new MyDriver() : BytePusherIODriver
 * var vm = new BytePusher();
 * every 60th of a second {
 *   vm.run()
 * }
 * 
 * Author: Mark Middleton
 */
namespace BytePusher
{

    public class BytePusherVM
    {

        byte[] mem = new byte[0xFFFFFF];
        BytePusherIODriver ioDriver;

        public BytePusherVM(BytePusherIODriver ioDriver)
        {
            this.ioDriver = ioDriver;
        }

        // load rom into memory
        public void load(string rom)
        {
            mem = new byte[0xFFFFFF];
            using (FileStream fs = new FileStream(rom, FileMode.Open))
            {
                int pc = 0;
                int i = 0;
                while ((i = fs.ReadByte()) != -1)
                {
                    mem[pc++] = (byte)i;
                }
            }
        }

        public void run()
        {
                // run 65536 instructions
                var s = ioDriver.getKeyPress();
                mem[0] = (byte) ((s & 0xFF00) >> 8);
                mem[1] = (byte)(s & 0xFF);
                var i = 0x10000;
                var pc = getVal(2, 3);
                while (i-- != 0)
                {
                    mem[getVal(pc + 3, 3)] = mem[getVal(pc, 3)];
                    pc = getVal(pc + 6, 3);
                }
                ioDriver.renderAudioFrame(copy(getVal(6, 2) << 8, 256));
                ioDriver.renderDisplayFrame(copy(getVal(5, 1) << 16, 256 * 256));
        }

        int getVal(int pc, int length, int t = 0)
        {
            var v = 0;
            for (var i = 0; i < length; i++)
            {
                v = (v << 8) + mem[pc++];
            }
            return v;
        }

        byte[] copy(int start, int length)
        {
            var b = new byte[length];
            Array.Copy(mem, start, b, 0, length);
            return b;
        }

    }

    /**
     * Interface which is called by the BytePusherVM to process Audio, Graphics and keyboard input
     */
    public interface BytePusherIODriver
    {
        /**
         * Get the current pressed key (0-9 A-F
         */
        ushort getKeyPress();

        /**
         * Render 256 bytes of audio 
         */
        void renderAudioFrame(byte[] data);

        /**
         * Render 256*256 pixels.  
         */
        void renderDisplayFrame(byte[] data);
    }
}

