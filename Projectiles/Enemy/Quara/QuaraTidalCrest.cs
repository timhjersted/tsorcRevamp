using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///The Hydromancer's tidal crest: a ~3-tile-tall breaking wave sweeping along the ground.
    ///Layered rendering: Omnir's 4-frame tsunami sprite (QuaraTidalCrest.png, base art travels
    ///right — leftward waves just flip, the old Reverse texture is redundant) drawn translucent
    ///UNDERNEATH the dust sculpture (crest curl, liquid body, foam, wet trail) that sold the wave
    ///on its own. Set DrawWaveSprite = false to go back to pure dust — everything else is intact.
    ///Ground-hugging (steps 2 up / 4 down), dies at walls and pits. Jump it or roll through.
    ///Soaks (Wet) + Chilled on hit. ai[0] = direction (-1/1).
    ///</summary>
    class QuaraTidalCrest : ModProjectile
    {
        //File lives in the Quara/ subfolder but the namespace (tsorcRevamp.Projectiles.Enemy, matching every
        //other file in this folder) doesn't -- default texture lookup follows the namespace, not the folder,
        //so it needs pointing at the actual .png location explicitly.
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Quara/QuaraTidalCrest";

        static readonly bool DrawWaveSprite = true; //false = the pure dust-sculpture wave
        const float SpriteScale = 0.42f;  //194px art vs a 40px hitbox — generous visual, tight hitbox

        const float WaveSpeed = 5f;
        // Matches EnemyVFX's 104px QuaraWaterBurst quad. This is a debuff-only release: the wave
        // already owns the damaging travel lane, while the splash makes its final water mass matter.
        const float ExitSplashRadius = 52f;
        const int ExitSplashBuffTime = 6 * 60;

        int Direction => (int)Projectile.ai[0] >= 0 ? 1 : -1;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 40;
            Projectile.height = 44;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 60; //~19 tiles of travel
            Projectile.light = 0.25f;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            //Rolling animation (loops, unlike the original one-shot)
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
            Projectile.position.X += Direction * WaveSpeed;
            if (!SnapToGround())
            {
                Projectile.Kill();
                return;
            }

            float front = Direction > 0 ? Projectile.position.X + Projectile.width : Projectile.position.X;
            //The curl: crest dust arcing forward and DOWN off the top-front — the breaking-wave read
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(front - Direction * Main.rand.NextFloat(10f), Projectile.position.Y + Main.rand.NextFloat(10f));
                int crest = Dust.NewDust(pos, 4, 4, DustID.Water, 0f, 0f, 40, default, Main.rand.NextFloat(1.3f, 1.8f));
                Main.dust[crest].noGravity = true;
                Main.dust[crest].velocity = new Vector2(Direction * Main.rand.NextFloat(2.5f, 4f), Main.rand.NextFloat(1f, 3f));
            }
            //The body: liquid mass drifting slightly backward, so the wave rolls THROUGH the water
            for (int i = 0; i < 4; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int body = Dust.NewDust(pos, 4, 4, DustID.Water, 0f, 0f, 60, default, Main.rand.NextFloat(1.1f, 1.5f));
                Main.dust[body].noGravity = true;
                Main.dust[body].velocity = new Vector2(-Direction * 0.6f, Main.rand.NextFloat(-0.5f, 0.5f));
            }
            //Foam flecks off the crest + the wet trail left behind
            if (Main.rand.NextBool(2))
            {
                int foam = Dust.NewDust(new Vector2(front - Direction * 6f, Projectile.position.Y), 6, 8, DustID.Cloud, Direction * 2f, -2f, 120, default, 0.9f);
                Main.dust[foam].noGravity = true;
            }
            if (Main.rand.NextBool(3))
            {
                float backX = Direction > 0 ? Projectile.position.X : Projectile.position.X + Projectile.width;
                int trail = Dust.NewDust(new Vector2(backX, Projectile.position.Y + Projectile.height - 8f), 4, 6, DustID.Water, 0f, 0f, 100, default, 0.9f);
                Main.dust[trail].velocity *= 0.2f;
            }
            Lighting.AddLight(Projectile.Center, 0.15f, 0.3f, 0.5f);

            if (Main.rand.NextBool(20))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.35f, Pitch = -0.2f }, Projectile.Center);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Wet, 5 * 60);
            target.AddBuff(BuffID.Chilled, 2 * 60); //knocked soggy
        }

        ///<summary>Aligns the wave bottom with the ground; false = blocked by a wall or open pit.</summary>
        bool SnapToGround()
        {
            int tileX = (int)((Projectile.Center.X + Direction * Projectile.width / 2f) / 16f);
            int tileY = (int)((Projectile.position.Y + Projectile.height - 8f) / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 10)
            {
                return false;
            }
            int climb = 0;
            while (IsSolid(tileX, tileY) && climb <= 2)
            {
                tileY--;
                climb++;
            }
            if (climb > 2)
            {
                return false;
            }
            int drop = 0;
            while (!IsSolid(tileX, tileY + 1) && drop <= 4)
            {
                tileY++;
                drop++;
            }
            if (drop > 4)
            {
                return false;
            }
            Projectile.position.Y = (tileY + 1) * 16f - Projectile.height;
            return true;
        }

        static bool IsSolid(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }

        ///<summary>The tsunami sprite, bottom-anchored on the hitbox and translucent — dust always
        ///renders above projectiles, so the sculpture layers on top of it automatically.</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (!DrawWaveSprite)
            {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);

            EnemyVFX.DrawQuaraTidalCrest(Projectile.Center, new Vector2(texture.Width * SpriteScale, frameHeight * SpriteScale), texture, frame, Direction);

            Vector2 bottomCenter = new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height + 2f) - Main.screenPosition;
            SpriteEffects flip = Direction > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(texture, bottomCenter, frame, lightColor * 0.8f, 0f,
                new Vector2(texture.Width / 2f, frameHeight), SpriteScale, flip, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 splashCenter = new Vector2(Projectile.Center.X, Projectile.Bottom.Y - 6f);
            EnemyShaderBurst.Spawn(Projectile.GetSource_Death(), splashCenter, EnemyVFXBurstKind.QuaraWaterBurst);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.7f, Pitch = -0.15f }, splashCenter);

            // MIDGROUND BODY — 36 medium water motes make the release read as a broad, forward
            // breaking splash. Max scale is 1.2 * 1.35 = 1.62, safely below blocky territory.
            for (int i = 0; i < 36; i++)
            {
                Vector2 position = splashCenter + new Vector2(Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(-5f, 4f));
                Vector2 velocity = new Vector2(Direction * Main.rand.NextFloat(-1.5f, 4.5f), Main.rand.NextFloat(-5.6f, -1.2f));
                Dust water = Dust.NewDustPerfect(position, DustID.Water, velocity, Main.rand.Next(45, 95), default,
                    Main.rand.NextFloat(0.75f, 1.35f));
                water.noGravity = true;
            }

            // FOREGROUND DETAIL — 18 small, fast blue droplets give the breaking lip a crisp edge
            // without turning the splash into a single oversized dust square.
            for (int i = 0; i < 18; i++)
            {
                Vector2 position = splashCenter + new Vector2(Direction * Main.rand.NextFloat(4f, 34f), Main.rand.NextFloat(-12f, 4f));
                Vector2 velocity = new Vector2(Direction * Main.rand.NextFloat(2.5f, 6.8f), Main.rand.NextFloat(-6.8f, -2.2f));
                Dust droplet = Dust.NewDustPerfect(position, DustID.BubbleBurst_Blue, velocity, Main.rand.Next(35, 85), default,
                    Main.rand.NextFloat(0.45f, 0.85f));
                droplet.noGravity = true;
            }

            // BACKGROUND RESIDUE — 14 slow droplets start small and grow toward 1.2, lingering
            // after the bright burst so the attack does not visually stop on a hard cut.
            for (int i = 0; i < 14; i++)
            {
                Vector2 position = splashCenter + new Vector2(Main.rand.NextFloat(-42f, 42f), Main.rand.NextFloat(-3f, 6f));
                Dust mist = Dust.NewDustPerfect(position, DustID.Water,
                    new Vector2(Direction * Main.rand.NextFloat(-0.7f, 1.5f), Main.rand.NextFloat(-1.6f, -0.25f)),
                    Main.rand.Next(125, 175), default, Main.rand.NextFloat(0.50f, 0.80f));
                mist.noGravity = true;
                mist.fadeIn = Main.rand.NextFloat(1.05f, 1.25f);
            }

            // The burst shader is 104px across, so this true circle is radius 52. Apply its debuffs
            // only on the server, once, and without additional damage; the travelling crest remains
            // the attack's sole damaging band.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (!player.active || player.dead)
                    {
                        continue;
                    }

                    Vector2 closest = Vector2.Clamp(splashCenter,
                        new Vector2(player.Hitbox.Left, player.Hitbox.Top),
                        new Vector2(player.Hitbox.Right, player.Hitbox.Bottom));
                    if (Vector2.DistanceSquared(splashCenter, closest) <= ExitSplashRadius * ExitSplashRadius)
                    {
                        player.AddBuff(BuffID.Wet, ExitSplashBuffTime);
                        player.AddBuff(BuffID.Chilled, ExitSplashBuffTime);
                    }
                }
            }
        }
    }
}
