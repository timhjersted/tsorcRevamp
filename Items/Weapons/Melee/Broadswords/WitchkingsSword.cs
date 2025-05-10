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
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class WitchkingsSword : ModItem
    {
        public static int DebuffDuration = 3;
        public override void SetStaticDefaults()
        {
            //ItemID.Sets.BonusAttackSpeedMultiplier[Type] = 1f;
        }
        public override void SetDefaults()
        {
            Item.damage = 420;
            Item.width = 100;
            Item.height = 100;
            Item.knockBack = 8;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 24;
            Item.useTime = 24;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Main.DiscoColor;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
                int buffSelection = Main.rand.Next(8 + 1);
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
                    case 7:
                        {
                            target.AddBuff(BuffID.Daybreak, DebuffDuration * 60, false);
                            break;
                    }
                    case 8:
                        {
                            target.AddBuff(ModContent.BuffType<MorgulPoisoning>(), DebuffDuration * 60, false);
                            break;
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
