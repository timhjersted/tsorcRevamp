using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class ThermalRise : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Updraft");
            Description.SetDefault("Heat from the lasers is extending your flight!");

            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        //Refills wing/rocket boot flight time
        public override void Update(Player player, ref int buffIndex)
        {
            player.wingTime = 60;
            /*Vector2 dustPos = player.position;
            dustPos.Y += player.Hitbox.Height; //Spawn dust at their feet
            Dust.NewDustDirect(dustPos, player.width, 0, DustID.Torch, 0, 5).velocity.X = 0;

            dustPos = player.Center + Main.rand.NextVector2Circular(80, 80);
            Dust.NewDustDirect(dustPos, 0, 0, 259, 0, -5, Scale: 1).noGravity = true;
            player.wingTime = player.wingTimeMax;
            player.rocketTime = player.rocketTimeMax;
            player.buffTime[buffIndex] = 500;

            if (player.wingTime < player.wingTimeMax)
            {
                //player.wingTime += 5;
            }
            if (player.rocketTime < player.rocketTimeMax)
            {
                //player.rocketTime += 5;
            }*/
        }
    }
}
