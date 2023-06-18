using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class ScoutsBoost2Omega : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.buffTime[buffIndex] == ToxicShot.ScoutsBoost2Duration * 60 - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/SuperBuffCast") with { Volume = 1f }, player.Center);
            }
            player.moveSpeed *= 1.4f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 1.2f;
            if (player.buffTime[buffIndex] == 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/SuperBuffEnd") with { Volume = 1f }, player.Center);
            }
        }
    }
}
