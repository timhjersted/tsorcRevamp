sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float time;
float2 textureSize;
float2 effectSize;
float splitAngle;
float rotation;
float width;
float4 effectColor;

const float PI = 3.141592;
const float TWOPI = 6.283184;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{    
    //Convert uv from rectangular to polar coordinates
    float2 dir = coords - float2(0.5, 0.5);
    float angle = atan2(dir.y, dir.x);
    angle += PI;
    
    float compAngle = splitAngle + rotation;
    
    if (angle > compAngle || (angle < rotation && angle > rotation - (2 * PI - splitAngle)))
    {
        return float4(0, 0, 0, 0);
    }
        
    //Calculate how close the current pixel is to the center line of the screen
    float distanceIntensity = distance(coords, float2(0.5, 0.5));
    
    //Convert uv from rectangular to polar coordinates
    float normalAngle = (angle + rotation) % 6.28;
    float2 samplePoint = float2(distanceIntensity - time, normalAngle / TWOPI);
    
    
    //distanceFactor *= 60;
    //Calculate how intense a pixel should be based on the noise generator
    float intensity = tex2D(uImage0, samplePoint).r;
    float r = pow(intensity, 4.5) * 1.5f;
    float g = pow(intensity, 3.6) * distanceIntensity;
    float b = pow(intensity, 5.4) * 0.15f * distanceIntensity;

    return float4(r, g, b, 1) * 100;
}

technique FireWaveShader
{
    pass FireWaveShaderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}