using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Melee.Spears;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Specialist
{
    class DarkTrident : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<DarkTridentHeld>();
            Item.channel = true;

            Item.damage = 90;
            Item.width = 24;
            Item.height = 48;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.Shoot; // arm/body pose; ChargedSpearHeld drives the front arm itself (composite) while held
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 4f;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.Expert;
            Item.UseSound = SoundID.Item7;
            Item.shootSpeed = 24f;
            Item.channel = true;
        }

        // Every use needs a fresh press. Low stamina cuts the channel, which DarkTridentHeld treats as a release; with
        // auto-reuse (Speed Talisman, Challenger's Glove...) a held button would then fire stab after stab.
        public override bool? CanAutoReuseItem(Player player)
        {
            return false;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.GetTotalDamage(DamageClass.Ranged).ApplyTo(100) < player.GetTotalDamage(DamageClass.Melee).ApplyTo(100))
            {
                Item.DamageType = DamageClass.Melee;
            }
            else
            {
                Item.DamageType = DamageClass.Ranged;
            }

            // Start in the ready stance (level at the cursor, Shoot pose) and start charging. The release decides
            // stab or throw; the held trident swings itself overhead if held past the tap window (see ChargedSpearHeld).
            // Pose set here as well as in the projectile, or the first frame shows the raised arm.
            Item.useStyle = ItemUseStyleID.Shoot;
            int readyFacing = 1;
            if (velocity.X < 0)
            {
                readyFacing = -1;
            }
            player.ChangeDir(readyFacing);
            player.itemRotation = (velocity * player.direction).ToRotation();

            if (Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<DarkTridentHeld>(), damage, knockback, player.whoAmI, type);
            }
            return false;
        }
    }
}
