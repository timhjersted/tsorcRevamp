using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Melee.Hammers;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Projectiles.Melee;
using tsorcRevamp.Projectiles.Melee.Axes;
using tsorcRevamp.Projectiles.Melee.Hammers;

namespace tsorcRevamp.Items.Weapons.Melee.Hammers
{
    class Doomhammer : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.rare = ItemRarityID.Pink;
            Item.damage = 120;
            Item.scale = 1f;
            Item.crit += 46;
            Item.width = 65;
            Item.height = 66;
            Item.knockBack = 8;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 38;
            Item.useTime = 38;
            Item.value = PriceByRarity.Pink_5;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shootSpeed = 12f;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<DoomhammerFireball>();
            Item.noMelee = false;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true; 
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
                instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DeepSkyBlue; 
            }
            else
            {
                Item.UseSound = SoundID.Item20;
                tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
                instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed; 
                
            }
            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2) 
            {
                if (Main.myPlayer == player.whoAmI)
                {
                    position += velocity * 3;
                    Projectile.NewProjectile(
                        source,
                        position,
                        velocity,
                        ModContent.ProjectileType<Projectiles.Bolt1Revamped>(),
                        (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), 
                        0.1f, 
                        player.whoAmI,
                        0,
                        1
                    );
                }

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].HasBuff(ModContent.BuffType<Buffs.BoltChainImmunity>()))
                    {
                        Main.npc[i].DelBuff(Main.npc[i].FindBuffIndex(ModContent.BuffType<Buffs.BoltChainImmunity>()));
                    }
                }
                return false;
            }
            else 
            {
                float[] angles = { -10f, 0f, 10f };
                int projDamage = (int)(player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage) * 0.66f); 

                foreach (float angle in angles)
                {
                    Vector2 rotatedVelocity = velocity.RotatedBy(MathHelper.ToRadians(angle));
                    Projectile.NewProjectile(
                        source,
                        position,
                        rotatedVelocity,
                        ModContent.ProjectileType<DoomhammerFireball>(),
                        projDamage,
                        knockback,
                        player.whoAmI
                    );
                }
                return false; 
            }
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (player.altFunctionUse == 2) 
            {
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Bolt2Bolt>(),
                    (int)(player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage) * 0.33f), 
                    player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack),
                    Main.myPlayer,
                    player.GetTotalCritChance(DamageClass.Melee) + Item.crit
                );
            }
            else 
            {
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ProjectileID.Flames,
                    (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), 
                    player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack),
                    Main.myPlayer,
                    player.GetTotalCritChance(DamageClass.Melee) + Item.crit
                );
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Mjolnir>());
            recipe.AddIngredient(ModContent.ItemType<MagmaTooth>());
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}