using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee.Broadswords;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class SwordOfLordGwyn : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Broadswords/SwordOfGwyn";
        public const int DashManaCost = 28;
        public const int SpearManaCost = 22;
        public const int NovaManaCost = 45;

        public override void SetDefaults()
        {
            Item.damage = 315;
            Item.DamageType = DamageClass.Melee;
            Item.width = 128;
            Item.height = 128;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.knockBack = 9f;
            Item.crit = 8;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.Purple_11;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<SwordOfLordGwynController>();
            Item.shootSpeed = 1f;

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.OrangeRed;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<SwordOfLordGwynController>()] == 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int mode = Main.MouseWorld.Y > player.Bottom.Y ? 1 : 0;
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI, mode);
            return false;
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            int dust = Dust.NewDust(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.Torch, player.velocity.X * 0.2f + player.direction * 3f, player.velocity.Y * 0.2f, 80, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }
    }
}
