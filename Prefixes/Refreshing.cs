using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.Prefixes
{
    public class Refreshing : ModPrefix
    {
        public virtual float _power => 0.02f;

        public override float RollChance(Item item) //we don't want players getting it naturally
        {
            return 0.8f;                              //NOTE: Because we are using PrefixCategory.Custom, we should use ChoosePrefix from tsorcInstancedGlobalItem instead
        }

        public override bool CanRoll(Item item)     //This also gets ignored, do it via ChoosePrefix from tsorcInstancedGlobalItem
            => true;


        //Defaults to Custom
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

    public class Revitalizing : Refreshing {
        public override float _power => 0.04f;
    }

    public class Invigorating : Refreshing {
        public override float _power => 0.08f;
    }
}