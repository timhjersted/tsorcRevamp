using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Prefixes
{
    public class Commanding : ModPrefix
    {
        public override PrefixCategory Category => PrefixCategory.AnyWeapon;
        public override float RollChance(Item item)
        {
            return 1f;
        }
        public override bool CanRoll(Item item)
        {
            if (item.DamageType == DamageClass.SummonMeleeSpeed)
                return true;
            else return false;
        }
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 1f + 0.2f;
            useTimeMult *= 1f - 0.12f;
            knockbackMult *= 1f + 0.15f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 2.0985f;
        }

        public override void Apply(Item item)
        {
        }
    }
    public class Brave : ModPrefix
    {
        public override PrefixCategory Category => PrefixCategory.AnyWeapon;
        public override float RollChance(Item item)
        {
            return 2.25f;
        }
        public override bool CanRoll(Item item)
        {
            if (item.DamageType == DamageClass.SummonMeleeSpeed)
                return true;
            else return false;
        }
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 1f + 0.22f;
            useTimeMult *= 1f - 0.15f;
			scaleMult *= 1f - 0.1f;
            knockbackMult *= 1f - 0.15f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 1.2497f;
        }

        public override void Apply(Item item)
        {
        }
    }
    public class Reckless : ModPrefix
    {
        public override PrefixCategory Category => PrefixCategory.AnyWeapon;
        public override float RollChance(Item item)
        {
            return 1.5f;
        }
        public override bool CanRoll(Item item)
        {
            if (item.DamageType == DamageClass.SummonMeleeSpeed)
                return true;
            else return false;
        }
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 1f + 0.25f;
            useTimeMult *= 1f - 0.2f;
            scaleMult *= 1f - 0.2f;
            knockbackMult *= 1f - 0.2f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 2.0985f;
        }

        public override void Apply(Item item)
        {
        }
    }
    public class Sadistic : ModPrefix
	{
		public override PrefixCategory Category => PrefixCategory.AnyWeapon;
		public override float RollChance(Item item)
		{
			return 3f;
		}
		public override bool CanRoll(Item item)
		{
			if (item.DamageType == DamageClass.SummonMeleeSpeed)
				return true;
			else return false;
		}
		public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
		{
			damageMult = 1f + 0.18f;
			knockbackMult = 1f + 0.15f;
		}

		public override void ModifyValue(ref float valueMult)
		{
			valueMult = 1.8374f;
		}

		public override void Apply(Item item)
		{
		}
	}
	public class  Tenacious: ModPrefix
	{
		public override PrefixCategory Category => PrefixCategory.AnyWeapon;
		public override float RollChance(Item item)
		{
			return 2.5f;
		}
		public override bool CanRoll(Item item)
		{
			if (item.DamageType == DamageClass.SummonMeleeSpeed)
				return true;
			else return false;
		}
		public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
		{
			useTimeMult *= 1f - 0.2f;
		}

		public override void ModifyValue(ref float valueMult)
		{
			valueMult = 1.4629f;
		}

		public override void Apply(Item item)
		{
		}
	}
	public class Undisciplined: ModPrefix
	{
		public override PrefixCategory Category => PrefixCategory.AnyWeapon;
		public override float RollChance(Item item)
		{
			return 4f;
		}
		public override bool CanRoll(Item item)
		{
			if (item.DamageType == DamageClass.Summon)
				return true;
			else return false;
		}
		public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
		{
			knockbackMult *= 1f - 0.9f;
		}

		public override void ModifyValue(ref float valueMult)
		{
			valueMult = 0.64f;
		}

		public override void Apply(Item item)
		{
		}
	}
	public class  Perfectionist: ModPrefix
	{
		public override PrefixCategory Category => PrefixCategory.AnyWeapon;
		public override float RollChance(Item item)
		{
			return 1.3f;
		}
		public override bool CanRoll(Item item)
		{
			if (item.DamageType == DamageClass.Summon)
				return true;
			else return false;
		}
		public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
		{
			damageMult *= 1f + 0.20f;
			useTimeMult *= 1f - 0.15f;
		}

		public override void ModifyValue(ref float valueMult)
		{
			valueMult = 2.0985f;
		}

		public override void Apply(Item item)
		{
		}
	}
	public class Domineering : ModPrefix
	{
		public override PrefixCategory Category => PrefixCategory.AnyWeapon;
		public override float RollChance(Item item)
		{
			return 4f;
		}
		public override bool CanRoll(Item item)
		{
			if (item.DamageType == DamageClass.Summon)
				return true;
			else return false;
		}
		public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
		{
			knockbackMult *= 1f + 1f;
		}

		public override void ModifyValue(ref float valueMult)
		{
			valueMult = 1.64f;
		}

		public override void Apply(Item item)
		{
		}
	}
	public class Impatient : ModPrefix
	{
		public override PrefixCategory Category => PrefixCategory.AnyWeapon;
		public override float RollChance(Item item)
		{
			return 3f;
		}
		public override bool CanRoll(Item item)
		{
			if (item.DamageType == DamageClass.Summon)
				return true;
			else return false;
		}
		public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
		{
			useTimeMult *= 1f - 0.3f;
		}

		public override void ModifyValue(ref float valueMult)
		{
			valueMult = 1.2497f;
		}

		public override void Apply(Item item)
		{
		}
	}
}
