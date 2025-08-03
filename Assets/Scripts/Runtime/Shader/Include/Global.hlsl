#ifndef GLOBAL_HLSL
#define GLOBAL_HLSL

static const float pi = acos(-1.0);
static const float tau = 2.0 * pi;
static const float degree = tau / 360.0;
static const float goldenRatio = 0.5 + 0.5 * sqrt(5.0);
static const float goldenAngle = tau / goldenRatio / goldenRatio;

#endif