using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class SeveringDusk : ModItem
    {
        public const float DashBonusDmg = 50f; //weapon gains bonus dmg when buff is active from dash
        public const int BuffDuration = 5;
        public const float BaseStaminaCost = 30;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DashBonusDmg);

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Red;
            Item.damage = 200;
            Item.width = 78;
            Item.height = 78;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 12;
            Item.useTime = 12;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Red_10;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkMagenta;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                tsorcRevampStaminaPlayer playerStamina = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                float staminaCost = (BaseStaminaCost * modPlayer.WeaponStaminaMult) / player.GetWeaponAttackSpeed(player.HeldItem);
                if (playerStamina.staminaResourceCurrent > staminaCost && modPlayer.SeveringDuskDashTime < 1)
                {
                    playerStamina.staminaResourceCurrent -= staminaCost;
                    player.velocity = UsefulFunctions.Aim(player.Center, Main.MouseWorld, 30);
                    player.immuneTime = 30;
                    modPlayer.SeveringDuskDashTime = 20;
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI, 0f, 0f, 0f, 0);
                    }
                    modPlayer.effectRadius = 350;
                    player.AddBuff(ModContent.BuffType<SeveringDuskBuff>(), BuffDuration * 60);
                    playerStamina.PauseStaminaRegen(120);
                }
            }
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.HasBuff(ModContent.BuffType<SeveringDuskBuff>()))
            {
                damage *= 1f + DashBonusDmg / 100f;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string tooltip0 = "Tooltip0";
            int ttindex1 = tooltips.FindIndex(t => t.Text.Contains(tooltip0));
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                Player player = Main.LocalPlayer;
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                tsorcRevampStaminaPlayer playerStamina = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                float staminaCost = (BaseStaminaCost * modPlayer.WeaponStaminaMult) / player.GetWeaponAttackSpeed(player.HeldItem);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Tooltip0", Language.GetTextValue(Tooltip.Key + "0", (int)staminaCost)));
            }
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void HoldItem(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.SetAuraState(tsorcAuraState.Darkness);
            if (modPlayer.SeveringDuskDashTime > 0)
            {
                player.ResetMeleeHitCooldowns();
                player.immune = true;
                modPlayer.SeveringDuskDashTime--;
                if (modPlayer.SeveringDuskDashTime == 0)
                {
                    player.velocity *= 0.1f;
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI, 0f, 0f, 0f, 0);
                    }
                }
                /*for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.Distance(player.Center) < 70)
                    {
                        npc.StrikeNPC(npc.CalculateHitInfo((int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage * 4), 0, true, 0, DamageClass.Melee, true), false, true);
                    }
                }*/
            }
            base.HoldItem(player);
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
    }
}
