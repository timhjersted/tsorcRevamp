using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class LunarEventItemNerfs : GlobalItem
    {
        public static int Meow = 0;
        public static int Wrath = 0;
        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.DayBreak)
            {
                item.useAnimation = 32;
                item.useTime = 32;
                item.damage = 125;
            }
            if (item.type == ItemID.Terrarian)
            {
                item.damage = 130;
            }


            if (item.type == ItemID.Phantasm)
            {
                item.damage = 35;
            }


            if (item.type == ItemID.LastPrism)
            {
                item.mana = 36;
            }
            if (item.type == ItemID.LunarFlareBook)
            {
                item.mana = 39;
            }

            if (item.type == ItemID.StardustDragonStaff)
            {
                item.damage = 35;
            }
        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (item.type == ItemID.Meowmere)
            {
                if (Main.GameUpdateCount % 20 == 0)
                {
                    Meow--;
                }
            }
            if (item.type == ItemID.StarWrath)
            {
                if (Main.GameUpdateCount % 20 == 0)
                {
                    Wrath--;
                }
            }
        }
        public override bool CanShoot(Item item, Player player)
        {
            if (item.type == ItemID.Meowmere & Meow <= 0)
            {
                return false;
            }
            if (item.type == ItemID.StarWrath & Wrath <= 0)
            {
                return false;
            }
            return true;
        }
        public override void OnHitNPC(Item item, Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (item.type == ItemID.Meowmere)
            {
                Meow = 2;
            }
            if (item.type == ItemID.StarWrath)
            {
                Wrath = 2;
            }
        }
    }
}
