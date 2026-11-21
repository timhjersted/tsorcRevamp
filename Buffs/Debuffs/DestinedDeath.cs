using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class DestinedDeath : ModBuff
    {
        // Description numbers come from DestinedDeathPlayer: {0}% per stack, {1}% cap, {2}% damage bonus at the cap.
        public override LocalizedText Description => base.Description.WithFormatArgs(DestinedDeathPlayer.LossPercentPerStack,
            DestinedDeathPlayer.MaxStacks * DestinedDeathPlayer.LossPercentPerStack, DestinedDeathPlayer.DamageBonusPercent);

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.pvpBuff[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            Player player = Main.LocalPlayer;
            DestinedDeathPlayer deathPlayer = player.GetModPlayer<DestinedDeathPlayer>();
            float currentLossPercent = deathPlayer.CurrentLossPercent;

            if (player.HasBuff(Type))
            {
                int stacks = Math.Max(1, deathPlayer.Stacks);
                int targetLossPercent = stacks * DestinedDeathPlayer.LossPercentPerStack;
                tip = $"Maximum life reduced by {currentLossPercent:0.#}% (Target: {targetLossPercent}%, Stacks: {stacks}/{DestinedDeathPlayer.MaxStacks})";
                if (deathPlayer.HasDamageBonus)
                {
                    tip += $"\nMax life loss cap reached! Gained +{DestinedDeathPlayer.DamageBonusPercent}% increased damage!";
                }
            }
            else if (currentLossPercent > 0f)
            {
                tip = $"Recovering maximum life! Current loss: {currentLossPercent:0.#}% (+{DestinedDeathPlayer.RecoveryPercentPerSecond}%/sec recovery)";
            }
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            DestinedDeathPlayer deathPlayer = player.GetModPlayer<DestinedDeathPlayer>();
            int previousStacks = Math.Max(1, deathPlayer.Stacks);
            deathPlayer.Stacks = Math.Min(previousStacks + 1, DestinedDeathPlayer.MaxStacks);
            player.buffTime[buffIndex] = time;
            return true;
        }
    }

    public class DestinedDeathPlayer : ModPlayer
    {
        // 12 stacks of 5% each: the same 60% cap as before, reached in smaller steps.
        internal const int MaxStacks = 12;
        internal const int LossPercentPerStack = 5;
        internal const int RecoveryPercentPerSecond = 10;
        internal const int DamageBonusPercent = 15;
        private const int DrainPercentPerSecond = 5;
        private const int DrainTicksPerStack = LossPercentPerStack * 60 / DrainPercentPerSecond;
        private const int RecoveredTicksPerTick = RecoveryPercentPerSecond / DrainPercentPerSecond;
        private int lifeLossTicks;

        internal int Stacks { get; set; }
        internal float CurrentLossPercent => (float)lifeLossTicks * LossPercentPerStack / DrainTicksPerStack;
        internal bool HasDamageBonus => lifeLossTicks >= MaxStacks * DrainTicksPerStack;

        public override void PostUpdateBuffs()
        {
            // Apply after the buff timer updates and before MaxHealthCalculator resolves max life.
            // Count drain ticks so the damage bonus starts exactly when the full penalty is reached.
            if (Player.HasBuff<DestinedDeath>())
            {
                Stacks = Math.Max(1, Stacks);
                int targetTicks = Stacks * DrainTicksPerStack;
                lifeLossTicks = Math.Min(targetTicks, lifeLossTicks + 1);
            }
            else
            {
                lifeLossTicks = Math.Max(0, lifeLossTicks - RecoveredTicksPerTick);
                if (lifeLossTicks == 0)
                {
                    Stacks = 0;
                }
            }

            if (lifeLossTicks == 0)
            {
                return;
            }

            MaxHealthCalculator healthCalculator = Player.GetModPlayer<MaxHealthCalculator>();
            healthCalculator.MaxHealthMultiplier -= CurrentLossPercent / 100f;

            if (HasDamageBonus)
            {
                Player.GetDamage(DamageClass.Generic) += DamageBonusPercent / 100f;
            }
        }

        public override void UpdateDead()
        {
            Stacks = 0;
            lifeLossTicks = 0;
        }
    }
}
