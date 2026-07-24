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
    class GreatSerpentHead : ModNPC, IStaggerable
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
            //Head sprite sheet = 76 x (3 frames). Frame height auto-adapts to the PNG (76x303 -> 101/frame now;
            //a 76x468 sheet -> 156/frame, both slice cleanly). Hitbox is a fair sub-rect of the visible head.
            //The neck-removed crop leaves ~15-25px of transparent padding below the rounded stub within each
            //frame. That padding is compensated in PreDraw's rotation ORIGIN (HeadStubInsetFromBottom), not via
            //DrawOffsetY -- DrawOffsetY is a fixed SCREEN-space nudge that never rotates with the head, so it
            //produced a visual gap that stayed constant no matter how the neck was bent (the "head always N px
            //above the body" bug). Leave this at 0; see PreDraw.
            NPC.width = 50;
            NPC.height = 64;
            DrawOffsetY = 0;
            NPC.aiStyle = -1; // fully custom AI (SerpentAI); -1 stops vanilla worm AI + its dig sound
            NPC.scale = 1f;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22500;
            NPC.damage = 0; //Contact damage is enabled per-attack in SerpentAI (bite/pounce lunge + charge on the head, stab on the tail). Body pieces are always 0.
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
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.GreatSerpentHead.DespawnHandler"), textColor, 174);

            //3s flop instead of the default 2s -- a boss this size reads as sturdier when it goes down.
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerDurationTicks = 180;
        }

        NPCDespawnHandler despawnHandler;

        //Total chain length: this head + BodySegmentCount body pieces + tail. More, smaller segments = denser
        //packing = smoother curves, more length for the tail attack to move, and more body left on the ground.
        public const int BodySegmentCount = 48;
        public const int TotalSegmentCount = BodySegmentCount + 2;
        //Front pieces (head + these many body segments, counted from the head) may lift off the ground while
        //climbing. Rear pieces stay ground-snapped. See SerpentAI.
        public const int FrontFreeSegmentCount = 14;
        //Only the last TailAttackSegmentCount body pieces (+ tail) rear up for the tail attack; everything ahead
        //of them stays on the ground. Keeps most of the snake grounded while the tail-end strikes.
        public const int TailAttackSegmentCount = 16;

        //-- Head movement (kiting) --
        //Facing is held with hysteresis (only flips once the player is clearly past a deadzone) so a player
        //sitting near the head's X can't make it jitter left/right every frame.
        public int Facing = 1;
        //CrossOver: once it reaches the player it may advance THROUGH to the far side, then attack from there,
        //instead of endlessly ramming. Never moves backwards otherwise.
        public bool CrossingOver;
        public int CrossOverCooldown;
        public int CrossOverDir;

        //Anti-stuck failsafe. If the head can't make progress (wedged on a ledge, marooned in a room above the
        //player, boxed in), it forces a terrain-respecting RunWander window for a while. Without this it can end
        //up permanently marooned, since it otherwise only ever rides the surface it's standing on. NEVER clips
        //through terrain -- a prior version of this failsafe (Unstick) did, and was removed per feedback.
        public Vector2 StuckCheckPos = Vector2.Zero;
        public int StuckTimer;
        public int StuckWanderTimer;
        //Climb budget: how many more tiles of vertical rise this climb is allowed, so it can't ascend forever
        //(that's how it ended up inside a ceiling).
        public int ClimbBudget;

        //-- SmartSerpent4AI navigation (see NPCs/SmartSerpent4AI.cs) --
        //Span-graph A* plan state. Lives directly on the head alongside the other nav-adjacent fields
        //(ClimbBudget/StuckTimer), per this boss's convention -- not a separate NavState object like SF4's.
        public System.Collections.Generic.List<SmartSerpent4AI.PlanStep> Plan;
        public int PlanIndex;
        public int PlanStepTimer;        //progress/timeout countdown for the current step
        public int ReplanCooldownTimer;  //min gap between graph rebuilds (SF4's ReplanCooldown convention)
        //Recently-failed step targets (tile coords) -> expiry tick; the planner penalizes routes through them.
        public System.Collections.Generic.Dictionary<(int x, int y), int> BadEdges = new System.Collections.Generic.Dictionary<(int, int), int>();
        public string LastPlanResult = "";

        //How long it's been unable to reach the player (no LOS / out of range). Drives wander, then despawn.
        public int UnreachableTimer;
        public int WanderDir = 1;

        //Diagnostics (Logs/tsorcRevamp-serpent.log)
        public string LastAction = "init";

        //Repeating slither-movement sound cadence (only plays near top speed).
        public int MoveSoundTimer;

        //Tail hide/reveal: 0 = fully collapsed/invisible into the last body segment, 1 = fully out. Idles at 0;
        //ramps to 1 the instant a tail attack starts (so it "pokes out" through Coiling) and back to 0 after.
        public float TailExtend;

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

        //-- TailStab: above-ground tail strike (no burrowing). Two modes chosen by geometry at trigger time:
        //  OverheadC   -- head faces the player (whole snake to one side): the tail-end curls into a C that arcs
        //                 OVER the head and stabs down past it at the player.
        //  HorizontalS -- head faces away / is far (it slithered past the player, tail still near them): the
        //                 tail-end whips a shallow horizontal S sideways at the player.
        //The head holds still on the ground during either; posed pieces come from SerpentAI.PoseTailStabArc.
        public enum TailStabState { None, Coiling, Aiming, Stabbing, Recover, Retracting }
        public enum TailStabKind { OverheadC, HorizontalS }
        public TailStabState TailStab = TailStabState.None;
        public TailStabKind TailStabMode = TailStabKind.OverheadC;
        public int TailStabTimer;
        public int TailStabCooldown;
        public int TailStabCombo;       //stabs performed this cycle
        public Vector2 TailStabTarget;  //locked player pos at aim time
        public Vector2 TailStabTip;     //current driven tail-tip world pos (lerped toward the phase target)
        public Vector2 TailStabAnchor;  //junction (last grounded segment) world center = the arc's base
        public bool TailStabDamaging;   //true only during the downward stab -> tail contact damage on

        //-- Predator-style hunting cloak + scripted HP-threshold ambush --
        //0 = fully visible; >0 = the alpha the WHOLE chain (every segment reads this off the head) should show.
        //Two different depths: a light cloak while hunting without line of sight, a much deeper one during the
        //scripted ambush below. See SerpentAI's IdleCloakAlpha/AmbushCloakAlpha/CloakCooldownTicks.
        public int CloakAlphaTarget;
        public int CloakCooldown; //ticks left before the hunting cloak (not the ambush) may trigger again

        //Retreat-then-sneak-attack: at each 30%-of-max-HP step lost, vanish, sneak back in from the shadows,
        //and reveal itself right as the next attack's telegraph begins (see SerpentAI.RunAmbush).
        public enum AmbushState { None, Vanish, Reposition }
        public AmbushState Ambush = AmbushState.None;
        public int AmbushTimer;
        public int AmbushThresholdsTriggered; //how many 30% HP steps have already spent their ambush, so each fires once

        //-- Scripted death: fade to invisible + spray blood instead of an instant vanilla pop --
        //CheckDead() intercepts the killing blow (see below) so this can play out over DeathFadeDuration ticks
        //before the real kill (loot/OnKill/etc) actually happens.
        public bool IsDying;
        public int DeathFadeTimer;

        public override bool CheckActive()
        {
            return false;
        }

        ///<summary>
        ///Intercepts the killing blow: the first time life would hit 0, cancel the real death (return false),
        ///peg life at 1 and stop damage, and let SerpentAI's RunDeathFade play a fade-to-invisible + blood-spray
        ///sequence in AI() for DeathFadeTimer ticks. Once that finishes it sets life back to 0 and calls
        ///NPC.checkDead() itself, which calls back in here -- by then DeathFadeTimer is <=0, so this returns true
        ///and the real kill (loot, OnKill, despawn) finally goes through, exactly once.
        ///</summary>
        public override bool CheckDead()
        {
            if (!IsDying)
            {
                IsDying = true;
                DeathFadeTimer = SerpentAI.DeathFadeDuration;
                NPC.life = 1;
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;
                return false;
            }
            return DeathFadeTimer <= 0;
        }

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            int[] bodyTypes = SerpentAI.BuildBodyTypes();
            //4f pursue speed -- deliberately slow (hardmode pacing); kiting caps it further near the player.
            SerpentAI.Run(NPC, ModContent.NPCType<GreatSerpentHead>(), bodyTypes, ModContent.NPCType<GreatSerpentTail>(), TotalSegmentCount, 4f);
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

        //The frame's rotation pivot: normally frame-center, but the neck-removed crop leaves transparent padding
        //BELOW the actual neck stub, so the true joint sits above the frame's bottom edge by this many pixels.
        //MUST live in the rotation origin (rotates with the sprite), not a translated draw position -- see the
        //DrawOffsetY comment in SetDefaults for why. Bumped 20 -> 60 (+40px) per in-game feedback: the first
        //guess still left a visible gap between the head and the neck.
        const float HeadStubInsetFromBottom = 60f;

        //Full custom draw (returns false): sprite echoes during the bite/pounce lunges, same convention as
        //LeonhardPhase2.PreDraw, then the real head using a pivot that stays correct under rotation.
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height - HeadStubInsetFromBottom);

            if (IsLunging)
            {
                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] + new Vector2(NPC.width / 2f, NPC.height / 2f) - screenPos;
                    Color color = NPC.GetAlpha(drawColor) * ((float)(NPC.oldPos.Length - k) / NPC.oldPos.Length) * 0.6f;
                    spriteBatch.Draw(texture, drawPos, NPC.frame, color, NPC.rotation, origin, NPC.scale, effects, 0f);
                }
            }

            spriteBatch.Draw(texture, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, origin, NPC.scale, effects, 0f);
            return false;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.OolacileSerpentBag>()));
        }

        //Poise break: cancel whatever SerpentAI was mid-doing so the flop doesn't fight leftover state.
        //Also strips the cloak -- a staggered flop should always be fully visible, win or lose.
        public void OnStagger(NPC npc)
        {
            SerpentAI.OnStagger(npc);
            CloakAlphaTarget = 0;
            npc.alpha = 0;
        }
    }
}
