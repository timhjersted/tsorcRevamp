using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Runeterra;

class RuneMage : ModNPC
{
    public override string LocalizationCategory => "NPCs.Runeterra";
    public const int Frames = 28;
    private NPCDespawnHandler despawnHandler;
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = Frames;
        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
    }

    public const int ContactDmg = 0;
    public const int Defense = 10;
    public const int BaseMaxHP = 5000;
    public float HealthScale;
    public const int Value = 50000;
    public override void SetDefaults()
    {
        NPC.aiStyle = -1;
        NPC.friendly = false;
        NPC.lavaImmune = true;
        NPC.boss = true;
        NPC.knockBackResist = 0f;
        NPC.noGravity = false;
        NPC.noTileCollide = false;
        NPC.width = 60;
        NPC.height = 104;
        NPC.scale = 0.6f;
        NPC.damage = ContactDmg;
        NPC.defense = Defense;
        HealthScale = Main.masterMode ? 1.5f : 1f;
        NPC.lifeMax = (int)(BaseMaxHP * HealthScale);
        NPC.timeLeft = 22500;
        NPC.value = Value;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Runeterra.RuneMage.DespawnHandler"), Color.DarkBlue, DustID.MagicMirror);
    }

    public const int SpellFluxDmg = 20;
    public const int OverloadDmg = 20;
    public const int RunePrisonDmg = 20;

    public ref float AiState => ref NPC.ai[0];
    public ref float AiTimer1 => ref NPC.ai[1];
    
    public ref float AiTimer2 => ref NPC.ai[2];
    public ref float AiTimer3 => ref NPC.ai[3];
    
    public Vector2 WorldRunePosition = new Vector2(63600, 19400);
    public Vector2 WarningPosition = new Vector2(63240, 19400);
    public Vector2 SpawnPosition;
    public bool AppliedOnSpawn = false;
    public bool ShouldDrawWorldRune = true;
    public override void AI()
    {
        NPC.TargetClosest(false);
        var target = Main.player[NPC.target];
        NPC.chaseable = true;

        if (!AppliedOnSpawn)
        {
            SpawnPosition = NPC.position;
            AppliedOnSpawn = true;
        }
        //Main.NewText(NPC.position.Distance(WorldRunePosition));
        //WorldRunePosition = new Vector2(63600, 19400);
        //WarningPosition = new Vector2(63240, 19400);
        
        
        switch (AiState)
        {
            case (float)ActionState.IdlePatrolling:
            {
                IdlePatrolling(target);
                break;
            }
            case (float)ActionState.IsWarning:
            {
                IsWarning(target);
                break;
            }
            case (float)ActionState.StartingFight:
            {
                StartFight(target);
                break;
            }
            case (float)ActionState.WalkingPhase1:
            {
                WalkingPhase1(target);
                break;
            }
            case (float)ActionState.JumpingPhase1:
            {
                JumpingPhase1(target);
                break;
            }
            case (float)ActionState.PhaseTransition:
            {
                PhaseTransition(target);
                break;
            }
            case (float)ActionState.HoveringPhase2:
            {
                HoveringPhase2(target);
                break;
            }
            case (float)ActionState.SpellFlux:
            {
                SpellFlux(target);
                break;
            }
            case (float)ActionState.Overload:
            {
                Overload(target);
                break;
            }
            case (float)ActionState.RunePrison:
            {
                RunePrison(target);
                break;
            }
        }
        //Player player = Main.player[NPC.target];
        //player.position.Distance(WarningPosition);
        //Main.NewText(AiState);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (ShouldDrawWorldRune)
        {
            DrawWorldRune(spriteBatch, screenPos, drawColor);
        }
        return base.PreDraw(spriteBatch, screenPos, drawColor);
    }

    Texture2D WorldRuneSprite;
    public void DrawWorldRune(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        WorldRuneSprite = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Materials/WorldRune"); 
        Rectangle WorldRuneSourceRectangle = new Rectangle(0, 0, WorldRuneSprite.Width, WorldRuneSprite.Height); 
        Main.EntitySpriteDraw(WorldRuneSprite, WorldRunePosition - screenPos, WorldRuneSourceRectangle,
            Color.White, 0, WorldRuneSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
        Lighting.AddLight(WorldRunePosition, Color.DarkRed.ToVector3() * 1f);
    }

    public override void OnSpawn(IEntitySource source)
    {
        NPC.direction = 1;
        ServerPreRollingRandoms(1);
    }

    private int IdlePatrolRoll = 0;
    private void ServerPreRollingRandoms(in int rollType)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            switch (rollType)
            {
                case 1: //rolling for IdlePatrolling random
                {
                    if (AiTimer1 > 0) //if positive
                    {
                        IdlePatrolRoll = Main.rand.Next(-420, -180 + -1);
                    }
                    else //if negative
                    {
                        IdlePatrolRoll = Main.rand.Next(120, 300 + 1);
                    }
                    NPC.netUpdate = true;
                    break;
                }
            }
            
        }
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(IdlePatrolRoll);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        reader.ReadInt32();
    }

    private int FrameDuration;
    public override void FindFrame(int frameHeight)
    {
        switch (AiState)
        {
            case (float)ActionState.IdlePatrolling:
            {
                if (AiTimer1 < 0) NPC.frame.Y = (int)FrameState.Idle * frameHeight;
                FrameDuration = 5;
                if (NPC.velocity.X != 0)
                {
                    NPC.frameCounter++;
                    if (NPC.frameCounter >= FrameDuration)
                    {
                        NPC.frame.Y += frameHeight;
                        NPC.frameCounter = 0;

                        if (NPC.frame.Y >= ((int)FrameState.Walking14 + 1) * frameHeight)
                            NPC.frame.Y = (int)FrameState.Walking1 * frameHeight;
                    }
                }
                else
                {
                    NPC.frame.Y = (int)FrameState.Idle * frameHeight;
                }

                if (NPC.velocity.Y > 0 | NPC.velocity.Y < 0)
                {
                    NPC.frame.Y = (int)FrameState.Jumping * frameHeight;
                }

                break;
            }
            case (float)ActionState.IsWarning:
            {
                FrameDuration = 3;
                if (NPC.velocity.X != 0)
                {
                    NPC.frameCounter++;
                    if (NPC.frameCounter >= FrameDuration)
                    {
                        NPC.frame.Y += frameHeight;
                        NPC.frameCounter = 0;

                        if (NPC.frame.Y >= ((int)FrameState.Walking14 + 1) * frameHeight)
                            NPC.frame.Y = (int)FrameState.Walking1 * frameHeight;
                    }
                }
                else
                {
                    NPC.frame.Y = (int)FrameState.Idle * frameHeight;
                }
                
                if (NPC.velocity.Y > 0 | NPC.velocity.Y < 0)
                {
                    NPC.frame.Y = (int)FrameState.Jumping * frameHeight;
                }

                break;
            }
            case (float)ActionState.StartingFight:
            {
                var dividend = FirstRunePrisonCastTime / TotalRunePrisonFrames;
                switch (AiTimer1)
                {
                    case float one when AiTimer1 < dividend:
                    {
                        NPC.frame.Y = (int)FrameState.RunePrison1 * frameHeight;
                        Main.NewText(1);
                        break;
                    }
                    case float two when AiTimer1 > dividend && AiTimer1 < dividend * 2f:
                    {
                        NPC.frame.Y = (int)FrameState.RunePrison2 * frameHeight;
                        Main.NewText(2);
                        break;
                    }
                    case float three when AiTimer1 > dividend * 2f && AiTimer1 < dividend * 3f:
                    {
                        NPC.frame.Y = (int)FrameState.RunePrison3 * frameHeight;
                        Main.NewText(3);
                        break;
                    }
                    case float four when AiTimer1 > dividend * 3f && AiTimer1 < dividend * 4f:
                    {
                        NPC.frame.Y = (int)FrameState.RunePrison4 * frameHeight;
                        Main.NewText(4);
                        break;
                    }
                }
                FrameDuration = 3;
                if (NPC.velocity.X != 0)
                {
                    NPC.frameCounter++;
                    if (NPC.frameCounter >= FrameDuration)
                    {
                        NPC.frame.Y += frameHeight;
                        NPC.frameCounter = 0;

                        if (NPC.frame.Y >= ((int)FrameState.Walking14 + 1) * frameHeight)
                            NPC.frame.Y = (int)FrameState.Walking1 * frameHeight;
                    }
                }
                
                if (NPC.velocity.Y > 0 | NPC.velocity.Y < 0)
                {
                    NPC.frame.Y = (int)FrameState.Jumping * frameHeight;
                }

                if (AiTimer2 > 0)
                {
                    var dividend2 = PickupDuration / TotalRunePrisonFrames;
                    switch (AiTimer2)
                    {
                        case float one when AiTimer2 < dividend2:
                        {
                            NPC.frame.Y = (int)FrameState.RunePrison4 * frameHeight;
                            break;
                        }
                        case float two when AiTimer2 > dividend2 && AiTimer2 < dividend2 * 2f:
                        {
                            NPC.frame.Y = (int)FrameState.RunePrison3 * frameHeight;
                            break;
                        }
                        case float three when AiTimer2 > dividend2 * 2f && AiTimer2 < dividend2 * 3f:
                        {
                            NPC.frame.Y = (int)FrameState.RunePrison4 * frameHeight;
                            break;
                        }
                        case float four when AiTimer2 > dividend2 * 3f && AiTimer2 < dividend2 * 4f:
                        {
                            NPC.frame.Y = (int)FrameState.RunePrison1 * frameHeight;
                            break;
                        }
                    }
                }
                break;
            }
        }
        NPC.spriteDirection = -NPC.direction;
        /*
        FrameDuration = 10;

        switch (AiState)
        {
            case (float)ActionState.Wandering:
            {
                if (AiTimer == 1) NPC.frame.Y = (int)FrameState.Wandering1 * frameHeight;
                FrameDuration = 10;
                if (NPC.velocity.X != 0)
                {
                    NPC.frameCounter++;
                    if (NPC.frameCounter >= FrameDuration)
                    {
                        NPC.frame.Y += frameHeight;
                        NPC.frameCounter = 0;

                        if (NPC.frame.Y >= ((int)FrameState.Wandering5 + 1) * frameHeight)
                            NPC.frame.Y = (int)FrameState.Wandering1 * frameHeight;
                    }
                }
                else
                {
                    NPC.frame.Y = (int)FrameState.Wandering1 * frameHeight;
                }

                break;
            }
            case (float)ActionState.Provoked:
            {
                var dividend = ProvokedDuration / TotalProvokedFrames;
                switch (AiTimer)
                {
                    case float one when AiTimer < dividend:
                    {
                        NPC.frame.Y = (int)FrameState.Provoked1 * frameHeight;
                        break;
                    }
                    case float two when AiTimer > dividend && AiTimer < dividend * 2f:
                    {
                        NPC.frame.Y = (int)FrameState.Provoked2 * frameHeight;
                        break;
                    }
                    case float three when AiTimer > dividend * 2f && AiTimer < dividend * 3f:
                    {
                        NPC.frame.Y = (int)FrameState.Provoked3 * frameHeight;
                        break;
                    }
                    case float four when AiTimer > dividend * 3f && AiTimer < dividend * 4f:
                    {
                        NPC.frame.Y = (int)FrameState.Provoked4 * frameHeight;
                        break;
                    }
                    case float five when AiTimer > dividend * 4f && AiTimer < dividend * 5f:
                    {
                        NPC.frame.Y = (int)FrameState.Provoked5 * frameHeight;
                        break;
                    }
                    case float six when AiTimer > dividend * 5f && AiTimer < dividend * 6f:
                    {
                        NPC.frame.Y = (int)FrameState.Provoked6 * frameHeight;
                        break;
                    }
                    case float seven when AiTimer > dividend * 6f && AiTimer < dividend * 7f:
                    {
                        NPC.frame.Y = (int)FrameState.Provoked7 * frameHeight;
                        break;
                    }
                    case float eight when AiTimer > dividend * 7f && AiTimer < dividend * 8f:
                    {
                        NPC.frame.Y = (int)FrameState.Provoked8 * frameHeight;
                        break;
                    }
                }

                break;
            }
        }*/
    }
    private const float IdleWalkSpeed = 1.25f;
    /// <summary>
    /// Patrols the area around the World Rune. Will also talk to the player occasionally, if he can see them.
    /// AiTimer1 determines when he will walk around or idle and look around.
    /// AiTimer2 determines whether he should turn around while idling.
    /// AiTimer3 is just a cooldown for turning around when running into a solid tile.
    /// </summary>
    /// <param name="target"></param>
    private void IdlePatrolling(Player target)
    {
        if (Collision.CanHitLine(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height))
        {
            DialogueHandler(Main.netMode != NetmodeID.MultiplayerClient);
        }
        NPC.chaseable = false;
        if (AiTimer1 == 60) //roll a second in advance to decide what it does next
        {
            //roll some random action, decide whether he walks again or keeps standing still, maybe turn around
            ServerPreRollingRandoms(1);
            AiTimer1--;
        }        
        else if (AiTimer1 == -60)
        {            
            //roll some random action, decide whether he walks again or keeps standing still, maybe turn around
            ServerPreRollingRandoms(1);
            AiTimer1++;
        }
        else if (AiTimer1 == 0)
        {
            AiTimer1 = IdlePatrolRoll;
            AiTimer2 = 0;
            if (IdlePatrolRoll > 0)
            {
                NPC.velocity.X = IdleWalkSpeed * NPC.direction; //walk into direction it is facing
            }

            if (TotalWarnings > 1)
            {
                TotalWarnings--;
            }
        }
        else if (AiTimer1 > 0) //if positive
        {
            AiTimer1--;
            AiTimer3++;

            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed,
                ref NPC.gfxOffY, (int)Main.LocalPlayer.gravDir);
            
            Collision.StepDown(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, 
                ref NPC.gfxOffY, (int)Main.LocalPlayer.gravDir);
            
            if (NPC.collideX && AiTimer3 > 1) //if colliding with a wall
            {
                NPC.velocity.X *= -1f; //turn back
                SetDirection();
                NPC.velocity.X = IdleWalkSpeed * NPC.direction; //walk into direction it is facing
                AiTimer3 = 0;
            }
            
        }
        else if (AiTimer1 < 0) //if negative
        {
            AiTimer1++;
            
            NPC.velocity.X = 0; //stand still

            AiTimer2++;
            
            if (AiTimer2 >= 180) //turn around every three seconds
            {
                NPC.direction *= -1;
                AiTimer2 = 0;
            }
        }
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.position.Distance(WarningPosition) < 470) //476 is the closest distance between player and warningpos through blocks outside of the room
                {
                    NPC.target = player.whoAmI;
                    AiState = (float)ActionState.IsWarning;
                    AiTimer1 = 0;
                    AiTimer2 = 0;
                    AiTimer3 = 0;
                    break;
                }
            }
            if (TotalOnHitWarnings > 3)
            {
                AiState = (float)ActionState.StartingFight;
                AiTimer1 = 0;
                AiTimer2 = 0;
                AiTimer3 = 0;
                NPC.netUpdate = true;
            }
        }
    }

    public float HurryingWalkSpeed = 1.75f;
    /// <summary>
    /// Runs towards the World Rune and positions himself in front of it, warning the player that he'll have to fight the player
    /// if they won't step away form the World Rune. If the player ignores the warnings by getting too close to the World Rune
    /// or hitting him too often, he traps the player in a Rune Prison and picks up the World Rune, then starts the boss fight.
    /// AiTimer1 is just a timer for whether the player is still in the Warning Zone or not and whether he should aggro or not. 
    /// </summary>
    /// <param name="target"></param>
    public void IsWarning(Player target)
    {
        NPC.chaseable = false;
        if (NPC.position.Distance(WarningPosition) > 19.1f)
        {
            HurryingWalkSpeed = 3.25f;
            NPC.velocity.X = NPC.DirectionTo(WarningPosition).X * HurryingWalkSpeed;
            
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed,
                ref NPC.gfxOffY, (int)Main.LocalPlayer.gravDir);
            
            Collision.StepDown(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, 
                ref NPC.gfxOffY, (int)Main.LocalPlayer.gravDir);
            
            SetDirection();
        }
        else
        {
            NPC.velocity = Vector2.Zero;
            NPC.TargetClosest();
        }

        if (target.position.Distance(WarningPosition) > 470)
        {
            AiTimer1++;
        }
        else
        {
            DialogueHandler(Main.netMode != NetmodeID.MultiplayerClient);
            AiTimer1--;
        }
        if (AiTimer1 >= 180)
        {
            NPC.target = target.whoAmI;
            AiState = (float)ActionState.IdlePatrolling;
            AiTimer1 = -61;
            AiTimer2 = 0;
            AiTimer3 = 0;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.position.X > WarningPosition.X + 40 & Collision.CanHitLine(NPC.Center, 2, 2, player.Center, 2, 2))
                {
                    AiState = (float)ActionState.StartingFight;
                    AiTimer1 = 0;
                    AiTimer2 = 0;
                    AiTimer3 = 0;
                    NPC.netUpdate = true;
                    break;
                }
            }

            if (TotalWarnings > 5 | TotalOnHitWarnings > 3)
            {
                AiState = (float)ActionState.StartingFight;
                AiTimer1 = 0;
                AiTimer2 = 0;
                AiTimer3 = 0;
                NPC.netUpdate = true;
            }
        }
    }

    private int FirstRunePrisonCastTime = 60;
    private int TotalRunePrisonFrames = 4;
    private int PickupDuration = 60;
    /// <summary>
    /// Starts the fight by trapping the player in an undodgeable Rune Prison and then picking up the World Rune.
    /// Then the player is let go and it goes into normal first phase AI.
    /// </summary>
    /// <param name="target"></param>
    public void StartFight(Player target)
    {
        switch (AiTimer1)
        {
            case float one when AiTimer1 <= FirstRunePrisonCastTime:
            {
                NPC.velocity = Vector2.Zero;
                NPC.TargetClosest();
                Main.NewText("ye");
                break;
            }
            case 61:
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    foreach (Player player in Main.ActivePlayers)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center + new Vector2(0, -10), Vector2.Zero,
                            ModContent.ProjectileType<FirstRunePrison>(), 0, 0, player.whoAmI, NPC.whoAmI);
                    }
                }
                NPC.velocity = Vector2.Zero;
                NPC.TargetClosest();
                Main.NewText("stuck");
                break;
            }
            case float three when AiTimer1 > FirstRunePrisonCastTime + 1:
            {
                if (NPC.position.Distance(WorldRunePosition) > 40f)
                {
                    HurryingWalkSpeed = 3.25f;
                    NPC.velocity.X = NPC.DirectionTo(WorldRunePosition).X * HurryingWalkSpeed;
            
                    Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed,
                        ref NPC.gfxOffY, (int)Main.LocalPlayer.gravDir);
            
                    Collision.StepDown(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, 
                        ref NPC.gfxOffY, (int)Main.LocalPlayer.gravDir);
            
                    SetDirection();
                }
                else
                {
                    NPC.velocity = Vector2.Zero;
                    NPC.TargetClosest(false);
                    AiTimer2++;
                }

                break;
            }
        }
        AiTimer1++;
        if (AiTimer2 > 60)
        {
            Main.NewText("Done");
        }
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            switch (AiState)
            {
                case (float)ActionState.IdlePatrolling:
                {
                    AiTimer1 = -61;
                    AiTimer2 = 0;
                    NPC.FaceTarget();
                    NPC.netUpdate = true;
                    DialogueHandler(Main.netMode != NetmodeID.MultiplayerClient, true);
                    break;
                }
                case (float)ActionState.IsWarning:
                {
                    DialogueHandler(Main.netMode != NetmodeID.MultiplayerClient, true);
                    break;
                }
            }
        }
        TotalOnHitWarnings++;
        ChatTimer = 0;
    }

    private int ChatTimer = 0;
    private int ChatTimerRequirement = 120;
    private int ChatWarningTimer = 0;
    private int MonologueProgress = 1;
    private int TotalOnHitWarnings = 1;
    private int TotalWarnings = 1;
    private void DialogueHandler(bool isNotMpClient, bool warnImmediately = false)
    {
        bool SendText = false;
        string TextAddon = "";
        Color TextColor = Color.White;
        int TextNumber = 0;
        if (isNotMpClient)
        {
            switch (AiState)
            {
                case (float)ActionState.IdlePatrolling:
                {
                    ChatTimer++;
                    if (ChatTimer >= ChatTimerRequirement && MonologueProgress < 4)
                    {
                        SendText = true;
                        TextAddon = "Monologue";
                        TextColor = Color.Blue;
                        TextNumber = MonologueProgress;
                        MonologueProgress++;
                        ChatTimer = 0;
                        ChatTimerRequirement = 120 * MonologueProgress;
                    }
                    break;
                }
                case (float)ActionState.IsWarning:
                {
                    ChatWarningTimer++;

                    if (ChatWarningTimer > 240)
                    {
                        SendText = true;
                        TextAddon = "Warning";
                        TextColor = Color.Red;
                        TextNumber = TotalWarnings;
                        TotalWarnings++;
                        ChatWarningTimer = 0;
                    }
                    break;
                }
            }
            if (warnImmediately)
            {
                switch (AiState)
                {
                    case (float)ActionState.IdlePatrolling:
                    {
                        TextAddon = "IdleOnHitWarning";
                        break;
                    }
                    case (float)ActionState.IsWarning:
                    {
                        TextAddon = "AgitatedOnHitWarning";
                        break;
                    }
                }
                SendText = true;
                TextColor = Color.Red;
                TextNumber = TotalOnHitWarnings;
            }
        }
        if (SendText)
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (Collision.CanHitLine(NPC.Center, 2, 2, player.Center, 2, 2) | NPC.Distance(player.position) < 520)
                {
                    ChatHelper.SendChatMessageToClient(NetworkText.FromKey(LangUtils.GetTextValue("NPCs.Runeterra.RuneMage." + TextAddon + TextNumber)), TextColor, player.whoAmI);
                }
            }
        }
    }

    public const float ChaseSpeed = 5f;
    /// <summary>
    /// Runs around, either away from the player for a while or towards, not very fast. Should be able to dodgeroll like tsorcs fighter ai, or jump to occasionally dodge attacks.
    /// </summary>
    /// <param name="target"></param>
    private void WalkingPhase1(Player target)
    {
        NPC.velocity.X = NPC.Center.DirectionTo(target.Center).X * ChaseSpeed;
        SetDirection();

        Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed,
            ref NPC.gfxOffY,
            (int)target.gravDir);

        if (NPC.collideX && NPC.velocity.Y >= 0)
        {
            AiState = (float)ActionState.JumpingPhase1;
            AiTimer1 = 0;
        }
    }
    private void JumpingPhase1(Player target)
    {
        NPC.velocity.Y += -8f;
    }
    /// <summary>
    /// Transitioning into Phase 2. Invincible during this. Casts a magic platform for him to stand on (or makes his scroll fly and stands on it? Kinda like a magic carpet) and jumps on it. Does not attack. 
    /// </summary>
    /// <param name="target"></param>
    private void PhaseTransition(Player target)
    {
        SetDirection();
    }
    /// <summary>
    /// Hovers above the player, moving left or right randomly while not attacking. Casts spells much more often. 
    /// </summary>
    /// <param name="target"></param>
    private void HoveringPhase2(Player target)
    {
        SetDirection();
    }

/// <summary>
/// Debuff attack that homes in on you and can only be dodged via rolling correctly
/// </summary>
/// <param name="target"></param>
    private void SpellFlux(Player target)
    {
        NPC.velocity = Vector2.Zero;
    }
/// <summary>
/// Simple damage projectile, deals more damage if hit by Spell Flux
/// </summary>
/// <param name="target"></param>
    private void Overload(Player target)
    {
        NPC.velocity = Vector2.Zero;
    }
/// <summary>
/// Casts prison on top of player, this slows the player or roots them if they were hit by Spell Flux
/// </summary>
/// <param name="target"></param>
    private void RunePrison(Player target)
    {
        NPC.velocity = Vector2.Zero;
    }    
    private void SetDirection()
    {
        NPC.direction = NPC.velocity.X > 0f ? 1 : -1;
    }

    public enum ActionState
    {
        IdlePatrolling,
        IsWarning,
        StartingFight,
        WalkingPhase1,
        JumpingPhase1,
        PhaseTransition,
        HoveringPhase2,
        SpellFlux,
        Overload,
        RunePrison,
    }

    private enum FrameState
    {
        Idle,
        Jumping,
        Walking1,
        Walking2,
        Walking3,
        Walking4,
        Walking5,
        Walking6,
        Walking7,
        Walking8,
        Walking9,
        Walking10,
        Walking11,
        Walking12,
        Walking13,
        Walking14,
        SpellFlux1,
        SpellFlux2,
        SpellFlux3,
        SpellFlux4,
        Overload1,
        Overload2,
        Overload3,
        Overload4,
        RunePrison1,
        RunePrison2,
        RunePrison3,
        RunePrison4,
    }
}