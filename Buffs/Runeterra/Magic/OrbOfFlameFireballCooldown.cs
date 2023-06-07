using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Magic
{
    public class OrbOfFlameFireballCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.buffTime[buffIndex] == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfFlame/FireballReady") with { Volume = 1f }, player.Center);
            }
        }
    }
}
