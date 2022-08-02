/*
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Runeterra.Magic
{
    public class OoDItem1 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Orb of Deception");
            Tooltip.SetDefault("Throws a magic orb which will return to you after a certain distance" +
                "\nYou cannot throw more than one orb at a time" +
                "\nCan be channeled with mana for higher damage and speed" +
                "\nYou can recast with mana to force it to return early" +
                "\nA third recast returns the Orb instantly" +
                "\nThe orb deals more damage on the way back" +
                "\nEach hit gathers a stack of Essence Thief" +
                "\nUpon reaching 9 stacks, the next cast will have 10% lifesteal");

        }

        public override void SetDefaults()
        {

            Item.width = 24;
            Item.height = 28;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.useTurn = false;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.holdStyle = ItemHoldStyleID.HoldFront;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.damage = 40;
            Item.autoReuse = false;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.shootSpeed = 1f;
            Item.mana = 100;
            Item.DamageType = DamageClass.Magic;
        }
        public override void UpdateInventory(Player player)
        {
            if (Main.GameUpdateCount % 1 == 0)
            {
                OoDOrb1.holditemtimer1 -= 0.3f;
            }
        }
        public override void HoldItem(Player player)
        {
            bool OoDOrb1Exists = false;
            OoDOrb1.holditemtimer1 = 0.2f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<OoDOrb1>() && Main.projectile[i].owner == player.whoAmI)
                {
                    OoDOrb1Exists = true;
                    break;
                }
            }
            if (!OoDOrb1Exists)
            {
                Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<OoDOrb1>(), 0, 0, Main.myPlayer);
            }
        }
    }
}
*/