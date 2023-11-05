using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    class PrimeIon : ModNPC
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
            NPC.width = 150;
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
            get => primeHost != null && ((TheMachine)primeHost.ModNPC).MoveIndex == 1;
        }
        int phase
        {
            get => ((TheMachine)primeHost.ModNPC).Phase;
        }

        bool damaged;


        public Vector2 Offset = new Vector2(-304, 80);
        public int ionDamage = 200;
        public override void AI()
        {
            AttackTimer++;
            if (NPC.life == 1)
            {
                damaged = true;
            }
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


            if (((TheMachine)primeHost.ModNPC).aiPaused)
            {
                return;
            }

            if (((TheMachine)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(1200, 0).RotatedBy(-MathHelper.PiOver2 - MathHelper.Pi / 11f);
            }

            NPC.rotation = (Target.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;

            if (active)
            {
                if (damaged)
                {
                    if (AttackTimer % 200 == 0)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarShot"), NPC.Center);
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
                    if (AttackTimer % 120 == 0)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarShot"), NPC.Center);
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
                    if (AttackTimer % 600 == 100)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarShot"), NPC.Center);
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
                    if (AttackTimer % 400 == 150)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarShot"), NPC.Center);
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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0f, Main.myPlayer, 300, 25);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0f, Main.myPlayer, 300, 25);
                }
                UsefulFunctions.SimpleGore(NPC, "Ion_Damaged_1");
                UsefulFunctions.SimpleGore(NPC, "Ion_Damaged_1");
                UsefulFunctions.SimpleGore(NPC, "Ion_Damaged_1");
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
        public static Texture2D glowmask;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight(NPC.Center, TorchID.Blue);
            TheMachine.DrawMachineAura(Color.Cyan, active, NPC, auraBonus);
            auraBonus *= 0.8f;

            if (Main.timeForVisualEffects % 3 == 0)
            {
                NPC.frameCounter++;
                if (NPC.frameCounter >= 4)
                {
                    NPC.frameCounter = 0;
                }
            }

            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeIon");
            UsefulFunctions.EnsureLoaded(ref glowmask, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeIon_Glowmask");
            Rectangle sourceRectangle = new Rectangle(0, (int)NPC.frameCounter * texture.Height / 8, texture.Width, texture.Height / 8);
            if (damaged)
            {
                sourceRectangle.Y += texture.Height / 2;
            }
            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, NPC.rotation, drawOrigin, 1f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glowmask, NPC.Center - Main.screenPosition, sourceRectangle, Color.White, NPC.rotation, drawOrigin, 1f, SpriteEffects.None, 0);

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
            UsefulFunctions.SimpleGore(NPC, "Ion_Destroyed_1");
            UsefulFunctions.SimpleGore(NPC, "Ion_Destroyed_2");
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}