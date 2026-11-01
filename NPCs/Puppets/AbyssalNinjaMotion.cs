using Microsoft.Xna.Framework;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    // Pure authored pose functions shared with SwingPreview. World/path/landing decisions stay in the NPC.
    public static class AbyssalNinjaMotion
    {
        public const float High = -1.55f, Low = 2.36f;
        public static float BackCut(int tick) => SwingEase.ApplyWeighted(Low, High, tick, 40, 8, 32, 6);
        public static float BackSpin(int tick, int facing) => -facing * MathHelper.TwoPi
            * MathHelper.SmoothStep(0, 1, MathHelper.Clamp((tick - 15) / 24f, 0, 1));
        public static float RisingCut(int tick, int strikeAt)
        {
            // Counter-rotate the arm through the somersault: the blade cuts up/front instead
            // of facing behind the NPC for the entire interception. World arc is -100 to +60 degrees.
            float arc = MathHelper.SmoothStep(MathHelper.ToRadians(-100), MathHelper.ToRadians(60),
                MathHelper.Clamp((tick - strikeAt) / 10f, 0, 1));
            float pose = MathHelper.PiOver4 + arc - RisingSpin(tick, strikeAt, 1);
            return tick < strikeAt - 8 ? MathHelper.SmoothStep(Low, pose, MathHelper.Clamp(tick / (float)System.Math.Max(1, strikeAt - 8), 0, 1)) : pose;
        }
        public static float RisingSpin(int tick, int strikeAt, int facing) => tick > strikeAt + 18 ? 0
            : facing * MathHelper.TwoPi * MathHelper.SmoothStep(0, 1, MathHelper.Clamp((tick - strikeAt + 8) / 26f, 0, 1));
        public static float PassingCut(int tick, int strikeAt) => MathHelper.SmoothStep(High, 1.94f, MathHelper.Clamp((tick - strikeAt) / 10f, 0, 1));
        public static float FallingCut(int tick) => MathHelper.SmoothStep(High, Low, MathHelper.Clamp(tick / 10f, 0, 1));
    }
}
