using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Projectiles.Enemy.DemonFamily;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // Ancient Demon of the Abyss — SHM wandering elite, tier 3 of the demon family (shared AI in NPCs/Bosses/DemonBase.cs).
    // Element: bright red flame with black smoke; every hit adds a Destined Death stack (max 6) and refreshes Abyss 30s.
    // Drawn with a dark-red silhouette rim. Signatures:
    //   Doom Orb       (bag x1) darkness gathers over the horns → a harmless orb drifts 10 tiles above the player and
    //                  fires 4 straight bolts, each glinting 20t first, then implodes. Max one alive.
    //   Death's Grasp  (phase 2, bag x1) fire pools at its feet → a low red-black wave crawls along the floor to where the
    //                  player stood (jump it), simmers 30t, then erupts into a 12-tile column (leave the spot).
    class AncientDemon : DemonBase
    {
        // Doom Orb: 60t tell → orb at the horns → 30t recovery. The orb's own timeline is in DemonDoomOrb.
        const int OrbTellTicks = 60;
        const int OrbRecoveryTicks = 30;
        const float OrbMaxRange = 800f;
        const float OrbSpawnHeight = 48f; // px above the horns

        // Death's Grasp: 40t tell → wave → 40t recovery. Locked crawl distance 48..640px; only on the player's own floor.
        const int GraspTellTicks = 40;
        const int GraspRecoveryTicks = 40;
        const float GraspMinDistance = 48f;
        const float GraspMaxRange = 640f;
        const float GraspMaxFloorStep = 96f; // player's feet within 6 tiles of its own

        // Silhouette: the frame re-drawn behind itself at 8 offsets this many px out, in SilhouetteColor.
        const float SilhouetteOffset = 2f;
        static readonly Color SilhouetteColor = new Color(90, 0, 0, 200);

        int orbBoltDamage;
        int graspDamage;

        protected override DemonTier Tier => DemonTier.Ancient;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 7;
            NPC.width = 50;
            NPC.height = 120;
            NPC.damage = 110;
            NPC.defense = 70;
            NPC.lifeMax = 15000; // Systems/EnemyBalance.cs overrides (SHM 35000)
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 60000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.AncientDemonOfTheAbyssBanner>();
            SetSharedDefaults();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.AncientDemon.DespawnHandler"), Color.Red, DustID.RedTorch);
        }

        // Lower Cavern into the Underworld, SHM only; doubled in the outer 30% of the world's width and on a Blood Moon.
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool magmaCavern = spawnInfo.Player.position.Y >= Main.maxTilesY * 0.6f && !spawnInfo.Player.ZoneUnderworldHeight;
            bool outerWidth = spawnInfo.Player.position.X < Main.maxTilesX * 0.3f || spawnInfo.Player.position.X > Main.maxTilesX * 0.7f;

            float chance = 0;
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (spawnInfo.Player.ZoneUnderworldHeight)
                {
                    chance = 0.001f;

                    if (outerWidth)
                    {
                        chance *= 2;
                    }
                }

                if (magmaCavern)
                {
                    chance = 0.0005f;
                }
            }

            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }

        protected override void InitializeStats()
        {
            breathDamage = 70;
            lobDamage = 72;
            punisherDamage = 85;
            orbBoltDamage = 75;
            graspDamage = 80;
        }

        protected override void AddSignatureCards(List<DemonAttack> cards, bool isPhaseTwo)
        {
            cards.Add(DemonAttack.DoomOrb);

            if (isPhaseTwo)
            {
                cards.Add(DemonAttack.DeathsGrasp);
            }
        }

        protected override bool IsSignatureOpen(DemonAttack attack, float distance, bool lineOfSight)
        {
            if (attack == DemonAttack.DoomOrb)
            {
                return distance <= OrbMaxRange && !HasLiveOrb();
            }

            if (attack == DemonAttack.DeathsGrasp)
            {
                Player target = Main.player[NPC.target];
                bool targetGrounded = target.velocity.Y == 0f;
                float floorStep = System.Math.Abs(target.Bottom.Y - NPC.Bottom.Y);
                bool sameFloor = floorStep <= GraspMaxFloorStep;
                return lineOfSight && distance <= GraspMaxRange && targetGrounded && sameFloor;
            }

            return false;
        }

        // Server-only. One Doom Orb per demon: DemonDoomOrb.ai[1] is the owner's whoAmI.
        bool HasLiveOrb()
        {
            int orbType = ModContent.ProjectileType<DemonDoomOrb>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.type == orbType && (int)projectile.ai[1] == NPC.whoAmI)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void TickSignature(Player target)
        {
            if (State == DemonAttack.DoomOrb)
            {
                TickDoomOrb(target);
                return;
            }

            if (State == DemonAttack.DeathsGrasp)
            {
                TickDeathsGrasp(target);
                return;
            }

            EndAttack();
        }

        void TickDoomOrb(Player target)
        {
            Vector2 orbSpot = Horns - new Vector2(0f, OrbSpawnHeight);

            // Tell: black smoke and red embers drawn into a point above the horns, body flushing red.
            if (AttackTimer <= OrbTellTicks)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.85f;
                FaceTowards(target.Center.X);

                if (AttackTimer == 1)
                {
                    PlaySound(SoundID.Item103 with { Volume = 0.7f, Pitch = -0.5f });
                }

                float progress = AttackTimer / (float)OrbTellTicks;
                bodyFlash = progress * 0.5f;

                // Wraith (black) motes from 80px in to 6px, every tick; one RedTorch ember in three.
                if (!Main.dedServ)
                {
                    float radius = MathHelper.Lerp(80f, 6f, progress);
                    Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                    Dust mote = Dust.NewDustPerfect(orbSpot + offset, DustID.Wraith, -offset * 0.08f, 60, default, MathHelper.Lerp(1.2f, 2f, progress));
                    mote.noGravity = true;

                    if (Main.rand.NextBool(3))
                    {
                        Dust ember = Dust.NewDustPerfect(orbSpot + offset * 0.6f, DustID.RedTorch, -offset * 0.06f, 40, default, 1.4f);
                        ember.noGravity = true;
                    }

                    Lighting.AddLight(orbSpot, DemonElement.LightColor(Tier) * progress);
                }

                if (AttackTimer == OrbTellTicks)
                {
                    PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = -0.6f });

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), orbSpot, Vector2.Zero, ModContent.ProjectileType<DemonDoomOrb>(), orbBoltDamage, 0f, Main.myPlayer, NPC.target, NPC.whoAmI);
                    }
                }

                return;
            }

            NPC.velocity.X *= 0.9f;

            if (AttackTimer >= OrbTellTicks + OrbRecoveryTicks)
            {
                EndAttack();
            }
        }

        void TickDeathsGrasp(Player target)
        {
            // Tell: red fire and black smoke pool outward along the floor at its feet; growl at tick 1.
            if (AttackTimer <= GraspTellTicks)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.8f;
                FaceTowards(target.Center.X);

                if (AttackTimer == 1)
                {
                    PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.7f, Pitch = -0.4f });
                }

                float progress = AttackTimer / (float)GraspTellTicks;
                bodyFlash = progress * 0.45f;

                if (!Main.dedServ)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float spreadX = Main.rand.NextFloat(-1f, 1f) * MathHelper.Lerp(20f, 70f, progress);
                        Vector2 spot = NPC.Bottom + new Vector2(spreadX, -2f);
                        Dust pool = Dust.NewDustPerfect(spot, DustID.RedTorch, new Vector2(spreadX * 0.03f, -Main.rand.NextFloat(0.3f, 1.5f)), 40, default, 1.6f);
                        pool.noGravity = true;
                    }

                    if (Main.rand.NextBool(2))
                    {
                        DemonElement.SpawnAccent(Tier, NPC.BottomLeft - new Vector2(20f, 8f), NPC.width + 40, 8, new Vector2(0f, -1f), 1.4f);
                    }
                }

                // Release: lock the crawl toward where the player stands now. Server-only spawn; the wave carries the lock.
                if (AttackTimer == GraspTellTicks)
                {
                    PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.5f });

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float deltaX = target.Center.X - NPC.Center.X;
                        float direction = 1f;
                        if (deltaX < 0f)
                        {
                            direction = -1f;
                        }

                        float crawlDistance = MathHelper.Clamp(System.Math.Abs(deltaX), GraspMinDistance, GraspMaxRange);
                        Vector2 spawn = NPC.Bottom + new Vector2(direction * NPC.width * 0.5f, -12f);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<DemonGraspWave>(), graspDamage, 0f, Main.myPlayer, direction, crawlDistance);
                    }
                }

                return;
            }

            NPC.velocity.X *= 0.9f;

            if (AttackTimer >= GraspTellTicks + GraspRecoveryTicks)
            {
                EndAttack();
            }
        }

        // Dark-red rim: the frame drawn 8 times behind itself, SilhouetteOffset px out in each direction. Then the base's
        // leap afterimages and the normal draw.
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (i * MathHelper.PiOver4).ToRotationVector2() * SilhouetteOffset;
                DrawBodyFrame(spriteBatch, texture, NPC.Center + offset, screenPos, SilhouetteColor * NPC.Opacity);
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new CommonDrop(ModContent.ItemType<StrengthPotion>(), 100, 10, 10, 40));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<FlameOfTheAbyss>(), 1, 2, 4));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<CursedSoul>(), 1, 5, 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 1, 2));
        }

        protected override void SpawnDeathGores()
        {
            int[] gores = { 1, 2, 3, 2, 3 };

            for (int i = 0; i < gores.Length; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                int goreType = Mod.Find<ModGore>("Great Abyss Demon Gore " + gores[i]).Type;
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, velocity, goreType, 1f);
            }
        }
    }
}
