using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static tsorcRevamp.SpawnHelper;


namespace tsorcRevamp.NPCs.Enemies
{
    public class MountedSandsprogMage : ModNPC
    {
        int boltDamage = 12;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.GiantWalkingAntlion];
        }
        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.GiantWalkingAntlion);
            AIType = NPCID.GiantWalkingAntlion;
            NPC.height = 44;
            NPC.damage = 34;
            NPC.lifeMax = 80;
            NPC.defense = 28;
            NPC.value = 0;
            NPC.knockBackResist = 0.2f;
            AnimationType = NPCID.GiantWalkingAntlion;
            NPC.DeathSound = SoundID.DD2_KoboldHurt;

            if (Main.hardMode)
            {
                NPC.lifeMax = 300;
                NPC.defense = 40;
                NPC.damage = 55;
                NPC.value = 0;
                boltDamage = 25;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 900;
                NPC.defense = 70;
                NPC.damage = 80;
                NPC.value = 0;
                boltDamage = 36;
            }
        }

        #region Spawning
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            Player p = spawnInfo.Player;

            if (spawnInfo.Invasion || Sky(p) || spawnInfo.Player.ZoneSnow)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneUndergroundDesert) return 0.05f;

            return chance;
        }
        #endregion

        int boltTimer;
        public override void AI()
        {

            bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);

            #region check if standing on a solid tile

            int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
            bool standing_on_solid_tile = false;
            if (NPC.velocity.Y == 0f) // no jump/fall
            {
                int x_left_edge = (int)NPC.position.X / 16;
                int x_right_edge = (int)(NPC.position.X + (float)NPC.width) / 16;
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (Main.tile[l, y_below_feet] == null) // null tile means ??
                        return;

                    if (Main.tile[l, y_below_feet].HasTile && Main.tileSolid[(int)Main.tile[l, y_below_feet].TileType]) // tile exists and is solid
                    {
                        standing_on_solid_tile = true;
                        break; // one is enough so stop checking
                    }
                } // END traverse blocks under feet
            } // END no jump/fall
            #endregion

            if (boltTimer == 0) // Make sure it spawns in with the timer high, for ambush events
            {
                boltTimer += 90;
            }

            if (boltTimer < 140)
            {
                boltTimer++;
            }

            if (Main.player[NPC.target].Distance(NPC.Center) < 700f && boltTimer >= 140 && boltTimer < 200 && lineOfSight)
            {
                boltTimer++;
            }

            if (Main.player[NPC.target].Distance(NPC.Center) > 750f && boltTimer >= 140)
            {
                boltTimer = 140;
            }


            if (NPC.velocity.X > 0f) //If moving right
            {
                NPC.velocity.X -= 0.04f; //Take away a bit of accel.
            }
            else //If moving left
            {
                NPC.velocity.X += 0.04f; //Take away a bit of accel.
            }


            //Main.NewText(boltTimer);
            //Main.NewText(lineOfSight);


            if (NPC.direction == 1)
            {

                if (NPC.oldPosition.X > NPC.position.X && boltTimer > 180) //This prevents shooting bolt visually too soon after player has just rolled through enemy
                {
                    boltTimer = 180;
                }

                if (boltTimer > 160 && Main.rand.Next(8) == 0 && Main.player[NPC.target].Distance(NPC.Center) < 700f)
                {

                    int dust = Dust.NewDust(new Vector2(NPC.Center.X + 12, NPC.Center.Y - 20), 6, 3, 226, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;

                }

                if (boltTimer > 190 && Main.rand.Next(5) == 0 && Main.player[NPC.target].Distance(NPC.Center) < 700f)
                {

                    int dust = Dust.NewDust(new Vector2(NPC.Center.X + 12, NPC.Center.Y - 20), 6, 3, 226, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;

                }

                if (NPC.HasValidTarget && lineOfSight && Main.player[NPC.target].Distance(NPC.Center) < 700f)
                {

                    if (boltTimer == 200 && Main.player[NPC.target].position.X > NPC.position.X) //Spawn bolt here
                    {
                        float num48 = 9f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-1, 1);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-1, 1);


                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ProjectileID.MartianTurretBolt;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X + 12, vector8.Y - 18, speedX, speedY, type, boltDamage, 0f, Main.myPlayer, 0, 1);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_GoblinBomberThrow, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarShot") with { Volume = 0.6f, PitchVariance = 0.3f }, NPC.Center);
                        boltTimer = 1;
                    }
                }
                else if (Main.player[NPC.target].Distance(NPC.Center) < 700f && boltTimer >= 140)
                {
                    boltTimer = 140;
                }
            }

            else
            {

                if (NPC.oldPosition.X < NPC.position.X && boltTimer > 180) //This prevents shooting bolt visually too soon after player has just rolled through enemy
                {
                    boltTimer = 180;
                }

                if (boltTimer > 160 && Main.rand.Next(8) == 0)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.Center.X - 22, NPC.Center.Y - 20), 6, 3, 226, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }

                if (boltTimer > 190 && Main.rand.Next(5) == 0)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.Center.X - 22, NPC.Center.Y - 20), 6, 3, 226, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }

                if (NPC.HasValidTarget && lineOfSight && Main.player[NPC.target].Distance(NPC.Center) < 700f)
                {
                    if (boltTimer == 200 && Main.player[NPC.target].position.X < NPC.position.X)
                    {
                        float num48 = 9f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-1, 1);
                        float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-1, 1);


                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ProjectileID.MartianTurretBolt;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X - 6, vector8.Y - 18, speedX, speedY, type, 11, 0f, Main.myPlayer, 0, 1);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_GoblinBomberThrow, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarShot") with { Volume = 0.6f, PitchVariance = 0.3f }, NPC.Center);
                        boltTimer = 1;
                    }
                }
                else if (Main.player[NPC.target].Distance(NPC.Center) < 700f && boltTimer >= 140)
                {
                    boltTimer = 140;
                }
            }

            // Jaw dust

            float quantity = (float)((float)NPC.lifeMax / (float)NPC.life * 100) / 10f - 8f; //Fun. quantity is 2 at 100% hp, 30+ with low hp.

            if (Main.rand.Next((int)quantity) == 0 && NPC.spriteDirection == 1) { Dust.NewDust(new Vector2(NPC.Center.X + 18, NPC.Center.Y + 6), 26, -2, 226, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 100, default(Color), .4f); }
            if (Main.rand.Next((int)quantity) == 0 && NPC.spriteDirection != 1) { Dust.NewDust(new Vector2(NPC.Center.X - 48, NPC.Center.Y + 6), 26, -2, 226, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 100, default(Color), .4f); }


            base.AI();

        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            Texture2D armTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/Sandsprog_Mounted_Throw_Arm");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (Main.player[NPC.target].Distance(NPC.Center) < 700f && (boltTimer > 140))
            {
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(armTexture, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(NPC.frame.X, NPC.frame.Y, 84, 66), drawColor, NPC.rotation, new Vector2(42, 38), NPC.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(armTexture, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(NPC.frame.X, NPC.frame.Y, 84, 66), drawColor, NPC.rotation, new Vector2(42, 38), NPC.scale, effects, 0);
                }
            }

            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float opacity = NPC.life / 100f; // Texture fades as hp decreases
            Texture2D jawTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/Mounted_Sandsprog_Electro_Jaw");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (NPC.spriteDirection == -1)
            {
                spriteBatch.Draw(jawTexture, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(NPC.frame.X, NPC.frame.Y, 84, 66), Color.White * opacity, NPC.rotation, new Vector2(42, 40), NPC.scale, effects, 0);
            }
            else
            {
                spriteBatch.Draw(jawTexture, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(NPC.frame.X, NPC.frame.Y, 84, 66), Color.White * opacity, NPC.rotation, new Vector2(42, 40), NPC.scale, effects, 0);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            //float chance = ((NPC.life * -0.04f) + 7f); //Higher chance the higher the enemy hp. DONT USE
            float chance = (9f * (NPC.life / (float)NPC.lifeMax)) + 1f; // 100% chance of applying debuff at max hp, 10% chance at 0hp
            //Main.NewText(chance);
            if (Main.rand.NextFloat(0, 10) <= chance)
            {
                target.AddBuff(BuffID.Electrified, 3 * 60, false);
            }
        }

        public override void OnKill()
        {

            NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height) + NPC.velocity.Y), NPCID.GiantWalkingAntlion);
            NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + (float)(NPC.width / 2) + NPC.velocity.X), (int)(NPC.position.Y + (float)(NPC.height + 20) + NPC.velocity.Y - 40), ModContent.NPCType<SandsprogMage>());

            base.OnKill();
        }
    }
}
