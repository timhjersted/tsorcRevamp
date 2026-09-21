using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    /// <summary>
    /// Player vulnerability after taking a hit while stamina is overdrawn. Kept separate from
    /// <see cref="ShieldGuardBreak"/> so its duration and penalties can be tuned independently.
    /// Shield raising is disabled by tsorcRevampActiveShieldPlayer while this buff is present.
    /// </summary>
    public class Stagger : CooldownDebuff
    {
        public const int DebtHitDurationTicks = 1 * 60;
        public const float MovementSpeedMultiplier = 0.75f;
        public const float ScreenShakeStrength = 1.25f;
        public const int ScreenShakeFrames = 6;

        public override bool PlaysSoundOnLastTick => false;

        public static void Apply(Player player) => Apply(player, DebtHitDurationTicks);

        /// <summary>
        /// Same stagger with an explicit duration, for sources that want a custom lockout duration.
        /// </summary>
        public static void Apply(Player player, int durationTicks)
        {
            player.AddBuff(ModContent.BuffType<Stagger>(), durationTicks);
            if (player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
            {
                UsefulFunctions.ScreenShake(player.Center, ScreenShakeStrength, ScreenShakeFrames,
                    distanceFalloff: 300f, uniqueIdentity: "PlayerStagger");
            }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            base.Update(player, ref buffIndex);
            // player.noItems = true;
            player.noKnockback = false;
            player.wingTime = 0f;
            player.canRocket = false;
            player.rocketTime = 0;
            player.moveSpeed *= MovementSpeedMultiplier;
            player.maxRunSpeed *= MovementSpeedMultiplier;
            player.runAcceleration *= MovementSpeedMultiplier;
        }
    }
}
