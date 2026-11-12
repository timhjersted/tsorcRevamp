using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Armor.Magic;
using tsorcRevamp.Content.Items.ConsumableSoul;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.Lifegem;
using tsorcRevamp.Content.Items.Weapons.Classless;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies.Hollow
{
    class FirebombHollow : ModNPC, IHitReactor
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 14;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.5f;
            NPC.lifeMax = 60;
            NPC.damage = 16;
            NPC.value = 300; // life / 2 for preHM : was 25
            NPC.height = 40;
            NPC.width = 20;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.defense = 6;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.FirebombHollowBanner>();

            if (Main.hardMode)
            {
                NPC.lifeMax = 500;
                NPC.defense = 25;
                NPC.value = 2500; // was 155
                NPC.damage = 42;
                NPC.knockBackResist = 0.2f;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 2000;
                NPC.defense = 80;
                NPC.value = 8000; // was 225 with 1500 health
                NPC.damage = 74;
                NPC.knockBackResist = 0.0f;
            }

            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.HealthScaledSpeedBase = 2f;
            g.HealthScaledSpeedMultiplier = -1.25f;
            g.NavSearchRadius = 70;
            g.CanUseRopes = true;
            g.StandoffDistance = 8;
            g.MaxJumpPower = 7f;
            g.RemembersLastKnownPos = true;
            // Unreachable-but-visible player: give up after ~3s, ignore them for 10s and patrol around the give-up spot
            // instead of standing frozen (see HollowSoldier / GlobalNPC.UnreachableWanderTicks). A hit re-engages.
            g.NavGiveUpTicks = 180;
            g.UnreachableWanderTicks = 600;
            g.PatrolAnchorSource = NPCs.PatrolAnchorSource.GiveUpLocation;
            g.PatrolRange = 24;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 2, 4));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Firebomb>(), 1, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FadingSoul>(), 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OilPot>(), 20, 1, 3));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<LifegemItem>()));

            int[] armorIDs = new int[]
            {
                ModContent.ItemType<RedClothHat>(),
                ModContent.ItemType<RedClothTunic>(),
                ModContent.ItemType<RedClothPants>(),
            };
            npcLoot.Add(new DropMultiple(armorIDs, 30, 1, !NPC.downedBoss1));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 10; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 2.5f * hit.HitDirection, -1.5f, 70, default(Color), 1f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, 1.5f * hit.HitDirection, -2.5f, 50, default(Color), 1f);
                }
            }
        }


        #region AI

        private const int AI_State_Slot = 0;
        private const int AI_Universal_Timer_Slot = 1;
        private const int AI_State_Timer_1_Slot = 2;
        private const int AI_State_Timer_2_Slot = 3;

        private const int State_Pursuing = 0;
        private const int State_Firebombing = 1;


        public float AI_State
        {
            get => NPC.ai[AI_State_Slot];
            set => NPC.ai[AI_State_Slot] = value;
        }
        public float AI_Universal_Timer
        {
            get => NPC.ai[AI_Universal_Timer_Slot];
            set => NPC.ai[AI_Universal_Timer_Slot] = value;
        }

        public float AI_State_Timer_1
        {
            get => NPC.ai[AI_State_Timer_1_Slot];
            set => NPC.ai[AI_State_Timer_1_Slot] = value;
        }

        public float AI_State_Timer_2
        {
            get => NPC.ai[AI_State_Timer_2_Slot];
            set => NPC.ai[AI_State_Timer_2_Slot] = value;
        }



        public override void AI()
        {
            Player player = Main.player[NPC.target];


            // Movement is now handled by the shared fighter mover. It walks/jumps terrain, pursues, and —
            // via StandoffDistance (set in SetDefaults) — halts at firebomb range. The old duplicated
            // jump-ladder / platform-drop / boredom block was deleted; only the bespoke firebomb attack
            // state machine remains in-class. (Phase 3 legacy-AI migration.)
            tsorcRevampAIs.FighterAI(NPC, 2f, 0.10f, 0.1f, canPounce: false, canDodgeroll: false);

            bool grounded = NPC.velocity.Y == 0; // proxy for the old standing_on_solid_tile check

            #region AI_State Independent


            if (AI_Universal_Timer < 80)
            {
                AI_Universal_Timer++;
            }

            /*if (AI_Universal_Timer >= 60 && AI_Universal_Timer <= 70)
            {
                if (npc.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 1.5f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

            }

            if (AI_Universal_Timer >= 70 && AI_Universal_Timer <= 80)
            {
                if (npc.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 9, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(npc.position.X + 3, npc.position.Y + 1), 4, 4, 183, npc.velocity.X, npc.velocity.Y, 180, default(Color), 0.8f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += npc.velocity;
                }
            }*/


            #endregion


            // PURSUING
            if (AI_State == State_Pursuing)
            {
                // Movement (pursue / terrain jumps / platform-drop / stop-at-range) is handled by FighterAI
                // above. All that remains here is the decision to begin a throw: grounded, within firebomb
                // range, with line of sight, once the inter-throw timer (AI_Universal_Timer) has filled.
                if (AI_Universal_Timer == 80 && grounded && Math.Abs(NPC.Center.X - player.Center.X) < 15 * 16 && Collision.CanHit(NPC.position, NPC.width, NPC.height, player.position, player.width, player.height))
                {
                    AI_State = State_Firebombing;
                    AI_State_Timer_1 = 0;
                    NPC.frameCounter = 0;
                }
            }

            // FIREBOMBING
            if (AI_State == State_Firebombing)
            {
                AI_State_Timer_1 += 1f;

                if (!grounded)
                {
                    AI_State = State_Pursuing;
                    AI_Universal_Timer = 40;
                    AI_State_Timer_1 = 0;
                    NPC.frameCounter = 0;
                }

                if (AI_State_Timer_1 < 0)
                {
                    if (NPC.velocity.X > 2f) //hard limit of 6f
                    {
                        NPC.velocity.X = 2f;
                    }
                    if (NPC.velocity.X < -2f) //both directions
                    {
                        NPC.velocity.X = -2f;
                    }
                }

                if (AI_State_Timer_1 <= 10)
                {
                    if (NPC.direction == 1 && NPC.velocity.Y > 0.5f) { NPC.velocity.X -= 0.15f; }
                    if (NPC.direction == -1 && NPC.velocity.Y < -0.5f) { NPC.velocity.X += 0.15f; }
                }
                if (AI_State_Timer_1 > 10 && grounded)
                {
                    NPC.velocity.X = 0;
                }
                if (AI_State_Timer_1 < 45)
                {
                    NPC.TargetClosest(true);
                }
                if (AI_State_Timer_1 == 45)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, player.Center);

                    Vector2 difference = Main.player[NPC.target].Center - NPC.Center; //Distance between player center and npc center
                    Vector2 velocity = new Vector2(0.1f, 0).RotatedBy(difference.ToRotation()); //Give it velocity so it can face the right direction
                    Vector2 throwpower = (Main.player[NPC.target].Center - NPC.Center) / 30;

                    // Arc only when there's enough horizontal room; skip for close-range and steep-upward throws
                    bool tightAngle = Math.Abs(difference.X) < 4 * 16f || difference.Y < -Math.Abs(difference.X);
                    if (!tightAngle)
                        throwpower.Y += Main.rand.Next(-3, -1);
                    velocity += throwpower;

                    if (throwpower.Y < -8)
                    {
                        throwpower.Y = -8;
                    }

                    if (throwpower.X > 8)
                    {
                        throwpower.X = 8;
                    }
                    if (throwpower.X < -8)
                    {
                        throwpower.X = -8;
                    }
                    if (throwpower.X < 3 && throwpower.X >= 0)
                    {
                        throwpower.X = 3;
                    }
                    if (throwpower.X > -3 && throwpower.X < 0)
                    {
                        throwpower.X = -3;
                    }

                    //Main.NewText(throwpower);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (!Main.hardMode)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), throwpower, ModContent.ProjectileType<EnemyFirebomb>(), 18, 0f, Main.myPlayer);
                        }
                        else if (Main.hardMode && !tsorcRevampWorld.SuperHardMode)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), throwpower, ModContent.ProjectileType<EnemyFirebomb>(), 30, 0f, Main.myPlayer);
                        }
                        else if (tsorcRevampWorld.SuperHardMode)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -14), throwpower, ModContent.ProjectileType<EnemyFirebomb>(), 40, 0f, Main.myPlayer);
                        }
                    }
                }

                if (AI_State_Timer_1 > 65)
                {
                    AI_State = State_Pursuing;
                    AI_Universal_Timer = 0;
                    AI_State_Timer_1 = 0;
                }
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.FighterOnHit(NPC, true);
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            bool melee = projectile.DamageType == DamageClass.Melee;
            tsorcRevampAIs.FighterOnHit(NPC, melee);
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC, melee);
        }

        /// <summary>Getting hit mid-throw sets the windup back: a melee hit staggers the arm further (-10) than a shot
        /// does (10), so a rushdown buys more time than plinking. Only while the throw is still early (&lt; 45), so a
        /// committed bomb still lands. Server/singleplayer only (see IHitReactor) — AI_State_Timer_1 is an npc.ai slot,
        /// so vanilla carries the change to clients with the next NPC sync.</summary>
        void IHitReactor.OnServerHit(NPC npc, bool melee)
        {
            if (AI_State != State_Firebombing || AI_State_Timer_1 >= 45)
            {
                return;
            }

            AI_State_Timer_1 = melee ? -10 : 10;
        }

        #endregion


        #region Drawing & Animation

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (AI_Universal_Timer > 60)
            {
                Texture2D firebombTexture = (Texture2D)ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(FirebombHollow)) + "_Firebomb");
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(firebombTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 64, 54), drawColor, NPC.rotation, new Vector2(32, 30), NPC.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(firebombTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 64, 54), drawColor, NPC.rotation, new Vector2(32, 30), NPC.scale, effects, 0);
                }
            }
        }

        public override void DrawEffects(ref Color drawColor)
        {
            if (Main.rand.NextBool(20) && AI_State == State_Pursuing && AI_Universal_Timer >= 60)
            {
                if (NPC.direction == 1)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.Center.X + 2, NPC.position.Y + 22), 12, 12, 6, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }

                else
                {
                    int dust = Dust.NewDust(new Vector2(NPC.Center.X - 16, NPC.position.Y + 22), 12, 12, 6, NPC.velocity.X * 0f, NPC.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (AI_State == State_Firebombing && NPC.velocity.X == 0)
            {
                NPC.spriteDirection = NPC.direction;

                if (AI_State_Timer_1 <= 25)
                {
                    NPC.frame.Y = 10 * frameHeight;
                }
                else if (AI_State_Timer_1 < 45)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (AI_State_Timer_1 <= 51)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
            }

            else
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.velocity.Y != 0) //If airborn
                {
                    NPC.frame.Y = 1 * frameHeight;
                }
                else if (NPC.velocity.X == 0)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }

                else
                {
                    float framecountspeed = Math.Abs(NPC.velocity.X) * 2.2f;
                    NPC.frameCounter += framecountspeed;


                    if (NPC.frameCounter < 12)
                    {
                        NPC.frame.Y = 2 * frameHeight;
                    }
                    else if (NPC.frameCounter < 24)
                    {
                        NPC.frame.Y = 3 * frameHeight;
                    }
                    else if (NPC.frameCounter < 36)
                    {
                        NPC.frame.Y = 4 * frameHeight;
                    }
                    else if (NPC.frameCounter < 48)
                    {
                        NPC.frame.Y = 5 * frameHeight;
                    }
                    else if (NPC.frameCounter < 60)
                    {
                        NPC.frame.Y = 6 * frameHeight;
                    }
                    else if (NPC.frameCounter < 72)
                    {
                        NPC.frame.Y = 7 * frameHeight;
                    }
                    else if (NPC.frameCounter < 84)
                    {
                        NPC.frame.Y = 8 * frameHeight;
                    }
                    else if (NPC.frameCounter < 96)
                    {
                        NPC.frame.Y = 9 * frameHeight;
                    }
                    else
                    {
                        NPC.frameCounter = 0;
                    }
                }
            }
        }



        #endregion


    }
}