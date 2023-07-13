using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.Projectiles.Throwing
{
    class OilPotProjectile : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = -1;
            Projectile.knockBack = 9;
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.ContinuouslyUpdateDamageStats = true;

            // These 2 help the projectile hitbox be centered on the projectile sprite.
            DrawOffsetX = -5;
            DrawOriginOffsetY = -5;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target.type == NPCID.EaterofWorldsHead || target.type == NPCID.EaterofWorldsBody || target.type == NPCID.EaterofWorldsTail)
            {
                modifiers.FinalDamage /= 2;
            }
            Projectile.timeLeft = 2; //sets it to 2 frames, to let the explosion ai kick in. Setdefaults is -1 pen, this allows it to only hit one npc, then run explosion ai.
            Projectile.netUpdate = true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 1) //the one frame make the explosion only deal damage once.
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 1 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 150;
                Projectile.height = 150;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.knockBack = 9f;
                Projectile.DamageType = DamageClass.Throwing;
                Projectile.netUpdate = true;
            }
            target.AddBuff(BuffID.Oiled, OilPot.OiledDurationInSeconds * 60);
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.timeLeft = 2;
            return false;
        }
        public override void AI()
        {
            if (/*projectile.owner == Main.myPlayer &&*/ Projectile.timeLeft <= 2)
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 150;
                Projectile.height = 150;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.knockBack = 9f;
                Projectile.DamageType = DamageClass.Throwing;
            }
            else
            {
                // Smoke and fuse dust spawn.
                if (Main.rand.NextBool(4))
                {
                    int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Asphalt, 0f, 0f, 100, default, 1f);
                    Main.dust[dustIndex].scale = 0.1f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].fadeIn = .5f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].noGravity = true;
                }
            }
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 10f;
                // Roll speed dampening.
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.97f;
                    //if (projectile.type == 29 || projectile.type == 470 || projectile.type == 637)
                    {
                        Projectile.velocity.X = Projectile.velocity.X * 0.99f;
                    }
                    if ((double)Projectile.velocity.X > -0.01 && (double)Projectile.velocity.X < 0.01)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
            }
            // Rotation increased by velocity.X 
            Projectile.rotation += Projectile.velocity.X * 0.08f;
            return;
        }

        public override void Kill(int timeLeft)
        {
            // Play explosion sound
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { PitchVariance = 0.5f }, Projectile.Center);

            // Fire Dust spawn
            for (int i = 0; i < 200; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X + 36, Projectile.position.Y + 36), Projectile.width - 100, Projectile.height - 100, DustID.Asphalt, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, default, 1f);
                Main.dust[dustIndex].noGravity = true;
            }
            // reset size to normal width and height.
            Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);

            int MaxVel = 15;
            Vector2 RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<OilDroplet1>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<OilDroplet1>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<OilPotShard1>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<OilPotShard2>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<OilPotShard3>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<OilPotShard4>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }
    }
}
