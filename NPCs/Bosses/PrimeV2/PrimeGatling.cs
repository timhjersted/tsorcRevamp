using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Enemy.DarkCloud;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using System;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    class PrimeGatling : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 12;
            NPCID.Sets.TrailCacheLength[NPC.type] = (int)TRAIL_LENGTH;    //The length of old position to be recorded
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            NPC.width = 35;
            NPC.height = 60;
            NPC.damage = 53;
            NPC.defense = 0;
            NPC.lifeMax = TheMachine.PrimeArmHealth;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 0;
            NPC.knockBackResist = 0f;
            NPC.timeLeft = 99999;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.damage = 0;
        }
        const float TRAIL_LENGTH = 12;

        public int AttackTimer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        NPC primeHost
        {
            get => Main.npc[(int)NPC.ai[1]];
        }
        public Player Target
        {
            get => Main.player[primeHost.target];
        }

        bool active
        {
            get => primeHost != null && ((TheMachine)primeHost.ModNPC).MoveIndex == 3;
        }
        int phase
        {
            get => ((TheMachine)primeHost.ModNPC).Phase;
        }

        bool damaged;


        public Vector2 Offset = new Vector2(604, 250);
        int cooldown;
        public override void AI()
        {
            int LaserDamage = 100;

            if (primeHost == null || primeHost.active == false || primeHost.type != ModContent.NPCType<TheMachine>())
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 10, 0, Main.myPlayer, 500, 60);
                }
                NPC.active = false;
                return;
            }

            UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.1f, 50, primeHost.velocity);

            if (((TheMachine)primeHost.ModNPC).aiPaused)
            {
                return;
            }

            if (((TheMachine)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(1200, 0).RotatedBy(-MathHelper.Pi / 5f);
            }


            NPC.rotation = (NPC.Center - Target.Center).ToRotation() + MathHelper.PiOver2;
            Vector2 shotOffset = NPC.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 54;

            if (active)
            {
                if (!damaged)
                {
                    if (cooldown <= 15)
                    {
                        cooldown = 15;
                    }
                    //Fire increasingly fast bursts of 3 projectiles
                    if (Main.GameUpdateCount % cooldown == cooldown - 1)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            cooldown = (int)(cooldown * 0.965);
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, Target.Center, 10f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + shotOffset, velocity, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage / 4, 0.5f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + shotOffset, velocity * 0.8f, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage / 4, 0.5f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + shotOffset, velocity * 0.66f, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage / 4, 0.5f, Main.myPlayer);
                        }
                        auraBonus = 0.1f;
                    }
                }
                else
                {
                    //Just spam shots everywhere
                    if (Main.GameUpdateCount % 10 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, Target.Center, 6f);
                            velocity = velocity.RotatedBy(Main.rand.NextFloat(-1, 1));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + shotOffset, velocity, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage / 4, 0.5f, Main.myPlayer);
                        }
                        auraBonus = 0.1f;
                    }
                }
            }
            else
            {
                if (!damaged)
                {
                    cooldown = 60;
                    if (Main.GameUpdateCount % 90 == 45)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, Target.Center, 8.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + shotOffset, velocity, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage / 4, 0.5f, Main.myPlayer);
                        }
                        auraBonus = 0.1f;
                    }
                }
                else
                {
                    if (Main.GameUpdateCount % 90 == 45)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + shotOffset, UsefulFunctions.Aim(NPC.Center, Target.Center, 7.5f).RotatedBy(Main.rand.NextFloat(-1, 1)), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage / 4, 0.5f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + shotOffset, UsefulFunctions.Aim(NPC.Center, Target.Center, 7.5f).RotatedBy(Main.rand.NextFloat(-1, 1)), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage / 4, 0.5f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + shotOffset, UsefulFunctions.Aim(NPC.Center, Target.Center, 7.5f).RotatedBy(Main.rand.NextFloat(-1, 1)), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage / 4, 0.5f, Main.myPlayer);
                        }
                        auraBonus = 0.1f;
                    }
                }
            }
            
        }
        public override bool CheckDead()
        {
            if (((TheMachine)primeHost.ModNPC).dying)
            {
                return true;
            }
            else
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0f, Main.myPlayer, 300, 25);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0f, Main.myPlayer, 300, 25);
                }
                UsefulFunctions.SimpleGore(NPC, "Gatling_Damaged_1");
                UsefulFunctions.SimpleGore(NPC, "Gatling_Damaged_2");
                UsefulFunctions.SimpleGore(NPC, "Gatling_Damaged_3");
                NPC.life = 1;
                damaged = true;
                NPC.dontTakeDamage = true;
                return false;
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            TheMachine.PrimeProjectileBalancing(ref projectile);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            TheMachine.PrimeDamageShare(NPC.whoAmI, damageDone);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            TheMachine.PrimeDamageShare(NPC.whoAmI, damageDone);
        }

        float auraBonus;
        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeGatling");
            Lighting.AddLight(NPC.Center, TorchID.Cursed);
            TheMachine.DrawMachineAura(Color.GreenYellow, active, NPC, auraBonus);
            auraBonus *= 0.8f;

            drawColor.A = 255;
            drawColor = Color.Lerp(drawColor, Color.GreenYellow, 0.15f);
            drawColor = Color.Lerp(drawColor, Color.White, 0.25f);

            int frameTime = 3;
            if (active)
            {
                frameTime = 2;
            }

            NPC.frame.Height = texture.Height / 12;
            if (NPC.frameCounter % frameTime == 0)
            {
                NPC.frame.Y += texture.Height / 12;
                if (NPC.frame.Y >= texture.Height / 2)
                {
                    NPC.frame.Y = 0;
                }
            }

            if (damaged)
            {
                NPC.frame.Y += texture.Height / 2;
            }
            Vector2 drawOrigin = new Vector2(NPC.frame.Width * 0.5f, NPC.frame.Height * 0.5f);
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, NPC.frame, drawColor, NPC.rotation, drawOrigin, 1f, SpriteEffects.None, 0);


            //Draw metal bones
            //Draw shadow trail (and maybe normal trail?)
            if (active)
            {
                //Draw aura
            }
            if (damaged)
            {
                //Draw damaged version
            }
            else
            {
                //Draw normal version
            }
            return false;
        }

        public override void OnKill()
        {
            UsefulFunctions.SimpleGore(NPC, "Gatling_Destroyed_1");
            UsefulFunctions.SimpleGore(NPC, "Gatling_Destroyed_2");
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}