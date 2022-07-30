using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class RuneBlade : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A sword used to kill magic users." +
                                "\nDoes up to 8x damage to mages" +
                                "\nEvery third hit with the Blade generates a magic slash");
        }
        public int shootstacks = 0;
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.damage = 20;
            Item.height = 36;
            Item.knockBack = 5;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.scale = 1f;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.value = PriceByRarity.Green_2;
            Item.width = 36;
            Item.shoot = ProjectileID.DD2SquireSonicBoom;
            Item.shootSpeed = 7.5f;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
        {
            shootstacks += 1;
            //todo add mod NPCs to this list
            if (target.type == NPCID.DarkCaster
                || target.type == NPCID.GoblinSorcerer
                || target.type == ModContent.NPCType<UndeadCaster>()
                || target.type == ModContent.NPCType<MindflayerServant>()
                )
            {
                damage *= 8;
            }
            if (target.type == NPCID.Tim
                || target.type == ModContent.NPCType<DungeonMage>()
                || target.type == ModContent.NPCType<DemonSpirit>()
                || target.type == ModContent.NPCType<ShadowMage>()
                || target.type == ModContent.NPCType<AttraidiesIllusion>()
                || target.type == ModContent.NPCType<AttraidiesManifestation>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.BrokenOkiku>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.AttraidiesMimic>()
                || target.type == ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>()
                )
            {
                damage *= 4;
            }
            if (target.type == ModContent.NPCType<CrazedDemonSpirit>()

                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DarkShogunMask>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.SecondForm.DarkDragonMask>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.Okiku>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()

                || target.type == ModContent.NPCType<NPCs.Enemies.MindflayerKingServant>()
                || target.type == ModContent.NPCType<NPCs.Enemies.MindflayerServant>()
                || target.type == ModContent.NPCType<NPCs.Enemies.MindflayerIllusion>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Fiends.LichKingDisciple>()
                )
            {
                damage *= 5;
            }
        }
        public override bool CanShoot(Player player)
        {
            if (shootstacks < 3)
            {
                return false;
            }
            else
            {
                shootstacks = 0;
                return true;
            }
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.LightsBane);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
