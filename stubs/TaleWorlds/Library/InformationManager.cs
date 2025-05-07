namespace TaleWorlds.Library
{
    /// <summary>
    /// Stub implementation of InformationManager for displaying messages in-game
    /// </summary>
    public static class InformationManager
    {
        public static void DisplayMessage(InformationMessage message)
        {
            // Stub implementation - in real game would display a message on screen
            System.Console.WriteLine($"GAME MESSAGE: {message.Information}");
        }
    }

    /// <summary>
    /// Represents a message to be displayed in-game
    /// </summary>
    public class InformationMessage
    {
        public string Information { get; }

        public InformationMessage(string message)
        {
            Information = message;
        }
    }

    /// <summary>
    /// Standard 2D vector type
    /// </summary>
    public struct Vec2
    {
        public float x;
        public float y;

        public Vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vec2 Zero => new Vec2(0, 0);
    }
}