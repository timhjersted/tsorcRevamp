using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords {
    class Drakarthus : ModItem {


        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Right click to throw a dagger that\n" +
                               "sticks to enemies and tiles it hits\n" + 
                               "Right click again to teleport towards the\n" +
                               "dagger, destroying it and firing homing blades\n" +
                               $"Throwing a dagger costs {MANA_COST} mana");
        }

        const int MANA_COST = 100;
        public override void SetDefaults() {
            Item.rare = ModContent.RarityType<CDW_Drakarthus>();
            Item.damage = 182;
            Item.height = 71;
            Item.width = 71;
            Item.knockBack = 3f;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useAnimation = 13;
            Item.useTime = 13;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 1000000;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            Item.shootSpeed = 24f;
        }

        public override bool AltFunctionUse(Player player) {
            //only allow right clicking when there's already a dagger out
            //or when the player has enough mana to throw a new one
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Swords.DrakarthusDagger>()] != 0) {
                return true;
            }
            return player.statMana > MANA_COST;

        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (player.altFunctionUse != 2) //shoot Nothing
                return true;
            

            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Swords.DrakarthusDagger>()] == 0) {
                if (player.statMana <= MANA_COST) return false;
                player.statMana -= MANA_COST;
                player.manaRegenDelay = 220;
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<Projectiles.Swords.DrakarthusDagger>(), damage, knockback, player.whoAmI);
            }
            else {
                if (Main.myPlayer != player.whoAmI) return false;
                for (int i = 0; i < Main.maxProjectiles; i++) {
                    Projectile proj = Main.projectile[i];
                    if (!proj.active) continue;
                    if (proj.owner != player.whoAmI || proj.type != ModContent.ProjectileType<Projectiles.Swords.DrakarthusDagger>()) {
                        continue;
                    }
                    Vector2 teleportOffset = player.Center - proj.Center;
                    teleportOffset.Normalize();
                    teleportOffset *= 96;
                    for (int j = 0; j < 32; j++) {
                        Vector2 dir = Main.rand.NextVector2CircularEdge(32, 32);
                        Vector2 dustPos = proj.Center + dir;
                        Dust.NewDustPerfect(dustPos, DustID.GemRuby, Vector2.Zero, 200).noGravity = true;
                    }
                    Point tpDestination = (proj.Center + teleportOffset).ToTileCoordinates();
                    if (!WorldGen.SolidTile(tpDestination) && (Collision.CanHit(proj.Center, 1, 1, proj.Center + teleportOffset, 1, 1) || Collision.CanHitLine(proj.Center, 1, 1, proj.Center + teleportOffset, 1, 1) )) {
                        player.SafeTeleport(proj.Center + teleportOffset);
                    }
                    float randOffset = Main.rand.NextVector2CircularEdge(4, 4).ToRotation();
                    for (int j = 0; j < 6; j++) {
                        Vector2 shotDir = new Vector2(0, 12).RotatedBy(MathHelper.ToRadians(0 - (60f * j)) + randOffset);
                        Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, shotDir, ModContent.ProjectileType<Projectiles.Swords.DrakarthusDagger2>(), damage, knockback, player.whoAmI);
                    }
                    proj.Kill();
                    break;
                }
            }
            return false;
        }


        public override void MeleeEffects(Player player, Rectangle hitbox) {
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.GemRuby, player.velocity.X, player.velocity.Y, 100, default, .8f);
            Main.dust[dust].noGravity = true;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
    //Color for Dedicated Weapon
    class CDW_Drakarthus : ModRarity {
        public override Color RarityColor => new Color(238, 192, 63);
    }
}
