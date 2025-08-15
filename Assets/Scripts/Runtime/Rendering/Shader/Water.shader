Shader "RS/Water"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.2, 0.5, 0.7, 0.5)
        _Shininess("Shininess", Range(1, 100)) = 50
        _FogColor("Water Fog Color", Color) = (0.2, 0.5, 0.7, 1)
        _FogDensity("Water Fog Density", Range(0, 1)) = 0.1
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _WaveSpeed("Wave Speed", Range(0, 2)) = 1.0
        _WaveFrequency("Wave Frequency", Range(0, 2)) = 1.0
        _WavePersistence("Wave Persistence", Range(0, 2)) = 1.0
        _WaveLacunarity("Wave Lacunarity", Range(0, 2)) = 1.0
        _WaveIterations("Wave Iterations", Range(1, 8)) = 3
        
        _SSR_MaxDistance("SSR Max Distance", Float) = 50
        _SSR_StepSize("SSR Step Size", Float) = 0.8
        _SSR_Thickness("SSR Thickness", Range(0, 1)) = 0.05
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 1.0
        _FresnelPower("Fresnel Power", Range(0.1, 20)) = 5.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "WaterForwardPass"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Scripts/Runtime/Rendering/Shader/Include/Global.hlsl"
            #include "Assets/Scripts/Runtime/Rendering/Shader/Include/Math.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                // float3 normalOS: NORMAL;
                // float4 tangentOS: TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 positionOS : TEXCOORD2;
                float4 positionCS : TEXCOORD3;
                float3 vsRay : TEXCOORD4;
                
                // float3 normalWS : TEXCOORD1;
                // float3 tangentWS : TEXCOORD2;
                // float3 bitangentWS : TEXCOORD3;
            };

            float4 _BaseColor;
            float _Shininess;
            float4 _FogColor;
            float _FogDensity;
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            float _WaveSpeed;
            float _WaveFrequency;
            float _WavePersistence;
            float _WaveLacunarity;
            int _WaveIterations;
            float4 _NoiseTex_ST;

            // ssr
            #define SSR_MAX_STEPS_CONST 32
            #define SSR_MAX_BINARY_CONST 8
            
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            TEXTURECUBE(_SkyBoxCubeMap);
            SAMPLER(sampler_SkyBoxCubeMap);
            float _SSR_MaxDistance;
            float _SSR_StepSize;
            float _SSR_Thickness;
            float _ReflectionStrength;
            float _FresnelPower;

            /// Water Wave
            /// https://github.com/sixthsurge/photon/blob/iris-stable/shaders/include/surface/water_normal.glsl
            float GerstnerWave(float2 coord, float2 waveDir, float t, float noise, float waveLength)
            {
                const float g = 9.8;

                float k = tau / waveLength;
                float w = sqrt(g * k);
                float x = w * t - k * (dot(waveDir, coord) + noise);

                return square(sin(x) * 0.5 + 0.5);
            }
            
            float GetWaterHeight(float2 coord, float2 waveDir, float2x2 waveRot, float t)
            {
                const float waveFrequnecy = 0.7 * _WaveFrequency;
                const float persistence = 0.5 * _WavePersistence;
                const float lacunarity = 1.7 * _WaveLacunarity;

                const float noiseFrequency = 0.007;
                const float noiseStrenth = 2.0;

                const float heightVariationFrequency = 0.001;
                const float minHeight = 0.4;
                const float heightVariationScale = 2.0;
                const float heightVariationOffset = -0.5;
                const float heightVariationScrollSpeed = 0.1;

                const float amplitudeNormalizationFactor = (1.0 - persistence) / (1.0 - pow(persistence, float(_WaveIterations)));

                float waveNoise[8];
                float2 noiseCoord = (coord + float2(0.0, 0.25 * t)) * noiseFrequency;
                for (int i = 0; i < _WaveIterations; i++)
                {
                    waveNoise[i] = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseCoord).y;
                    noiseCoord *= 2.5;
                }

                float heightVariationNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (coord + float2(0.0, heightVariationScrollSpeed * t)) * heightVariationFrequency).y;

                float height = 0.0;
                float amplitudeSum = 0.0;
                float waveLength = 1.0;
                float amplitude = 1.0;
                float frequency = waveFrequnecy;

                for (int i = 0; i < _WaveIterations; i++)
                {
                    height += GerstnerWave(coord * frequency, waveDir, t, waveNoise[i] * noiseStrenth, waveLength) * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                    waveLength *= 1.5;

                    waveDir = mul(waveDir, waveRot);
                }

                height *= max(minHeight, heightVariationNoise * heightVariationScale + heightVariationOffset);

                return height * amplitudeNormalizationFactor;
            }

            float3 GetWaterNormal(float2 coord)
            {
                const float waveAngle = 30 * degree;
                float2 waveDir = float2(cos(waveAngle), sin(waveAngle));
                float2x2 waveRot = float2x2(cos(goldenAngle), sin(goldenAngle), -sin(goldenAngle), cos(goldenAngle));
                float t = 0.5 * _WaveSpeed * _Time.y;
                const float h = 0.1;

                float wave0 = GetWaterHeight(coord, waveDir, waveRot, t);
                float wave1 = GetWaterHeight(coord + float2(h, 0.0), waveDir, waveRot, t);
                float wave2 = GetWaterHeight(coord + float2(0.0, h), waveDir, waveRot, t);

                return normalize(float3(wave1 - wave0, wave2 - wave0, h));
            }


            /// SSR
            float2 ViewPosToCS(float3 vpos)
            {
                float4 clipPos = mul(unity_CameraProjection, float4(vpos, 1));
                float3 screenPos = clipPos.xyz / clipPos.w;
                return float2(screenPos.x, screenPos.y) * 0.5 + 0.5;
            }
            
            float CompareWithDepth(float3 vpos)
            {
                float2 uv = ViewPosToCS(vpos);
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
                depth = LinearEyeDepth(depth, _ZBufferParams);
                int isInside = uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1;
                return lerp(0, vpos.z + depth, isInside);
            }
            
            bool RayMarching(float3 o, float3 r, out float2 hitUV)
            {
                float3 end = o;
                float stepSize = 0.15;
                float thickness = 0.1;
                float travelled = 0;
                int maxMarching = 256;
                float maxDistance = 500;

                UNITY_LOOP
                for (int i = 0; i < maxMarching; i++)
                {
                    end += r * stepSize;
                    travelled += stepSize;

                    if (travelled > maxDistance)
                    {
                        return false;
                    }

                    float collide = CompareWithDepth(end);
                    if (collide < 0)
                    {
                        if (abs(collide) < thickness)
                        {
                            hitUV = ViewPosToCS(end);
                            return true;
                        }

                        end -= r * stepSize;
                        travelled -= stepSize;
                        stepSize *= 0.5;
                    }
                }

                return false;
            }
            
            Varyings vert(Attributes v)
            {
                Varyings o;
                
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                // o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                // o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                // o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;

                o.positionOS = v.positionOS;

                float4 screenPos = o.positionHCS;
                screenPos.xyz /= screenPos.w;
                screenPos.xy = screenPos.xy * 0.5 + 0.5;
                o.positionCS = screenPos;
#if UNITY_UV_STARTS_AT_TOP
                o.positionCS.y = 1 - o.positionCS.y;
#endif

                float zFar = _ProjectionParams.z;
                float4 vsRay = float4(float3(o.positionCS.xy * 2.0 - 1.0, 1) * zFar, zFar);
                vsRay = mul(unity_CameraInvProjection, vsRay);
                o.vsRay = vsRay;
                
                return o;
            }

            

            half4 frag(Varyings i) : SV_Target
            {
                // float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                // float3 normalWS = normalize(mul(normalTS, TBN));
                float3 normalTS = GetWaterNormal(-i.positionWS.xz);

                // 始终朝上
                const float3x3 tbn = float3x3
                (
		            -1.0, 0.0, 0.0,
		            0.0, 0.0, -1.0,
		            0.0, 1.0, 0.0
	            );

                float3 waveNormal = normalize(mul(normalTS, tbn));

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);

                float3 lightDir = normalize(_MainLightPosition.xyz);
                float3 diffuse = max(0.2, dot(waveNormal, lightDir));

                float viewDistance = length(_WorldSpaceCameraPos.xyz - i.positionWS);

                float fogFactor = 1 - exp(-_FogDensity * viewDistance);

                float3 h = normalize(viewDir + lightDir);
                float specular = pow(max(0, dot(waveNormal, h)), _Shininess);
                half3 specularColor = half3(1, 1, 1) * specular;
                
                half3 color = lerp(_BaseColor.rgb, _FogColor.rgb, fogFactor) * diffuse + specularColor;
                half alpha = lerp(_BaseColor.a, _FogColor.a, fogFactor);

                // ssr
                float4 screenPos = i.positionCS;
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, screenPos.xy);
                depth = Linear01Depth(depth, _ZBufferParams);
                
                float3 wsNormal = normalize(float3(0, 1, 0));;
                float3 vsNormal = TransformWorldToViewDir(wsNormal);
                
                float3 vsRayOrigin = i.vsRay * depth;
                float3 reflectionDir = normalize(reflect(vsRayOrigin, vsNormal));
                
                float2 hitUV = 0;
                float3 hitCol = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenPos.xy).xyz;
                
                if (RayMarching(vsRayOrigin, reflectionDir, hitUV))
                {
                    hitCol += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, hitUV);
                }
                else
                {
                    float3 viewPosToWorld = normalize(i.positionWS.xyz - _WorldSpaceCameraPos.xyz);
                    float3 reflectDir = reflect(viewPosToWorld, wsNormal);
                    hitCol = SAMPLE_TEXTURECUBE(_SkyBoxCubeMap, sampler_SkyBoxCubeMap, reflectDir);
                }

                return half4(hitCol, 1.0);

                // half3 finalColor = lerp(color, hitCol.rgb, _ReflectionStrength);
                // return half4(finalColor, alpha);

            }
            ENDHLSL
        }
    }
    FallBack Off
}