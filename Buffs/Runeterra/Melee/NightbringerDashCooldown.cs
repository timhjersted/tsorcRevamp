using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class NightbringerDashCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            var dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Torch, Scale: 3f);
            dust.noGravity = true;
            if (npc.buffTime[buffIndex] == 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/DashReady") with { Volume = 2f }, npc.Center);
            }
        }
    }
}
