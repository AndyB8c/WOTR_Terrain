
#if !defined(SHADERGRAPH_PREVIEW) || ( defined(LIGHTWEIGHT_LIGHTING_INCLUDED) || defined(UNIVERSAL_LIGHTING_INCLUDED) )

//  As we do not have access to the vertex lights we will make the shader always sample add lights per pixel
    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
        #undef _ADDITIONAL_LIGHTS_VERTEX
        #define _ADDITIONAL_LIGHTS
    #endif

    #if(RECEIVE_SHADOWS_OFF)
    	#define _RECEIVE_SHADOWS_OFF
    #endif

    #if defined(LIGHTWEIGHT_LIGHTING_INCLUDED) || defined(UNIVERSAL_LIGHTING_INCLUDED)

        half3 LightingSpecular_Toon (Light light, half lightingRemap, half3 normalWS, half3 viewDirectionWS, half3 specular, half specularSmoothness, half smoothness, half specularStep, half specularUpper, bool energyConservation){
            half3 halfVec = SafeNormalize(light.direction + viewDirectionWS);
            half NdotH = saturate(dot(normalWS, halfVec));
            half modifier = pow(NdotH /* lightingRemap*/, specularSmoothness);
        //  Normalization? Na, we just multiply by smoothness in the return statement.
            // #define ONEOVERTWOPI 0.159155h
            // half normalization = (specularSmoothness + 1) * ONEOVERTWOPI;
        //  Sharpen
            half modifierSharpened = smoothstep(specularStep, specularUpper, modifier);
            half toonNormalization = (energyConservation) ? smoothness : 1;
            return light.color * specular * modifierSharpened * toonNormalization; // * smoothness;
        }

        half3 LightingSpecularAniso_Toon (Light light, half NdotL, half3 normalWS, half3 viewDirectionWS, half3 tangentWS, half3 bitangentWS, half anisotropy, half3 specular, half specularSmoothness, half smoothness, half specularStep, half specularUpper, bool energyConservation){

        //  This does not let us fade from isotropic to anisotropic...            
        //     half3 H = SafeNormalize(light.direction + viewDirectionWS);
        //     half3 T = cross(normalWS, tangent);
        //     T = lerp(tangent, bitangent, (anisotropy + 1) * 0.5);
        //     float TdotH = dot(T, H);
        //     float sinTHSq = saturate(1.0 - TdotH * TdotH);
        //     float exponent = RoughnessToBlinnPhongSpecularExponent_Lux(1 - smoothness);
        //     float modifier = dirAttn * pow(sinTHSq, 0.5 * exponent);
        //     float norm = smoothness; //(exponent + 2) * rcp(2 * PI);
        // //  Sharpen
        //     half modifierSharpened = smoothstep(specularStep, specularUpper, modifier);
        //     half toonNormalization = (energyConservation == 1.0h) ? norm : 1;
        //     return light.color * specular * modifierSharpened * toonNormalization;

        //  ///////////////////////////////
        //
        //  GGX "like" distribution in order to be able to fade from isotropic to anisotropic
        //  We skip visbility here as it is toon lighting.

        //  NOTE: Further normalization does not help here to fixe the final shape...
            half3 H = SafeNormalize(light.direction + viewDirectionWS);

        //  TdotH and BdotH should be unclamped here
            half TdotH = dot(tangentWS, H);
            half BdotH = dot(bitangentWS, H);
            half NdotH = dot(normalWS, H);
            half roughness = 1.0h - smoothness;
            
        //  roughness^2 would be correct here - but in order to get it a bit closer to our blinn phong isotropic specular we go with ^4 instead
            roughness *= roughness * roughness * roughness;

            half at = roughness * (1.0h + anisotropy);
            half ab = roughness * (1.0h - anisotropy);
            
            half a2 = at * ab;
            half3 v = half3(ab * TdotH, at * BdotH, a2 * NdotH);
            
            half v2 = dot(v, v);
            half w2 = a2 / v2;
            half res = a2 * w2 * w2 * (1.0h / PI); 

        //  Sharpen
            half modifierSharpened = smoothstep(specularStep, specularUpper, res);
            half toonNormalization = (energyConservation == 1.0h) ? smoothness : 1.0h;
            return light.color * specular * modifierSharpened * toonNormalization; 
        }

    #endif

    half aaStep(half compValue, half gradient, half softness){
	    half change = fwidth(gradient) * softness;
	//  Base the range of the inverse lerp on the change over two pixels
	    half lowerEdge = compValue - change;
	    half upperEdge = compValue + change;
	//  Do the inverse interpolation
	    half stepped = (gradient - lowerEdge) / (upperEdge - lowerEdge);
	    stepped = saturate(stepped);
	    return stepped;
	}

#endif

void Lighting_half(

//  Base inputs
    float3 positionWS,
    half3 viewDirectionWS,

//  Normal inputs    
    half3 normalWS,
    half3 tangentWS,
    half3 bitangentWS,
    half3 normalTS,

//  Surface description
    half3 albedo,
    half3 shadedAlbedo,
    half anisotropy,
    bool energyConservation,
    half3 specular,
    half smoothness,
    half occlusion,

//  Smoothsteps
    half steps,
    half diffuseStep,
    half diffuseFalloff,
    half specularStep,
    half specularFalloff,
    half shadowFalloff,
    half shadowBiasDirectional,
    half shadowBiasAdditional,

//  Colorize shaded parts
    half colorizeMainLight,
    half colorizeAddLights,

    half lightColorContribution,
    half addLightFalloff,

//  Rim Lighting
    half rimPower,
    half rimFalloff,
    half4 rimColor,
    half rimAttenuation,

//  Lightmapping
    float2 lightMapUV,

//	Ramp 
	Texture2D GradientMap,
    float GradientWidth,
	SamplerState sampler_Linear,
	SamplerState sampler_Point,

    bool receiveSSAO,

//  Final lit color
    out half3 Lighting,
    out half3 MetaAlbedo,
    out half3 MetaSpecular
)
{

#if defined(SHADERGRAPH_PREVIEW) || ( !defined(LIGHTWEIGHT_LIGHTING_INCLUDED) && !defined(UNIVERSAL_LIGHTING_INCLUDED) )
    Lighting = albedo;
    MetaAlbedo = half3(0,0,0);
    MetaSpecular = half3(0,0,0);
#else

//  Real Lighting ----------
    half3 tnormal = normalWS;

//  Normal mapping
    #if defined(NORMAL_ON) //if (enableNormalMapping) {
        tnormal = TransformTangentToWorld(normalTS, half3x3(tangentWS.xyz, bitangentWS.xyz, normalWS.xyz));
    #endif //}
    normalWS = NormalizeNormalPerPixel(tnormal);
    viewDirectionWS = SafeNormalize(viewDirectionWS);

//  Remap values - old version
    //half diffuseUpper = saturate(diffuseStep + diffuseFalloff);

    float4 clipPos = TransformWorldToHClip(positionWS);
//  Get Shadow Sampling Coords / Unfortunately per pixel...
    #if SHADOWS_SCREEN
        float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    #endif

//  Shadow mask 
    #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
        half4 shadowMask = SAMPLE_SHADOWMASK(lightMapUV);
    #elif !defined (LIGHTMAP_ON)
        half4 shadowMask = unity_ProbesOcclusion;
    #else
        half4 shadowMask = half4(1, 1, 1, 1);
    #endif

    //Light mainLight = GetMainLight(shadowCoord);
    Light mainLight = GetMainLight(shadowCoord, positionWS, shadowMask);

//  SSAO
    #if defined(_SCREEN_SPACE_OCCLUSION)
        AmbientOcclusionFactor aoFactor;
        aoFactor.indirectAmbientOcclusion = 1;
        aoFactor.directAmbientOcclusion = 1;
        if(receiveSSAO) {
            float4 ndc = clipPos * 0.5f;
            float2 normalized = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
            normalized /= clipPos.w;
            normalized *= _ScreenParams.xy;
        //  We could also use IN.Screenpos(default) --> ( IN.Screenpos.xy * _ScreenParams.xy)
        //  HDRP 10.1
            normalized = GetNormalizedScreenSpaceUV(normalized);
            aoFactor = GetScreenSpaceAmbientOcclusion(normalized);
            mainLight.color *= aoFactor.directAmbientOcclusion;
            occlusion = min(occlusion, aoFactor.indirectAmbientOcclusion);
            //occlusion = smoothstep(diffuseStep, diffuseUpper, occlusion);
        }
    #endif

//  GI Lighting
    half3 bakedGI = 0;
    #ifdef LIGHTMAP_ON
        lightMapUV = lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
        bakedGI = SAMPLE_GI(lightMapUV, half3(0,0,0), normalWS);
    #else
        UNITY_BRANCH if(occlusion > 0) {
            bakedGI = SampleSH(normalWS) * occlusion;
        }
    #endif

    //mainLight.shadowAttenuation = smoothstep(0.0h, shadowFalloff, mainLight.shadowAttenuation);
    mainLight.shadowAttenuation = smoothstep( (1 - shadowFalloff) * shadowFalloff, shadowFalloff, mainLight.shadowAttenuation);
    MixRealtimeAndBakedGI(mainLight, normalWS, bakedGI, half4(0, 0, 0, 0));

//  Set up Lighting
    half lightIntensity = 0;
    half3 specularLighting = 0;
    half3 rimLighting = 0;
    half3 lightColor = 0;
    half luminance;

//  Adjust tangent and reconstruct bitangent in case anisotropic specular is active as otherwise normal mapping has no effect
    #if defined(ANISO_ON) && defined(SPECULAR_ON)
        #if defined(NORMAL_ON)   
            tangentWS = Orthonormalize(tangentWS, normalWS);
        #endif
        bitangentWS = cross(normalWS, tangentWS);
    #endif
    
//  Main Light

//	Old version
    //half NdotL = saturate(dot(normalWS, mainLight.direction)); 
    //NdotL = smoothstep(diffuseStep, diffuseUpper, NdotL);

//	New version which lets you use wrapped around diffuse lighting and shift away shadowed areas if gradient are disabled
//
//	Remap old diffuseStep and diffuseFalloff in order to match new function
	diffuseStep = diffuseStep + 1.0h;
    diffuseFalloff = diffuseFalloff * 4.0h + 1.0h;

	half NdotL = dot(normalWS, mainLight.direction);
    NdotL = saturate((NdotL + 1.0h) - diffuseStep);
	
    #if !defined(GRADIENT_ON)
    //  We have to use steps - 1 here!
        half oneOverSteps = 1.0h / steps;
        half quantizedNdotL = floor(NdotL * steps);
    //  IMPORTANT: no saturate on the 2nd param: NdotL - 0.01. 0.01 is eyballed.
        NdotL = (quantizedNdotL + aaStep(saturate(quantizedNdotL * oneOverSteps), NdotL - 0.01h, diffuseFalloff )) * oneOverSteps;
	#else
        #if defined(SMOOTHGRADIENT_ON)
		    NdotL = SAMPLE_TEXTURE2D(GradientMap, sampler_Linear, float2 (NdotL, 0.5f)).r;
        #else
            float oneOverTexelWidth = rcp(GradientWidth);
            half NdotL0 = SAMPLE_TEXTURE2D(GradientMap, sampler_Point, float2 (NdotL, 0.5f)).r ;
            half NdotL1 = SAMPLE_TEXTURE2D(GradientMap, sampler_Point, float2 (NdotL + fwidth(NdotL) * oneOverTexelWidth, 0.5f)).r;
            NdotL = (NdotL0 + NdotL1) * 0.5h;
        #endif
	#endif

    half atten = NdotL * mainLight.distanceAttenuation * saturate(shadowBiasDirectional + mainLight.shadowAttenuation);
    mainLight.color = lerp(Luminance(mainLight.color).xxx, mainLight.color, lightColorContribution.xxx);
    // if (colorizeMainLight) {
    //     lightColor = mainLight.color * mainLight.distanceAttenuation;  
    // }
    // else {
    //     lightColor = mainLight.color * atten;
    // }
    lightColor = mainLight.color * lerp(atten, mainLight.distanceAttenuation, colorizeMainLight);
    luminance = Luminance(mainLight.color); 
    lightIntensity += luminance * atten;

//  Specular
    half specularSmoothness;
    half3 spec;
    half specularUpper;
    
    #if defined(SPECULAR_ON)
        specularSmoothness = exp2(10 * smoothness + 1);
        specularUpper = saturate(specularStep + specularFalloff * (1.0h + smoothness));
        #if defined(ANISO_ON)
            spec = LightingSpecularAniso_Toon (mainLight, NdotL, normalWS, viewDirectionWS, tangentWS, bitangentWS, anisotropy, specular, specularSmoothness, smoothness, specularStep, specularUpper, energyConservation);
        #else
            spec = LightingSpecular_Toon(mainLight, NdotL, normalWS, viewDirectionWS, specular, specularSmoothness, smoothness, specularStep, specularUpper, energyConservation);
        #endif
        specularLighting = spec * atten;
    #endif
    
//  Rim Lighting
    #if defined(RIM_ON)
        half rim = saturate(1.0h - saturate( dot(normalWS, viewDirectionWS)) );
        //rimLighting = smoothstep(rimPower, rimPower + rimFalloff, rim) * rimColor.rgb;
    //  Stabilize rim
        float delta = fwidth(rim);
        rimLighting = smoothstep(rimPower - delta, rimPower + rimFalloff  + delta, rim) * rimColor.rgb;
    #endif
    
//  Handle additional lights
    #ifdef _ADDITIONAL_LIGHTS
        uint pixelLightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < pixelLightCount; ++i) {
            Light light = GetAdditionalLight(i, positionWS);
            #if defined(_SCREEN_SPACE_OCCLUSION)
                if(receiveSSAO) {
                    light.color *= aoFactor.directAmbientOcclusion;
                }
            #endif
            light.shadowAttenuation = smoothstep(0.0h, shadowFalloff, light.shadowAttenuation);

            NdotL = dot(normalWS, light.direction);
            NdotL = saturate((NdotL + 1.0h) - diffuseStep);
			#if !defined(GRADIENT_ON)
			    half quantizedNdotL = floor(NdotL * steps);
            //  IMPORTANT: no saturate on the 2nd param: NdotL - 0.01. 0.01 is eyballed.
                NdotL = (quantizedNdotL + aaStep(saturate(quantizedNdotL * oneOverSteps), NdotL - 0.01h, diffuseFalloff )) * oneOverSteps;
            #else
				#if defined(SMOOTHGRADIENT_ON)
                    NdotL = SAMPLE_TEXTURE2D(GradientMap, sampler_Linear, float2 (NdotL, 0.5f)).r;
                #else
                    NdotL0 = SAMPLE_TEXTURE2D(GradientMap, sampler_Point, float2 (NdotL, 0.5f)).r;
                    NdotL1 = SAMPLE_TEXTURE2D(GradientMap, sampler_Point, float2 (NdotL + fwidth(NdotL) * oneOverTexelWidth, 0.5f)).r;
                    NdotL = (NdotL0 + NdotL1) * 0.5h;
                #endif
			#endif

            half distanceAttenuation = (addLightFalloff < 1.0h) ? saturate(light.distanceAttenuation / addLightFalloff) : light.distanceAttenuation;
            atten = NdotL * distanceAttenuation * saturate(shadowBiasAdditional + light.shadowAttenuation);
            light.color = lerp(Luminance(light.color).xxx, light.color, lightColorContribution.xxx);
            // if(colorizeAddLights) { 
            //     lightColor += light.color * light.distanceAttenuation;
            // }
            // else {
            //     lightColor += light.color * atten;
            // }
            lightColor += light.color * lerp(atten, distanceAttenuation, colorizeAddLights);
            luminance = Luminance(light.color);
            lightIntensity += luminance * atten;
            //if (enableSpecular) {
            #if defined(SPECULAR_ON)
                #if defined(ANISO_ON)
                    spec = LightingSpecularAniso_Toon (light, NdotL, normalWS, viewDirectionWS, tangentWS, bitangentWS, anisotropy, specular, specularSmoothness, smoothness, specularStep, specularUpper, energyConservation);
                #else
                    spec = LightingSpecular_Toon(light, NdotL, normalWS, viewDirectionWS, specular, specularSmoothness, smoothness, specularStep, specularUpper, energyConservation);
                #endif
                specularLighting += spec * atten;
            #endif
            //}
        }
    #endif

//  Combine Lighting
    half3 litAlbedo = lerp(shadedAlbedo, albedo, saturate(lightIntensity.xxx));
    Lighting =
    //  ambient diffuse lighting
        bakedGI * albedo
    //  direct diffuse lighting
        + ( litAlbedo
    //  spec and rim lighting    
        #if defined(SPECULAR_ON)
        	+ specularLighting * lightIntensity
        #endif
        #if defined(RIM_ON)
        	+ rimLighting * lerp(1.0h, lightIntensity, rimAttenuation) 
       	#endif
        ) * lightColor
    ;

//  Set Albedo for meta pass
    #if defined(LIGHTWEIGHT_META_PASS_INCLUDED) || defined(UNIVERSAL_META_PASS_INCLUDED)
        Lighting = half3(0,0,0);
        MetaAlbedo = albedo;
        MetaSpecular = half3(0.02,0.02,0.02);
    #else
        MetaAlbedo = half3(0,0,0);
        MetaSpecular = half3(0,0,0);
    #endif

//  End Real Lighting ----------

#endif
}

// Unity 2019.1. needs a float version

void Lighting_float(

//  Base inputs
    float3 positionWS,
    half3 viewDirectionWS,

//  Normal inputs    
    half3 normalWS,
    half3 tangentWS,
    half3 bitangentWS,
    half3 normalTS,

//  Surface description
    half3 albedo,
    half3 shadedAlbedo,
    half anisotropy,
    bool energyConservation,
    half3 specular,
    half smoothness,
    half occlusion,

//  Smoothsteps
    half steps,
    half diffuseStep,
    half diffuseFalloff,
    half specularStep,
    half specularFalloff,
    half shadowFalloff,
    half shadowBiasDirectional,
    half shadowBiasAdditional,

//  Colorize shaded parts
    half colorizeMainLight,
    half colorizeAddLights,

    half lightColorContribution,
    half addLightFalloff,

//  Rim Lighting
    half rimPower,
    half rimFalloff,
    half4 rimColor,
    half rimAttenuation,

//  Lightmapping
    float2 lightMapUV,

//	Ramp 
	Texture2D GradientMap,
    float GradientWidth,
	SamplerState sampler_Linear,
	SamplerState sampler_Point,

    bool receiveSSAO,

//  Final lit color
    out half3 Lighting,
    out half3 MetaAlbedo,
    out half3 MetaSpecular
)
{
    Lighting_half(
        positionWS, viewDirectionWS, normalWS, tangentWS, bitangentWS, normalTS, 
        albedo, shadedAlbedo, anisotropy, energyConservation, specular, smoothness, occlusion,
        steps, diffuseStep, diffuseFalloff, specularStep, specularFalloff, shadowFalloff, shadowBiasDirectional, shadowBiasAdditional, 
        colorizeMainLight, colorizeAddLights, lightColorContribution, addLightFalloff,
        rimPower, rimFalloff, rimColor, rimAttenuation,
        lightMapUV,
        GradientMap, GradientWidth, sampler_Linear, sampler_Point,
        receiveSSAO,
        Lighting, MetaAlbedo, MetaSpecular
    );
}