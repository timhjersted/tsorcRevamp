﻿using Microsoft.Xna.Framework;
using Steamworks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Runeterra.WorldEnder;
using tsorcRevamp.Projectiles.Melee.Swords;
using tsorcRevamp.Projectiles.Summon;

namespace tsorcRevamp.Items.Weapons.Summon
{
    [Autoload(false)]
    public class DarkSword : ModItem
    {
        public string SoundPath = "tsorcRevamp/Sounds/DST/DarkSword";
        public int SwingSoundStyle = 0;
        public float SoundVolume = 1f;
        public const float DebuffDuration = 8;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 58;
            Item.height = 72;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.damage = 68;
            Item.crit = 6;
            Item.mana = 10;
            Item.knockBack = 2f;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.Black;
        }
        public override void ModifyItemScale(Player player, ref float scale)
        {
            scale = player.whipRangeMultiplier;
        }
        public override bool AltFunctionUse(Player player)
        {
            if (Main.mouseRight && !Main.mouseLeft)
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                switch (SwingSoundStyle)
                {
                    case 0:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing1") with { Volume = SoundVolume * 18f });
                            SwingSoundStyle++;
                            break;
                        }
                    case 1:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing2") with { Volume = SoundVolume * 18f });
                            SwingSoundStyle++;
                            break;
                        }
                    case 2:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing3") with { Volume = SoundVolume * 18f });
                            SwingSoundStyle = 0;
                            break;
                        }
                    default:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing3") with { Volume = SoundVolume * 18f });
                            SwingSoundStyle = 0;
                            break;
                        }
                }
            }
            else
            {
                player.AddBuff(ModContent.BuffType<TerrorbeakBuff>(), 2);
                Projectile Terrorbeak = Projectile.NewProjectileDirect(source, position, velocity, ModContent.ProjectileType<TerrorbeakProjectile>(), damage, knockback);
                Terrorbeak.originalDamage = Item.damage;
                switch (SwingSoundStyle)
                {
                    case 0:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Summon1") with { Volume = SoundVolume });
                            SwingSoundStyle++;
                            break;
                        }
                    case 1:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Summon2") with { Volume = SoundVolume });
                            SwingSoundStyle = 0;
                            break;
                        }
                    default:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Summon2") with { Volume = SoundVolume });
                            SwingSoundStyle = 0;
                            break;
                        }
                }
            }
            return false;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Insanity>(), (int)(DebuffDuration * player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * 60f));
            player.MinionAttackTargetNPC = target.whoAmI;
        }
        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            Dust dust = Dust.NewDustDirect(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, DustID.WhiteTorch, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, Color.Black, 1.9f);
            dust.noGravity = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Humanity>(), 5);
            recipe.AddIngredient(ModContent.ItemType<LivingLog>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}