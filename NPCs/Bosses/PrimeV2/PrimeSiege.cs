using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    class PrimeSiege : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.TrailCacheLength[NPC.type] = (int)TRAIL_LENGTH;    //The length of old position to be recorded
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            NPC.width = 70;
            NPC.height = 150;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = (int)(TheMachine.PrimeArmHealth * (Main.masterMode ? 1.5f : 1));
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 0;
            NPC.knockBackResist = 0f;
            NPC.timeLeft = 99999;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }
        const float TRAIL_LENGTH = 12;

        public int AttackTimer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        NPC primeHost
        {
            get
            {
                if (Main.npc[(int)NPC.ai[1]].active && Main.npc[(int)NPC.ai[1]].type == ModContent.NPCType<TheMachine>())
                {
                    return Main.npc[(int)NPC.ai[1]];
                }
                else
                {
                    return null;
                }
            }
        }
        public Player Target
        {
            get => Main.player[primeHost.target];
        }

        bool active
        {
            get => primeHost != null && ((TheMachine)primeHost.ModNPC).MoveIndex == 4;
        }
        int phase
        {
            get => ((TheMachine)primeHost.ModNPC).Phase;
        }

        bool damaged;

        float rotationTarget;
        float rotationSpeed;
        float rotationOffset = MathHelper.PiOver4;

        public Vector2 Offset = new Vector2(200, 70);
        public override void AI()
        {
            if (NPC.life == 1)
            {
                damaged = true;
            }
            AttackTimer++;
            if (primeHost == null)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 10, 0, Main.myPlayer, 500, 60);
                }
                NPC.active = false;
                return;
            }

            UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.1f, 50, primeHost.velocity);
            rotationSpeed = 0.03f;

            if (((TheMachine)primeHost.ModNPC).aiPaused)
            {
                return;
            }
            if (((TheMachine)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(1200, 0).RotatedBy(4 * MathHelper.TwoPi / 5f);
            }

            rotationTarget = rotationOffset + MathHelper.Pi;
            if (!damaged)
            {
                rotationTarget += (NPC.Center - Target.Center).ToRotation() + MathHelper.PiOver2;
            }

            if (damaged && active)
            {
                rotationTarget += (NPC.Center - Target.Center).ToRotation() + MathHelper.PiOver2 - MathHelper.Pi;

            }
            Vector2 targetRotation = rotationTarget.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = 0;

            float rotationDiff = Math.Abs(rotationTarget - NPC.rotation);
            if (rotationDiff < 0.4f || Math.Abs(rotationDiff - MathHelper.TwoPi) < 0.4f)
            {
                rotationOffset *= -1;
            }

            if (active)
            {
                if (!damaged)
                {
                    if ((AttackTimer % 120) % 10 == 0 && AttackTimer % 120 <= 20)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Projectiles.Enemy.Prime.HomingMissile>(), ai0: Target.whoAmI);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.6f }, NPC.Center);
                        auraBonus = 0.1f;
                        reloadProgress = 0;
                    }
                }
                else
                {
                    if (AttackTimer % 40 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Projectiles.Enemy.Prime.HomingMissile>(), ai0: Target.whoAmI, ai1: 1);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.6f }, NPC.Center);
                        auraBonus = 0.1f;
                        reloadProgress = 0;
                    }
                }
            }
            else
            {
                if (!damaged)
                {
                    if (AttackTimer % 550 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Projectiles.Enemy.Prime.HomingMissile>(), ai0: Target.whoAmI);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.8f }, NPC.Center);
                        auraBonus = 0.2f;
                        reloadProgress = 0;
                    }
                }
                else
                {
                    if (AttackTimer % 75 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Projectiles.Enemy.Prime.HomingMissile>(), ai0: Target.whoAmI, ai1: 2);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.6f }, NPC.Center);
                        auraBonus = 0.2f;
                        reloadProgress = 0;
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
                UsefulFunctions.SimpleGore(NPC, "Siege_Damaged_1");
                UsefulFunctions.SimpleGore(NPC, "Siege_Damaged_2");
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

        //Controls what reload sprite to use, rounded to an int
        float reloadProgress;
        float auraBonus;
        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight(NPC.Center, TorchID.Orange);
            drawColor = Color.Lerp(drawColor, Color.OrangeRed, 0.25f);
            drawColor = Color.Lerp(drawColor, Color.White, 0.25f);

            TheMachine.DrawMachineAura(Color.Orange, active, NPC, auraBonus);
            auraBonus *= 0.8f;

            //Scroll up by 5 sprites over a period of 40 frames. Left as an unsimplified fraction for clarity.
            reloadProgress += 5f / 40f;
            if (reloadProgress > 5)
            {
                reloadProgress = 5;
            }


            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeSiege");
            Rectangle sourceRectangle = new Rectangle(0, (texture.Height / 12) * (int)reloadProgress, texture.Width, texture.Height / 12);
            if (damaged)
            {
                sourceRectangle.Y += texture.Height / 2;
            }
            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, NPC.rotation, drawOrigin, 1f, SpriteEffects.None, 0);

            return false;
        }

        public override void OnKill()
        {
            UsefulFunctions.SimpleGore(NPC, "Siege_Destroyed_1");
            UsefulFunctions.SimpleGore(NPC, "Siege_Destroyed_2");
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}