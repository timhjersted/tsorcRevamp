using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Magic;

namespace tsorcRevamp.Items.Weapons.Magic
{
    ///<summary>
    ///The Holy Gigas's spell tome: four of its moves in one book.
    ///  • Cursor high tap:   Heavenly Spears — three golden spears form over the cursor and lance down
    ///  • Cursor high hold:  Halo Volley — suns gather in orbit around you and hunt nearby foes
    ///  • Cursor low tap:    Sun Pillar — a column of judgment light slams down at the cursor
    ///  • Cursor low hold:   Lightning of the First Sun — charge, then release a long golden sweep
    ///Routed through WrathOfGoldController (tap = released within 18 ticks, otherwise hold).
    ///Placeholder art: vanilla Golden Shower tome — swap the Texture override for a bespoke sprite.
    ///</summary>
    class WrathOfGold : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.GoldenShower;

        public override void SetDefaults()
        {
            Item.damage = 85;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 14;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.channel = true; //the controller watches player.channel to split tap vs hold
            Item.knockBack = 4.5f;
            Item.value = Item.sellPrice(gold: 12);
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = SoundID.Item29;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<WrathOfGoldController>();
            Item.shootSpeed = 1f;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<WrathOfGoldController>()] == 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int mode = Main.MouseWorld.Y > player.Bottom.Y ? 1 : 0;
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI, mode);
            return false;
        }
    }
}
