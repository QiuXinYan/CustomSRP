Shader "Custom Shader/SRP002Shader"
{
    Properties
    {
        _baseColor("base color", Color) = (1.0, 1.0, 1.0, 1.0)
        _albedoMap("albedoMap", 2D) = "white" {}
        _normalMap("normalMap", 2D) = "bump" {}
        _normalScale("normalScale", Range(0, 1)) = 0.0
        _metallicMap("metallicMap", 2D) = "white" {}
        _roughnessMap("roughnessMap", 2D) = "white" {}
        _aoMap("aoMap", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }         
        Pass
        {
            Tags { "LightMode" = "SPR002_PBR_Pass" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "../../ShaderLibrary/Common.hlsl"
            #include "../../ShaderLibrary/PBR.hlsl"
            #define MAX_DIRECTIONAL_LIGHT_COUNT 4

            TEXTURE2D(_albedoMap); SAMPLER(sampler_albedoMap);
            TEXTURE2D(_normalMap); SAMPLER(sampler_normalMap);
            TEXTURE2D(_metallicMap); SAMPLER(sampler_metallicMap);
            TEXTURE2D(_roughnessMap); SAMPLER(sampler_roughnessMap);
            TEXTURE2D(_aoMap); SAMPLER(sampler_aoMap);
            float4 _baseColor;
            float  _normalScale;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 baseUV: TEXCOORD0;
            };

            struct Varyings
            {    
                float3 normalWS : VAR_NORMAL;
                float4 positionCS : SV_POSITION;
                float3 positionWS : VAR_POSITION;
                float4 tangentWS : VAR_TANGENT;
                float3 binormalWS: TEXCOORD1;
                float2 baseUV : VAR_BASE_UV;
            };

            CBUFFER_START(_CustomLight)
                int _DirectionalLightCount;
                float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
                float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
            CBUFFER_END

            // ----------------------------------------------------------------------------
            float DistributionGGX(float3 N, float3 H, float roughness)
            {
                float a = roughness*roughness;
                float a2 = a*a;
                float NdotH = max(dot(N, H), 0.0);
                float NdotH2 = NdotH*NdotH;

                float nom   = a2;
                float denom = (NdotH2 * (a2 - 1.0) + 1.0);
                denom = PI * denom * denom;

                return nom / denom;
            }

            // ----------------------------------------------------------------------------
            float GeometrySchlickGGX(float NdotV, float roughness)
            {
                float r = (roughness + 1.0);
                float k = (r*r) / 8.0;

                float nom   = NdotV;
                float denom = NdotV * (1.0 - k) + k;

                return nom / denom;
            }

            // ----------------------------------------------------------------------------
            float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
            {
                float NdotV = max(dot(N, V), 0.0);
                float NdotL = max(dot(N, L), 0.0);
                float ggx2 = GeometrySchlickGGX(NdotV, roughness);
                float ggx1 = GeometrySchlickGGX(NdotL, roughness);

                return ggx1 * ggx2;
            }

            // ----------------------------------------------------------------------------
            float3 fresnelSchlick(float cosTheta, float3 F0)
            {
                return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
            }

            // ----------------------------------------------------------------------------
            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);;
                output.binormalWS = cross(output.normalWS,output.tangentWS)*input.tangentOS.w;
                output.baseUV = input.baseUV;
                return output;
            }
            
            float4 frag (Varyings input) : SV_Target
            {
                
                float3 albedo = pow(SAMPLE_TEXTURE2D(_albedoMap, sampler_albedoMap, input.baseUV).rgb, 2.2) * _baseColor.rgb;
                float metallic = SAMPLE_TEXTURE2D(_metallicMap, sampler_metallicMap, input.baseUV).r;
                float roughness = SAMPLE_TEXTURE2D(_roughnessMap, sampler_roughnessMap, input.baseUV).r;
                float ao = SAMPLE_TEXTURE2D(_aoMap, sampler_aoMap, input.baseUV).r;

                float4 normalMap = SAMPLE_TEXTURE2D(_normalMap, sampler_normalMap, input.baseUV);
                float3 normal = DecodeNormal(normalMap, _normalScale);

                float3 N = normalize(NormalTangentToWorld(normal, input.normalWS, input.tangentWS));
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);

                float3 L0 = float3(0,0,0);
                // calculate reflectance at normal incidence; if dia-electric (like plastic) use F0 
                // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow)    
                float3 F0 = float3(0.04, 0.04, 0.04); 
                F0 = lerp(F0, albedo, metallic);
                for(int i = 0; i < _DirectionalLightCount; i++) {
                    float3 L = normalize(_DirectionalLightDirections[i].xyz);
                    float3 H = normalize(V + L);

                    float3 radiance = _DirectionalLightColors[i].xyz;

                    // Cook-Torrance BRDF
                    float NDF = DistributionGGX(N, H, roughness);   
                    float G   = GeometrySmith(N, V, L, roughness);      
                    float3 F    = fresnelSchlick(max(dot(H, V), 0.0), F0);
                    
                    float3 numerator    = NDF * G * F; 
                    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001; // + 0.0001 to prevent divide by zero
                    float3 specular = numerator / denominator;
                    
                    // kS is equal to Fresnel
                    float3 kS = F;
                    // for energy conservation, the diffuse and specular light can't
                    // be above 1.0 (unless the surface emits light); to preserve this
                    // relationship the diffuse component (kD) should equal 1.0 - kS.
                    float3 kD = float3(1.0, 1.0,1.0) - kS;
                    // multiply kD by the inverse metalness such that only non-metals 
                    // have diffuse lighting, or a linear blend if partly metal (pure metals
                    // have no diffuse light).
                    kD *= 1.0 - metallic;	  

                    // scale light by NdotL
                    float NdotL = max(dot(N, L), 0.0);        

                    // add to outgoing radiance Lo
                    L0 += (kD * albedo / PI + specular) * radiance * NdotL;  // note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
                    // L0 += BRDF(L,V,N,X,Y) * radiance * NdotL;
                }
                // ambient lighting (note that the next IBL tutorial will replace 
                // this ambient lighting with environment lighting).
                float3 ambient = float3(0.03, 0.03, 0.03) * albedo * ao;
                
                float3 color = ambient + L0;

                // HDR tonemapping
                color = color / (color + float3(1.0, 1.0, 1.0));
                // gamma correct
                color = pow(color, float3(1.0/2.2,1.0/2.2,1.0/2.2)); 
                return float4(color, 1.0);
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
