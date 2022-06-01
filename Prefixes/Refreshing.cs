using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Prefixes
{
    public class Refreshing : ModPrefix
    {
        private readonly byte _power;

        public override float RollChance(Item item) //we don't want players getting it naturally
        {
            return 1f;                              //NOTE: Because we are using PrefixCategory.Custom, we should use ChoosePrefix from tsorcInstancedGlobalItem instead
        }

        public override bool CanRoll(Item item)     //This also gets ignored, do it via ChoosePrefix from tsorcInstancedGlobalItem
            => true;


        //Defaults to Custom
        public override PrefixCategory Category
            => PrefixCategory.Accessory;


        public Refreshing()
        {
        }

        public Refreshing(byte power)
        {
            _power = power;
        }

        public override void Load()
        {
            Mod.AddContent(new Refreshing(1));
            Mod.AddContent(new Refreshing(2));
        }

        /*public override void Apply(Item item)
			=> item.GetGlobalItem<tsorcInstancedGlobalItem>().refreshing = _power;*/

        public override void ModifyValue(ref float valueMult)
        {
            float multiplier = 1f + 0.1f * _power;
            valueMult *= multiplier;
        }
    }
}