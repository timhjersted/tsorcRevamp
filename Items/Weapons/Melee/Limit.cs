using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class Limit : ModItem
    {

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.width = 24;
            Item.height = 24;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Melee;
            Item.channel = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.Limit>();
            Item.useAnimation = 40;
            Item.useTime = Item.useAnimation / 4;
            Item.shootSpeed = 0.08f;
            Item.damage = 151;
            Item.knockBack = 6.5f;
            Item.value = Item.sellPrice(0, 20);
            Item.crit = 10;
            Item.rare = ItemRarityID.Red;
            //item.glowMask = 271; was this actually trying to do something?
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            Vector2 mousePos = Main.MouseWorld;
            Vector2 playerToMouse = mousePos - player.Center;

            if (playerToMouse.Length() > 60f)
            {
                playerToMouse *= 60f / playerToMouse.Length();
                mousePos = player.Center + playerToMouse;
            }
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), mousePos, speed, ModContent.ProjectileType<Projectiles.Limit>(), Item.damage, Item.knockBack, Item.owner);
            return false;
        }
    }
}
