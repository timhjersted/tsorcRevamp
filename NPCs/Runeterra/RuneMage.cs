using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
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

    public const int ContactDmg = 20;
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
        NPC.width = 50;
        NPC.height = 50;
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
    public ref float AiTimer => ref NPC.ai[1];
    
    public ref float SpellFluxCooldown => ref NPC.ai[2];
    public ref float OverloadCooldown => ref NPC.ai[3];
    public float RunePrisonCooldown;
    
    public override void AI()
    {
        var target = Main.player[NPC.target];
        switch (AiState)
        {
            case (float)ActionState.Patrolling:
            {
                IdlePatrolling(target);
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
    }

    public override void FindFrame(int frameHeight)
    {
        /*
        FrameDuration = 10;
        NPC.spriteDirection = NPC.direction;

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

    private void IdlePatrolling(Player target)
    {
        if (Main.netMode == NetmodeID.Server | Main.netMode == NetmodeID.SinglePlayer)
        {
            foreach (Player player in Main.ActivePlayers)
            {
                var modPlayer = Main.player[player.whoAmI].GetModPlayer<tsorcRevampPlayer>();
                modPlayer.RuneMageChatTimer++;
                if (player.position.Distance(NPC.position) < 500 && modPlayer.RuneMageChatTimer > 120)
                {
                    ChatHelper.SendChatMessageToClient(NetworkText.FromKey("LocationinLangFile"), Color.Aqua, player.whoAmI);
                    modPlayer.RuneMageChatTimer = 0;
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
            AiTimer = 0;
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

    private enum ActionState
    {
        Patrolling,
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