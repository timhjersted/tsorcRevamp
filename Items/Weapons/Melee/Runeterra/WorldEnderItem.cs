using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Steamworks;
using System.CodeDom;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Projectiles.Melee.Runeterra.WorldEnder;
using tsorcRevamp.Projectiles.Melee.Swords;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    public class WorldEnderItem : ModItem 
    { 
        public const float TimeWindow = 3;
        public const float FirstSwingCooldown = 4;
        public const float SecondSwingCooldown = 9;
        public const float ThirdSwingCooldown = 15;
        public const string SoundPath = "tsorcRevamp/Sounds/Runeterra/Melee/WorldEnder/";
        public const float SoundVolume = 0.7f;
        public int SwingSoundStyle = 1;
        public override void SetDefaults()
        {
            Item.width = 132;
            Item.height = 132;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.damage = 666;
            Item.knockBack = 20f;
            Item.rare = ItemRarityID.Red;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            Item.shootSpeed = 100f;
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.DarkRed;
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<WorldEnderCooldown>()))
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override float UseSpeedMultiplier(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                return 0.75f;
            }
            return base.UseSpeedMultiplier(player) * player.GetTotalAttackSpeed(DamageClass.Melee);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2) //shoot Nothing
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noMelee = false;
                switch (SwingSoundStyle)
                {
                    case 0:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing1") with { Volume = SoundVolume });
                            SwingSoundStyle++;
                            break;
                        }
                    case 1:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing2") with { Volume = SoundVolume });
                            SwingSoundStyle++;
                            break;
                        }
                    case 2:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing3") with { Volume = SoundVolume });
                            SwingSoundStyle = 0;
                            break;
                        }
                    default:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "Swing3") with { Volume = SoundVolume });
                            SwingSoundStyle = 0;
                            break;
                        }
                }
                return true;
            }
            Item.noMelee = true;
            switch (player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing)
            {
                case 1:
                    {
                        Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<WorldEnderSwordSwing1>(), damage, 0, Main.myPlayer);
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "Q1Cast") with { Volume = SoundVolume });
                        player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing++;
                        break;
                    }
                case 2:
                    {
                        Projectile.NewProjectile(source, position, velocity * 0.6f, ModContent.ProjectileType<WorldEnderSwordSwing2>(), damage, 0, Main.myPlayer);
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "Q2Cast") with { Volume = SoundVolume });
                        player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing++;
                        break;
                    }
                case 3:
                    {
                        Projectile.NewProjectile(source, position, velocity * 0.6f, ModContent.ProjectileType<WorldEnderSwordSwing3>(), damage, 0, Main.myPlayer);
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "Q3Cast") with { Volume = SoundVolume });
                        player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing = 1;
                        break;
                    }
            }
            return true;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int HitSoundStyle = Main.rand.Next(1, 4);
            switch (HitSoundStyle)
            {
                case 1:
                    {
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "SwingHit1") with { Volume = SoundVolume });
                        break;
                    }
                case 2:
                    {
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "SwingHit2") with { Volume = SoundVolume });
                        break;
                    }
                default:
                    {
                        SoundEngine.PlaySound(new SoundStyle(SoundPath + "SwingHit3") with { Volume = SoundVolume });
                        break;
                    }
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", LangUtils.GetTextValue("Items.WorldEnderItem.Details")));
                }
            }
            else
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Shift", LangUtils.GetTextValue("CommonItemTooltip.Details")));
                }
            }
        }
    }
}
