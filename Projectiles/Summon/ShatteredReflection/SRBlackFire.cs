using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;


namespace tsorcRevamp.Projectiles.Summon.ShatteredReflection
{
    class SRBlackFire : Projectiles.VFX.DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.scale = 1.5f;
            Projectile.alpha = 50;
            Projectile.timeLeft = 360;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.knockBack = 9;
            Projectile.DamageType = DamageClass.Summon;

            trailWidth = 25;
            trailPointLimit = 150;
            trailYOffset = 30;
            trailMaxLength = 150;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 3;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/BlackFireball", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            trailCollision = true;
        }

        public override void AI()
        {
            base.AI();
            Projectile.velocity *= 1.03f;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Vector2 dir = new Vector2(1f, 0f);

            // determine how many flamelets to spew (5~8)
            int children = Main.rand.Next(5) + 3;

            // set the angle offset by the number of flamelets
            int offset = 180 / (children - 1);

            // rotate theta by a random angle between 0 and 90 degrees
            int preOffset = Main.rand.Next(90);
            dir = new Vector2((float)Math.Cos(preOffset) * dir.X - (float)Math.Sin(preOffset) * dir.Y, (float)Math.Sin(preOffset) * dir.X + (float)Math.Cos(preOffset) * dir.Y);

            Vector2 averageCenter = Vector2.Zero;
            for (int i = 0; i < trailPositions.Count; i++)
            {
                averageCenter += trailPositions[i] / trailPositions.Count;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), averageCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 10, 0, Main.myPlayer, 120, 15);
            }

            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 4f;
                velY *= 4f;
                Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, 54, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, 58, velX, velY, 160, default, 1.5f);
            }

            // terminate projectile
            Projectile.active = false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(ModContent.BuffType<DarkInferno>(), 240);
            }
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (target.immune[Projectile.owner] > 0)
            {
                return false;
            }
            else
            {
                return null;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (info.PvP && Main.rand.NextBool(5))
            {
                target.AddBuff(ModContent.BuffType<DarkInferno>(), 240);
            }
        }

        public override float CollisionWidthFunction(float progress)
        {
            return 12;
        }

        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            collisionEndPadding = trailPositions.Count / 3;
            collisionPadding = trailPositions.Count / 8;
            timeFactor++;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Color shaderColor = Color.DarkViolet;
            shaderColor = UsefulFunctions.ShiftColor(shaderColor, timeFactor, 0.03f);
            effect.Parameters["shaderColor"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
