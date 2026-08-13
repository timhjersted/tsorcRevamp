// Gigas's lingering holy fire. The bright ember bed maps to the existing 64x16 hostile patch;
// this shader's 88x36 quad only adds a decorative, eroded flame canopy around that true boundary.
sampler MacroNoise : register(s0);
sampler DetailNoise : register(s1);

float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float Remaining;

float4 GigasConsecratedGroundPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // The visual shell is 88px wide, so .136..864 is the exact 64px hostile span.
    float damageX = (coords.x - 0.5) * 1.375 + 0.5;
    float macro = tex2D(MacroNoise, float2(damageX * 2.45 - Time * 0.10, coords.y * 1.25 + Time * 0.18)).r;
    float detail = tex2D(DetailNoise, float2(damageX * 7.40 + Time * 0.29, coords.y * 2.15 - Time * 0.64)).r;

    // The actual danger span has its own eroded end caps. emberBottom varies from .83 to .95,
    // so the lower edge breaks into flame tongues; all material is still zero at the quad's edge.
    float endCaps = saturate(damageX * 5.2) * saturate((1.0 - damageX) * 5.2);
    float emberBottom = 0.83 + macro * 0.12;
    float floorFade = saturate((emberBottom - coords.y) * 18.0);
    float tongueTop = 0.64 - macro * 0.46;
    float tongues = saturate((coords.y - tongueTop) * 10.5) * floorFade * endCaps;
    float emberBed = saturate((coords.y - 0.55) * 10.5) * floorFade * endCaps;

    // Two short side flames give the patch a readable edge without extending the bright hazard.
    float leftFoot = saturate((0.18 - abs(damageX - 0.11)) * 18.0);
    float rightFoot = saturate((0.18 - abs(damageX - 0.89)) * 18.0);
    float edgeFlame = (leftFoot + rightFoot) * saturate((coords.y - (0.62 - detail * 0.22)) * 13.0) * floorFade;
    float coals = tongues * saturate(macro * 0.68 + detail * 0.58 - 0.14);
    float hotCracks = emberBed * saturate((detail - 0.43) * 1.75) + edgeFlame * 0.35;

    float age = Remaining;
    float alpha = saturate(coals * 0.92 + hotCracks * 0.54) * age * Opacity;
    float3 color = lerp(OuterColor, MiddleColor, saturate(coals * 0.56 + macro * 0.44));
    // hotCracks is bounded below 1.4; .70 therefore stays below one without a clamp.
    color = lerp(color, CoreColor, hotCracks * 0.70);
    // AlphaBlend is premultiplied: solid gold material keeps its identity over daylight.
    return float4(sampleColor.rgb * color * alpha, sampleColor.a * alpha);
}

technique GigasConsecratedGround
{
    pass GigasConsecratedGroundPass
    {
        PixelShader = compile ps_2_0 GigasConsecratedGroundPixel();
    }
}
