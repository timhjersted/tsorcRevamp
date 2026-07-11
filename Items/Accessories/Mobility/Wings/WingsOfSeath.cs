using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Accessories.Mobility.Wings
{
    [AutoloadEquip(EquipType.Wings)]
    [LegacyName("DragonWings")]
    public class WingsOfSeath : ModItem
    {
        public bool Slow = false;
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(999999999, 10f, 1.4f, true, 3, 3);
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 42;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.expert = true;

        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising,
                    ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 1.1f;
            ascentWhenRising = 0.35f;
            maxCanAscendMultiplier = 1.3f;
            maxAscentMultiplier = 3.6f;
            constantAscend = 0.2f;



            if (player.TryingToHoverDown && player.controlJump && player.wingTime > 0f && !player.merman)
            {
                player.velocity.Y = 0;
                ascentWhenFalling = 0;
                ascentWhenRising = 0;
                maxCanAscendMultiplier = 0;
                maxAscentMultiplier = 0;
                constantAscend = 0;
            }
        }
        public override void HorizontalWingSpeeds(Player player, ref float speed, ref float acceleration)
        {
            speed = 8f;
            acceleration = 0.45f;
            if (player.TryingToHoverDown && player.controlJump && player.wingTime > 0f && !player.merman)
            {
                speed = 9f;
                acceleration = 1f;
            }
            if (SoulsModeMobility.Enabled(player))
            {
                speed = SoulsModeMobility.WingsOfSeathFlightSpeed;
                acceleration = SoulsModeMobility.WingsOfSeathFlightAcceleration;
                if (player.TryingToHoverDown && player.controlJump && player.wingTime > 0f && !player.merman)
                {
                    speed = SoulsModeMobility.WingsOfSeathHoverFlightSpeed;
                    acceleration = SoulsModeMobility.WingsOfSeathHoverFlightAcceleration;
                }
            }
            SoulsModeMobility.ApplyFlightCap(player, ref speed, ref acceleration);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var WingsSpeedToggle = tsorcRevamp.WingsOfSeath.GetAssignedKeys();
            string WingsSpeedToggleString = WingsSpeedToggle.Count > 0 ? WingsSpeedToggle[0] : LangUtils.GetTextValue("Keybinds.Wings of Seath speed toggle.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.WingsOfSeath.Keybind1") + WingsSpeedToggleString + Language.GetTextValue("Mods.tsorcRevamp.Items.WingsOfSeath.Keybind2")));
            }
            if (SoulsModeMobility.Enabled(Main.LocalPlayer))
            {
                tooltips.Add(new TooltipLine(Mod, "SoulsModeMobilityLimit", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.SoulsModeMobilityLimitWingsOfSeath", SoulsModeMobility.WingsOfSeathRunSpeed, SoulsModeMobility.WingsOfSeathFlightSpeed, SoulsModeMobility.WingsOfSeathHoverFlightSpeed, SoulsModeMobility.WingsOfSeathFlightTime / 60)));
            }
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().supersonicLevel = SoulsModeMobility.WingsOfSeathLevel;
            if (player.TryingToHoverDown && player.controlJump && player.wingTime > 0f && !player.merman)
            {
                player.velocity.Y = 1f;
            }

            // Fall faster if player holds down
            if (player.TryingToHoverDown && !player.controlJump &&
                !ModContent.GetInstance<tsorcRevampConfig>().DisableModWingsFallControlDuringFlight)
            {
                player.gravity += 0.15f;
                player.maxFallSpeed += 15f;
            }

            bool restricted = false;
            if (player.mount.Active || player.vortexStealthActive)
            {
                restricted = true;
            }

            if (!restricted)
            {
                player.GetModPlayer<tsorcRevampPlayer>().supersonicLevel = SoulsModeMobility.SupersonicWingsLevel;

                // Fall faster if player holds down
                if (player.TryingToHoverDown && !player.controlJump &&
                    !ModContent.GetInstance<tsorcRevampConfig>().DisableModWingsFallControlDuringFlight)
                {
                    player.gravity += 0.15f;
                    player.maxFallSpeed += 15f;
                }


                /** W1K's original code
                if (player.controlLeft) {
                    if (player.velocity.X > -3) player.velocity.X -= (float)(player.moveSpeed - 1f) / 10;
                    if (player.velocity.X < -3 && player.velocity.X > -6 * player.moveSpeed) {
                        if (player.velocity.Y != 0) player.velocity.X -= 0.1f;
                        else player.velocity.X -= 0.2f;
                        player.velocity.X -= 0.02f + ((player.moveSpeed - 1f) / 10);
                    }
                }
                if (player.controlRight) {
                    if (player.velocity.X < 3) player.velocity.X += (float)(player.moveSpeed - 1f) / 10;
                    if (player.velocity.X > 3 && player.velocity.X < 6 * player.moveSpeed) {
                        if (player.velocity.Y != 0) player.velocity.X += 0.1f;
                        else player.velocity.X += 0.2f;
                        player.velocity.X += 0.02f + ((player.moveSpeed - 1f) / 10);
                    }
                }**/

                if (player.velocity.X > 6 || player.velocity.X < -6)
                {
                    player.waterWalk = true;
                    int sonicDust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 16, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, default, 2f);
                    Main.dust[sonicDust].noGravity = true;
                    Main.dust[sonicDust].noLight = false;

                }
            }

            if (SoulsModeMobility.Enabled(player))
            {
                player.wingTimeMax = SoulsModeMobility.WingsOfSeathFlightTime;
                if (player.wingTime > SoulsModeMobility.WingsOfSeathFlightTime)
                {
                    player.wingTime = SoulsModeMobility.WingsOfSeathFlightTime;
                }
                player.rocketTimeMax = SoulsModeMobility.WingsOfSeathFlightTime;
                if (player.rocketTime > SoulsModeMobility.WingsOfSeathFlightTime)
                {
                    player.rocketTime = SoulsModeMobility.WingsOfSeathFlightTime;
                }
            }
            else
            {
                player.wingTime = 999999999;
            }
            //player.GetWingStats(22);
            player.jumpBoost = true;
            player.jumpSpeedBoost = 1.4f;
            player.lavaImmune = true;
            player.fireWalk = true;
            player.noKnockback = true;
            player.buffImmune[BuffID.Darkness] = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.CursedInferno] = true;
            player.buffImmune[BuffID.Frostburn] = true;
            player.buffImmune[BuffID.Burning] = true;
            player.buffImmune[BuffID.Ichor] = true;
            player.buffImmune[BuffID.ShadowFlame] = true;
            player.nightVision = true;
            player.AddBuff(BuffID.Hunter, 1);

            if (Main.netMode != NetmodeID.MultiplayerClient && player.whoAmI == Main.myPlayer)
            {
                if (tsorcRevamp.WingsOfSeath.JustReleased)
                {
                    Slow = !Slow;
                }
                if (Slow)
                {
                    player.velocity.X *= 0.95f;
                    player.slowFall = true;
                }
            }

            if (Slow)
            {
                player.GetModPlayer<tsorcRevampPlayer>().SlowfallWingActive = true;
            }
        }
    }
}
