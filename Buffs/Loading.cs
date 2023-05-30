using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class Loading : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (Collision.CanHit(player.Center, 1, 1, player.Center + new Vector2(0, 30), 1, 1) && Collision.CanHitLine(player.Center, 1, 1, player.Center + new Vector2(0, 30), 1, 1))
            {
                player.buffTime[buffIndex] = 15;
            }
            else
            {
                player.buffTime[buffIndex] = 0;
            }

            player.velocity = Vector2.Zero;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.velocity = Vector2.Zero;
        }
    }
}
