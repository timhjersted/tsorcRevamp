using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{

    public class MasterBuster : ModProjectile
    {
        private const float MAX_CHARGE = 1f;
        private const float MOVE_DISTANCE = 90f;

        public float Distance
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public float Charge
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public bool IsAtMaxCharge => Charge == MAX_CHARGE;

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;
            Projectile.damage = 25;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MasterBuster];

            if (IsAtMaxCharge)
            {
                DrawLaser(Main.spriteBatch, texture, Main.player[Projectile.owner].Center,
                    Projectile.velocity, 10, Projectile.damage, -1.57f, 1f, 2000f, Color.White, (int)MOVE_DISTANCE);
            }
            return false;
        }

        public void DrawLaser(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, int damage, float rotation = 0f, float scale = 1f, float maxDist = 2000f, Color color = default, int transDist = 50)
        {
            float r = unit.ToRotation() + rotation;

            // Draws the laser 'body'
            for (float i = transDist; i <= Distance; i += step)
            {


                Color c = Color.White;
                var origin = start + i * unit;

                Main.EntitySpriteDraw(texture, origin - Main.screenPosition,
                    new Rectangle(0, 26, 26, 30), i < transDist ? Color.Transparent : c, r,
                    new Vector2(26 * .5f, 30 * .5f), 2.9f, 0, 0);

            }

            // Draws the laser 'tail'
            Main.EntitySpriteDraw(texture, start + unit * (transDist - step) - Main.screenPosition,
                new Rectangle(0, 0, 26, 30), Color.White, r, new Vector2(26 * .5f, 30 * .5f), 2.9f, 0, 0);

            // Draws the laser 'head'
            Main.EntitySpriteDraw(texture, start + (Distance + step) * unit - Main.screenPosition,
                new Rectangle(0, 52, 26, 30), Color.White, r, new Vector2(26 * .5f, 30 * .5f), 2.9f, 0, 0);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!IsAtMaxCharge) return false;

            Player player = Main.player[Projectile.owner];
            Vector2 unit = Projectile.velocity;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), player.Center,
                player.Center + unit * Distance, 22, ref point);
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.position = player.Center + Projectile.velocity * MOVE_DISTANCE;
            Projectile.timeLeft = 2;

            UpdatePlayer(player);
            ChargeLaser(player);

            if (Charge < MAX_CHARGE) return;

            SetLaserPosition(player);
            SpawnDusts(player);
            CastLights();
            if (Main.GameUpdateCount % 8 == 0)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/MasterBuster"), player.Center);
            }

        }

        private void SpawnDusts(Player player)
        {
            Vector2 unit = Projectile.velocity * -1;
            Vector2 dustPos = player.Center + Projectile.velocity;
            for (int i = 0; i < 2; ++i)
            {

                float num1 = Projectile.velocity.ToRotation() + (Main.rand.Next(2) == 1 ? -1.0f : 1.0f) * 1.57f;
                float num2 = (float)(Main.rand.NextDouble() * 0.8f + 1.0f);
                Vector2 dustVel = new Vector2((float)Math.Cos(num1) * num2, (float)Math.Sin(num1) * num2);
                Dust dust = Main.dust[Dust.NewDust(dustPos, 0, 0, 226, dustVel.X, dustVel.Y)];
                dust.noGravity = true;
                dust.scale = 1.2f;
                Dust.NewDustDirect(Main.player[Projectile.owner].Center, 0, 0, 45,
                    -unit.X * Distance, -unit.Y * Distance, 255, Color.White, 10.0f);
            }

            for (int j = 0; j < 100; j++)
            {
                if (Main.rand.Next(5) == 0)
                {
                    Vector2 offset = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(8));
                    Dust dust = Main.dust[Dust.NewDust((player.Center + (Projectile.velocity * (Distance * (float)(j / 100f)))) + offset - Vector2.One * 4f, 8, 8, 45, 0.0f, 0.0f, 125, Color.LightBlue, 4.0f)];
                    dust.velocity = Vector2.Zero;
                    dust.noGravity = true;
                    dust.rotation = Projectile.rotation;
                }
            }
        }

        private void SetLaserPosition(Player player)
        {
            Distance = 2000f;
        }

        private void ChargeLaser(Player player)
        {
            if (!player.channel)
            {
                Projectile.Kill();
            }
            else
            {
                // Do we still have enough mana? If not, we kill the projectile because we cannot use it anymore
                if (Main.GameUpdateCount % 10 < 1 && !player.CheckMana(player.inventory[player.selectedItem].mana, true))
                {
                    Projectile.Kill();
                }
                Vector2 offset = Projectile.velocity;
                offset *= MOVE_DISTANCE - 20;
                Vector2 pos = player.Center + offset - new Vector2(10, 10);
                if (Charge < MAX_CHARGE)
                {
                    Charge++;
                }
                int chargeFact = (int)(Charge / 20f);
                Vector2 dustVelocity = Vector2.UnitX * 18f;
                dustVelocity = dustVelocity.RotatedBy(Projectile.rotation - 1.57f);
                Vector2 spawnPos = Projectile.Center + dustVelocity;
                for (int k = 0; k < chargeFact + 1; k++)
                {
                    Vector2 spawn = spawnPos + ((float)Main.rand.NextDouble() * 6.28f).ToRotationVector2() * (12f - chargeFact * 2);
                    Dust dust = Main.dust[Dust.NewDust(pos, 20, 20, 226, Projectile.velocity.X / 2f, Projectile.velocity.Y / 2f)];
                    dust.velocity = Vector2.Normalize(spawnPos - spawn) * 1.5f * (10f - chargeFact * 2f) / 10f;
                    dust.noGravity = true;
                    dust.scale = Main.rand.Next(10, 20) * 0.05f;
                }
            }
        }

        private void UpdatePlayer(Player player)
        {
            // Multiplayer support here, only run this code if the client running it is the owner of the projectile
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 diff = Main.MouseWorld - player.Center;
                diff.Normalize();
                Projectile.velocity = diff;
                Projectile.direction = Main.MouseWorld.X > player.position.X ? 1 : -1;
                Projectile.netUpdate = true;
            }
            float targetrotation = (float)Math.Atan2(((Main.mouseY + Main.screenPosition.Y) - player.position.Y), ((Main.mouseX + Main.screenPosition.X) - player.position.X));
            float m = (float)Math.Cos(targetrotation) * 560;
            float n = (float)Math.Sin(targetrotation) * 560;
            player.velocity.X -= m / 1250f;
            player.velocity.Y -= n / 1000f;
            if (player.velocity.X > 32) player.velocity.X = 32;
            if (player.velocity.X < -32) player.velocity.X = -32;
            if (player.velocity.Y > 32) player.velocity.Y = 32;
            if (player.velocity.Y < -32) player.velocity.Y = -32;
            int dir = Projectile.direction;
            player.ChangeDir(dir); // Set player direction to where we are shooting
            player.heldProj = Projectile.whoAmI; // Update player's held projectile
            player.itemTime = 2; // Set item time to 2 frames while we are used
            player.itemAnimation = 2; // Set item animation time to 2 frames while we are used
            player.itemRotation = (float)Math.Atan2(Projectile.velocity.Y * dir, Projectile.velocity.X * dir); // Set the item rotation to where we are shooting
        }

        private void CastLights()
        {
            // Cast a light along the line of the laser
            DelegateMethods.v3_1 = new Vector3(0.8f, 0.8f, 1f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * (Distance - MOVE_DISTANCE), 26, DelegateMethods.CastLight);
        }

        public override bool ShouldUpdatePosition() => false;

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 unit = Projectile.velocity;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * Distance, (Projectile.width + 16) * Projectile.scale, DelegateMethods.CutTiles);
        }
    }
}
