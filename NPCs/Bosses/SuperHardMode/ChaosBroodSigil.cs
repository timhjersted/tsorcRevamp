using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Chaos;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    ///<summary>
    ///A sigil from Chaos's Fiend's Brood: an immobile rune that fades in, then snipes the nearest player with
    ///a telegraphed bolt until it is destroyed.
    ///
    ///It is an NPC rather than a projectile for one reason — projectiles have no health, and being destructible
    ///IS the attack. This is the only thing in Chaos's kit that asks the player a question other than "dodge":
    ///spend damage clearing sigils, or eat the bolts and keep hitting the boss.
    ///
    ///Spawned by Chaos and orphaned deliberately: it outlives the cast that made it, so it overlaps whatever
    ///Chaos does next. ai[0] = index of the Chaos that owns it, so the brood dies with its parent.
    ///</summary>
    class ChaosBroodSigil : ModNPC
    {
        public const int FadeInTicks = 45;
        public const int BoltInterval = 90;
        public const int BoltTelegraphTicks = 30;
        public const int LifeTicks = 1200;          // ~20 seconds if nobody kills it

        const float BoltSpeed = 13f;
        const float BoltRange = 900f;   // how far the telegraph line is drawn; the bolt itself flies further
        const float SigilRadius = 41f;              // half the 82px sprite

        int Age => (int)NPC.ai[1];
        int OwnerIndex => (int)NPC.ai[0];

        ///<summary>Staggered per sigil so the three never volley in unison — ai[2] holds this one's offset.</summary>
        int BoltOffset => (int)NPC.ai[2];

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 82;
            NPC.height = 82;
            NPC.aiStyle = -1;
            AnimationType = -1;
            NPC.damage = 0;                 // it snipes; touching it is harmless
            NPC.defense = 0;
            NPC.lifeMax = 4000;
            NPC.HitSound = SoundID.Item27;
            NPC.DeathSound = SoundID.Item14;
            NPC.value = 0f;
            NPC.npcSlots = 0f;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontCountMe = true;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return 0f;
        }

        public override void AI()
        {
            NPC.ai[1]++;
            NPC.velocity = Vector2.Zero;

            // Die with the parent: a brood outliving its Chaos would be an unkillable turret with no boss bar.
            if (OwnerIndex < 0 || OwnerIndex >= Main.maxNPCs || !Main.npc[OwnerIndex].active || Main.npc[OwnerIndex].type != ModContent.NPCType<Chaos>())
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.active = false;
                return;
            }

            if (Age >= LifeTicks)
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.active = false;
                return;
            }

            // Arrival: dust gathers into the rune, and it cannot fire until it has fully formed.
            if (Age < FadeInTicks)
            {
                NPC.dontTakeDamage = true;

                if (!Main.dedServ)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(120f, 120f);
                        Dust mote = Dust.NewDustPerfect(NPC.Center + offset, DustID.DemonTorch, -offset / 14f, 90, default, 1.3f);
                        mote.noGravity = true;
                    }
                }

                return;
            }

            NPC.dontTakeDamage = false;
            NPC.TargetClosest(false);

            Player target = Main.player[NPC.target];

            if (!target.active || target.dead)
            {
                return;
            }

            int tickInCycle = (Age - FadeInTicks + BoltOffset) % BoltInterval;

            // Lock the heading when the telegraph starts and draw the line ONCE. Redrawing it every tick fanned
            // one lane into thirty as the player moved, and recomputing the aim at fire time meant the bolt did
            // not go where the line promised. ai[3] carries the locked angle so both agree.
            if (tickInCycle == BoltInterval - BoltTelegraphTicks)
            {
                Vector2 aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                NPC.ai[3] = aim.ToRotation();
                NPC.netUpdate = true;

                if (!Main.dedServ)
                {
                    // A single thread rather than the usual three: a sigil bolt is a thin shot, and three sigils
                    // telegraphing at once was a lot of line on screen.
                    Chaos.DrawTelegraphLine(NPC.Center, NPC.Center + aim * BoltRange, Color.MediumPurple, strands: 1);
                }
            }

            // Skipped on the very first cycle of the offset-0 sigil, whose tickInCycle starts AT 0 and so would
            // otherwise fire before it had ever telegraphed.
            if (tickInCycle == 0 && Age > FadeInTicks + BoltTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 aim = NPC.ai[3].ToRotationVector2();

                // Flat damage: the sigil's own contact damage is 0, so there is nothing to scale off.
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, aim * BoltSpeed,
                    ModContent.ProjectileType<ChaosDemonBolt>(), Chaos.BroodBoltDamage, 1f);

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/mage_lord_projectile_impact") with { Volume = 0.5f, PitchVariance = 0.1f }, NPC.Center);
                }
            }

            Lighting.AddLight(NPC.Center, 0.5f, 0.15f, 0.7f);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
            {
                return;
            }

            int motes = 6;
            if (NPC.life <= 0)
            {
                motes = 45;
            }

            for (int i = 0; i < motes; i++)
            {
                Vector2 burst = Main.rand.NextVector2Circular(5f, 5f);
                Dust mote = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(SigilRadius, SigilRadius), DustID.DemonTorch, burst, 80, default, 1.5f);
                mote.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

            // Scale and alpha both ride the fade-in, so "not yet dangerous" is visible at a glance.
            float formed = MathHelper.Clamp(Age / (float)FadeInTicks, 0f, 1f);
            float pulse = 1f + (float)System.Math.Sin(Age * 0.06f) * 0.06f;

            spriteBatch.Draw(texture, NPC.Center - screenPos, null, Color.White * formed,
                Age * 0.01f, origin, formed * pulse, SpriteEffects.None, 0f);

            return false;
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}
