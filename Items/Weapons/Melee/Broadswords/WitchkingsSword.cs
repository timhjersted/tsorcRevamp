using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items.Weapons.Summon.Whips;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class WitchkingsSword : ModItem
    {
        public static int DebuffDuration = 3;
        public static int TagDuration = 2; 
        public override void SetStaticDefaults()
        {
            ItemID.Sets.BonusAttackSpeedMultiplier[Type] = 0.5f;
        }
        public override void SetDefaults()
        {
            Item.damage = 366;
            Item.width = 100;
            Item.height = 100;
            Item.knockBack = 8;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Red;
            Item.value = PriceByRarity.Red_10;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Main.DiscoColor;
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().WitchPower)
            {
                Item.DamageType = DamageClass.SummonMeleeSpeed;
                damage /= 3;
            }
            else
            {
                Item.DamageType = DamageClass.Melee;
            }
        }
        public override void ModifyItemScale(Player player, ref float scale)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().WitchPower)
            {
                scale = player.whipRangeMultiplier;
            }
            else
            {
                scale = 1;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().WitchPower)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Details", Language.GetTextValue("Mods.tsorcRevamp.Items.WitchkingsSword.Empowered")));
                }
            }
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int buffSelection = Main.rand.Next(6 + 1);
            for (int i = 0; i < 2; i++)
            {
                switch (buffSelection)
                {
                    case 0:
                        {
                            target.AddBuff(BuffID.OnFire3, DebuffDuration * 60, false);
                            break;
                        }
                    case 1:
                        {
                            target.AddBuff(BuffID.ShadowFlame, DebuffDuration * 60, false);
                            break;
                        }
                    case 2:
                        {
                            target.AddBuff(BuffID.CursedInferno, DebuffDuration * 60, false);
                            break;
                        }
                    case 3:
                        {
                            target.AddBuff(BuffID.BetsysCurse, DebuffDuration * 60, false);
                            break;
                        }
                    case 4:
                        {
                            target.AddBuff(BuffID.Ichor, DebuffDuration * 60, false);
                            break;
                        }
                    case 5:
                        {
                            target.AddBuff(ModContent.BuffType<CrimsonBurn>(), DebuffDuration * 60, false);
                            break;
                        }
                    case 6:
                        {
                            target.AddBuff(ModContent.BuffType<DarkInferno>(), DebuffDuration * 60, false);
                            break;
                        }
                }
                buffSelection = Main.rand.Next(6 + 1);
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().WitchPower)
            {
                int whipBuffSelection = Main.rand.Next(21 + 1);
                for (int i = 0; i < 2; i++)
                {
                    switch (whipBuffSelection)
                    {
                        case 0:
                            {
                                target.AddBuff(ModContent.BuffType<LeatherWhipDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 1:
                            {
                                if (!player.HasBuff(BuffID.ScytheWhipPlayerBuff) && !player.HasBuff(BuffID.SwordWhipPlayerBuff))
                                {
                                    target.AddBuff(ModContent.BuffType<SnapthornDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                    player.AddBuff(BuffID.ThornWhipPlayerBuff, (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                }
                                break;
                            }
                        case 2:
                            {
                                target.AddBuff(ModContent.BuffType<SpinalTapDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 3:
                            {
                                target.AddBuff(ModContent.BuffType<FirecrackerDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 4:
                            {
                                target.AddBuff(ModContent.BuffType<CoolWhipDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.AddBuff(BuffID.CoolWhipPlayerBuff, (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 5 * 60));
                                if (player.ownedProjectileCounts[ProjectileID.CoolWhipProj] == 0 && Main.myPlayer == player.whoAmI)
                                {
                                    Projectile Snowflake = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, Vector2.Zero, ProjectileID.CoolWhipProj, 15, 0, player.whoAmI, 1);
                                }
                                break;
                            }
                        case 5:
                            {
                                if (!player.HasBuff(BuffID.ThornWhipPlayerBuff) && !player.HasBuff(BuffID.ScytheWhipPlayerBuff))
                                {
                                    target.AddBuff(ModContent.BuffType<DurendalDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                    player.AddBuff(BuffID.SwordWhipPlayerBuff, (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                }
                                break;
                            }
                        case 6:
                            {
                                if (!player.HasBuff(BuffID.ThornWhipPlayerBuff) && !player.HasBuff(BuffID.SwordWhipPlayerBuff))
                                {
                                    target.AddBuff(ModContent.BuffType<DarkHarvestDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                    player.AddBuff(BuffID.ScytheWhipPlayerBuff, (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                }
                                break;
                            }
                        case 7:
                            {
                                target.AddBuff(ModContent.BuffType<MorningStarDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 8:
                            {
                                target.AddBuff(ModContent.BuffType<KaleidoscopeDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 9:
                            {
                                target.AddBuff(ModContent.BuffType<UrumiDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 10:
                            {
                                target.AddBuff(ModContent.BuffType<EnchantedWhipDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 11:
                            {
                                target.AddBuff(ModContent.BuffType<DominatrixDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 12:
                            {
                                target.AddBuff(ModContent.BuffType<SearingLashDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 13:
                            {
                                target.AddBuff(ModContent.BuffType<CrystalNunchakuDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * CrystalNunchaku.BuffDuration * 60));
                                break;
                            }
                        case 14:
                            {
                                target.AddBuff(ModContent.BuffType<PyrosulfateDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 15:
                            {
                                target.AddBuff(ModContent.BuffType<NightsCrackerDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.AddBuff(ModContent.BuffType<NightsCrackerBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 16:
                            {
                                player.AddBuff(ModContent.BuffType<PolarisLeashBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * 5 * 60));
                                break;
                            }
                        case 17:
                            {
                                target.AddBuff(ModContent.BuffType<DragoonLashDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.AddBuff(ModContent.BuffType<DragoonLashBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.GetModPlayer<tsorcRevampPlayer>().DragoonLashFireBreathTimer += 0.7f;
                                break;
                            }
                        case 18:
                            {
                                target.AddBuff(ModContent.BuffType<TerraFallDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.AddBuff(ModContent.BuffType<TerraFallBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 19:
                            {
                                target.AddBuff(ModContent.BuffType<DetonationSignalDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 20:
                            {
                                target.AddBuff(ModContent.BuffType<RustedChainDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 21:
                            {
                                target.AddBuff(ModContent.BuffType<PyromethaneDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                    }
                    whipBuffSelection = Main.rand.Next(21 + 1);
                }
            }
        }
        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Main.DiscoColor;
            Dust dust = Dust.NewDustDirect(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, DustID.WhiteTorch, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, Main.DiscoColor, 1.9f);
            dust.noGravity = true;
        }
    }
}
