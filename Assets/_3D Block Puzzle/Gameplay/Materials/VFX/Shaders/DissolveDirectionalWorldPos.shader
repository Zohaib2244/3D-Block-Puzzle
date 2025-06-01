Shader "Saf/DissolveDirectionalWorldPlastic"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _BrightnessMultiplier("Brightness Multiplier", Range(0.1, 5.0)) = 1.2
        [Toggle] _ReceiveShadows("Receive Shadows", Float) = 1
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0.6
        _SpecColor("Specular Color", Color) = (1,1,1,1)
        [Toggle(_USE_SPECULAR)] _UseSpecular("Use Specular Highlights", Float) = 1

        // Ambient control properties
        [Toggle] _UseAmbient("Use Ambient Lighting", Float) = 1
        _AmbientIntensity("Ambient Intensity", Range(0, 2)) = 1.0

        _GatePosition("Gate Position", Vector) = (0,0,0,0)
        _GateDirection("Gate Direction", Vector) = (0,0,1,0)
        _DissolveStart("Dissolve Start Point", Range(0,1)) = 0.2
        _DissolveLength("Dissolve Length", Range(0.01,1)) = 0.3

        _NoiseScale("Noise Scale", Float) = 5
        _EdgeThickness("Edge Thickness", Range(0,0.5)) = 0.1
        _EdgeIntensity("Edge Intensity", Range(0, 2.0)) = 1.0
        [HDR]_EdgeColor("Edge Color", Color) = (1,1,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Lighting and shadow features
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _USE_SPECULAR
            #pragma multi_compile_fog
            
            // Required includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float objectProgress : TEXCOORD4;
                float fogCoord : TEXCOORD5;
            };

            sampler2D _BaseMap;
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _BrightnessMultiplier;
                half _Smoothness;
                half _ReceiveShadows;
                half4 _SpecColor;
                float _UseSpecular;

                // Ambient control variables
                float _UseAmbient;
                float _AmbientIntensity;

                float3 _GatePosition;
                float3 _GateDirection;
                float _DissolveStart;
                float _DissolveLength;

                float _NoiseScale;
                float _EdgeThickness;
                float _EdgeIntensity;
                half4 _EdgeColor;
            CBUFFER_END

            float noise3D(float3 p)
            {
                p *= _NoiseScale;
                return sin(dot(p, float3(12.9898, 78.233, 45.543))) * 0.5 + 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                float3 gateDir = normalize(_GateDirection);
                float3 toGate = output.positionWS - _GatePosition;
                output.objectProgress = dot(toGate, gateDir);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                #if defined(_MAIN_LIGHT_SHADOWS)
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #else
                    output.shadowCoord = float4(0, 0, 0, 0);
                #endif
                
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture and apply color
                half4 albedo = tex2D(_BaseMap, input.uv) * _BaseColor;
                albedo.rgb *= _BrightnessMultiplier;

                // Calculate dissolve effect
                float dissolveProgress = saturate((input.objectProgress - _DissolveStart) / _DissolveLength);
                float noise = noise3D(input.positionWS);
                float threshold = dissolveProgress + noise * 0.2;
                float edge = smoothstep(0, _EdgeThickness, threshold);
                clip(threshold - 0.5);

                // Initialize lighting data
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 reflectVector = reflect(-viewDirWS, normalWS);
                
                // Get main light with shadows
                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) && defined(_RECEIVESHADOWS_ON)
                    mainLight = GetMainLight(input.shadowCoord);
                #else
                    mainLight = GetMainLight();
                #endif

                // Calculate shadow attenuation
                float shadowAtten = _ReceiveShadows > 0.5 ? mainLight.shadowAttenuation : 1.0;

                // Plastic-like diffuse lighting
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = albedo.rgb * mainLight.color * NdotL * shadowAtten;
                
                // Ambient lighting (with toggle control)
                half3 ambient = 0;
                if (_UseAmbient > 0.5) {
                    ambient = SampleSH(normalWS) * albedo.rgb * _AmbientIntensity;
                }
                
                // Specular highlights (plastic reflection)
                half3 specular = 0;
                #if defined(_USE_SPECULAR)
                    float3 halfVec = normalize(mainLight.direction + viewDirWS);
                    float NdotH = saturate(dot(normalWS, halfVec));
                    float specularPower = exp2(10 * _Smoothness + 1);
                    specular = _SpecColor.rgb * pow(NdotH, specularPower) * mainLight.color * shadowAtten;
                #endif
                
                // Environment reflections (plastic shine)
                half3 reflection = 0;
                float perceptualRoughness = 1.0 - _Smoothness;
                float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
                
                // Sample reflection probe
                half3 reflectColor = GlossyEnvironmentReflection(reflectVector, 
                    input.positionWS, 
                    roughness, 
                    1.0);
                
                // Fresnel effect for plastic
                float fresnel = pow(saturate(1.0 - dot(normalWS, viewDirWS)), 5.0);
                reflection = reflectColor * fresnel * _Smoothness;
                
                // Combine all lighting components
                half3 litColor = ambient + diffuse + specular + reflection;
                
                // Add edge glow effect
                half3 edgeGlow = (1 - edge) * _EdgeColor.rgb * _EdgeIntensity;
                litColor += edgeGlow * _EdgeColor.a;
                
                // Apply fog
                litColor = MixFog(litColor, input.fogCoord);
                
                return half4(litColor, albedo.a);
            }
            ENDHLSL
        }

        // Shadow caster pass remains unchanged
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float objectProgress : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float3 _GatePosition;
                float3 _GateDirection;
                float _DissolveStart;
                float _DissolveLength;
                float _NoiseScale;
            CBUFFER_END

            float noise3D(float3 p)
            {
                p *= _NoiseScale;
                return sin(dot(p, float3(12.9898, 78.233, 45.543))) * 0.5 + 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);

                float3 gateDir = normalize(_GateDirection);
                float3 toGate = output.positionWS - _GatePosition;
                output.objectProgress = dot(toGate, gateDir);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float dissolveProgress = saturate((input.objectProgress - _DissolveStart) / _DissolveLength);
                float noise = noise3D(input.positionWS);
                float threshold = dissolveProgress + noise * 0.2;
                clip(threshold - 0.5);
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "SafShaderGUI"
}