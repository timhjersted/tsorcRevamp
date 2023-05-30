using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class Hold : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.velocity = Vector2.Zero;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.velocity = Vector2.Zero;
        }
    }
}
