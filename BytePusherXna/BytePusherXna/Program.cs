using System;

namespace BytePusherXna
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (BytePusherXna game = new BytePusherXna())
            {
                game.Run();
            }
        }
    }
#endif
}

