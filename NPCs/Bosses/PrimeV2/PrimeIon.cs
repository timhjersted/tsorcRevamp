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

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    [AutoloadBossHead]
    class PrimeIon : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
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
            get => primeHost != null && ((TheMachine)primeHost.ModNPC).MoveIndex == 1;
        }
        int phase
        {
            get => ((TheMachine)primeHost.ModNPC).Phase;
        }

        bool damaged;


        public Vector2 Offset = new Vector2(-304, 80);
        public override void AI()
        {
            int ionDamage = 200;

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
                NPC.rotation = MathHelper.PiOver2;
                return;
            }

            if (((TheMachine)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(1200, 0).RotatedBy(3 * MathHelper.TwoPi / 5f);
            }

            NPC.rotation = (Target.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;

            if (active)
            {
                if (damaged)
                {
                    if (Main.GameUpdateCount % 200 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 9), ModContent.ProjectileType<Projectiles.Enemy.Prime.IonBomb>(), ionDamage / 4, 0.5f, Main.myPlayer, Target.whoAmI, 1);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 9), ModContent.ProjectileType<Projectiles.Enemy.Prime.IonBomb>(), ionDamage / 4, 0.5f, Main.myPlayer, Target.whoAmI, 1); //An ai1 of 1 means a wider random spread
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 9), ModContent.ProjectileType<Projectiles.Enemy.Prime.IonBomb>(), ionDamage / 4, 0.5f, Main.myPlayer, Target.whoAmI, 2); //An ai1 of 2 means teleport right on the player
                        }
                        auraBonus = 0.2f;
                    }
                }
                else
                {
                    if (Main.GameUpdateCount % 120 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 9), ModContent.ProjectileType<Projectiles.Enemy.Prime.IonBomb>(), ionDamage / 4, 0.5f, Main.myPlayer, Target.whoAmI);
                        }
                        auraBonus = 0.2f;
                    }
                }
            }
            else
            {
                if (damaged)
                {
                    if (Main.GameUpdateCount % 600 == 100)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 9), ModContent.ProjectileType<Projectiles.Enemy.Prime.IonBomb>(), ionDamage / 4, 0.5f, Main.myPlayer, Target.whoAmI, 1);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 9), ModContent.ProjectileType<Projectiles.Enemy.Prime.IonBomb>(), ionDamage / 4, 0.5f, Main.myPlayer, Target.whoAmI, 2);
                        }
                        auraBonus = 0.2f;
                    }
                }
                else
                {
                    if (Main.GameUpdateCount % 400 == 150)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 9), ModContent.ProjectileType<Projectiles.Enemy.Prime.IonBomb>(), ionDamage / 4, 0.5f, Main.myPlayer, Target.whoAmI);
                        }
                        auraBonus = 0.2f;
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
            Lighting.AddLight(NPC.Center, TorchID.Ice);
            TheMachine.DrawMachineAura(Color.Cyan, active, NPC, auraBonus);
            auraBonus *= 0.8f;

            drawColor = Color.Lerp(drawColor, Color.Cyan, 0.15f);
            drawColor = Color.Lerp(drawColor, Color.White, 0.25f);

            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeIon");
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, NPC.rotation, drawOrigin, 1f, SpriteEffects.None, 0);

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
            //Explosion
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}