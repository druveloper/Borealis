#include <metal_stdlib>

constant uint Width = {{WIDTH}};
constant uint Height = {{HEIGHT}};
constant float HalfWidth = Width / 2.0;
constant float HalfHeight = Height / 2.0;

struct Moon {
    float2 position;
    float3 color;
};

//constant Moon Moon1, Moon Moon2, Moon Moon3;
//Moon1.x = {{MOON1X}}; Moon1.y = {{MOON1Y}};
//Moon2.x = {{MOON2X}}; Moon2.y = {{MOON2Y}};
//Moon3.x = {{MOON3X}}; Moon3.y = {{MOON3Y}};

using namespace metal;

float fclamp(float value, float minN, float maxN)
{
    return fmin(maxN, fmax(minN, value));
}

int iclamp(int value, int minN, int maxN)
{
    return min(maxN, max(minN, value));
}

uint getPixelIndex(uint x, uint y)
{
    return(4 * (Width * y + x));
}

float2 getPointFromPixel(uint2 pixel)
{
    float2 point;

    point.x = (pixel.x - HalfWidth) / HalfWidth;
    point.y = (HalfHeight - pixel.y) / HalfHeight;

    return point;
}

uint2 getPixelFromPoint(float2 point)
{
    uint2 pixel;

    pixel.x = point.x * HalfWidth + HalfWidth;
    pixel.y = HalfHeight - (point.y * HalfHeight);

    return pixel;
}

float3 drawMoon(Moon moon, float2 drawPoint)
{
    float x = drawPoint.x;
    float y = drawPoint.y;
    float mx = moon.position.x;
    float my = moon.position.y;
    float3 drowColor;
    float r, g, b;
    r = moon.color.x;
    g = moon.color.y;
    b = moon.color.z;

    // fclamp(fabs(x - mx) + fabs(y - my), 0, .5);

    drowColor.r = r * (1 - pow(fclamp(fabs(x - mx) + fabs(y - my), 0, .5) / 0.50, .5));
    drowColor.g = g * (1 - pow(fclamp(fabs(x - mx) + fabs(y - my), 0, .5) / 0.50, .5));
    drowColor.b = b * (1 - pow(fclamp(fabs(x - mx) + fabs(y - my), 0, .5) / 0.50, .5));

    return drowColor;
}

float3 overlayColors(float3 oldColor, float3 newColor)
{
    // blend newColor with oldColor
    
    float brightness = max3(newColor.r, newColor.g, newColor.b);
    
    float3 output;

    output.r = (1 - brightness) * oldColor.r + brightness * newColor.r;
    output.g = (1 - brightness) * oldColor.g + brightness * newColor.g;
    output.b = (1 - brightness) * oldColor.b + brightness * newColor.b;

    return output;
}

kernel void generate_image(device const uint8_t* input  [[ buffer(0) ]],
                           device uint8_t*       result [[ buffer(1) ]],
                           device const float*    moonArray   [[ buffer(2) ]],
                           uint2 pixel [[ thread_position_in_grid ]]) 
{
    float2 point = getPointFromPixel(pixel);
    uint i =  getPixelIndex(pixel.x, pixel.y);

    float3 oldColor;
    oldColor.r = input[i] / 255.0;
    oldColor.g = input[i+1] / 255.0;
    oldColor.b = input[i+2] / 255.0;

    Moon moon;
    moon.position.x = moonArray[0];
    moon.position.y = moonArray[1];
    moon.color.r = moonArray[2];
    moon.color.g = moonArray[3];
    moon.color.b = moonArray[4];

    float3 newColor = drawMoon(moon, point);
    float3 finalColor = overlayColors(oldColor, newColor);

    result[i] =   iclamp(finalColor.r * 255, 0, 255);
    result[i+1] = iclamp(finalColor.g * 255, 0, 255);
    result[i+2] = iclamp(finalColor.b * 255, 0, 255);
    result[i+3] = 255;
}

