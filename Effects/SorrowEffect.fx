sampler uImage0 : register(s0);
texture noiseTexture;
sampler WavySampler = sampler_state
{
    Texture = (noiseTexture);
    AddressU = wrap;
    AddressV = wrap;
};
texture noiseVoronoi;
sampler VoronoiSampler = sampler_state
{
    Texture = (noiseVoronoi);
    AddressU = wrap;
    AddressV = wrap;
};

float time; //Causes the flames to flow with time
float length; //The maximum length
float opacity; //Multiplies the output by this to let it fade in
float sourceRectY;

//Various constants
const float INVERSETWOPI = 0.1591549;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{    
    //Convert uv from rectangular to polar coordinates
    float frameY = (coords.y * 1540 - sourceRectY) / 187;
    float2 dir = float2(coords.x, frameY) - float2(0.5, 0.5);
    
    //float pixelSize = 0.002;
    //coords.x -= coords.x % .003;
    //coords.y -= coords.y % pixelSize;
        
    //Calculate how close the current pixel is to the center line of the screen
    float dist = distance(float2(coords.x, frameY), float2(0.5, 0.5));
    
    //Convert uv from rectangular to polar coordinates
    float2 samplePoint = coords * 0.5;
    samplePoint.x /= 4;
    float pixelTime = time;
    //pixelTime -= pixelTime % 0.02;
    samplePoint.y -= 0.5 * pixelTime;
    
    //Check the alpha of the current texture
    float textureIntensity = tex2D(uImage0, coords).a * 1.5;
    
    //Calculate how intense a pixel should be based on the noise generator and the blur texture
    float intensity = textureIntensity * tex2D(WavySampler, samplePoint).r;
    
    float2 voronoiPoint = coords;
    voronoiPoint.x *= 4;
    voronoiPoint.y -= time / 10;
    voronoiPoint.y *= 16;
    float voronoiPercent = opacity / 2;
    
    //Pixelate it to limit the pallette
    //intensity = intensity - intensity % 0.08;
    
    float blueExp = 2.5;
    float blueScale = 0.2;    
    if (opacity == 1)
    {
        blueExp = 1.8;
        blueScale = 0.02;
        voronoiPercent =  0.7;

    }
    
    intensity *= lerp(1, tex2D(VoronoiSampler, voronoiPoint).r * 10, voronoiPercent);

    //Scale 'intensity' into the RGB channels. Values are fine-tuned to turn noise into a fire-like effect.
    return float4(pow(intensity, 30.0), pow(intensity, blueExp) * blueScale, pow(intensity, 2), 1) * 100 * opacity;
}

technique SorrowEffectShader
{
    pass SorrowEffectShader
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}