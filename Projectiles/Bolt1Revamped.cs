using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Bolt1Revamped : ModProjectile
    {
        //Disclaimer: This projectile does a ton of weird shit and doesn't really behave like any other projectile or laser in the game. Its behavior is controlled by its draw code instead of the other way around.
        //I am not liable for any headaches, bugs, burns, magic smoke, locusts, fires, or curses that may result from attempting to read/modify it.

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.penetrate = 999;
            Projectile.tileCollide = false;
        }

        Vector2 lightingVector;
        bool initialized = false;
        float minX = 0;
        float maxX = 1;
        float rangeLimit;
        public override void AI()
        {
            //On the first frame of spawning, play a sound, convert its velocity to a rotation, and calculate its length
            if (!initialized)
            {
                initialized = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit53 with { Volume = 0.8f, PitchVariance = 0.3f }, Projectile.Center);
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.velocity.Normalize();
                lightingVector = Projectile.velocity;
                Projectile.velocity = Vector2.Zero;

                //Set the range depending on whether it was fired from a bolt 1 or 2 tome
                float maxRange;
                if (Projectile.ai[1] == 0)
                {
                    maxRange = 250;
                }
                else
                {
                    maxRange = 700;
                }

                for (rangeLimit = 0; rangeLimit <= maxRange; rangeLimit += 5f)
                {
                    Vector2 start = Projectile.Center + lightingVector * rangeLimit;
                    if (!Collision.CanHit(Projectile.Center, 1, 1, start, 1, 1) && !Collision.CanHitLine(Projectile.Center, 1, 1, start, 1, 1))
                    {
                        rangeLimit -= 5f;
                        break;
                    }
                }
            }

            //Aka it didn't draw *any* segments* last frame, which means it's done
            if (minX == 0 && maxX == 0) 
            {
                Projectile.Kill();
            }
            

            Vector2 startpoint = Projectile.Center + lightingVector * minX;
            Vector2 endpoint = Projectile.Center + lightingVector * maxX;            

            //Cast light
            DelegateMethods.v3_1 = Color.Cyan.ToVector3() * 1f;            
            Utils.PlotTileLine(startpoint, endpoint, 1, DelegateMethods.CastLight);

            //Cut plants
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(startpoint, endpoint, 100, DelegateMethods.CutTiles);


            //Set the projectile time scalar depending on whether it was fired from a bolt 1 or 2 tome
            float ticksPerTick;
            if (Projectile.ai[1] == 0)
            {
                ticksPerTick = 3;
            }
            else
            {
                ticksPerTick = 2;
            }

            if (Main.GameUpdateCount % ticksPerTick == 0)
            {
                Projectile.frame++;
            }            
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            //Give hit targets a debuff that "marks" them as hit, so they can't chain it twice
            target.AddBuff(ModContent.BuffType<Buffs.BoltChainImmunity>(), 60);

            target.AddBuff(ModContent.BuffType<Buffs.ElectrocutedBuff>(), 120);

            //Controls how many times it can chain. For level 1, the max is 2
            if (Projectile.ai[0] < 2)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    //No chaining to dead NPCs, NPCs who have already been chained to (aka those with the debuff), and no chaining to the target that just got hit
                    if (Main.npc[i].active && !Main.npc[i].HasBuff(ModContent.BuffType<Buffs.BoltChainImmunity>()) && !Main.npc[i].friendly && i != target.whoAmI)
                    {
                        //No chaining to targets too far away to hit
                        float distance = target.Distance(Main.npc[i].Center);
                        if (distance < 142)
                        {
                            NPC newTarget = Main.npc[i];
                            Vector2 direction = UsefulFunctions.GenerateTargetingVector(target.Center, newTarget.Center, 1);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, direction, ModContent.ProjectileType<BoltChaining>(), (int)(Projectile.damage * 0.5f), 0.5f, Projectile.owner, Projectile.ai[0] + 1, distance);

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
            Vector2 startpoint = Projectile.Center + lightingVector * minX;
            Vector2 endpoint = Projectile.Center + lightingVector * maxX;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), startpoint,
                endpoint, 22, ref point);
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

            minX = 0;
            maxX = 0;

            //Draw the first bit
            Rectangle frame = new Rectangle(0, (BoltStart.Height) / 7 * Projectile.frame, BoltStart.Width, BoltStart.Height / 7);
            Vector2 origin = new Vector2(0, frame.Height / 2);
            if (Projectile.frame < 6)
            {
                Main.EntitySpriteDraw(BoltStart, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
                minX = 1;
                maxX = -(origin.X -frame.Width);
            }

            int i = 0;
            //Draw this many segments max
            for (; i < 20; i++)
            {
                //Projectile.ai[1] stores the "length" of the projectile, calculated in AI(). origin.X stores how far offset our current drawing position is
                //This statement ends the drawing process once our drawing position passes that length.
                if (-origin.X >= rangeLimit)
                {
                    break;
                }

                {
                    if (i % 2 == 0)
                    {
                        //Draw the second bit, which overlaps with the first bit (hence why they share the same origin)
                        frame = new Rectangle(0, (BoltMiddle1.Height / 7) * (Projectile.frame - i / 2), BoltMiddle1.Width, BoltMiddle1.Height / 7);
                        if (Projectile.frame - i / 2 < 6 && Projectile.frame - i / 2 >= 0)
                        {
                            if (minX == 0)
                            {
                                minX = -(origin.X - frame.Width);
                            }
                            Main.EntitySpriteDraw(BoltMiddle1, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
                            if (maxX < -origin.X)
                            {
                                maxX = -origin.X;
                            }
                        }
                        //Main.EntitySpriteDraw(BoltMiddle1, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation + MathHelper.PiOver4, origin, Projectile.scale, SpriteEffects.None, 0);
                        //Main.EntitySpriteDraw(BoltMiddle1, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation - MathHelper.PiOver4, origin, Projectile.scale, SpriteEffects.None, 0);

                        //Each segment after the second bit subtracts from the "origin" it draws relative to, making it draw slightly further away than the last.
                        //Doing it like this makes it very easy to add new segments, which is how the dynamic length works. We can just keep shifting the origin for each piece we draw.
                        origin.X -= frame.Width;
                    }
                    else
                    {
                        //Draw the third bit
                        frame = new Rectangle(0, (BoltMiddle2.Height / 7) * (Projectile.frame - i / 2), BoltMiddle2.Width, BoltMiddle2.Height / 7);
                        if (Projectile.frame - i / 2 < 6 && Projectile.frame - i / 2 >= 0)
                        {
                            if (minX == 0)
                            {
                                minX = -(origin.X - frame.Width);
                            }
                            Main.EntitySpriteDraw(BoltMiddle2, Projectile.Center - Main.screenPosition - new Vector2(4, 4).RotatedBy(Projectile.rotation), frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
                            if (maxX < -origin.X)
                            {
                                maxX = -origin.X;
                            }
                        }
                        //Main.EntitySpriteDraw(BoltMiddle2, Projectile.Center - Main.screenPosition + new Vector2(-20, -62), frame, Color.White, Projectile.rotation + MathHelper.PiOver4 / 2, origin, Projectile.scale, SpriteEffects.None, 0);
                        //Main.EntitySpriteDraw(BoltMiddle2, Projectile.Center - Main.screenPosition + new Vector2(-20, 32), frame, Color.White, Projectile.rotation , origin, Projectile.scale, SpriteEffects.None, 0);

                        //Shift the origin again
                        origin.X -= frame.Width - 10;
                    }
                }
            }

            //We don't need that last - 10 shift for the final piece, so we revert it
            origin.X -= 10;

            //Draw the ending bit
            frame = new Rectangle(0, (BoltEnd.Height / 7) * (Projectile.frame - (i / 2)), BoltEnd.Width, BoltEnd.Height / 7);
            if (Projectile.frame - (i / 2) < 6 && Projectile.frame - (i / 2) >= 0)
            {
                Main.EntitySpriteDraw(BoltEnd, Projectile.Center - Main.screenPosition + new Vector2(-6, 2).RotatedBy(Projectile.rotation), frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }            
            return false;
        }
    }

}
