using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Pets
{
    class RTQ2 : ModItem
    {

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DD2PetGhost);
            Item.shoot = ModContent.ProjectileType<Projectiles.Pets.RTQ2>();
            Item.buffType = ModContent.BuffType<Buffs.RTQ2>();
        }

        public override void UseStyle(Player player, Rectangle rectangle)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
            {
                player.AddBuff(Item.buffType, 3600, true);
            }
        }
    }
}
