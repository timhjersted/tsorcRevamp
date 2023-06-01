using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.Projectiles;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Items.Weapons.Melee.Axes
{
    class ForgottenRuneAxe : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("4 times as effective against magic users.");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Cyan;
            Item.damage = 105;
            Item.width = 56;
            Item.height = 46;
            Item.knockBack = 5;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 23;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.Cyan_9;
            Item.shoot = ModContent.ProjectileType<Nothing>();
        }

        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            //todo add mod NPCs to this list
            if (target.type == NPCID.DarkCaster
                || target.type == NPCID.GoblinSorcerer
                || target.type == NPCID.Tim
                || target.type == ModContent.NPCType<UndeadCaster>()
                || target.type == ModContent.NPCType<MindflayerServant>()
                || target.type == ModContent.NPCType<DungeonMage>()
                || target.type == ModContent.NPCType<DemonSpirit>()
                || target.type == ModContent.NPCType<ShadowMage>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()
                || target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>()
                || target.type == ModContent.NPCType<AttraidiesIllusion>()
                || target.type == ModContent.NPCType<AttraidiesManifestation>()
                || target.type == ModContent.NPCType<CrazedDemonSpirit>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.BrokenOkiku>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.AttraidiesMimic>()
                || target.type == ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DarkShogunMask>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.SecondForm.DarkDragonMask>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.Okiku>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()
                || target.type == ModContent.NPCType<NPCs.Enemies.MindflayerKingServant>()
                || target.type == ModContent.NPCType<NPCs.Enemies.MindflayerServant>()
                || target.type == ModContent.NPCType<NPCs.Enemies.MindflayerIllusion>()
                || target.type == ModContent.NPCType<LichKingDisciple>()
                )
            {
                modifiers.FinalDamage *= 4;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldAxe);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
