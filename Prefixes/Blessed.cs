using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.Prefixes
{
    public class Blessed : ModPrefix
    {
        public static float BlessedLifeRegen = 1f;
        private readonly byte _power;

        public override float RollChance(Item item) //we don't want players getting it naturally
        {
            return 0f;                              //NOTE: Because we are using PrefixCategory.Custom, we should use ChoosePrefix from tsorcInstancedGlobalItem instead
        }

        public override bool CanRoll(Item item)     //This also gets ignored, do it via ChoosePrefix from tsorcInstancedGlobalItem
            => true;


        // Defaults to Custom
        /*public override PrefixCategory Category
			=> PrefixCategory.AnyWeapon;
		*/

        public Blessed()
        {
        }

        public Blessed(byte power)
        {
            _power = power;
        }

        public override void Load()
        {
            //Mod.AddContent(new Blessed(1));
        }

        public override void Apply(Item item)
            => item.GetGlobalItem<tsorcInstancedGlobalItem>().blessed = _power;

        public override void ModifyValue(ref float valueMult)
        {
            float multiplier = BlessedLifeRegen + 0.1f * _power;
            valueMult *= multiplier;
        }
    }
}