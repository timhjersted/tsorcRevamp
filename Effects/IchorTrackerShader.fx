matrix WorldViewProjection;
texture noiseTexture;
sampler textureSampler = sampler_state
{
    Texture = (noiseTexture);
    AddressU = wrap;
    AddressV = wrap;
};

float fadeOut;
float time;
float4 shaderColor;

struct VertexShaderInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
};

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates;        
    //uv.x = uv.x % 0.5;
    //uv.y = uv.y % 0.5;
    //uv *= 2;
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);    
    
    //Raise it to a high exponent, resulting in sharply increased intensity at the center that trails off smoothly
    //Higher number = more narrow and compressed trail
    intensity = pow(intensity, 6.0);
    
    //Flat doubling to incrase the total intensity
    intensity *= 2;        
    
    //This controls where the front of the bolt starts to curve
    float inflectionPoint = 0.82;
    //inflectionPoint *= fadeOut;
    
    //Make it fade out towards the end
    if (uv.x < inflectionPoint)
    {
        //intensity = lerp(0.0, intensity, pow(uv.x / inflectionPoint, 1));
        
        //intensity *=  uv.x;
    }
    
    //Make it fade in towards the start
    if (uv.x > inflectionPoint)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - inflectionPoint), 1));
    }

    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint = uv;
    
    //Zoom in on the noise texture, then shift it over time to make it appear to be flowing    
    samplePoint *= 0.75;
    samplePoint.x = (samplePoint.x + time * 0.40) * 3;
    
    //Compress it vertically
    samplePoint.y = samplePoint.y / 2;
    samplePoint.y += 0.1;

    //Get the noise texture at that point
    float sampleIntensity = 1- tex2D(textureSampler, samplePoint).r;
    
    //Mix it with the laser color
    float4 noiseColor = float4(1.0, 1.0, 1.0, 1.0);
    noiseColor.r = sampleIntensity * shaderColor.r;
    noiseColor.b = sampleIntensity * shaderColor.b;
    noiseColor.g = sampleIntensity * shaderColor.g;
    
    noiseColor = sampleIntensity * shaderColor;

    //Mix it with 'intensity' to make it more intense near the center
    intensity = pow(intensity, 2);
    if (intensity > 0.4)
    {
        intensity = 0.4;
    }
    
    float4 effectColor = pow(noiseColor, 6) * intensity * 10 * pow(uv.x, 3);
    effectColor *= 2;
    return effectColor * fadeOut; //    input.Color;
    
    //Looks cool as hell, but not the vibe i'm going for
    //float4 effectColor = noiseColor * noiseColor * pow(intensity, 2) * 8.0;
    
    return effectColor / pow(fadeOut, 1);
}

technique IchorTrackerShader
{
    pass IchorTrackerShaderPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};