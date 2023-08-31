sampler uImage0 : register(s0);
texture noiseTexture;
sampler uImage1 = sampler_state
{
    Texture = (noiseTexture);
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
    float frameY = (coords.y * 1302 - sourceRectY) / 187;
    float2 dir = float2(coords.x, frameY) - float2(0.5, 0.5);
   
    
    float distanceIntensity = distance(float2(coords.x, frameY), float2(0.5, 0.5)) * 2;
    
    //Convert uv from rectangular to polar coordinates
    //float2 dir = coords - float2(0.5, 0.5);
    float angle = atan2(dir.y, dir.x) * INVERSETWOPI;
    float2 samplePoint = float2(distanceIntensity, angle);
    
    //Stretch it so it looks good
    samplePoint = samplePoint * 1;
    samplePoint.y = samplePoint.y * 2;
    
    //Offset it based on time to create the flowing effect
    samplePoint.x = samplePoint.x - (time * 0.5);

    //Get the noise texture at that point
    float sampleIntensity = tex2D(uImage1, samplePoint).r;
    
    //Check the alpha of the current texture
    float textureIntensity = tex2D(uImage0, coords).a * 1.25;
    
    float intensity = textureIntensity * pow(sampleIntensity, 0.25);
    
    //Pixelate it to limit the pallette
    //intensity = intensity - intensity % 0.08;
    
    float yellowExp = 2.5;
    float yellowScale = 0.1;
    float whiteScale = 0.02;
    if (opacity == 1)
    {
        yellowExp = 1.8;
        yellowScale = 0.02;
        whiteScale = 0.007;

    }

    //Scale 'intensity' into the RGB channels. Values are fine-tuned to turn noise into a fire-like effect.
    return float4(pow(intensity, yellowExp) * yellowScale, pow(intensity, 3.0), pow(intensity, 4.5) * whiteScale, 1) * 100 * opacity;
}

technique HunterEffect
{
    pass EffectPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}