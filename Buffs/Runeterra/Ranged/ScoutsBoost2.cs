using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class ScoutsBoost2 : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs(ToxicShot.ScoutsBoostMoveSpeedMult * 2, ToxicShot.ScoutsBoostStaminaRegenMult * 2);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.buffTime[buffIndex] == ToxicShot.ScoutsBoost2Duration * 60 - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/SuperBuffCast") with { Volume = 1f }, player.Center);
            }
            player.moveSpeed *= 1f + ToxicShot.ScoutsBoostMoveSpeedMult * 2f / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 1f + ToxicShot.ScoutsBoostStaminaRegenMult * 2f / 100f;
            if (player.buffTime[buffIndex] == 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/SuperBuffEnd") with { Volume = 1f }, player.Center);
            }
        }
    }
}
