using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon
{
    public class ArcherStaff : ModItem {

        public override bool IsLoadingEnabled(Mod mod) => false;
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons a friendly archer to fight for you.");
        }

        public override void SetDefaults() {
            Item.damage = 100;
            Item.knockBack = 1f;
            Item.width = 44;
            Item.height = 50;
            Item.useTime = Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.buyPrice(0, 15, 0, 0);
            Item.mana = 10;
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = SoundID.Item44;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<Buffs.Summon.ArcherBuff>();
            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Archer.ArcherToken>();
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack) {
            player.AddBuff(Item.buffType, 2);
            int p = Projectile.NewProjectile(source, position, speed, type, damage, knockBack);
            Main.projectile[p].originalDamage = Item.damage;
            return true;
        }
    }
}