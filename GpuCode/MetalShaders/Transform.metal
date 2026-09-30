#include <metal_stdlib>


constant uint Width = {{WIDTH}};
constant uint Height = {{HEIGHT}};
constant float HalfWidth = Width / 2.0;
constant float HalfHeight = Height / 2.0;


using namespace metal;

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

float2 transformPoint(float2 point1)
{
    float x = point1.x;
    float y = point1.y;
    float2 point2;

    point2.x = x + .15 * sin(y * M_PI_F * 3.0);
    point2.y = y + .15 * cos(x * M_PI_F * 3.0);

    return point2;
}

kernel void generate_image(device const uint8_t* input  [[ buffer(0) ]],
                           device uint8_t*       result [[ buffer(1) ]],
                           uint2 pixel [[ thread_position_in_grid ]]) 
{
    float2 point1 = getPointFromPixel(pixel);
    float2 point2 = transformPoint(point1);

    uint2 pixel2 = getPixelFromPoint(point2);

    uint i =  getPixelIndex(pixel.x, pixel.y);
    uint i2 = getPixelIndex(pixel2.x, pixel2.y);

    result[i]   = clamp(input[i2]   - 10, 0, 255);
    result[i+1] = clamp(input[i2+1] - 10, 0, 255);
    result[i+2] = clamp(input[i2+2] - 10, 0, 255);
    result[i+3] = 255;
}

