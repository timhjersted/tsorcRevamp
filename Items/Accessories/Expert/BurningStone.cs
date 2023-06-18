using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class BurningStone : ModItem
    {
        public static float DamageIncrease = 5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageIncrease);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.expert = true;
        }


        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "killcount", LanguageUtils.GetTextValue("Items.BurningStone.Count") + ((tsorcRevampWorld.NewSlain == null) ? 0 : tsorcRevampWorld.NewSlain.Count)));
            base.ModifyTooltips(tooltips);
        }
        public override void UpdateEquip(Player player)
        {
            tsorcRevampPlayer ModPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            ModPlayer.BurningStone = true;
            Lighting.AddLight(player.Center, Color.Orange.ToVector3());
            if (ModPlayer.isDodging)
            {
                if (Main.GameUpdateCount % 5 == 0)
                {
                    int? target = UsefulFunctions.GetClosestEnemyNPC(player.Center);
                    if (Main.netMode != NetmodeID.Server && player == Main.LocalPlayer)
                    {
                        if (target != null && Main.npc[target.Value].Distance(player.Center) < 1000)
                        {
                            Vector2 velocity = UsefulFunctions.Aim(player.Center, Main.npc[target.Value].Center, 10);
                            int damage = 1 + (tsorcRevampWorld.NewSlain.Count * 3);
                            if (tsorcRevampWorld.SuperHardMode)
                            {
                                damage *= 2;
                            }

                            for (int i = 0; i < 5; i++)
                            {
                                Dust.NewDustPerfect(player.Center, DustID.InfernoFork, player.velocity, 200, Scale: 0.65f).noGravity = true;
                            }
                            Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, velocity, ModContent.ProjectileType<Projectiles.HomingFireball>(), damage, 0.5f, player.whoAmI);
                        }
                    }
                }
            }
        }

        public override void UpdateVanity(Player player)
        {
            Vector2 dir = Main.rand.NextVector2CircularEdge(60, 60);
            Vector2 dustPos = player.Center + dir;
            Dust.NewDustPerfect(dustPos, DustID.InfernoFork, player.velocity, 200, Scale: 0.65f).noGravity = true;
        }
    }
}
