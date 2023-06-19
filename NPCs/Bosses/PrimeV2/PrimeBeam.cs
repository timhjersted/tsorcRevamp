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
    class PrimeBeam : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
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
            get => primeHost != null && ((TheMachine)primeHost.ModNPC).MoveIndex == 0;
        }

        int phase
        {
            get => ((TheMachine)primeHost.ModNPC).Phase;
        }

        bool damaged;

        float rotationTarget;
        float rotationSpeed;
        bool counterClockwise;

        public Vector2 Offset = new Vector2(-604, 250);
        public int cooldown = 60;
        public override void AI()
        {
            int BeamDamage = 150;
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
            rotationSpeed = 0.03f;

            if(((TheMachine)primeHost.ModNPC).aiPaused)
            {
                return;
            }

            if (((TheMachine)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(1200, 0).RotatedBy(3 * MathHelper.TwoPi / 5f);
            }

            Vector2 targetRotation = rotationTarget.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = nextRotationVector.ToRotation();

            if (active)
            {
                if (!damaged)
                {
                    //Sweep back and forth across the player
                    if (((TheMachine)primeHost.ModNPC).MoveTimer == 10)
                    {
                        counterClockwise = true;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeBeam>(), BeamDamage / 4, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                        }
                    }

                    if (counterClockwise)
                    {
                        rotationTarget = MathHelper.Pi + (Target.Center - NPC.Center).ToRotation() + MathHelper.Pi / 2f;
                    }
                    else
                    {
                        rotationTarget = MathHelper.Pi + (Target.Center - NPC.Center).ToRotation() - MathHelper.Pi / 2f;
                    }

                    if (Math.Abs(rotationTarget - NPC.rotation) < 0.5f)
                    {
                        counterClockwise = !counterClockwise;
                    }
                }
                else
                {
                    //Spam stationary wide beams around the target player
                    if (Main.GameUpdateCount % 60 == 0)
                    {
                        rotationTarget = (NPC.Center - Target.Center).ToRotation() + Main.rand.NextFloat(-0.25f, 0.25f) + MathHelper.PiOver2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.rotation + MathHelper.PiOver2).ToRotationVector2(), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeRapidBeam>(), BeamDamage / 4, 0.5f, Main.myPlayer, ai0: 1, ai1: NPC.whoAmI);
                        }
                    }
                }
            }
            else
            {
                if (damaged)
                {
                    if (Main.GameUpdateCount % 300 == 140)
                    {
                        rotationTarget = (NPC.Center - Target.Center).ToRotation() + Main.rand.NextFloat(-0.25f, 0.25f) + MathHelper.PiOver2;
                        rotationSpeed = 0.1f;
                    }
                    //Spam stationary wide beams around the target player
                    if (Main.GameUpdateCount % 300 == 200)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.rotation + MathHelper.PiOver2).ToRotationVector2(), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeRapidBeam>(), BeamDamage / 4, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                        }
                    }
                }
                else
                {
                    if (Main.GameUpdateCount % 600 > 239 && Main.GameUpdateCount % 600 < 300)
                    {
                        rotationTarget = (Target.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;
                    }
                    if (Main.GameUpdateCount % 600 == 299)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 1), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeRapidBeam>(), BeamDamage / 4, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                        }
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
                UsefulFunctions.SimpleGore(NPC, "Beam_Damaged_1");
                UsefulFunctions.SimpleGore(NPC, "Beam_Damaged_2");
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

        public static Texture2D texture;
        public static Texture2D glowmask;
        public static Texture2D firingGlowmask;
        public float firingChargeup;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight(NPC.Center, TorchID.Red);
            TheMachine.DrawMachineAura(Color.Red, active, NPC, 0.2f);

            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeBeam");
            UsefulFunctions.EnsureLoaded(ref glowmask, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeBeam_Glowmask");
            UsefulFunctions.EnsureLoaded(ref firingGlowmask, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeBeam_GlowmaskBeam");

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height / 2);
            if (damaged)
            {
                sourceRectangle.Y += texture.Height / 2;
            }
            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, NPC.rotation, drawOrigin, 1f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glowmask, NPC.Center - Main.screenPosition, sourceRectangle, Color.White, NPC.rotation, drawOrigin, 1f, SpriteEffects.None, 0);

            if (UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeRapidBeam>()) || UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeBeam>()))
            {
                firingChargeup += 1 / 30f;
            }
            else
            {
                firingChargeup -= 1 / 30f;
            }
            firingChargeup = Math.Clamp(firingChargeup, 0, 1);

            Main.EntitySpriteDraw(firingGlowmask, NPC.Center - Main.screenPosition, sourceRectangle, Color.White * firingChargeup, NPC.rotation, drawOrigin, 1f, SpriteEffects.None, 0);

            return false;
        }

        public override void OnKill()
        {
            UsefulFunctions.SimpleGore(NPC, "Beam_Destroyed_1");
            UsefulFunctions.SimpleGore(NPC, "Beam_Destroyed_2");
            UsefulFunctions.SimpleGore(NPC, "Beam_Destroyed_3");
            UsefulFunctions.SimpleGore(NPC, "Beam_Destroyed_4");
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}