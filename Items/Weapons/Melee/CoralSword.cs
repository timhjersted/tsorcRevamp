using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class CoralSword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Edged to slay those of the sea. Deals 4x damage to water enemies.");
        }
        public override void SetDefaults() {
            Item.rare = ItemRarityID.Blue;
            Item.damage = 32;
            Item.height = 36;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 23;
            Item.useTime = 23;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Blue_1;
            Item.width = 36;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            //todo add mod NPCs to this list
            if (target.type == NPCID.Shark
                || target.type == NPCID.Goldfish
                || target.type == NPCID.CorruptGoldfish
                || target.type == NPCID.BlueJellyfish
                || target.type == NPCID.GreenJellyfish
                || target.type == NPCID.PinkJellyfish
                || target.type == NPCID.Piranha
                || target.type == NPCID.AnglerFish
                //|| target.type == ModContent.NPCType<SahaginChief>()
                //|| target.type == ModContent.NPCType<SahaginPrince>()
                //|| target.type == ModContent.NPCType<QuaraCosntrictor>()
                //|| target.type == ModContent.NPCType<QuaraHydromancer>()
                //|| target.type == ModContent.NPCType<QuaraMantassin>()
                //|| target.type == ModContent.NPCType<QuaraPincher>()
                //|| target.type == ModContent.NPCType<QuaraPredator>()
                ) {
                damage *= 4;
            }
        }
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.Coral, 5);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
