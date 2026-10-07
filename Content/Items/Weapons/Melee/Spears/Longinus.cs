using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Melee.Spears;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears
{
    class Longinus : ModItem
    {
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<LonginusHeld>();
            Item.channel = true;
            Item.damage = 1200;
            Item.crit = 16;
            Item.width = 24;
            Item.height = 48;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot; // arm/body pose; ChargedSpearHeld drives the front arm itself (composite) while held
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 4f;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.UseSound = SoundID.Item7;
            Item.shootSpeed = 21f;
            Item.channel = true;
        }

        // Every use needs a fresh press. Low stamina cuts the channel, which LonginusHeld treats as a release; with
        // auto-reuse (Speed Talisman, Challenger's Glove...) a held button would then fire stab after stab.
        public override bool? CanAutoReuseItem(Player player)
        {
            return false;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Start in the ready stance (level at the cursor, Shoot pose) and start charging. The release decides
            // stab or throw; the held spear swings itself overhead if held past the tap window (see ChargedSpearHeld).
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
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<LonginusHeld>(), damage, knockback, player.whoAmI, type);
            }
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<GaeBolg>());
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 80000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
