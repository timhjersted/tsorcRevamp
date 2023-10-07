using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class NightbringerThrustCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.buffTime[buffIndex] == 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/ThrustReady") with { Volume = 0.75f });
            }
        }
    }
}
