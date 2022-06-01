using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenGaiaSword : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A blade made to slay the Witchking.\n" + "Does 3x damage and dispels the defensive shield of the Witchking");
        }

        public override void SetDefaults() {
            Item.rare = ItemRarityID.Red;
            Item.autoReuse = true;
            Item.damage = 185;
            Item.height = 50;
            Item.knockBack = 8;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1.1f;
            Item.useAnimation = 21;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.Red_10;
            Item.width = 50;
        }



        /*public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FallenStar, 120);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("WhiteTitanite").Type, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 100000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }*/  

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>())
            {
                target.AddBuff(ModContent.BuffType<Buffs.DispelShadow>(), 36000);
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.AddNPCBuff, number: target.whoAmI, number2: ModContent.BuffType<Buffs.DispelShadow>(), number3: 36000);
                    ModPacket shadowPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                    shadowPacket.Write((byte)tsorcPacketID.DispelShadow);
                    shadowPacket.Write(target.whoAmI);
                    shadowPacket.Send();
                }
            }
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            if (target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>()) { 
                damage *= 3;
            }
        }
    }
}
