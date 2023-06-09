using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    class DarkUltimaWeapon : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dark Ultima Weapon");
            NPCID.Sets.TrailCacheLength[NPC.type] = 6;
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.NeedsExpertScaling[NPC.type] = false;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.width = 12;
            NPC.height = 12;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontTakeDamage = true;
            NPC.lifeMax = 500000;
            NPC.scale = 1.2f;
            NPC.damage = DarkCloud.swordDamage;
            NPC.behindTiles = false;
            AttackModeCounter = 3;
        }

        public NPC HolderDarkCloud
        {
            get => Main.npc[(int)NPC.ai[0]];
            set => Main.npc[(int)NPC.ai[0]] = value;
        }
        public float AttackModeCounter
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        public Player Target
        {
            get => Main.player[HolderDarkCloud.target];
        }


        //Yet again this is timer based instead of state based.
        //Huge mess, but for now it works.
        float[] trailRotations = new float[6] { 0, 0, 0, 0, 0, 0 };
        bool spawnedSubProjectiles = false;
        public override void AI()
        {
            if (HolderDarkCloud.active == false)
            {
                NPC.active = false;
            }

            if (NPC.ai[2] == 0)
            {
                NPC.rotation = MathHelper.ToRadians(-20);

                if (spawnedSubProjectiles == false)
                {
                    //These projectiles track the sword in a line formation and do the actual damage, because fuck getting both the sword hitbox *and* visuals both right at the same time
                    //Also, this makes the hitbox fit the sprite *way* better than an enormous square
                    //One projectile sits on the hilt, and the other sits at the end of the sword
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.position, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkUltimaWeaponDummyProjectile>(), DarkCloud.swordDamage, 0.5f, Main.myPlayer, NPC.whoAmI, i);
                        }
                    }

                    spawnedSubProjectiles = true;
                }

                if (HolderDarkCloud == null || HolderDarkCloud.active == false)
                {
                    NPC.active = false;
                }

                //Always stay in Dark Cloud's hand except for during the throw attack
                if (AttackModeCounter < 120 || AttackModeCounter > 240)
                {
                    InHand();
                }

                //Swing
                if (AttackModeCounter > 60 && AttackModeCounter < 80)
                {
                    Swing(AttackModeCounter - 60, 20);
                }
                if (AttackModeCounter >= 80 && AttackModeCounter <= 120)
                {
                    DownAngle();
                }

                //Launch toward the player
                if (AttackModeCounter == 120)
                {
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center + new Vector2(0, -62), Target.Center, 25);
                }

                //Rotate as it flies
                if (AttackModeCounter > 120 && AttackModeCounter < 180)
                {
                    NPC.rotation += 0.05f * AttackModeCounter;
                }

                //Launch back toward Dark Cloud
                if (AttackModeCounter > 180 && AttackModeCounter < 240)
                {
                    //If not close to the Dark Cloud, accelerate toward it until close enough to teleport into its hands.
                    if (Vector2.Distance(NPC.Center, HolderDarkCloud.Center) > 100)
                    {
                        NPC.velocity = UsefulFunctions.Aim(NPC.Center, HolderDarkCloud.Center, 25);
                    }
                    else
                    {
                        InHand();
                    }
                }

                //Swing as dark cloud falls
                if (AttackModeCounter >= 240 && AttackModeCounter < 300)
                {
                    int rotProgress = (int)AttackModeCounter;
                    if (rotProgress > 280)
                    {
                        rotProgress = 280;
                    }
                    Swing(rotProgress - 240, 40);
                }


                float projSpeed = 9;
                //Swing as dark cloud fires off a projectile
                if (AttackModeCounter >= 300 && AttackModeCounter < 530)
                {
                    NPC.rotation = UsefulFunctions.Aim(NPC.Center, Target.Center, 1).ToRotation() + MathHelper.ToRadians(45);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (AttackModeCounter == 302)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -62), UsefulFunctions.Aim(NPC.Center, Target.Center + new Vector2(0, -62), projSpeed).RotatedBy(MathHelper.ToRadians(45)), ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                    }
                    if (AttackModeCounter == 305)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -62), UsefulFunctions.Aim(NPC.Center, Target.Center + new Vector2(0, -62), projSpeed), ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                    }
                    if (AttackModeCounter == 308)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -62), UsefulFunctions.Aim(NPC.Center, Target.Center + new Vector2(0, -62), projSpeed).RotatedBy(MathHelper.ToRadians(-45)), ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                    }

                    if (AttackModeCounter == 422)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -62), UsefulFunctions.Aim(NPC.Center, Target.Center + new Vector2(0, -62), projSpeed).RotatedBy(MathHelper.ToRadians(90)), ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                    }
                    if (AttackModeCounter == 425)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -62), UsefulFunctions.Aim(NPC.Center, Target.Center + new Vector2(0, -62), projSpeed).RotatedBy(MathHelper.ToRadians(45)), ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                    }
                    if (AttackModeCounter == 428)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -62), UsefulFunctions.Aim(NPC.Center, Target.Center + new Vector2(0, -62), projSpeed), ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                    }

                    if (AttackModeCounter == 522)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -62), UsefulFunctions.Aim(NPC.Center, Target.Center + new Vector2(0, -62), projSpeed).RotatedBy(MathHelper.ToRadians(45)), ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                    }
                    if (AttackModeCounter == 525)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -62), UsefulFunctions.Aim(NPC.Center, Target.Center + new Vector2(0, -62), projSpeed), ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                    }
                    if (AttackModeCounter == 528)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -62), UsefulFunctions.Aim(NPC.Center, Target.Center + new Vector2(0, -62), projSpeed).RotatedBy(MathHelper.ToRadians(-45)), ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkWave>(), DarkCloud.darkSlashDamage, 0.5f, Main.myPlayer);
                    }
                }

                //Point straight down for the final falling attack
                if (AttackModeCounter >= 600)
                {
                    NPC.rotation = MathHelper.ToRadians(135);
                }

                //End the attack phase
                if (AttackModeCounter == 750)
                {
                    NPC.active = false;
                    //Maybe add dust?
                }


                AttackModeCounter++;
            }


            if (NPC.ai[2] == DarkCloud.DarkCloudAttackID.TeleportingSlashes)
            {
                if (spawnedSubProjectiles == false)
                {
                    //These projectiles track the sword in a line formation and do the actual damage, because fuck getting both the sword hitbox *and* visuals both right at the same time
                    //Also, this makes the hitbox fit the sprite *way* better than an enormous square
                    //One projectile sits on the hilt, and the other sits at the end of the sword
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.position, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.DarkCloud.DarkUltimaWeaponDummyProjectile>(), DarkCloud.swordDamage, 0.5f, Main.myPlayer, NPC.whoAmI, i);
                        }
                    }

                    spawnedSubProjectiles = true;
                }
                AttackModeCounter++;
                if (HolderDarkCloud.Center.X < Target.Center.X)
                {
                    NPC.rotation = MathHelper.ToRadians(-20);
                    NPC.Center = HolderDarkCloud.Center + new Vector2(0, 55);
                }
                else
                {
                    NPC.rotation = MathHelper.ToRadians(-70);

                    NPC.Center = HolderDarkCloud.Center + new Vector2(10, 55);
                }

                if (AttackModeCounter == 640)
                {
                    NPC.active = false;
                }
            }



            for (int i = 5; i > 0; i--)
            {
                trailRotations[i] = trailRotations[i - 1];
            }
            trailRotations[0] = NPC.rotation;
        }
        void InHand()
        {
            NPC.direction = HolderDarkCloud.direction;
            Vector2 offset = new Vector2(0, 45);

            if (AttackModeCounter < 120)
            {
                offset.Y = 55;
            }
            if (AttackModeCounter > 120)
            {
                offset.Y = 75;
            }

            NPC.Center = HolderDarkCloud.Center + offset;
        }

        void DownAngle()
        {
            NPC.rotation = MathHelper.ToRadians(110);
            if (NPC.direction == -1)
            {
                NPC.rotation = MathHelper.ToRadians(170);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.DarkUltimaWeapon];
            Texture2D glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.DarkUltimaWeaponGlowmask];
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height / Main.npcFrameCount[NPC.type]);
            Vector2 origin = sourceRectangle.Size() / 2f;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            Vector2 offset = new Vector2(0, 45);

            //Draw shadow trails
            for (float i = NPCID.Sets.TrailCacheLength[NPC.type] - 1; i >= 0; i--)
            {
                Main.spriteBatch.Draw(texture, NPC.oldPos[(int)i] - Main.screenPosition - offset, sourceRectangle, drawColor * ((6 - i) / 6), trailRotations[(int)i], origin, NPC.scale, spriteEffects, 0);
                Main.spriteBatch.Draw(glowTexture, NPC.oldPos[(int)i] - Main.screenPosition - offset, sourceRectangle, Color.White * ((6 - i) / 6), trailRotations[(int)i], origin, NPC.scale, spriteEffects, 0);
            }

            //Draw actual npc
            Main.spriteBatch.Draw(texture, NPC.position - Main.screenPosition - offset, sourceRectangle, drawColor, NPC.rotation, origin, NPC.scale, spriteEffects, 0);
            Main.spriteBatch.Draw(glowTexture, NPC.position - Main.screenPosition - offset, sourceRectangle, Color.White, NPC.rotation, origin, NPC.scale, spriteEffects, 0);

            return false;
        }

        //Make the projectile swing.
        void Swing(float progress, float maxProgress)
        {
            NPC.rotation = MathHelper.ToRadians(-20) + (MathHelper.ToRadians(130) * (progress / maxProgress));
            if (NPC.direction == -1)
            {
                NPC.rotation = MathHelper.ToRadians(180) + MathHelper.ToRadians(-20) + (MathHelper.ToRadians(130) * ((maxProgress - progress) / maxProgress));
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
    }
}