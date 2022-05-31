using tsorcRevamp.Items;
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

		// Allow multiple prefix autoloading this way (permutations of the same prefix)
		public override bool Autoload(ref string name)
		{
			if (!base.Autoload(ref name))
			{
				return false;
			}

			Mod.AddPrefix("Refreshing", new Refreshing(1));
			Mod.AddPrefix("Revitalizing", new Refreshing(2));
			return false;
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