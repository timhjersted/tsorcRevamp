sampler spriteTexture : register(s0);

float4 tintColor; // rgb = glow tint, a = overall opacity multiplier

// Recolors an entire composed Player draw (PuppetNPC's spectral-duplicate pass wraps the whole
// Main.PlayerRenderer.DrawPlayer call — every body/armor/weapon layer's own SpriteBatch.Draw calls —
// in one Immediate-mode Begin/End using this effect, the same technique PuppetSpriteExporter proved
// works for capturing a full puppet draw in one SpriteBatch scope). AlphaBlend is premultiplied in
// XNA/FNA, so this MUST return color*alpha, alpha or the whole duplicate paints as a flat rectangle
// the size of its quad (see vfx-shader-tips \43).
float4 SpectralOverlayPS(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float sourceAlpha = tex2D(spriteTexture, coords).a * sampleColor.a;
    float alpha = sourceAlpha * tintColor.a;
    return float4(tintColor.rgb * alpha, alpha);
}

technique SpectralOverlay
{
    pass SpectralOverlayPass
    {
        PixelShader = compile ps_2_0 SpectralOverlayPS();
    }
}
