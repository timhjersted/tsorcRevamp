using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class AtmaWeapon : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword that draws power from the wielder.\n" +
                                "The true form of your father's sword revealed.\n" +
                                "Does 105 damage when at full health, and 80 damage at half health, scaling with current HP.");

        }

        public override void SetDefaults() {

            item.stack = 1;
            item.rare = ItemRarityID.Pink;
            item.damage = 55;
            item.height = 58;
            item.knockBack = (float)9;
            item.maxStack = 1;
            item.melee = true;
            item.autoReuse = true;
            item.useAnimation = 19;
            item.useTime = 19;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 4000000;
            item.width = 58;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Excalibur, 1);
            recipe.AddIngredient(ItemID.SoulofSight, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        //When I woke up one morning I found this number written on the back of my hand, again and again. I don't know what it does or why, but it works.
        //Jk, it's just 50 (the damage boost) / 55 (the weapon's base damage). It's used to convert the damage boost we come up with into a fraction of the base damage, since that's what add
        //It would've been easier to just use flat, but then it would have worked strangely with multiplicitive damage boosts or debuffs...
        static float multiplier = 0.90909090909f;
        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat) {
            //Get the player's health percentage
            add += ((float)player.statLife / (float)player.statLifeMax2) * multiplier * player.meleeDamage;
        }
    }
}
