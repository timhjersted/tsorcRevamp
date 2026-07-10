using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.OolacileSerpent
{
    [AutoloadBossHead]
    class OolacileSerpentHead : ModNPC, IStaggerable
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5; //How many copies of shadow/trail (Leonhard convention)
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Daybreak] = true;
        }
        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 6;
            NPC.width = 60;
            NPC.height = 60;
            DrawOffsetY = 42;
            NPC.aiStyle = 6;
            NPC.scale = 1.3f;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22500;
            NPC.damage = 60; //Only the head does contact damage (body/tail = 0); bite lunge contact = this
            NPC.defense = 100;
            NPC.HitSound = SoundID.NPCHit13;
            NPC.DeathSound = SoundID.Item119;
            NPC.lifeMax = (int)(650000 * (Main.masterMode ? 1.5f : 1));
            Music = 12;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 250000;
            NPC.lavaImmune = true;
            NPC.rarity = 46;
            Color textColor = new Color(175, 75, 255);
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.OolacileSerpentHead.DespawnHandler"), textColor, 174);

            //3s flop instead of the default 2s -- a boss this size reads as sturdier when it goes down.
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerDurationTicks = 180;
        }

        NPCDespawnHandler despawnHandler;

        //Total chain length: this head + BodySegmentCount body pieces + tail.
        public const int BodySegmentCount = 20;
        public const int TotalSegmentCount = BodySegmentCount + 2;
        //Front pieces (head + these many body segments, counted from the head) may lift off the ground.
        //Rear pieces (the remaining body segments + tail) stay ground-snapped. See SerpentAI.
        public const int FrontFreeSegmentCount = 10;

        //-- SerpentAI state (movement/behavior fields owned by the head, read/written via SerpentAI) --
        public enum BurrowPhase { None, Descending, Ascending }
        public BurrowPhase Burrow = BurrowPhase.None;
        public int BurrowTimer;
        public int BurrowCooldown;
        public Vector2 BurrowTargetHeadPos;
        public int FleeingTimer;
        public float LastPlayerDistance = float.MaxValue;
        public int NoLineOfSightTimer;
        //Reserved hook: a future attack can call RequestBurrow() to force a burrow-and-resurface next tick.
        public bool BurrowRequested;
        public void RequestBurrow() => BurrowRequested = true;

        public int ChargeTelegraphTimer;
        public int ChargeTimer;
        public int ChargeCooldown;
        public Vector2 ChargeDirection;

        public int RippleTimer;
        public int RippleCooldown;

        public float SwimWaveTimer;

        //-- Attack state --
        public enum AttackState
        {
            None,
            BiteTelegraph, BiteLunge, BiteRecover,
            PounceTelegraph, PounceLunge, PounceRecover,
            BreathTelegraph, BreathSweep, BreathRecover,
            SpitTelegraph, SpitCombo, SpitRecover
        }
        public AttackState Attack = AttackState.None;
        public int AttackTimer;          //counts down within the current attack phase
        public int AttackCooldown;       //global gap between attacks
        public float AttackAnchorY;      //head Y at telegraph start, so the arch/raise has a stable reference
        public Vector2 LungeVelocity;    //locked at lunge start (Leonhard-style: no homing, fairly dodgeable)
        public float BreathBaseAngle;
        public int SpitVariation;
        public int SpitTick;             //elapsed ticks within the spit combo

        public const int MouthTransitionTicks = 4;
        public int MouthTransitionTimer;

        public bool IsLunging => Attack == AttackState.BiteLunge || Attack == AttackState.PounceLunge;
        public bool IsMouthAttackActive =>
            Attack == AttackState.BiteTelegraph || Attack == AttackState.BiteLunge ||
            Attack == AttackState.PounceTelegraph || Attack == AttackState.PounceLunge ||
            Attack == AttackState.BreathTelegraph || Attack == AttackState.BreathSweep ||
            Attack == AttackState.SpitTelegraph || Attack == AttackState.SpitCombo;

        //-- AcidBody (below 50% HP): body pieces trail purple dust + acid pools while slithering --
        public int AcidBodyTimer;      //>0 = actively trailing acid
        public int AcidBodyCooldown;

        //-- GroundPierce: semi-independent tail attack from underground --
        //The tail never detaches: it TRAVELS underground (hugging the terrain contour at depth) to beneath
        //the player, dragging the rear body along via a taut-link rope pass, stabs, and travels back.
        public enum PierceState { None, Sinking, Traveling, Aiming, Stabbing, Retracting, Returning }
        public PierceState Pierce = PierceState.None;
        public int PierceTimer;        //phase countdown; in Traveling = min-gap-between-stabs countdown
        public int PierceTravelTimer;  //counts up while traveling; aborts the attack on timeout
        public int PierceCooldown;
        public int PierceCombo;        //stabs performed this cycle (max 4)
        public Vector2 PierceTarget;   //locked player position when the warning dusts fire
        public float PierceGroundY;    //ground surface world-Y below the target

        public override bool CheckActive()
        {
            return false;
        }

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            int[] bodyTypes = SerpentAI.BuildBodyTypes();
            SerpentAI.Run(NPC, ModContent.NPCType<OolacileSerpentHead>(), bodyTypes, ModContent.NPCType<OolacileSerpentTail>(), TotalSegmentCount, 18f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Attack == AttackState.BiteLunge)
            {
                target.AddBuff(BuffID.Bleeding, 16 * 60, false);
            }
            else if (Attack == AttackState.PounceLunge)
            {
                target.AddBuff(BuffID.Venom, 10 * 60, false);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            int frame = 0;
            if (IsMouthAttackActive)
            {
                frame = MouthTransitionTimer > 0 ? 1 : 2;
                if (MouthTransitionTimer > 0)
                {
                    MouthTransitionTimer--;
                }
            }
            else
            {
                MouthTransitionTimer = 0;
            }

            NPC.frame.Y = frameHeight * frame;
        }

        //Sprite echoes during the bite/pounce lunges, same convention as LeonhardPhase2.PreDraw.
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (IsLunging)
            {
                Texture2D texture = TextureAssets.Npc[NPC.type].Value;
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height / 2f);
                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] + new Vector2(NPC.width / 2f, NPC.height / 2f + DrawOffsetY + NPC.gfxOffY) - screenPos;
                    Color color = NPC.GetAlpha(drawColor) * ((float)(NPC.oldPos.Length - k) / NPC.oldPos.Length) * 0.6f;
                    spriteBatch.Draw(texture, drawPos, NPC.frame, color, NPC.rotation, origin, NPC.scale, effects, 0f);
                }
            }
            return true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.OolacileSerpentBag>()));
        }

        //Poise break: cancel whatever SerpentAI was mid-doing so the flop doesn't fight leftover state.
        public void OnStagger(NPC npc) => SerpentAI.OnStagger(npc);
    }
}
