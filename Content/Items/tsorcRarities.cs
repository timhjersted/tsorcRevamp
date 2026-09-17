using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items 
{
    public class DarkBlue : ModRarity
    {
        public override int GetPrefixedRarity(int offset, float valueMult)
        {
            return Type; 
        }

        public override Color RarityColor => new Color(50, 100, 255); 
    }

    public class OrangeRed : ModRarity
    {
        public override int GetPrefixedRarity(int offset, float valueMult)
        {
            return Type; 
        }

        public override Color RarityColor => new Color(255, 90, 55); 
    }

    public class Pinky : ModRarity
    {
        public override int GetPrefixedRarity(int offset, float valueMult)
        {
            return Type; 
        }

        public override Color RarityColor => new Color(255, 10, 170); 
    }
}