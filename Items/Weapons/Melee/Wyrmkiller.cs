using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Items.Weapons.Melee {
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
            //what a mess lmao, should probably be a switch but im lazy
            if (target.type == NPCID.WyvernBody
                || target.type == NPCID.WyvernBody2
                || target.type == NPCID.WyvernBody3
                || target.type == NPCID.WyvernHead
                || target.type == NPCID.WyvernTail
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.SecondForm.ShadowDragonBody>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.SecondForm.ShadowDragonHead>()
                || target.type == ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonBody>()
                || target.type == ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonBody2>()
                || target.type == ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonBody3>()
                || target.type == ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>()
                || target.type == ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonLegs>()
                || target.type == ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonTail>()
                || target.type == ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernBody>()
                || target.type == ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernBody2>()
                || target.type == ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernBody3>()
                || target.type == ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>()
                || target.type == ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernLegs>()
                || target.type == ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernTail>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonBody>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonBody2>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonBody3>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonLegs>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonTail>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonBody>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonBody2>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonBody3>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonLegs>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonTail>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessBody>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessBody2>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessBody3>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessLegs>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessTail>()
                ) {
                damage *= 8;
            }
        }
    }
}
