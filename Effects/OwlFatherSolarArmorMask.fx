// Sprite-attached solar material. This remains a duplicate of the solid armor/axe draw, but it now
// uses alpha as containment rather than painting one uniform gold silhouette. The separate puppet
// wisp ribbons are deliberately unmasked so flame can continue beyond these sprite boundaries.
#include "PixelShaderCommon.fxh"

sampler ArmorSampler : register(s0);
sampler FlowNoise : register(s1);

float3 uColor;            // bright solar yellow (base/low-heat tone)
float3 uSecondaryColor;   // near-white solar highlight (hot channels)
float uOpacity;
float uTime;
float4 uSourceRect;       // x/y/width/height in armor-sheet pixels
float2 uImageSize0;

float4 OwlFatherSolarArmorMaskPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 armor = tex2D(ArmorSampler, coords);
    float mask = armor.a * sampleColor.a;

    // NOTE: this mask cannot make flame escape the sprite's own silhouette - the draw quad is
    // exactly the armor's pixel bounds (DrawSize == uSourceRect, no spare margin), so dilating the
    // alpha sampled from this same texture has no unused space to reveal (confirmed by rendering it
    // through ShaderPreview: a 4-tap dilate produced zero visible change). Escaping flame is
    // deliberately a SEPARATE, unmasked layer - see EmitSpectralSolarWisps/OwlFatherSolarWispSystem
    // in OwlFather.cs, which spawn free-standing flame particles that travel beyond this sprite.

    float2 sheetPixel = coords * uImageSize0;
    float2 frameUV = (sheetPixel - uSourceRect.xy) / max(uSourceRect.zw, float2(1.0, 1.0));
    // Chunky 2x2 blocks (in armor-sheet pixels) instead of smooth-scrolling noise, same technique
    // as FireSlashArc's PixelGrid - 7x7 read as too coarse/blocky at gameplay zoom, so this settles
    // on the mod's usual fine pixel-art grain instead.
    float2 pixelFrameUV = PixelateShaderUV(frameUV, uSourceRect.zw, 2.0);

    // Two vertically biased, counter-drifting samples. SmoothNoise has real long flame filaments;
    // the anisotropic frequencies make them climb rather than read as round scrolling clouds.
    // Lower frequency across/up the frame than before (was 3.8/1.12 and 6.4/1.78) so individual
    // flame tongues read as bigger, chunkier shapes instead of a fine speckle (see vfx-shader-tips
    // "Directional Features Come From Anisotropic Sampling Frequency" - lower frequency = bigger
    // features). Positive Y offsets here (texture V increases downward) sample further down the
    // noise texture as uTime advances, which makes the pattern APPEAR to travel up the sprite on
    // screen - the apparent scroll direction is the negative of the offset added to the sample
    // coordinate.
    float n1 = tex2D(FlowNoise,
        frac(pixelFrameUV * float2(2.1, 0.62) + float2(uTime * 0.10, uTime * 0.58))).r;
    float n2 = tex2D(FlowNoise,
        frac(pixelFrameUV * float2(3.6, 1.0) + float2(-uTime * 0.13, uTime * 0.37))).r;

    // The broad layer owns the material body; the finer layer only carves moving hot channels.
    // Keeping a substantial threshold is what restores visible orange/yellow contrast instead of
    // the previous nearly constant amber wash.
    float body = saturate(n1 * 1.18 - 0.22);
    float detail = saturate(n2 * 1.35 - 0.40);
    float heat = saturate(body * 0.72 + detail * 0.52 - 0.12);
    float hot = heat * heat;
    float core = hot * hot;

    float3 solarColor = uColor * (0.11 + body * 0.24)
        + uSecondaryColor * (hot * 0.52)
        + float3(1.0, 0.98, 0.88) * (core * 0.38);

    // Player rendering uses premultiplied AlphaBlend. Low-heat areas retain enough alpha to shade
    // the flat sprite toward solar yellow; hot channels exceed alpha and read as emitted light.
    float alpha = mask * uOpacity * (0.34 + body * 0.24 + hot * 0.16);
    float3 rgb = solarColor * mask * uOpacity;
    return float4(rgb, alpha);
}

technique OwlFatherSolarArmorMask
{
    pass OwlFatherSolarArmorMaskPass
    {
        PixelShader = compile ps_2_0 OwlFatherSolarArmorMaskPixel();
    }
}
