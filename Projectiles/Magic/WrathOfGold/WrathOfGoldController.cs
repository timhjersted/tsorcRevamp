using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Wrath of Gold's held brain: watches whether the button is TAPPED (released within 18 ticks)
    ///or HELD, and routes to the right spell. ai[0] = 0 for left click (tap: heavenly spears at the
    ///cursor / hold: halo sun channel), 1 for right click (tap: sun pillar at the cursor / hold:
    ///charged golden sweep). Deals no damage itself — children inherit its damage.
    ///</summary>
    class WrathOfGoldController : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int TapWindow = 18;
        const int ChargeTicks = 50;    //right-hold: charge time after the window before the sweep
        const int HaloSpawnInterval = 15;
        const int HaloManaCost = 7;    //per sun spawned during the left-hold channel
        const int SweepManaCost = 25;  //surcharge on releasing the sweep
        const int MaxHaloSuns = 8;

        int Mode => (int)Projectile.ai[0]; //0 = left click, 1 = right click
        float Timer => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.hide = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = player.Center;
            Projectile.timeLeft = 60;
            Projectile.localAI[0]++;
            bool channeling = player.channel;

            if (channeling)
            {
                player.heldProj = Projectile.whoAmI;
                player.itemTime = 2;
                player.itemAnimation = 2;
                if (Main.myPlayer == Projectile.owner)
                {
                    player.ChangeDir(Main.MouseWorld.X > player.Center.X ? 1 : -1);
                }
            }

            if (Timer <= TapWindow)
            {
                if (!channeling)
                {
                    if (Main.myPlayer == Projectile.owner)
                    {
                        CastTap(player);
                    }
                    Projectile.Kill();
                }
                return;
            }

            if (Mode == 0)
            {
                RunHaloChannel(player, channeling);
            }
            else
            {
                RunSweepCharge(player, channeling);
            }
        }

        #region Tap spells

        void CastTap(Player player)
        {
            if (Mode == 0)
            {
                //Heavenly Spears: three golden spears form over the cursor and lance down onto it
                Vector2 cursor = Main.MouseWorld;
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = -0.2f }, player.Center);
                Span<Vector2> offsets = stackalloc Vector2[] { new Vector2(-60f, -170f), new Vector2(0f, -200f), new Vector2(60f, -170f) };
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = cursor + offsets[i];
                    for (int tries = 0; tries < 12 && IsSolidAt(pos); tries++)
                    {
                        pos.Y += 16f; //pull down out of ceilings toward the strike point
                    }
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, (cursor - pos).SafeNormalize(Vector2.UnitY),
                        ModContent.ProjectileType<TomeHeavenlySpear>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 18f + i * 8f);
                }
            }
            else
            {
                //Sun Pillar: a column of judgment light at the cursor's ground
                Vector2 cursor = Main.MouseWorld;
                float groundY = FindGroundY(cursor - new Vector2(0f, 16f), 25);
                if (groundY < 0f)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int dust = Dust.NewDust(cursor - new Vector2(8f, 8f), 16, 16, DustID.GoldFlame, 0f, 0f, 120, default, 1f);
                        Main.dust[dust].noGravity = true;
                    }
                    return;
                }
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.4f }, cursor);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(cursor.X, groundY - TomeSunPillar.PillarHeight / 2f), Vector2.Zero,
                    ModContent.ProjectileType<TomeSunPillar>(), (int)(Projectile.damage * 0.9f), Projectile.knockBack, Projectile.owner, 20f);
            }
        }

        #endregion

        #region Hold spells

        void RunHaloChannel(Player player, bool channeling)
        {
            if (!channeling)
            {
                Projectile.Kill();
                return;
            }
            //A sun joins the halo every 15 ticks while the channel holds
            if (Main.myPlayer == Projectile.owner && Timer % HaloSpawnInterval == 0
                && player.ownedProjectileCounts[ModContent.ProjectileType<TomeHaloSun>()] < MaxHaloSuns)
            {
                if (!player.CheckMana(HaloManaCost, true))
                {
                    Projectile.Kill();
                    return;
                }
                int slot = (int)(Timer / HaloSpawnInterval) % 6;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center + new Vector2(0f, -60f), Vector2.Zero,
                    ModContent.ProjectileType<TomeHaloSun>(), (int)(Projectile.damage * 0.9f), Projectile.knockBack * 0.5f, Projectile.owner, slot);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = 0.4f }, player.Center);
            }
            //Ambient rising sparkles while communing
            if (Main.rand.NextBool(3))
            {
                int sparkle = Dust.NewDust(player.position, player.width, player.height, DustID.GoldCoin, 0f, -1f, 0, default, 0.9f);
                Main.dust[sparkle].noGravity = true;
                Main.dust[sparkle].velocity *= 0.3f;
            }
        }

        void RunSweepCharge(Player player, bool channeling)
        {
            float charge = Timer - TapWindow;
            if (!channeling && charge < ChargeTicks)
            {
                for (int i = 0; i < 8; i++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, DustID.GoldFlame, 0f, -1f, 130, default, 1f);
                    Main.dust[dust].noGravity = true;
                }
                Projectile.Kill();
                return;
            }
            //Gold converging on the caster — the boss's channel language
            float progress = MathHelper.Min(1f, charge / ChargeTicks);
            int count = 1 + (int)(progress * 3f);
            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = MathHelper.Lerp(90f, 15f, progress) + Main.rand.NextFloat(12f);
                Vector2 pos = player.Center + angle.ToRotationVector2() * radius;
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (player.Center - pos) * 0.08f;
            }
            Lighting.AddLight(player.Center, 0.5f * progress, 0.42f * progress, 0.18f * progress);

            if (charge >= ChargeTicks)
            {
                if (Main.myPlayer == Projectile.owner && player.CheckMana(SweepManaCost, true))
                {
                    SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.4f, Pitch = 0.3f }, player.Center);
                    int dir = Main.MouseWorld.X > player.Center.X ? 1 : -1;
                    Vector2 beamCenter = new Vector2(player.Center.X + dir * (20f + TomeGoldSweep.BeamLength / 2f), player.Bottom.Y - TomeGoldSweep.BeamHeight / 2f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), beamCenter, Vector2.Zero,
                        ModContent.ProjectileType<TomeGoldSweep>(), (int)(Projectile.damage * 1.6f), Projectile.knockBack * 1.5f, Projectile.owner, dir);
                }
                Projectile.Kill();
            }
        }

        #endregion

        static bool IsSolidAt(Vector2 worldPos)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5 || ty < 5 || ty > Main.maxTilesY - 5)
            {
                return true;
            }
            Tile tile = Main.tile[tx, ty];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }

        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int d = 0; d <= maxTilesDown; d++)
            {
                int y = ty + d;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }
            return -1f;
        }
    }
}
