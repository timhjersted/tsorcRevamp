using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Loading : ModBuff
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Loading");
            // Description.SetDefault("You're being held in position while the world finishes loading!");
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
                player.velocity.X = 0;
            player.velocity.Y = 0;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.velocity.X = 0;
            npc.velocity.Y = 0;
        }
    }
}
