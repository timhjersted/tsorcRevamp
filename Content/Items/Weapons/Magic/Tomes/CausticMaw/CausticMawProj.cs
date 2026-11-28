using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Tomes.CausticMaw
{
    public class CausticMawProj : ModProjectile
    {
        // ai[0] carries reinforcement perk tier: 0 = base, 1 = +3 Acid Venom & stronger pools, 2 = +6 direct-hit droplets & wider pools.
        private int PerkTier => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.26f;
            if (Projectile.velocity.Y > 15f)
            {
                Projectile.velocity.Y = 15f;
            }

            Projectile.rotation += 0.14f * Projectile.direction;

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, 0.55f, 0.2f, 0.85f);

            // Interpolate 2 motes per tick along the step so the arcing glob leaves a dense boiling purple trail.
            for (int i = 0; i < 2; i++)
            {
                float stepFraction = i * 0.5f;
                Vector2 samplePos = Vector2.Lerp(Projectile.oldPosition, Projectile.position, stepFraction) + Projectile.Size * 0.5f;
                Vector2 offset = Main.rand.NextVector2Circular(4f, 4f);
                Dust glow = Dust.NewDustPerfect(
                    samplePos + offset,
                    DustID.AncientLight,
                    -Projectile.velocity * 0.14f,
                    55,
                    new Color(180, 75, 255),
                    Main.rand.NextFloat(0.95f, 1.25f));
                glow.noGravity = true;
            }

            Dust venom = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                DustID.Venom,
                -Projectile.velocity * 0.1f + new Vector2(0f, -0.5f),
                45,
                default,
                Main.rand.NextFloat(0.9f, 1.15f));
            venom.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (PerkTier >= 1)
            {
                target.AddBuff(BuffID.Venom, 4 * 60);
            }
            else
            {
                target.AddBuff(BuffID.Poisoned, 4 * 60);
            }

            if (PerkTier >= 2 && Main.myPlayer == Projectile.owner)
            {
                int dropletDamage = (int)(Projectile.damage * (CausticMawItem.Tier6EruptionDropletPercent / 100f));
                if (dropletDamage < 1)
                {
                    dropletDamage = 1;
                }

                // +6 Maw Eruption: direct hits burst into 3 arcing magic Acid Venom droplets so airborne bosses take full value.
                for (int i = 0; i < CausticMawItem.Tier6EruptionDropletCount; i++)
                {
                    float horizontalSpread = -3.4f + 3.4f * i;
                    Vector2 dropletVel = new Vector2(horizontalSpread + Main.rand.NextFloat(-0.7f, 0.7f), -Main.rand.NextFloat(3.6f, 5.4f));
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        target.Center,
                        dropletVel,
                        ModContent.ProjectileType<SerpentVenomDropletProj>(),
                        dropletDamage,
                        1.5f,
                        Projectile.owner,
                        SerpentVenomDropletProj.MagicDamageTag,
                        1f);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath1 with { Volume = 0.45f, Pitch = 0.25f }, Projectile.Center);

                for (int i = 0; i < 10; i++)
                {
                    Vector2 burstVel = Main.rand.NextVector2Circular(4.2f, 3.6f) + new Vector2(0f, -1.2f);
                    Dust splash = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, burstVel, 45, default, Main.rand.NextFloat(1.0f, 1.35f));
                    splash.noGravity = true;
                }

                for (int i = 0; i < 6; i++)
                {
                    Vector2 glowVel = Main.rand.NextVector2Circular(3.2f, 2.8f) + new Vector2(0f, -0.8f);
                    Dust glow = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, glowVel, 55, new Color(185, 80, 255), Main.rand.NextFloat(0.95f, 1.25f));
                    glow.noGravity = true;
                }
            }

            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }

            // Scan downward up to 16 tiles to seat the acid puddle flush on the ground surface below the splash.
            Vector2 poolCenter = Projectile.Center;
            int startTileX = (int)(Projectile.Center.X / 16f);
            int startTileY = (int)(Projectile.Center.Y / 16f);

            for (int offsetY = 0; offsetY <= 16; offsetY++)
            {
                int checkY = startTileY + offsetY;
                if (!WorldGen.InWorld(startTileX, checkY, 2))
                {
                    break;
                }

                Tile tile = Framing.GetTileSafely(startTileX, checkY);
                bool isSolidSurface = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (isSolidSurface)
                {
                    float surfaceTopY = checkY * 16f;
                    poolCenter = new Vector2(Projectile.Center.X, surfaceTopY - CausticMawAcidPoolProj.PoolHeight * 0.5f);
                    break;
                }
            }

            float poolScale = PerkTier >= 2 ? CausticMawItem.Tier6PoolDamagePercent / 100f : 0.45f;
            int poolDamage = (int)(Projectile.damage * poolScale);
            if (poolDamage < 1)
            {
                poolDamage = 1;
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                poolCenter,
                Vector2.Zero,
                ModContent.ProjectileType<CausticMawAcidPoolProj>(),
                poolDamage,
                0f,
                Projectile.owner,
                Projectile.ai[0]);
        }
    }
}
