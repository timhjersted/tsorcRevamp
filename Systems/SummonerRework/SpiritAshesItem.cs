using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems.SummonerRework
{
    /// <summary>
    /// Gates casting a summon weapon two ways: a mana cost (currently OFF - see
    /// SummonerRework.SummonManaCostEnabled; vanilla is moving summoner to 0 mana across the board, so
    /// charging on top of that would cut against the direction the class is heading, but the cost still
    /// fully computes below and every call site already treats 0 as "no charge, no tooltip" rather than
    /// "charge zero"), and the per-slot resummon cooldown in SpiritAshesRecoveryPlayer, which is always
    /// active regardless of the mana flag.
    /// </summary>
    public class SpiritAshesItem : GlobalItem
    {
        private static bool IsSummonWeapon(Item item)
        {
            if (item.damage <= 0 || item.accessory || item.shoot <= ProjectileID.None)
            {
                return false;
            }

            // Whips report as Summon - SummonMeleeSpeedDamageClass.GetEffectInheritance returns true for
            // DamageClass.Summon, so CountsAsClass says yes - but a whip is the summoner's ordinary
            // attack, not a summon. Charging one per swing would drain the pool in seconds.
            if (ProjectileID.Sets.IsAWhip[item.shoot])
            {
                return false;
            }

            return item.CountsAsClass(DamageClass.Summon);
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (item.shoot > ProjectileID.None)
            {
                SpiritAshesRecoveryPlayer.DebugLog(
                    $"CanUseItem entry: item={item.Name} shoot={item.shoot} active={SummonerRework.Active(player)} isSummonWeapon={IsSummonWeapon(item)}");
            }

            if (!SummonerRework.Active(player) || !IsSummonWeapon(item))
            {
                return base.CanUseItem(item, player);
            }

            // Refuse rather than allowing the cast into mana debt, matching how every other resource
            // gate in the mod behaves when you cannot afford an action.
            if (player.statMana < SummonerRework.SummonManaCost(player))
            {
                return false;
            }

            // Keyed off the REAL ash type this item produces, not necessarily item.shoot itself - some
            // items (Abigail's Flower) shoot an invisible spawner that creates the real minion as a
            // child; see SummonerRework.ResolveTrackedAshType.
            int ashType = SummonerRework.ResolveTrackedAshType(item.shoot);
            int currentAlive = SpiritAshesMinions.CountAlive(player, ashType);

            // Refuse a cast that has NEITHER an existing instance of this type to reposition NOR a free
            // minion slot for a genuinely new one. Without this, casting into a full army lets vanilla's
            // own per-tick slot enforcement (Projectile.Update: "if slotsMinions + minionSlots exceeds
            // max, Kill()") force some OTHER, unrelated ash out to make room instead - whichever one
            // processes last in the projectile array that tick, not necessarily anything to do with this
            // cast. That both looks like a bug (a totally different ash vanishing) and is a free way to
            // discard an inconvenient ash without it ever going through TakeDamage - see
            // SoulReliquaryBell.CanUseItem for the same explicit-refusal pattern on its own item.
            if (currentAlive <= 0 && player.maxMinions - player.slotsMinions < ItemID.Sets.StaffMinionSlotsRequired[item.type])
            {
                return false;
            }

            // Per-slot resummon cooldown - see SpiritAshesRecoveryPlayer. Never blocks repositioning an
            // already-alive army; only blocks climbing back toward a size this type has already proven it
            // can reach, while a recent death of that same type is still on cooldown.
            //
            // A snapshot on file (SpiritAshesRecoveryPlayer.RecordAliveState) means this type is not
            // actually gone - it was dismissed (buff-canceled, evicted for slots) rather than killed, so
            // bringing it back is always a reposition, exactly like recasting while it is still visibly
            // alive, and must never wait on the cooldown below. Only a genuine death - which clears the
            // snapshot, see TakeDamage - has anything pending here to wait out.
            SpiritAshesRecoveryPlayer recovery = player.GetModPlayer<SpiritAshesRecoveryPlayer>();
            bool wasOnlyDismissed = recovery.TryGetAliveState(ashType, out _, out _, out _);

            if (!wasOnlyDismissed && !recovery.CanSummon(ashType, currentAlive))
            {
                recovery.NotifyRefusal(ashType);
                return false;
            }

            return base.CanUseItem(item, player);
        }

        public override bool? UseItem(Item item, Player player)
        {
            if (!SummonerRework.Active(player) || !IsSummonWeapon(item))
            {
                return base.UseItem(item, player);
            }

            int manaCost = SummonerRework.SummonManaCost(player);

            // SummonManaCostEnabled is currently off, so this is 0 - skip the deduction AND its regen
            // delay penalty entirely rather than doing a no-op subtraction, so a free cast is actually
            // free and doesn't stall mana regen for something that cost nothing.
            if (manaCost > 0)
            {
                // Direct subtraction rather than routing through Player.manaCost. Arcane Sorcery already
                // applies a mod-wide mana cost multiplier to every Bearer of the Curse player, and this
                // cost is meant to stay a readable percentage of the pool regardless of how that is retuned.
                player.statMana -= manaCost;

                if (player.statMana < 0)
                {
                    player.statMana = 0;
                }

                player.manaRegenDelay = (int)player.maxRegenDelay;
            }

            // Consumes one recovery slot if (and only if) this cast is filling a gap left by a death -
            // pure repositioning or ordinary army-building never touches the recovery queue. Same
            // real-ash-type resolution as CanUseItem above.
            int ashType = SummonerRework.ResolveTrackedAshType(item.shoot);
            int currentAlive = SpiritAshesMinions.CountAlive(player, ashType);
            player.GetModPlayer<SpiritAshesRecoveryPlayer>().ConsumeRecoveryIfFillingGap(ashType, currentAlive);

            return base.UseItem(item, player);
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            if (!SummonerRework.Active(player) || !IsSummonWeapon(item))
            {
                return;
            }

            int manaCost = SummonerRework.SummonManaCost(player);

            if (manaCost <= 0)
            {
                return;
            }

            TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips,
                LangUtils.GetTextValue("CommonItemTooltip.SpiritAshSummonCost", manaCost));
        }
    }
}
