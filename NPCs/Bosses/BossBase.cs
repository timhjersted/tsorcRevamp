using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses
{
    abstract class BossBase : ModNPC
    {
        /// A base to build bosses quickly and easily from!
        /// 
        /// Make your boss NPC inherit from this by going
        /// class YourBossName : BossBase
        /// at the top instead of the usual
        /// class YourBossName : ModNPC

        /// <summary>
        /// Override this to set your bosses debuff immunities and frameCount
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        /// <summary>
        /// Override this to set your bosses stats and properties.
        /// Remember to call base.SetDefaults(); first so that you don't have to rewrite all the basic stuff
        /// </summary>
        public override void SetDefaults()
        {
            //These will always be the same for every boss
            //At the top of your SetDefaults, make sure to call base.SetDefaults()
            //That will take care of all of these for you
            NPC.aiStyle = -1;
            NPC.friendly = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;

            /* These will be different for every boss, and you will have to set them yourself
            NPC.width = 110;
            NPC.height = 170;
            NPC.damage = 50;
            NPC.defense = 35;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 325000;
            NPC.timeLeft = 22500;
            NPC.value = 600000;
            despawnHandler = new NPCDespawnHandler(LanguageUtils.GetTextValue("NPCs.BossBase.Despawnhandler"), Color.DeepSkyBlue, 180);
            */
        }


        //These variables control basic parts of the bosses behavior
        //Things like damage numbers, how long it should pause between attacks, etc
        #region Basic control variables

        private Dictionary<string, int> DamageNumbersInternal;

        /// <summary>
        /// Override this and put your projectile damage values in it, to keep them nice and neat (and let them be modified from other places!).
        /// Remember: Contact damage gets multiplied by 2, and projectile damage gets multiplied by 4!
        /// </summary>
        public virtual Dictionary<string, int> DamageNumbers
        {
            get => DamageNumbersInternal;
            set => DamageNumbersInternal = value;
        }

        /// <summary>
        /// If true it picks a random attack each time instead of going in a deterministic order
        /// </summary>
        public bool randomAttacks = false;

        /// <summary>
        /// If true it randomly picks every attack in its list once, then refills the list
        /// If false it always picks a truly random attack, allowing repeats
        /// Does nothing unless randomAttacks is true
        /// </summary>
        public bool attackKnockoutList = true;

        /// <summary>
        /// Set this to ModContent.ItemType<Items.Bossbags.YourBossBagType>(); to make it drop its bag
        /// </summary>
        public int bossBagType = -1;


        /// <summary>
        /// Controls how long the boss should wait while changing attacks
        /// </summary>
        public int attackTransitionDuration = 0;

        /// <summary>
        /// Controls long the boss should wait while changing phases
        /// </summary>
        public int phaseTransitionDuration = 0;

        /// <summary>
        /// Controls how long this bosses intro will be
        /// </summary>
        public int introDuration = 0;

        /// <summary>
        /// Controls how long this bosses death animation will be.
        /// It will die for real when the animation counter reaches this number.
        /// </summary>
        public int deathAnimationDuration = 0;

        /// <summary>
        /// Set this to true when skipping a move to mostly skip attackTransitionDuration (sets it to 30).
        /// To skip a move, check your condition for skipping at the start of the move, then set this bool to true and call NextMove();
        /// </summary>
        public bool justSkippedMove = false;
        #endregion



        //These variables are used inside the AI for various purposes
        //Things like timers, what move is active, etc
        #region Internal Variables

        /// <summary>
        /// The function containing the current move the boss is performing
        /// </summary>
        public BossMove CurrentMove
        {
            get => MoveList[MoveIndex];

            set
            {
                for(int i = 0; i < MoveList.Count; i++)
                {
                    if (MoveList[i] == value)
                    {
                        MoveIndex = i;
                    }
                }
            }
        }

        /// <summary>
        /// The list containing every move in the bosses arsenal
        /// </summary>
        public List<BossMove> MoveList;


        /// <summary>
        /// The list containing every move in the bosses arsenal
        /// </summary>
        public List<BossMove> UsableMoveList;

        /// <summary>
        /// Controls what move is currently being performed
        /// </summary>
        public int MoveIndex = 0;

        /// <summary>
        /// Stores what move will be performed next
        /// This is stored in ai[0] so that it will get synced in advance
        /// </summary>
        public int NextMoveIndex
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        /// <summary>
        /// Used by moves to keep track of how long they've been going for
        /// </summary>
        public int MoveTimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        /// <summary>
        /// If the boss has multiple phases, the one it is currently in is stored here
        /// </summary>
        public int Phase
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        /// <summary>
        /// The player that the boss is currently targeting
        /// </summary>
        public Player Target
        {
            get => Main.player[NPC.target];
        }

        /// <summary>
        /// Returns a vector pointing from the NPC toward their target, with length 1.
        /// Multiply it by whatever speed you want to use it as a projectile velocity.
        /// </summary>
        public Vector2 TargetVector
        {
            get => UsefulFunctions.Aim(NPC.Center, Target.Center, 1);
        }

        /// <summary>
        /// The angle the boss will smoothly rotate toward at a speed of rotationSpeed
        /// </summary>
        public float rotationTargetAngle = 0;

        /// <summary>
        /// The speed at which the boss will rotate toward its target rotation angle
        /// </summary>
        public float rotationSpeed = 0;

        /// <summary>
        /// Stores a reference to the NPCDespawnHandler object for this boss, which tracks what players have or have not been killed and only lets it target ones who have not yet died
        /// </summary>
        public NPCDespawnHandler despawnHandler;

        /// <summary>
        /// Use this to coordinate an introduction animation or behavior for a boss
        /// </summary>
        public int introTimer;

        /// <summary>
        /// Stores how far it is into its transition between attacks
        /// </summary>
        public int attackTransitionTimeRemaining;

        /// <summary>
        /// Stores how far it is into its transition between attacks
        /// </summary>
        public int phaseTransitionTimeRemaining;

        public bool inPhaseTransition
        {
            get => phaseTransitionTimeRemaining > 0;
        }

        public bool inIntro
        {
            get => introTimer < introDuration;
        }

        public bool dying
        {
            get => deathAnimationProgress > 0;
        }

        public bool aiPaused
        {
            get => inIntro || inPhaseTransition || dying;
        }

        /// <summary>
        /// Stores how far it is into its death animation
        /// </summary>
        public int deathAnimationProgress;
        #endregion



        //These functions control what the boss does
        //These are often very different between bosses, and are where its custom behavior is
        #region Behavior Functions

        /// <summary>
        /// Add all the moves for your boss in here!
        /// You need the function name, the time the attack lasts, and optionally can specify a color (for use with things like VFX or lighting)
        /// </summary>
        public virtual void InitializeMovesAndDamage() { }

        /// <summary>
        /// Controls what this boss does during its intro
        /// If it doesn't have one, this gets skipped
        /// </summary>
        public virtual void HandleIntro() { }

        /// <summary>
        /// If your boss has multiple phases or does things at certain health percents, it goes in here
        /// </summary>
        public virtual void HandleLife() { }

        /// <summary>
        /// Override this to make things happen when this boss is killed
        /// Useful for spawning dusts
        /// </summary>
        public virtual void HandleDeath()
        {
            NPC.dontTakeDamage = true;
            if (deathAnimationProgress == deathAnimationDuration)
            {
                NPC.dontTakeDamage = false;
                NPC.StrikeNPC(NPC.CalculateHitInfo(999999, 1, true, 0), false, false);
            }
        }

        /// <summary>
        /// Override this to make the boss do things while transitioning between attacks
        /// </summary>
        public virtual void AttackTransition() { }

        /// <summary>
        /// Override this to make the boss do things while transitioning between phases
        /// </summary>
        public virtual void PhaseTransition() { }

        /// <summary>
        /// Override this to add custom VFX to your boss
        /// </summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        /// <summary>
        /// Override this to add custom animation to your boss
        /// </summary>
        public override void FindFrame(int currentFrame) { }

        /// <summary>
        /// Override this to set what type of potion the boss should drop on death.
        /// The potions that should be dropped for bosses at various points in the game are as follows:  
        /// Pre-Hardmode > ItemID.HealingPotion,
        /// Hardmode > ItemID.GreaterHealingPotion,
        /// SuperHardmode > ItemID.SuperHealingPotion
        /// </summary>
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        #endregion



        //These functions control the core of how the boss works
        //Selecting and changing attacks, stopping it from despawning, etc
        //Most of these are very similar on every boss, but can always be overridden if necessary
        #region Core functions

        //Override this to add stuff directly to its AI itself
        public bool despawning;
        public override void AI()
        {
            despawning = despawnHandler.TargetAndDespawn(NPC.whoAmI);
            HandleLife();
            Rotate();

            //If the move list has not been initialized then do so
            if (MoveList == null)
            {
                InitializeMovesAndDamage();
                do
                {
                    NextMoveIndex = Main.rand.Next(MoveList.Count);
                } while (NextMoveIndex != 0);

                //Create the 'used move list'
                UsableMoveList = new List<BossMove>();
                for (int i = 0; i < MoveList.Count; i++)
                {
                    UsableMoveList.Add(MoveList[i]);
                }

                //Remove the two moves already picked (0 is always the first move)
                UsableMoveList.RemoveAt(0);
                UsableMoveList.RemoveAt(NextMoveIndex);
            }

            //If it's doing an attack or phase transition, then do nothing else until it's done
            if (attackTransitionTimeRemaining > 0)
            {
                AttackTransition();
                attackTransitionTimeRemaining--;
                return;
            }

            //If it's doing an attack or phase transition, then do nothing else until it's done
            if (phaseTransitionTimeRemaining > 0)
            {
                PhaseTransition();
                phaseTransitionTimeRemaining--;
                return;
            }

            //If it hasn't finished its intro, then do nothing else until it's done
            if (introTimer <= introDuration)
            {
                HandleIntro();
                introTimer++;
                return;
            }

            //If the NPC is dying, then do nothing else until it's done
            if (NPC.life == 1)
            {
                if (deathAnimationProgress <= deathAnimationDuration)
                {
                    HandleDeath();
                    deathAnimationProgress++;
                    return;
                }
            }

            //Otherwise, perform its current move normally
            PerformMove();
        }

        /// <summary>
        /// Makes the boss smoothly rotate toward its target rotation angle
        /// Does nothing unless you set the target rotation angle somewhere else
        /// </summary>
        public void Rotate()
        {
            Vector2 targetRotation = rotationTargetAngle.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = nextRotationVector.ToRotation();
        }

        public void PerformMove()
        {
            //Change -1 to another number to make the boss just perform that one attack on loop
            //Useful for testing, so you don't have to wait
            int testAttack = -1;
            if (testAttack != -1)
            {
                MoveIndex = 0;
            }

            //Run the current move, then increment the timer by 1 afterward
            CurrentMove.Move();
            Lighting.AddLight(NPC.Center / 16, CurrentMove.MoveColor.ToVector3());
            MoveTimer++;

            //If the time is up, then switch to the next move
            if (MoveTimer > CurrentMove.timeLimit)
            {
                NextMove();
            }
        }

        //Roll 30 sequences of attacks and spit the attacks chosen for each out in chat
        //Used for ensuring it's all working right
        void TestNextMove()
        {
            for (int i = 0; i < 30; i++)
            {
                String s = "";
                for (int j = 0; j < MoveList.Count; j++)
                {
                    s += MoveIndex + " ";
                    NextMove();
                    s += MoveIndex + " ";
                    NextMove();
                    s += MoveIndex + " ";
                    NextMove();
                }
                Main.NewText(s);
            }
        }

        /// <summary>
        /// Changes the attack to the next one
        /// If it hits the end of the list, it loops
        /// </summary>
        public void NextMove()
        {

            if (!randomAttacks)
            {
                //Increment the move list
                MoveIndex++;

                //If we're at the end of the move list then go back to the start
                if (MoveIndex >= MoveList.Count)
                {
                    MoveIndex = 0;
                }
            }
            else
            {
                //The default random mode is 'knockout list', where it uses each attack once in a random order, shuffles them, then repeats
                //This ensures it will never use the same one twice in a row, and that all attacks are used with the same frequency
                if (attackKnockoutList)
                {
                    MoveIndex = NextMoveIndex;

                    //If all the moves are used, reset the list
                    if (UsableMoveList.Count == 0)
                    {
                        for (int i = 0; i < MoveList.Count; i++)
                        {
                            UsableMoveList.Add(MoveList[i]);
                        }
                    }


                    //Pick a random entry from UsableMoveList, and make sure it's not the current move
                    //This is necessary as UsableMoveList could have been reset just now.
                    //This ensures the last move of one set can't be the same as the first move of the next set
                    do
                    {
                        NextMoveIndex = Main.rand.Next(MoveList.Count);
                    } while (MoveList[NextMoveIndex] == CurrentMove || !UsableMoveList.Contains(MoveList[NextMoveIndex]));

                    UsableMoveList.Remove(MoveList[NextMoveIndex]);
                }
                else
                {
                    //If it's not using a 'knockout list' and is truly random, then just pick the next one randomly
                    //Set the 'next move' to be the current move
                    MoveIndex = NextMoveIndex;

                    //Pick the next move randomly
                    NextMoveIndex = Main.rand.Next(MoveList.Count);
                }

                //Sync it
                NPC.netUpdate = true;
            }

            if (justSkippedMove) //If you skipped the previous move
            { 
                attackTransitionTimeRemaining = 30; //I set the time to half a second to allow tyme to sync... Should be enough - Chroma 
                justSkippedMove = false; //Reset justSkippedMove
            }
            else { attackTransitionTimeRemaining = attackTransitionDuration; }
            MoveTimer = 0;
        }

        public void NextPhase()
        {
            MoveTimer = 0;
            phaseTransitionTimeRemaining = phaseTransitionDuration;
            Phase++;
        }

        /// <summary>
        /// Makes the NPC drop its bag
        /// </summary>
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            if (bossBagType != -1)
            {
                npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(bossBagType));
            }
        }

        /// <summary>
        /// Makes the NPC not respawn due to distance
        /// </summary>
        public override bool CheckActive()
        {
            return false;
        }

        /// <summary>
        /// If the NPC has a death animation, this stops it from dying until it is complete
        /// </summary>
        public override bool CheckDead()
        {
            if (deathAnimationProgress < deathAnimationDuration)
            {
                if (NPC.life <= 0)
                {
                    NPC.life = 1;
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion
    }

    /// <summary>
    /// Stores all the data relevant to a bosses attack in one object.
    /// Feel free to add extra optional fields if you think of any useful ones.
    /// </summary>
    public class BossMove
    {
        public Action Move;
        public int timeLimit;
        public int ID;
        public Color MoveColor;
        public int id;

        public BossMove(Action MoveAction, int attackDuration, Color? color = null, int id = 0)

        {
            Move = MoveAction;
            timeLimit = attackDuration;
            if (color.HasValue)
            {
                MoveColor = color.Value;
            }
            else
            {
                MoveColor = new Color(0, 0, 0, 0);
            }

            this.id = id;
        }
    }
}