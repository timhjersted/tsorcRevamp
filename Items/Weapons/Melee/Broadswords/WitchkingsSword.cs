using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Summon;
using tsorcRevamp.Buffs.Summon.WhipDebuffs;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class WitchkingsSword : ModItem
    {
        public static int DebuffDuration = 5;
        public static int TagDuration = 4;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.expert = true;
            Item.damage = 337;
            Item.width = 100;
            Item.height = 100;
            Item.autoReuse = true;
            Item.knockBack = 8;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.value = PriceByRarity.Red_10;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().WitchPower)
            {
                Item.DamageType = DamageClass.SummonMeleeSpeed;
                Item.scale = player.whipRangeMultiplier;
            } else 
            { 
                Item.DamageType = DamageClass.Melee; 
                Item.scale = 1;
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
            if (player.GetModPlayer<tsorcRevampPlayer>().WitchPower) 
            {
                int buffSelection = Main.rand.Next(19 + 1);
                for (int i = 0; i < 2; i++)
                {
                    switch (buffSelection)
                    {
                        case 0:
                            {
                                target.AddBuff(ModContent.BuffType<LeatherWhipDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 1:
                            {
                                target.AddBuff(ModContent.BuffType<SnapthornDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.AddBuff(BuffID.ThornWhipPlayerBuff, (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
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
                                player.AddBuff(BuffID.CoolWhipPlayerBuff, (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 5:
                            {
                                target.AddBuff(ModContent.BuffType<DurendalDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.AddBuff(BuffID.SwordWhipPlayerBuff, (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 6:
                            {
                                target.AddBuff(ModContent.BuffType<DarkHarvestDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.AddBuff(BuffID.ScytheWhipPlayerBuff, (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
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
                                player.GetModPlayer<tsorcRevampPlayer>().SearingLashStacks = 4;
                                target.AddBuff(ModContent.BuffType<SearingLashDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 13:
                            {
                                target.AddBuff(ModContent.BuffType<CrystalNunchakuDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * 15 * 60));
                                break;
                            }
                        case 14:
                            {
                                target.AddBuff(ModContent.BuffType<PyrosulfateDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 15:
                            {
                                player.GetModPlayer<tsorcRevampPlayer>().NightsCrackerStacks = 4;
                                target.AddBuff(ModContent.BuffType<NightsCrackerDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.AddBuff(ModContent.BuffType<NightsCrackerBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 16:
                            {
                                player.AddBuff(ModContent.BuffType<PolarisLeashBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * 10 * 60));
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
                                player.GetModPlayer<tsorcRevampPlayer>().TerraFallStacks = 4;
                                target.AddBuff(ModContent.BuffType<TerraFallDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                player.AddBuff(ModContent.BuffType<TerraFallBuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                        case 19:
                            {
                                target.AddBuff(ModContent.BuffType<DetonationSignalDebuff>(), (int)(player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration * TagDuration * 60));
                                break;
                            }
                    }
                }
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.OnFire3, DebuffDuration * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.ShadowFlame, DebuffDuration * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.CursedInferno, DebuffDuration * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.BetsysCurse, DebuffDuration * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.Ichor, DebuffDuration * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(ModContent.BuffType<CrimsonBurn>(), DebuffDuration * 60, false);
            }
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(ModContent.BuffType<DarkInferno>(), DebuffDuration * 60, false);
            }
        }
        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }
    }
}
