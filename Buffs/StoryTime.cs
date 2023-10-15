using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs
{
    public class StoryTime : ModBuff
    {
        int storyEffectTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player == Main.LocalPlayer)
            {
                // Clears incombat debuff near sign
                if (player.HasBuff(ModContent.BuffType<InCombat>()))
                {
                    player.ClearBuff(ModContent.BuffType<InCombat>());
                }

                player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff = true;
                storyEffectTimer++;

                // Reset if player moves
                if (player.velocity.X != 0 || player.velocity.Y != 0)
                {
                    storyEffectTimer = 0;
                }

                bool bossActive = false;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].boss)
                    {
                        bossActive = true;
                        break;
                    }
                }

                // Only heal when no bosses are alive, hp isn't full and the player is standing still
                if (!bossActive && player.statLife < player.statLifeMax2 && player.velocity == Vector2.Zero)
                {
                    // Wind up 1
                    if (storyEffectTimer > 0 && storyEffectTimer <= 60)
                    {
                        player.lifeRegen = player.statLifeMax2 / 80;
                        if (Main.rand.NextBool(8))
                        {
                            GenerateDusts(player, Main.rand.Next(50, 100) * 0.015f, 10f);
                        }
                    }


                    // Wind up 2
                    if (storyEffectTimer > 60 && storyEffectTimer <= 100)
                    {
                        player.lifeRegen = player.statLifeMax2 / 60;

                        if (Main.rand.NextBool(4))
                        {
                            GenerateDusts(player, Main.rand.Next(50, 100) * 0.025f, 15f);
                        }
                    }

                    // Wind up 3
                    if (storyEffectTimer > 100 && storyEffectTimer <= 140)
                    {
                        player.lifeRegen = player.statLifeMax2 / 30;

                        if (Main.rand.NextBool(2))
                        {
                            GenerateDusts(player, Main.rand.Next(50, 100) * 0.035f, 20f);
                        }
                    }

                    // Full effect
                    if (storyEffectTimer > 140)
                    {
                        player.lifeRegen = player.statLifeMax2 / 15;
                        GenerateDusts(player, Main.rand.Next(80, 95) * 0.043f, 25f);
                    }
                }
            }
        }

        private static void GenerateDusts(Player player, float scaleFactor, float scaleFactor2)
        {
            var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.FlameBurst, Alpha: 120);
            dust.noGravity = true;
            dust.velocity *= 2.75f;
            dust.fadeIn = 1.3f;

            var vectorOther = new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
            vectorOther.Normalize();
            vectorOther *= scaleFactor;
            dust.velocity = vectorOther;

            vectorOther.Normalize();
            vectorOther *= scaleFactor2;
            dust.position = player.Center - vectorOther;
        }
    }
}
