using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    class BarrowBlade : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("An invisible blade wrought with spells of a fierce power." +
                                "\n[c/ffbf00:Dispels the defensive shields of Artorias and the Witchking]" +
                                "\nThe night reveals its connection to the Abyss..." +
                                "\nHas a chance to spread Ichor to those it pierces");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Quest; //so people know it's important
            Item.damage = 98;
            Item.height = 32;
            Item.knockBack = 5;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Generic;
            Item.scale = 0.9f;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.UseSound = SoundID.Item28;
            //Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            //Item.alpha = 240;
            Item.value = PriceByRarity.Blue_1;
            Item.width = 32;
            Item.shoot = ModContent.ProjectileType<Projectiles.CMSCrescent>();
            //Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.BarrowBladeProjectile>(); // The projectile is what makes a shortsword work
            Item.shootSpeed = 4.5f; // Was 2.1 - This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values
        }

        //public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (!Main.dayTime)
            {
                
                Item.shoot = ModContent.ProjectileType<Projectiles.CrescentTrue>();
                Item.shootSpeed = 14f;
                Item.useTime = 40;
                Item.useAnimation = 40;
                Item.damage = 186;
                Item.UseSound = SoundID.Item20;

            }
            if (Main.dayTime)
            {
                
                Item.shoot = ModContent.ProjectileType<Projectiles.CMSCrescent>();
                Item.shootSpeed = 4.5f;
                Item.UseSound = SoundID.Item28;
                Item.useTime = 20;
                Item.useAnimation = 20;
                Item.damage = 98;


            }

            
        }
        public override bool MeleePrefix()
        {
            return true;
        }


        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
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
            target.AddBuff(BuffID.Ichor, 1800);

        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
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
            target.AddBuff(BuffID.Ichor, 1800);
        }
    }
}
