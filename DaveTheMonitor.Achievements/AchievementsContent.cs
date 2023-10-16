using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StudioForge.Engine;

namespace DaveTheMonitor.Achievements
{
    internal static class AchievementsContent
    {
        public static ContentManager Content { get; private set; }
        public static Effect Grayscale { get; private set; }

        internal static void LoadContent(string path)
        {
            Content = new ContentManager(CoreGlobals.Content.ServiceProvider, path);
            Grayscale = Content.Load<Effect>("GrayscalePS");
        }

        internal static void UnloadContent()
        {
            Content.Unload();
            Content.Dispose();
        }
    }
}
