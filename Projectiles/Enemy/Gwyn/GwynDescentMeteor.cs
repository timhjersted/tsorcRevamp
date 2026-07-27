using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Descent of the Sun: Gwyn calls down a meteor of sunlight (GwynMeteor.png, 4 frames). It hangs
    ///high, marking the impact zone on the ground with a rune (drawn separately), grows brighter,
    ///then crashes — a huge fire explosion, screen shake, and a ring of ground fireballs bursting
    ///outward from the crater. ai[0] = damage, ai[1] = target ground X.
    ///</summary>
    class GwynDescentMeteor : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynDescentMeteor";

        const int HangTicks = 55;      // telegraph — the meteor gathers high above
        const float FallAccel = 0.9f;
        const int CraterFireballs = 8;
        const float SpriteScale = 0.6f;

        int Timer => (int)Projectile.localAI[0];
        bool Falling => Timer > HangTicks;
        float GroundY;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 120;   // tighter than the huge sprite
            Projectile.height = 150;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
            Projectile.light = 1f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            GroundY = FindGroundY(new Vector2(Projectile.ai[1], Projectile.Center.Y), 60);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.9f, Pitch = -0.5f }, Projectile.Center);
            if (GroundY > 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(source, new Vector2(Projectile.ai[1], GroundY), Vector2.Zero,
                    ModContent.ProjectileType<GwynDescentColumn>(), 0, 0f, Projectile.owner,
                    GwynDescentColumn.TelegraphMode, Projectile.whoAmI);
            }
        }

        public override bool? CanDamage()
        {
            return Falling;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }

            if (!Falling)
            {
                //Hang and gather, brightening
                Projectile.velocity = Vector2.Zero;
                for (int i = 0; i < 4; i++)
                {
                    int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, 0f, 0f, 40, default, 1.6f);
                    Main.dust[dust].noGravity = true;
                }
                Lighting.AddLight(Projectile.Center, 1.4f, 1f, 0.4f);
                return;
            }

            //Crash straight down toward the marked ground
            Projectile.velocity.Y += FallAccel;
            if (Projectile.velocity.Y > 22f)
            {
                Projectile.velocity.Y = 22f;
            }
            for (int i = 0; i < 6; i++)
            {
                Vector2 pos = Projectile.position + new Vector2(Main.rand.NextFloat(Projectile.width), Main.rand.NextFloat(Projectile.height));
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, -3f, 40, default, 1.8f);
                Main.dust[dust].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 1.5f, 1.1f, 0.4f);

            if (GroundY > 0f && Projectile.Bottom.Y >= GroundY)
            {
                Impact();
            }
        }

        bool _impacted;
        void Impact()
        {
            if (_impacted)
            {
                return;
            }
            _impacted = true;
            Vector2 impactPosition = new Vector2(Projectile.Center.X, GroundY > 0f ? GroundY : Projectile.Bottom.Y);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.5f }, impactPosition);
            UsefulFunctions.ScreenShake(impactPosition, 18f, 30);
            for (int i = 0; i < 24; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(9f, 9f);
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, vel.X, vel.Y, 40, default, 2f);
                Main.dust[dust].noGravity = true;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), impactPosition, Vector2.Zero,
                    ModContent.ProjectileType<GwynDescentColumn>(), 0, 0f, Projectile.owner,
                    GwynDescentColumn.ImpactMode);

                //Crater fireballs burst outward across the ground
                for (int i = 0; i < CraterFireballs; i++)
                {
                    float ang = MathHelper.Lerp(-2.5f, -0.65f, i / (float)(CraterFireballs - 1)); // fan upward-outward
                    Vector2 vel = ang.ToRotationVector2() * Main.rand.NextFloat(7f, 11f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), impactPosition, vel,
                        ModContent.ProjectileType<GwynFireball>(), (int)Projectile.ai[0], 3f, Projectile.owner, 0.3f);
                }
            }
            Projectile.Kill();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 8 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            float pulse = !Falling ? (1f + 0.15f * (float)Math.Sin(Timer * 0.3f)) : 1f;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, MathHelper.Pi,
                new Vector2(texture.Width / 2f, frameHeight / 2f), SpriteScale * pulse, SpriteEffects.None, 0);
            return false;
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
