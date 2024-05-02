using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee;
using tsorcRevamp.Items.Weapons.Melee.Spears;

namespace tsorcRevamp.Projectiles.Melee
{
    public class FetidExhaustProjectile : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
            // DisplayName.SetDefault("Noxious Jet");

        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 70;
            Projectile.width = 10;
            Projectile.height = 250;
            Projectile.usesIDStaticNPCImmunity = true;
        }

        float laserWidth = 30;

        bool initialized;
        float timeFactor;
        List<Vector2> lastPositions;
        List<float> lastRotations;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.idStaticNPCHitCooldown = (int)(FetidExhaust.BaseHitCooldown / Main.player[Projectile.owner].GetTotalAttackSpeed(DamageClass.Melee));
            Vector2 rrp = player.RotatedRelativePoint(player.MountedCenter, true);
            UpdatePlayerVisuals(player);

            if (player.whoAmI == Main.myPlayer)
            {
                UpdateAim(rrp, player.HeldItem.shootSpeed);
            }

            if (Main.player[Projectile.owner].channel && Projectile.timeLeft > 30)
            {
                Projectile.timeLeft++;
            }

            if (!initialized)
            {
                SoundEngine.PlaySound(SoundID.DD2_BetsyFlameBreath, Projectile.Center);
                initialized = true;
                lastPositions = new List<Vector2>();
                lastRotations = new List<float>();
            }

            //Trick to double accuracy on the trail lists. Ended up being unnecessary.
            if (lastPositions.Count > 1)
            {
                lastPositions.Add((lastPositions[lastPositions.Count - 1] + Projectile.Center) / 2f);
                lastRotations.Add((lastRotations[lastRotations.Count - 1] + Projectile.rotation) / 2f);
            }
            lastPositions.Add(Projectile.Center);
            lastRotations.Add(Projectile.rotation);

            laserWidth *= 1.15f;
            if (laserWidth > 450)
            {
                laserWidth = 450;
            }
            timeFactor++;

            //Cast light
            Vector3 colorVector = Color.GreenYellow.ToVector3() * 2f;

            Vector2 startPoint = Projectile.Center;
            Vector2 endpoint = Projectile.Center + Projectile.rotation.ToRotationVector2() * laserWidth;

            //Fade out at end
            if (Projectile.timeLeft < 30)
            {
                colorVector *= Projectile.timeLeft / 30;
                colorVector *= Projectile.timeLeft / 30;
            }
            DelegateMethods.v3_1 = colorVector;

            Utils.PlotTileLine(startPoint, endpoint, 100, DelegateMethods.CastLight);

            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            DelegateMethods.tileCutIgnore = TileID.Sets.TileCutIgnore.None;
            Utils.PlotTileLine(startPoint, endpoint, 100, DelegateMethods.CutTiles);
        }
        private void UpdatePlayerVisuals(Player player)
        {
            Projectile.Center = player.Center;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = Projectile.direction;

            player.ChangeDir(Projectile.direction);
            //player.heldProj = projectile.whoAmI;

            player.itemTime = 2;
            player.itemAnimation = 2;
            player.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
        }
        private void UpdateAim(Vector2 source, float speed)
        {
            Vector2 aim = Vector2.Normalize(Main.MouseWorld - source);
            if (aim.HasNaNs())
            {
                aim = -Vector2.UnitY;
            }

            aim = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(Projectile.velocity), aim, 1));
            aim *= speed;

            if (aim != Projectile.velocity)
            {
                Projectile.netUpdate = true;
            }
            Projectile.velocity = aim;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                    Projectile.Center + Projectile.rotation.ToRotationVector2() * laserWidth, Projectile.height / 3f, ref point))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.CursedInferno, 5 * 60);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.CursedInferno, 5 * 60);
        }

        public static ArmorShaderData data;
        public static ArmorShaderData targetingData;
        public bool additiveContext = false;
        public override bool PreDraw(ref Color lightColor)
        {
            if (!additiveContext)
            {
                return false;
            }

            //Apply the shader, caching it as well
            if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/SpazFireJet", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "SpazFireJetPass");
            }

            //Pass relevant data to the shader via these parameters
            float scaleDown = 1;
            if (Projectile.timeLeft < 30)
            {
                scaleDown = Projectile.timeLeft / 30f;
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)laserWidth, 150);
            data.UseTargetPosition(new Vector2(laserWidth, 150));
            data.UseColor(Color.GreenYellow);
            data.UseSaturation(timeFactor * 0.08f);

            //Apply the shader

            SpriteEffects spriteEffects = SpriteEffects.None;

            Vector2 origin = new Vector2(0, sourceRectangle.Height / 2);

            int drawCap = 120;
            if (lastPositions.Count < drawCap)
            {
                drawCap = lastPositions.Count;
            }
            for (int i = 0; i < drawCap; i++)
            {
                float opacity = 0.5f * ((float)Math.Pow(1f - (float)i / (float)drawCap, 0.5f));
                data.UseOpacity(opacity * scaleDown);
                data.Apply(null);
                Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, lastPositions[lastPositions.Count - i - 1] - Main.screenPosition, sourceRectangle, Color.White, lastRotations[lastRotations.Count - i - 1], origin, Projectile.scale, spriteEffects, 0);
            }

            data.UseOpacity(scaleDown);
            data.Apply(null);
            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            return false;
        }
    }
}