using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Content.Projectiles.Melee.Shortswords;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Shortswords.SerpentsFang
{
    public class SerpentsFangProj : ModdedShortswordProj
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(SerpentsFangItem));

        public override float HitboxWidth => 14f;
        public override float HitboxLength => 15f;

        public override int SpriteWidth => 54;
        public override int SpriteHeight => 54;

        private bool IsLunge => Projectile.ai[1] >= 0.5f;
        private int PerkTier => (int)Projectile.ai[2];
        private bool lungeBurstFired;

        public override int TotalDuration
        {
            get
            {
                if (IsLunge)
                {
                    return 22;
                }

                return 18;
            }
        }

        public override void Initialization(Player player)
        {
            base.Initialization(player);

            if (IsLunge)
            {
                // Slightly scale down per-step extension during the longer lunge so the hilt stays anchored in hand.
                Projectile.velocity *= 0.85f;
            }
        }

        public override void AI()
        {
            base.AI();

            if (Main.dedServ)
            {
                return;
            }

            Vector2 tipOffset = Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.spriteDirection) * 26f;
            Vector2 tipPosition = Projectile.Center + tipOffset;

            if (IsLunge)
            {
                Lighting.AddLight(tipPosition, 0.55f, 0.22f, 0.9f);
                Dust trail = Dust.NewDustPerfect(tipPosition, DustID.AncientLight, -Projectile.velocity * 0.4f, 50, new Color(195, 95, 255), 1.15f);
                trail.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    Dust venom = Dust.NewDustPerfect(tipPosition + Main.rand.NextVector2Circular(3f, 3f), DustID.Venom, -Projectile.velocity * 0.3f, 45, default, 1.0f);
                    venom.noGravity = true;
                }
            }
            else if (Main.rand.NextBool(2))
            {
                Dust glint = Dust.NewDustPerfect(tipPosition, DustID.Venom, -Projectile.velocity * 0.25f, 50, default, 0.9f);
                glint.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsLunge)
            {
                modifiers.SetCrit();
                if (PerkTier >= 2)
                {
                    modifiers.CritDamage += SerpentsFangItem.Tier6LungeCritBonusPercent / 100f;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsLunge || PerkTier >= 1)
            {
                target.AddBuff(BuffID.Venom, SerpentsFangItem.VenomDurationSeconds * 60);
            }
            else
            {
                target.AddBuff(BuffID.Poisoned, SerpentsFangItem.PoisonDurationSeconds * 60);
            }

            Player owner = Main.player[Projectile.owner];
            if (owner.whoAmI == Main.myPlayer)
            {
                // Immediately halt the forward step or lunge on hit so the player stops at blade tip instead of sliding into contact damage.
                owner.GetModPlayer<tsorcRevampPlayer>().HaltSerpentsFangDashOnEnemy(target);

                if (IsLunge && PerkTier >= 2 && !lungeBurstFired)
                {
                    lungeBurstFired = true;
                    Vector2 aimDir = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
                    int dropletDamage = (int)(Projectile.damage * (SerpentsFangItem.Tier6LungeDropletPercent / 100f));
                    if (dropletDamage < 1)
                    {
                        dropletDamage = 1;
                    }

                    // +6 Fanged Burst: erupt 3 melee-tagged Acid Venom droplets forward from the struck target.
                    for (int i = 0; i < SerpentsFangItem.Tier6LungeDropletCount; i++)
                    {
                        float spreadAngle = MathHelper.ToRadians(-16f + 16f * i);
                        Vector2 dropletVel = aimDir.RotatedBy(spreadAngle) * 7.8f + new Vector2(0f, -1.5f);
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            target.Center,
                            dropletVel,
                            ModContent.ProjectileType<SerpentVenomDropletProj>(),
                            dropletDamage,
                            1.5f,
                            Projectile.owner,
                            SerpentVenomDropletProj.MeleeDamageTag,
                            1f);
                    }
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            int burstCount = 6;
            if (IsLunge)
            {
                burstCount = 12;
            }

            for (int i = 0; i < burstCount; i++)
            {
                Vector2 sparkVel = Main.rand.NextVector2Circular(4.0f, 4.0f);
                Dust spark = Dust.NewDustPerfect(target.Center, DustID.Venom, sparkVel, 45, default, Main.rand.NextFloat(0.95f, 1.25f));
                spark.noGravity = true;
            }

            if (IsLunge)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 glowVel = Main.rand.NextVector2Circular(3.2f, 3.2f);
                    Dust glow = Dust.NewDustPerfect(target.Center, DustID.AncientLight, glowVel, 50, new Color(205, 110, 255), 1.15f);
                    glow.noGravity = true;
                }
            }
        }
    }
}
