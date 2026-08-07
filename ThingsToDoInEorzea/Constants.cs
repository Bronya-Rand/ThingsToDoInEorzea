using System.Drawing;
using System.Numerics;
using Dalamud.Interface;

namespace ThingsToDoInEorzea
{
    public static class Constants
    {
        public const string PluginName = "Things To Do In Eorzea";

        // Blue Edge Color (#0a6992ff) [based on 
        public static readonly Vector4 BlueOutlineColor = new(0.039f, 0.412f, 0.573f, 1f);
        // Gold Edge Color (#8e6a0cff) [from 
        public static readonly Vector4 GoldOutlineColor = new(0.557f, 0.416f, 0.047f, 1f);
        // Green Edge Color
        public static readonly Vector4 GreenOutlineColor = KnownColor.DarkGreen.Vector();
        // Red Edge Color
        public static readonly Vector4 RedOutlineColor = KnownColor.DarkRed.Vector();

        public const float TitleTextSize = 17.3f;
        public const float ObjectiveTextSize = 13.3f;
        public const float ObjectiveSpacing = 1.5f;

        public const int MaxItemResults = 150;
        public const int MaxGilOnFreeTrial = 300_000;
    }
}
