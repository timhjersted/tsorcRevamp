using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class ScoutsBoost : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs(ToxicShot.ScoutsBoostMoveSpeedMult, ToxicShot.ScoutsBoostStaminaRegenMult);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 1f + ToxicShot.ScoutsBoostMoveSpeedMult / 100f;
            // Additive on gainMult, not multiplicative on regenRate — see the convention on
            // tsorcRevampStaminaPlayer.staminaResourceGainMult. regenRate is a separate channel that sits
            // outside the additive gear pool.
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += ToxicShot.ScoutsBoostStaminaRegenMult / 100f;
        }
    }
}
