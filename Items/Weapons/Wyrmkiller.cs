using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class Wyrmkiller : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword used to kill wyverns and dragons." +
                                "\nDoes 8x damage against flying beasts.");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Green;
            item.damage = 46;
            item.height = 32;
            item.knockBack = 5;
            item.melee = true;
            item.scale = .9f;
            item.useAnimation = 21;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 140000;
            item.width = 32;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.GoldBroadsword, 1);
            recipe.AddIngredient(ItemID.SoulofFlight, 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            //todo add mod NPCs to this list
            if (target.type == NPCID.WyvernBody
                || target.type == NPCID.WyvernBody2
                || target.type == NPCID.WyvernBody3
                || target.type == NPCID.WyvernHead
                || target.type == NPCID.WyvernTail
                //|| target.type == ModContent.NPCType<ShadowDragonBody>()
                //|| target.type == ModContent.NPCType<ShadowDragonHead>()
                //|| target.type == ModContent.NPCType<WyvernMageDisciple>()
                //|| target.type == ModContent.NPCType<JungleWyvernBody>()
                //|| target.type == ModContent.NPCType<JungleWyvernHead>()
                //|| target.type == ModContent.NPCType<GhostDragonBody>()
                //|| target.type == ModContent.NPCType<GhostDragonHead>()
                //|| target.type == ModContent.NPCType<MechaDragonBody>()
                //|| target.type == ModContent.NPCType<MechaDragonHead>()
                //|| target.type == ModContent.NPCType<HelkiteDragonBody>()
                //|| target.type == ModContent.NPCType<HelkiteDragonHead>()
                //|| target.type == ModContent.NPCType<HelkiteDragonTail>()
                //|| target.type == ModContent.NPCType<SeathBody>()
                //|| target.type == ModContent.NPCType<SeathHead>()
                //|| target.type == ModContent.NPCType<SeathTail>()
                ) {
                damage *= 8;
            }
        }
    }
}
