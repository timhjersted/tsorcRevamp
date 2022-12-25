using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triad
{

    public class RetPiercingLaser : EnemyGenericLaser
    {


        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser");
        }


        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;
            Projectile.timeLeft = 999;

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            FiringDuration = 120;
            TelegraphTime = 30;
            MaxCharge = 30;
            LaserLength = 5000;
            LaserColor = Color.Red;
            TileCollide = false;
            LaserSize = 1.3f;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSound = SoundID.Item12 with { Volume = 0.5f };

            LaserDebuffs = new List<int>(); 
            DebuffTimers = new List<int>();

            CastLight = false;

            LaserDebuffs.Add(BuffID.OnFire);
            DebuffTimers.Add(300);

            Additive = true;
        }



        Vector2 target;
        Vector2 initialTarget;
        Vector2 initialPosition;
        Player targetPlayer;
        bool aimLeft = false;
        Vector2 simulatedVelocity;

        float rotDirection;

        bool rapid = false;
        public override void AI()
        {
            //Hacky way to pass one more bit of data through ai[0], but it works
            if(Projectile.ai[0] >= 1000)
            {
                rapid = true;
                Projectile.ai[0] -= 1000;
            }

            if (FiringTimeLeft > 0)
            {
                Vector2 origin = GetOrigin();
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead)
                    {
                        float point = 0;
                        if (Collision.CheckAABBvLineCollision(Main.player[i].Hitbox.TopLeft(), Main.player[i].Hitbox.Size(), origin, origin + Projectile.velocity * Distance, 120, ref point))
                        {
                            Main.player[i].AddBuff(ModContent.BuffType<Buffs.ThermalRise>(), 220);
                        }
                    }
                }
            }

            if (Main.player[(int)Projectile.ai[0]] != null && Main.player[(int)Projectile.ai[0]].active)
            {
                if (Main.npc[(int)Projectile.ai[1]] != null && Main.npc[(int)Projectile.ai[1]].active && Main.npc[(int)Projectile.ai[1]].type == ModContent.NPCType<NPCs.Bosses.RetinazerV2>())
                {
                    Projectile.velocity = (Main.npc[(int)Projectile.ai[1]].rotation + MathHelper.PiOver2).ToRotationVector2() ;
                }

                LaserName = "Piercing Gaze";

                if (rapid)
                {
                    TelegraphTime = 90;
                    FiringDuration = 15;
                }
                else
                {
                    FiringDuration = 60;
                }
                LineDust = true;
                LaserDust = DustID.OrangeTorch;
                if (targetPlayer == null)
                {
                    targetPlayer = Main.player[(int)Projectile.ai[0]];
                    target = Main.player[(int)Projectile.ai[0]].Center;
                }
            }
            else
            {
                Projectile.Kill();
            }

            base.AI();
            if (Charge == MaxCharge - 1)
            {
                LaserSize = 0;
                LaserAlpha = 0;
            }

            if (LaserSize < 1.3f)
            {
                LaserSize += (1.3f / 30f);
                LaserAlpha += 1f / 30f;
            }
        }

        public override Vector2 GetOrigin()
        {
            if (Main.npc[(int)Projectile.ai[1]] != null && Main.npc[(int)Projectile.ai[1]].active && Main.npc[(int)Projectile.ai[1]].type == ModContent.NPCType<NPCs.Bosses.RetinazerV2>())
            {
                return Main.npc[(int)Projectile.ai[1]].Center + new Vector2(90, 0).RotatedBy(Main.npc[(int)Projectile.ai[1]].rotation + MathHelper.PiOver2);
            }
            else
            {
                Projectile.Kill();
                return Vector2.Zero;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if ((Projectile.ai[0] == -3 || Projectile.ai[0] == -2 || Projectile.ai[0] == -1) && !IsAtMaxCharge)
            {

                Color color;
                if (LaserTargetingTexture == TransparentTextureHandler.TransparentTextureType.GenericLaserTargeting)
                {
                    color = LaserColor;
                }
                else
                {
                    color = Color.White;
                }

                color *= 0.85f + 0.15f * (float)(Math.Sin(Main.GameUpdateCount / 5f));

                DrawLaser(TransparentTextureHandler.TransparentTextures[LaserTargetingTexture], GetOrigin(),
                        Projectile.velocity, LaserTargetingHead, LaserTargetingBody, LaserTargetingTail, -1.57f, 0.37f, color);
            }
            else
            {
                base.PreDraw(ref lightColor);
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (FiringTimeLeft <= 0 || !IsAtMaxCharge || TargetingMode != 0)
            {
                return false;
            }

            float point = 0f;
            Vector2 origin = GetOrigin();
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin,
                origin + Projectile.velocity * Distance, 22, ref point);
        }

        public override bool CanHitPlayer(Player target)
        {

            string deathMessage = Terraria.DataStructures.PlayerDeathReason.ByProjectile(-1, Projectile.whoAmI).GetDeathText(target.name).ToString();
            deathMessage = deathMessage.Replace("Laser", LaserName);
            target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(deathMessage), Projectile.damage * 4, 1);

            return false;
        }
    }
}
