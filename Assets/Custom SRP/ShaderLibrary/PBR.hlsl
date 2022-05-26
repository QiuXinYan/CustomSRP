#ifndef CUSTOM_PBR_INCLUDED
#define CUSTOM_PBR_INCLUDED

#define PI 3.14159265358979323846

float sqr(float x) { return x*x; }

half Pow5(half v)
{
	return v * v * v * v * v;
}

// [Burley 2012, "Physically-Based Shading at Disney"]
float3 Diffuse_Burley_Disney( float3 DiffuseColor, float Roughness, float NoV, float NoL, float VoH )
{
	float FD90 = 0.5 + 2 * VoH * VoH * Roughness;
	float FdV = 1 + (FD90 - 1) * Pow5( 1 - NoV );
	float FdL = 1 + (FD90 - 1) * Pow5( 1 - NoL );
	return DiffuseColor * ( (1 / PI) * FdV * FdL );
}


float SchlickFresnel(float u)
{
   float m = clamp(1-u, 0, 1);
   float m2 = m*m;
   return m2*m2*m; 
}

float GTR1(float NdotH, float a)
{
   if (a >= 1) return 1/PI;
   float a2 = a*a;
   float t = 1 + (a2-1)*NdotH*NdotH;
   return (a2-1) / (PI*log(a2)*t);
}


float GTR2(float NdotH, float a)
{
   float a2 = a*a;
   float t = 1 + (a2-1)*NdotH*NdotH;
   return a2 / (PI * t*t);
}

//法线分布项
float GTR2_aniso(float NdotH, float HdotX, float HdotY, float ax, float ay)
{
   return 1 / (PI * ax*ay * sqr( sqr(HdotX/ax) + sqr(HdotY/ay) + NdotH*NdotH ));
}

float smithG_GGX(float NdotV, float alphaG)
{
   float a = alphaG*alphaG;
   float b = NdotV*NdotV;
   return 1 / (NdotV + sqrt(a + b - a*b));
}

float smithG_GGX_aniso(float NdotV, float VdotX, float VdotY, float ax, float ay)
{
   return 1 / (NdotV + sqrt( sqr(VdotX*ax) + sqr(VdotY*ay) + sqr(NdotV) ));
}

float3 mon2lin(float3 x)
{
   return float3(pow(x[0], 2.2), pow(x[1], 2.2), pow(x[2], 2.2));
}



#endif