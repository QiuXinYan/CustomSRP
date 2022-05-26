Shader "Custom Shader/PBRShader"
{
    Properties
    {
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _AmbientColor("Ambient Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _AmbientStrengh("Ambient Strengh", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Subsurface ("Subsurface", Range(0, 1)) = 0
        _Specular ("Specular", Range(0, 1)) = 0.5
        _Roughness ("Roughness", Range(0, 1)) = 0.5
        _SpecularTint ("SpecularTint", Range(0, 1)) = 0
        _Anisotropic ("Anisotropic", Range(0, 1)) = 0
        _Sheen ("Sheen", Range(0, 1)) = 0
        _SheenTint ("SheenTint", Range(0, 1)) = 0.5
        _Clearcoat ("Clearcoat", Range(0, 1)) = 0
        _ClearcoatGloss ("ClearcoatGloss", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }         
        Pass
        {
            Tags { "LightMode" = "SPR001_PBR_Pass" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "../../ShaderLibrary/Common.hlsl"
            #include "../../ShaderLibrary/PBR.hlsl"
            #define MAX_DIRECTIONAL_LIGHT_COUNT 4

            float4 _BaseColor,_AmbientColor;  //新加
            float  _Metallic , _Subsurface, _Specular ,_Roughness ,_SpecularTint ,_Anisotropic , _Sheen ,_SheenTint, _Clearcoat ,_ClearcoatGloss,_AmbientStrengh;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {    
                float3 normalWS : VAR_NORMAL;
                float4 positionCS : SV_POSITION;
                float3 positionWS : VAR_POSITION;
                float3 tangentWS : VAR_TANGENT;
                float3 binormalWS: TEXCOORD0;
            };

            CBUFFER_START(_CustomLight)
                int _DirectionalLightCount;
                float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
                float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
            CBUFFER_END

            //L:light
            //V:View
            //N:Normal
            //X:Tangent
            //Y:BiNormal
            float3 BRDF( float3 L, float3 V, float3 N, float3 X, float3 Y )
            {
                float NdotL = dot(N,L);
                float NdotV = dot(N,V);
                //if (NdotL < 0 || NdotV < 0) return float3(0,0,0);

                float3 H = normalize(L+V);
                float NdotH = dot(N,H);
                float LdotH = dot(L,H);

                float3 Cdlin = mon2lin(_BaseColor.xyz);
                float Cdlum = .3*Cdlin[0] + .6*Cdlin[1]  + .1*Cdlin[2]; // luminance approx.

                float3 Ctint = Cdlum > 0 ? Cdlin/Cdlum : float3(1,1,1); // normalize lum. to isolate hue+sat
                float3 Cspec0 = lerp(_Specular*.08* lerp(float3(1,1,1), Ctint, _SpecularTint), Cdlin, _Metallic);
                float3 Csheen = lerp(float3(1,1,1), Ctint, _SheenTint);

                // Diffuse fresnel - go from 1 at normal incidence to .5 at grazing
                // and lerp in diffuse retro-reflection based on _Roughness
                float FL = SchlickFresnel(NdotL), FV = SchlickFresnel(NdotV);
                float Fd90 = 0.5 + 2 * LdotH*LdotH * _Roughness;
                float Fd = lerp(1.0, Fd90, FL) * lerp(1.0, Fd90, FV);

                // Based on Hanrahan-Krueger brdf approximation of isotropic bssrdf
                // 1.25 scale is used to (roughly) preserve albedo
                // Fss90 used to "flatten" retroreflection based on _Roughness
                float Fss90 = LdotH*LdotH*_Roughness;
                float Fss = lerp(1.0, Fss90, FL) * lerp(1.0, Fss90, FV);
                float ss = 1.25 * (Fss * (1 / (NdotL + NdotV) - .5) + .5);

                // _Specular
                float aspect = sqrt(1-_Anisotropic*.9);
                float ax = max(.001, sqr(_Roughness)/aspect);
                float ay = max(.001, sqr(_Roughness)*aspect);
                float Ds = GTR2(NdotH, _Roughness);
                float FH = SchlickFresnel(LdotH);
                float3 Fs = lerp(Cspec0, float3(1,1,1), FH);
                float Gs;
                Gs  = smithG_GGX_aniso(NdotL, dot(L, X), dot(L, Y), ax, ay);
                Gs *= smithG_GGX_aniso(NdotV, dot(V, X), dot(V, Y), ax, ay);

                // sheen
                float3 Fsheen = FH * _Sheen * Csheen;

                // _Clearcoat (ior = 1.5 -> F0 = 0.04)
                float Dr = GTR1(NdotH, lerp(.1,.001,_ClearcoatGloss));
                float Fr = lerp(.04, 1.0, FH);
                float Gr = smithG_GGX(NdotL, .25) * smithG_GGX(NdotV, .25);

                return ((1/PI) * lerp(Fd, ss, _Subsurface)*Cdlin + Fsheen)
                * (1-_Metallic)
                + Gs*Fs*Ds + .25*_Clearcoat*Gr*Fr*Dr;
                // return Ds;
            }

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);;
                output.binormalWS = cross(output.normalWS,output.tangentWS)*input.tangentOS.w;
                return output;
            }
            
            float4 frag (Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 X = normalize(input.tangentWS.xyz);
                float3 Y = normalize(input.binormalWS);
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);
                float3 L0 = float3(0,0,0);
                for(int i = 0; i < _DirectionalLightCount; i++) {
                    float3 L = normalize(_DirectionalLightDirections[i].xyz);
                    float3 radiance = _DirectionalLightColors[i].xyz;
                    // scale light by NdotL
                    float NdotL = max(dot(N, L), 0.0); 
                    L0 += BRDF(L,V,N,X,Y) * radiance * NdotL;
                }
                float3 ambientColor = _AmbientColor.xyz * _AmbientStrengh;
                return float4(L0+ambientColor, 1.0);
            }

            ENDHLSL
        }
    }
}
