using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Pets
{
    class MiakodaFull : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Miakoda - Full Moon Form");
            Tooltip.SetDefault("Miakoda - an ancient being of light over 100 years old, " +
                                "\nwho has vowed to help you find your wife and defeat Attraidies." +
                                "\nMiakoda is an indigenous name that means \"Power of the Moon\"." +
                                "\nFull Moon Form - Will heal you when you get a crit" +
                                "\n(10 second cooldown - heal scales with max hp)" +
                                "\n+3% damage reduction" +
                                "\nCan switch between forms at an altar");

        }
        public override void SetDefaults()
        {
            item.CloneDefaults(ItemID.DD2PetGhost);
            item.shoot = ModContent.ProjectileType<Projectiles.Pets.MiakodaFull>();
            item.buffType = ModContent.BuffType<Buffs.MiakodaFull>();
        }

        public override void UseStyle(Player player)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
            {
                player.AddBuff(item.buffType, 3600, true);
            }
        }
    }
}
