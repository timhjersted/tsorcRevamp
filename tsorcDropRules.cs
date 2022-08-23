using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;

namespace tsorcRevamp {

    public class DropMultiple : IItemDropRule {
        private int[] _drops;
        private int _chanceDenominator;
        private int _chanceNumerator;
        private bool _condition;

        public List<IItemDropRuleChainAttempt> ChainedRules { get; private set; }

        /// <summary>
        /// Drop one each of every provided item.
        /// </summary>
        /// <param name="itemList">The list of items to drop.</param>
        /// <param name="chanceDenominator">The denominator of the drop rate fraction. Default 10.</param>
        /// <param name="chanceNumerator">The numerator of the drop rate fraction. Default 1.</param>
        /// <param name="condition">If these items should drop in specific circumstances, provide them here. Default true (always lootable).</param>
        public DropMultiple(int[] itemList, int chanceDenominator = 10, int chanceNumerator = 1, bool condition = true) {
            _drops = itemList;
            _condition = condition;
            _chanceDenominator = chanceDenominator;
            _chanceNumerator = chanceNumerator;
            ChainedRules = new List<IItemDropRuleChainAttempt>();
        }

        public bool CanDrop(DropAttemptInfo info) => _condition;

        public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) {
            ItemDropAttemptResult result = default;
            if (info.player.RollLuck(_chanceDenominator) < _chanceNumerator) {
                for (int i = 0; i < _drops.Length; i++) {
                    CommonCode.DropItem(info, _drops[i], 1);
                }
                result.State = ItemDropAttemptResultState.Success;
                return result;
            }
            result.State = ItemDropAttemptResultState.FailedRandomRoll;
            return result;
        }

        public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) {
            float origRate = (float)_chanceNumerator / (float)_chanceDenominator;
            float totalRate = origRate * ratesInfo.parentDroprateChance;
            for (int i = 0; i < _drops.Length; i++) {
                drops.Add(new DropRateInfo(_drops[i], 1, 1, totalRate, ratesInfo.conditions));
            }
            Chains.ReportDroprates(ChainedRules, origRate, drops, ratesInfo);
        }
    }

    public class SuperHardmodeRule : IItemDropRuleCondition, IProvideItemConditionDescription {
        public bool CanDrop(DropAttemptInfo info) => tsorcRevampWorld.SuperHardMode;
        public bool CanShowItemDropInUI() => true;
        public string GetConditionDescription() => "[c/ff9999:Only drops in Super Hardmode]";
    }

    public class FirstBagRule : IItemDropRuleCondition, IProvideItemConditionDescription {
        public virtual bool CanDrop(DropAttemptInfo info) {
            tsorcRevampPlayer modPlayer = info.player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.bagsOpened.Contains(info.item)) {
                return false;
            }
            return true;
        }

        public bool CanShowItemDropInUI() => true;

        public virtual string GetConditionDescription() => "[c/ff9999: Only drops from the first opened specific Bag";
    }

    public class FirstBagCursedRule : FirstBagRule {
        public override bool CanDrop(DropAttemptInfo info) {
            tsorcRevampPlayer modPlayer = info.player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.BearerOfTheCurse & base.CanDrop(info)) {
                return true;
            }
            return false;
        }

        public override string GetConditionDescription() => "[c/ff9999: Only drops from the first opened specific Bag while the player is a Bearer of the Curse";
    }

    public class AdventureModeRule : IItemDropRuleCondition, IProvideItemConditionDescription {
        public bool CanDrop(DropAttemptInfo info) => ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems;
        public bool CanShowItemDropInUI() => true;
        public string GetConditionDescription() => "[c/ff9999:Only drops in Adventure Mode]";
    }

    public class NonAdventureModeRule : IItemDropRuleCondition, IProvideItemConditionDescription {
        public bool CanDrop(DropAttemptInfo info) => !ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems;
        public bool CanShowItemDropInUI() => true;
        public string GetConditionDescription() => "[c/ff9999:Only drops in Non-Adventure Mode]";
    }
}
