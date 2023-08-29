using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items;


public abstract class Soul : ModItem
{
    //theres too many souls in this mod, im not making individual files for all of them 
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 4));
        ItemID.Sets.AnimatesAsSoul[Item.type] = true;
        ItemID.Sets.ItemIconPulse[Item.type] = true;
        ItemID.Sets.ItemNoGravity[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item refItem = new Item();
        refItem.SetDefaults(ItemID.SoulofSight);
        Item.width = refItem.width;
        Item.height = refItem.height;
        Item.maxStack = 999999;
        Item.value = 1;
        Item.rare = ItemRarityID.Lime;
    }
}

public class DarkSoul : BaseRarityItem
{

    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 4));
        ItemID.Sets.AnimatesAsSoul[Item.type] = true;
        ItemID.Sets.ItemIconPulse[Item.type] = true;
        ItemID.Sets.ItemNoGravity[Item.type] = true;
        Tooltip.SetDefault("Soul of a fallen creature." +
            "\nCan be used at Demon Altars to forge new weapons, items, and armors.");
    }
    public override void SetDefaults()
    {
        Item refItem = new Item();
        refItem.SetDefaults(ItemID.SoulofSight);
        Item.width = refItem.width;
        Item.height = refItem.height;
        Item.maxStack = 999999;
        Item.value = 1;
        Item.rare = ItemRarityID.Lime;
        DarkSoulRarity = 12;
    }
    public override bool GrabStyle(Player player)
    { //make pulling souls through walls more consistent
        Vector2 vectorItemToPlayer = player.Center - Item.Center;
        Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 10f;
        Item.velocity = movement;
        return true;
    }

    public override void GrabRange(Player player, ref int grabRange)
    {
        grabRange *= (2 + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper);
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.15f, 0.6f, 0.32f);

    }
    public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> list)
    {
        foreach (TooltipLine line2 in list)
        {
            if (line2.Mod == "Terraria" && line2.Name == "ItemName")
            {
                line2.OverrideColor = BaseColor.RarityExample;
            }
        }
    }

    public override bool OnPickup(Player player)
    {

        SoundStyle PickupSound = SoundID.NPCDeath52;
        PickupSound.Volume = 0.15f;
        PickupSound.PitchVariance = 0.3f;
        SoundEngine.PlaySound(PickupSound, player.position); // Plays sound.

        int quantity = Item.stack / 50;

        if (quantity > 10)
        {
            quantity = 10;
        }

        for (int j = 1; j < (6 + (1 * quantity)); j++)
        {
            int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default(Color), 1f);
            Main.dust[z].noGravity = true;
            Main.dust[z].velocity *= 2.75f;
            Main.dust[z].fadeIn = 1.3f;
            Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
            vectorother.Normalize();
            vectorother *= (float)Main.rand.Next(60, 100) * (0.04f);
            Main.dust[z].velocity = vectorother;
            vectorother.Normalize();
            vectorother *= 35f;
            Main.dust[z].position = player.Center - vectorother;
        }
        tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
        if (modPlayer.SoulSlot.Item.type != ModContent.ItemType<DarkSoul>())
        {
            modPlayer.SoulSlot.Item = Item.Clone();
        }
        else
        {
            modPlayer.SoulSlot.Item.stack += Item.stack;
        }
        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab, new Vector2(player.position.X, player.position.Y));
        PopupText.NewText(PopupTextContext.RegularItemPickup, Item, Item.stack);
        return false;
    }

    //allow picking up even when out of inventory space
    public override bool ItemSpace(Player player)
    {
        return true;
    }
}

public class GuardianSoul : Soul
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        Tooltip.SetDefault("Soul of an ancient guardian." +
            "\nCan be used at Demon Altars to forge powerful weapons and items.");
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.93f, 0.1f, 0.45f);
    }
}
public class SoulOfAttraidies : Soul
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        DisplayName.SetDefault("Soul of Attraidies");
        Tooltip.SetDefault("The essence of Attraidies' power burns within this soul." +
            "\nYou question whether you should even hold such a thing in your possession.");
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.93f, 0.1f, 0.45f);
    }
}

public class SoulOfArtorias : Soul
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        DisplayName.SetDefault("Soul of Artorias");
        Tooltip.SetDefault("The essence of Artorias of the Abyss.");
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.9f, 0.9f, 0.9f);
    }

}
public class SoulOfBlight : Soul
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        DisplayName.SetDefault("Soul of Blight");
        Tooltip.SetDefault("The essence of destruction.");
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.9f, 0.9f, 0.9f);
    }

}

public class SoulOfChaos : Soul
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        DisplayName.SetDefault("Soul of Chaos");
        Tooltip.SetDefault("The essence of chaos.");
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.70f, 0.20f, 0.13f);
    }

}

public class BequeathedSoul : Soul
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        DisplayName.SetDefault("Bequeathed Lord Soul Shard");
        Tooltip.SetDefault("Soul of the albino Seath the Scaleless." +
            "\nA fragment of a Lord Soul discovered at the dawn of the Age of Fire." +
            "\nSeath allied with Lord Gwyn and turned upon the dragons, and for this he was" +
            "\nawarded dukedom, embraced by the royalty, and given a fragment of a great soul.");
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.33f, 0.75f, 0.70f);
    }

}

public class GhostWyvernSoul : Soul
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        DisplayName.SetDefault("Soul of the Ghost Wyvern");
        Tooltip.SetDefault("The essence of the Ghost Wyvern.");
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.28f, 0.33f, 0.75f);
    }

}

public class CursedSoul : Soul
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        Tooltip.SetDefault("Soul of a cursed being.");
    }
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.85f, 0f, 0f);
    }
}
public class SoulOfLife : Soul
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        DisplayName.SetDefault("Soul of Life");
        Tooltip.SetDefault("The essence of growth, deeply connected to the Earth.");
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, Color.Green.ToVector3());
    }
}
