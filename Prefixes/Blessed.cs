using tsorcRevamp.Items;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Prefixes
{
	public class Blessed : ModPrefix
	{
		private readonly byte _power;

		public override float RollChance(Item item) //we don't want players getting it naturally
        {
			return 0f;								//NOTE: Because we are using PrefixCategory.Custom, we should use ChoosePrefix from tsorcInstancedGlobalItem instead
		}

		public override bool CanRoll(Item item)     //This also gets ignored, do it via ChoosePrefix from tsorcInstancedGlobalItem
			=> false;


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

		// Allow multiple prefix autoloading this way (permutations of the same prefix)
		public override bool Autoload(ref string name)
		{
			if (!base.Autoload(ref name))
			{
				return false;
			}

			mod.AddPrefix("Blessed", new Blessed(1));
			return false;
		}

		public override void Apply(Item item)
			=> item.GetGlobalItem<tsorcInstancedGlobalItem>().blessed = _power;

		public override void ModifyValue(ref float valueMult)
		{
			float multiplier = 1f + 0.1f * _power;
			valueMult *= multiplier;
		}
	}
}