using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Runeterra;

public class RunePrisonDebuff : ModBuff
{
    public const float RunePrisonSlowStrength = 50f;
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        var modPlayer = player.GetModPlayer<RunePrisonPlayer>();
        var tsorcPlayer = player.GetModPlayer<tsorcRevampPlayer>();
        if (player.ownedProjectileCounts[ModContent.ProjectileType<FirstRunePrison>()] == 1)
        {
            player.SetCCed();
            modPlayer.RunePrisonRoot = true;
        }

        if (player.ownedProjectileCounts[ModContent.ProjectileType<RunePrison>()] == 1)
        {
            if (player.HasBuff(ModContent.BuffType<SpellFluxDebuff>()))
            {
                modPlayer.RunePrisonRoot = true;
                tsorcPlayer.noDodge  = true;
            }
            else
            {
                modPlayer.RunePrisonSlow = true;
            }
        }
    }
}

public class RunePrisonPlayer : ModPlayer
{
    public bool RunePrisonRoot = false;
    public bool RunePrisonSlow = false;
    public override void ResetEffects()
    {
        RunePrisonRoot = false;
        RunePrisonSlow = false;
    }

    public int OldDirection;
    public override void PreUpdateMovement()
    {
        if (!RunePrisonRoot)
        {
            OldDirection = Player.direction;
        }
        if (RunePrisonRoot)
        {
            Player.velocity = Vector2.Zero;
            Player.ChangeDir(OldDirection);
        }
    }

    public override void PostUpdateMiscEffects()
    {
        if (RunePrisonSlow)
        {
            Player.moveSpeed *= 1f - RunePrisonDebuff.RunePrisonSlowStrength / 100f;
        }
    }
}