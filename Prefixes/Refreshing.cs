using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.Prefixes
{
    public class Refreshing : ModPrefix
    {
        public static float RefreshingPower = 4f;
        public virtual float _power => RefreshingPower / 100f;

        // NOTE: an older comment here claimed this prefix used PrefixCategory.Custom and that RollChance/CanRoll
        // were ignored in favour of tsorcInstancedGlobalItem.ChoosePrefix. That was never true of the shipped
        // code — Category below is Accessory, ChoosePrefix returns -1, and BOTH hooks below are live.
        public override float RollChance(Item item)
        {
            return 0.8f; // weight relative to a standard accessory prefix (1f)
        }

        /// <summary>
        /// Only mobility accessories may roll the stamina-regen prefixes.
        ///
        /// Reforging is unlimited, so without this a player could put Invigorating on all 7 accessory slots for
        /// +56% stamina regen — larger than any single item in the game, and invisible to a per-item audit.
        /// Gating by type rather than capping the stack means an ineligible accessory just rolls a normal prefix,
        /// so no roll is ever silently wasted. Registry lives in tsorcRevamp.PopulateArrays().
        /// </summary>
        public override bool CanRoll(Item item)
            => tsorcRevamp.StaminaPrefixAccessories != null
               && tsorcRevamp.StaminaPrefixAccessories.Contains(item.type);

        public override PrefixCategory Category
            => PrefixCategory.Accessory;

        public override void Apply(Item item)
            => item.GetGlobalItem<tsorcInstancedGlobalItem>().refreshing = _power;

        public override void ModifyValue(ref float valueMult)
        {
            //rougly matches vanilla modifier scaling
            float multiplier = 1.03f + (2f * _power);
            valueMult *= multiplier;
        }
    }

    public class Revitalizing : Refreshing
    {
        public static float RevitalizingPower = 6f;
        public override float _power => RevitalizingPower / 100f;
    }

    public class Invigorating : Refreshing
    {
        public static float InvigoratingPower = 8f;
        public override float _power => InvigoratingPower / 100f;
    }
}
