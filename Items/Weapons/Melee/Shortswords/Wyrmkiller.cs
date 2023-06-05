using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    class Wyrmkiller : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("A sword used to kill wyverns and dragons." +
                                "\nDoes 8x damage against flying beasts."); */
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.damage = 46;
            Item.height = 32;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.scale = .9f;
            Item.useAnimation = 21;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 140000;
            Item.width = 32;
            Item.noUseGraphic = true;
            Item.noMelee= true;
            Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.WyrmkillerProjectile>(); // The projectile is what makes a shortsword work
            Item.shootSpeed = 2.1f; // This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values
        }
        public override bool MeleePrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltSword, 1);
            recipe.AddIngredient(ItemID.SoulofFlight, 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
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
                )
            {
                modifiers.FinalDamage *= 8;
            }
        }
    }
}
