using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using System;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Projectiles.Melee.Runeterra
{
	public class PlasmaWhirlwindSpin : ModProjectile
	{
        public bool Hit = false;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 7;
        }
        public override void SetDefaults()
		{
			Projectile.aiStyle = -1; 
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.scale = 1f;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.ownerHitCheck = true;
            Projectile.usesOwnerMeleeHitCD = true;
            Projectile.timeLeft = 40;
            Projectile.hide = true;
            Projectile.ignoreWater = true;
            Projectile.width = 328;
            Projectile.height = 320;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 4;
            modifiers.FinalDamage.Flat += Math.Min(target.lifeMax * PlasmaWhirlwind.PercentHealthDamage / 100f, PlasmaWhirlwind.HealthDamageCap * 3);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (!Hit)
            {
                Hit = true;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/SpinHit") with { Volume = 1f });
            }
        }
        public override void Kill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            player.itemAnimation = 0;
            player.itemTime = 0;
            if (Hit && player.ownedProjectileCounts[ModContent.ProjectileType<PlasmaWhirlwindTornado>()] < 1)
            {
                player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks = 2;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/TornadoReady") with { Volume = 1f });
            }
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Vector2 mountedCenter = player.MountedCenter;
            float num = 50f;
            float num9 = 2f;
            float num10 = 20f;
            float num11 = -(float)Math.PI / 4f;
            Vector2 vector = player.RotatedRelativePoint(mountedCenter);
            Vector2 vector2 = Vector2.Zero;
            if (player.dead)
            {
                Projectile.Kill();
                return;
            }
            Lighting.AddLight(player.Center, 0.75f, 0.9f, 1.15f);
            int num3 = Math.Sign(Projectile.velocity.X);
            Projectile.velocity = new Vector2((float)num3, 0f);
            if (Projectile.ai[0] == 0f)
            {
                Projectile.rotation = Utils.ToRotation(new Vector2((float)num3, 0f - player.gravDir)) + num11 + (float)Math.PI;
                if (Projectile.velocity.X < 0f)
                {
                    Projectile.rotation -= (float)Math.PI / 2f;
                }
            }
            Projectile.alpha -= 128;
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            _ = Projectile.ai[0] / num;
            float num4 = 1f;
            Projectile.ai[0] += num4;
            Projectile.rotation += (float)Math.PI * 2f * num9 / num * (float)num3;
            bool flag2 = Projectile.ai[0] == (float)(int)(num / 2f);
            if (Projectile.ai[0] >= num || (flag2 && !player.controlUseItem))
            {
                Projectile.Kill();
                player.reuseDelay = 2;
            }
            else if (flag2)
            {
                Vector2 mouseWorld2 = Main.MouseWorld;
                int num5 = ((player.DirectionTo(mouseWorld2).X > 0f) ? 1 : (-1));
                if ((float)num5 != Projectile.velocity.X)
                {
                    player.ChangeDir(num5);
                    Projectile.velocity = new Vector2((float)num5, 0f);
                    Projectile.netUpdate = true;
                    Projectile.rotation -= (float)Math.PI;
                }
            }
            if ((Projectile.ai[0] == num4 || (Projectile.ai[0] == (float)(int)(num / 2f) && Projectile.active)) && Projectile.owner == Main.myPlayer)
            {
                Vector2 mouseWorld3 = Main.MouseWorld;
                _ = player.DirectionTo(mouseWorld3) * 0f;
            }
            float num6 = Projectile.rotation - (float)Math.PI / 4f * (float)num3;
            vector2 = (num6 + ((num3 == -1) ? ((float)Math.PI) : 0f)).ToRotationVector2() * (Projectile.ai[0] / num) * num10;
            Vector2 vector4 = Projectile.Center + (num6 + ((num3 == -1) ? ((float)Math.PI) : 0f)).ToRotationVector2() * 30f;
            Vector2 vector5 = num6.ToRotationVector2();
            Vector2 vector6 = vector5.RotatedBy((float)Math.PI / 2f * (float)Projectile.spriteDirection);
            /*if (Main.rand.NextBool(2))
            {
                Dust dust3 = Dust.NewDustDirect(vector4 - new Vector2(5f), 10, 10, 31, player.velocity.X, player.velocity.Y, 150);
                dust3.velocity = Projectile.DirectionTo(dust3.position) * 0.1f + dust3.velocity * 0.1f;
            }
            for (int j = 0; j < 4; j++)
            {
                float num7 = 1f;
                float num8 = 1f;
                switch (j)
                {
                    case 1:
                        num8 = -1f;
                        break;
                    case 2:
                        num8 = 1.25f;
                        num7 = 0.5f;
                        break;
                    case 3:
                        num8 = -1.25f;
                        num7 = 0.5f;
                        break;
                }
                if (!Main.rand.NextBool(6))
                {
                    Dust dust4 = Dust.NewDustDirect(Projectile.position, 0, 0, 226, 0f, 0f, 100);
                    dust4.position = Projectile.Center + vector5 * (60f + Main.rand.NextFloat() * 20f) * num8;
                    dust4.velocity = vector6 * (4f + 4f * Main.rand.NextFloat()) * num8 * num7;
                    dust4.noGravity = true;
                    dust4.noLight = true;
                    dust4.scale = 0.5f;
                    dust4.customData = Projectile;
                    if (Main.rand.NextBool(4))
                    {
                        dust4.noGravity = false;
                    }
                }
            }*/
            Projectile.position = vector - Projectile.Size / 2f;
            Projectile.position += vector2;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.timeLeft = 2;
            player.ChangeDir(Projectile.direction);
            player.heldProj = Projectile.whoAmI;
            player.SetDummyItemTime(2);
            player.itemRotation = MathHelper.WrapAngle(Projectile.rotation);
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }
        }
        public override void CutTiles()
        {
            float num = 60f;
            float f = Projectile.rotation - (float)Math.PI / 4f * (float)Math.Sign(Projectile.velocity.X);
            Utils.PlotTileLine(Projectile.Center + f.ToRotationVector2() * (0f - num), Projectile.Center + f.ToRotationVector2() * num, (float)Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            /*float f2 = Projectile.rotation - (float)Math.PI / 4f * (float)Math.Sign(Projectile.velocity.X);
            float collisionPoint15 = 0f;
            float HitboxSize = 240;
            if (Collision.CheckAABBvLineCollision(targetHitbox.Center(), targetHitbox.Size(), Projectile.Center + f2.ToRotationVector2() * (0f - HitboxSize), Projectile.Center + f2.ToRotationVector2() * HitboxSize, 23f * Projectile.scale, ref collisionPoint15))
            {
                return true;
            }*/
            Player owner = Main.player[Projectile.owner];
            float distance = Vector2.Distance(owner.MountedCenter, targetHitbox.Center.ToVector2());
            if (distance <= (190))
            {
                return true;
            }
            return false;
        }
    }
}
