using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class EsunaTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A lost tome known to cure all but the rarest of ailments.");
        }
        public override void SetDefaults() {
            Item.height = 10;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.Cyan;
            Item.magic = true;
            Item.noMelee = true;
            Item.mana = 40;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.value = PriceByRarity.Cyan_9;
            Item.width = 34;
        }

        public override bool? UseItem(Player player) {
            int buffIndex = 0;

            foreach (int buffType in player.buffType) {

                if ((buffType == BuffID.Bleeding) 
                    || (buffType == BuffID.Poisoned) 
                    || (buffType == BuffID.Confused) 
                    || (buffType == BuffID.BrokenArmor)
                    || (buffType == BuffID.Darkness)
                    || (buffType == BuffID.OnFire)
                    || (buffType == BuffID.Slow)
                    || (buffType == BuffID.Weak)
                    || (buffType == BuffID.CursedInferno)
                    || (buffType == BuffID.Cursed) // why are these in here? you can't use this item if you have cursed or silenced 
                    || (buffType == BuffID.Silenced) // the original tsorc mod has these in here, so im leaving them, but im *well* aware of how stupid this is
                    ) {
                    player.buffTime[buffIndex] = 0;
                }
                buffIndex++;
            }
            return true;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.SpellTome);
            recipe.AddIngredient(Mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(Mod.GetItem("HealingElixir"), 10);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 70000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
