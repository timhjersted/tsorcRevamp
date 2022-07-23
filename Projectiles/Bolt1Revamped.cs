using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Bolt1Revamped : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.penetrate = 999;
            Projectile.usesIDStaticNPCImmunity = true; //All bolt projectiles in the world share one immunity timer. NPCs standing in two bolt projectiles (such as those getting chained) only get hit once.
            Projectile.idStaticNPCHitCooldown = 15; //How often they can be hit by it
        }
        public override void AI()
        {
            //On the first frame of spawning, play a sound and convert its velocity to a rotation
            if (Projectile.velocity != Vector2.Zero)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit53 with { Volume = 0.8f, PitchVariance = 0.3f }, Projectile.Center);
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.velocity = Vector2.Zero;
            }

            //Cast light
            DelegateMethods.v3_1 = Color.Cyan.ToVector3() * 0.25f;
            Vector2 lightingVector = new Vector2(0, 1).RotatedBy(Projectile.rotation - MathHelper.PiOver2);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + lightingVector * 142, 1, DelegateMethods.CastLight);

            if (Main.GameUpdateCount % 3 == 0)
            {
                Projectile.frame++;
            }            
            if(Projectile.frame > 6)
            {
                Projectile.Kill();
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            //Give hit targets a debuff that "marks" them as hit, so they can't chain it twice
            target.AddBuff(ModContent.BuffType<Buffs.BoltChainImmunity>(), 60);

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(Mod.Find<ModBuff>("ElectrocutedBuff").Type, 120);
            }

            //Controls how many times it can chain. For level 1, the max is 2
            if (Projectile.ai[0] < 2)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    //No chaining to dead NPCs, NPCs who have already been chained to (aka those with the debuff), and no chaining to the target that just got hit
                    if (Main.npc[i].active && !Main.npc[i].HasBuff(ModContent.BuffType<Buffs.BoltChainImmunity>()) && !Main.npc[i].friendly && i != target.whoAmI)
                    {
                        //No chaining to targets too far away to hit
                        if (target.Distance(Main.npc[i].Center) < 142)
                        {
                            NPC newTarget = Main.npc[i];
                            Vector2 direction = UsefulFunctions.GenerateTargetingVector(target.Center, newTarget.Center, 1);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, direction, ModContent.ProjectileType<Bolt1Revamped>(), (int)(Projectile.damage * 0.5f), 0.5f, Projectile.owner, Projectile.ai[0] + 1);
                            
                            //Each bolt can only spawn one other bolt per hit. We could make later upgrades able to spawn more
                            return;
                        }
                    }
                }
            }
        }

        //Custom collision code
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            Vector2 lightingVector = new Vector2(0, 1).RotatedBy(Projectile.rotation - MathHelper.PiOver2);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + lightingVector * 142, 22, ref point);
        }

        public static Texture2D BoltStart;
        public static Texture2D BoltMiddle1;
        public static Texture2D BoltMiddle2;
        public static Texture2D BoltEnd;
        public override bool PreDraw(ref Color lightColor)
        {
            //Load all the textures if any are not loaded
            if(BoltStart == null || BoltStart.IsDisposed)
            {
                BoltStart = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/BoltBounceStart", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (BoltMiddle1 == null || BoltMiddle1.IsDisposed)
            {
                BoltMiddle1 = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/BoltMiddle1", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (BoltMiddle2 == null || BoltMiddle2.IsDisposed)
            {
                BoltMiddle2 = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/BoltMiddle2", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (BoltEnd == null || BoltEnd.IsDisposed)
            {
                BoltEnd = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/BoltEnd", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }       
            

            //Draw the first bit
            Rectangle frame = new Rectangle(0, (BoltStart.Height) / 7 * Projectile.frame, BoltStart.Width, BoltStart.Height / 7);
            Vector2 origin = new Vector2(0, frame.Height / 2);
            Main.EntitySpriteDraw(BoltStart, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
                        
            //Draw the second bit, which overlaps with the first bit (hence why they share the same origin)
            frame = new Rectangle(0, (BoltMiddle1.Height / 7) * Projectile.frame, BoltMiddle1.Width, BoltMiddle1.Height / 7);
            Main.EntitySpriteDraw(BoltMiddle1, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            //Each segment after the second bit subtracts from the "origin" it draws relative to, making it draw slightly further away than the last.
            //Doing it like this makes it very easy to add new segments, and potentially make the length dynamic in the future. We can just keep shifting the origin for each one.
            origin.X -= frame.Width;

            //Draw the third bit
            frame = new Rectangle(0, (BoltMiddle2.Height / 7) * Projectile.frame, BoltMiddle2.Width, BoltMiddle2.Height / 7);
            Main.EntitySpriteDraw(BoltMiddle2, Projectile.Center - Main.screenPosition - new Vector2(4, 2), frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            
            //Shift the origin again
            origin.X -= frame.Width;

            //Draw the ending bit
            frame = new Rectangle(0, (BoltEnd.Height / 7) * Projectile.frame, BoltEnd.Width, BoltEnd.Height / 7);
            Main.EntitySpriteDraw(BoltEnd, Projectile.Center - Main.screenPosition + new Vector2(-6, 2), frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }

}
