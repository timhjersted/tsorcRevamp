using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class CorruptedTooth : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A green ooze dribbles from the tooth, which deals" +
                                "\nextra damage to enemies of a similar nature." +
                                "\nHas a chance to heal the player on hit.");
        }
        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 21;
            item.autoReuse = true;
            item.useTime = 21;
            item.damage = 11;
            item.knockBack = 4f;
            item.UseSound = SoundID.Item1;
            item.value = PriceByRarity.Blue_1;
            item.melee = true;
            item.rare = ItemRarityID.Blue;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            if (target.type == NPCID.EaterofSouls
                || target.type == NPCID.BigEater
                || target.type == NPCID.LittleEater
                || target.type == NPCID.EaterofWorldsHead
                || target.type == NPCID.EaterofWorldsBody
                || target.type == NPCID.EaterofWorldsTail
                ) {
                damage *= 4;
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.GuardianCorruptor>()) {
                //please *DO* use this on a guardian corruptor!
                crit = false;
                damage = 100074;//reduced to 99999 after defense
            } 
            if (Main.rand.Next(10) == 0) {
                player.statLife += damage;
                player.HealEffect(damage);
            }
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SilverBroadsword);
            recipe.AddIngredient(ItemID.RottenChunk, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 800);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
