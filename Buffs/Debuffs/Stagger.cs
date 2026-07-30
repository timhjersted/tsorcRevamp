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
        public const int DebtHitDurationTicks = 2 * 60;
        public const float StaminaRegenMultiplier = 0.2f;
        public const float MovementSpeedMultiplier = 0.75f;
        public const float ScreenShakeStrength = 1.25f;
        public const int ScreenShakeFrames = 6;

        public override bool PlaysSoundOnLastTick => false;

        public static void Apply(Player player)
        {
            player.AddBuff(ModContent.BuffType<Stagger>(), DebtHitDurationTicks);
            player.AddBuff(BuffID.Ichor, DebtHitDurationTicks);
            if (player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
            {
                UsefulFunctions.ScreenShake(player.Center, ScreenShakeStrength, ScreenShakeFrames,
                    distanceFalloff: 300f, uniqueIdentity: "PlayerStagger");
            }
        }

        public override void PlayerCustomUpdate(Player player, ref int buffIndex)
        {
            player.noItems = true;
            player.wingTime = 0f;
            player.canRocket = false;
            player.rocketTime = 0;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= StaminaRegenMultiplier;
            player.moveSpeed *= MovementSpeedMultiplier;
            player.maxRunSpeed *= MovementSpeedMultiplier;
            player.runAcceleration *= MovementSpeedMultiplier;
        }
    }
}
