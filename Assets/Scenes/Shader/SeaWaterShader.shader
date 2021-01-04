Shader "Custom/Sea Water Shader"
{
Properties {
    _MainColor ("Main Colour (RGB)", Color) = (1,1,1,1)
    _EmissionColor ("Emission Colour (RGB)", Color) = (1,1,1,1)
    [PowerSlider(5.0)] _Emission ("Emission (Lightmapper)", Range(-1, 10)) = 0        
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Wobbliness ("Water Wobbliness", Range(0,1)) = 0.35
    [PowerSlider(5.0)] _Speed ("Water Speed", Range (0, 1)) = 0.02
    _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
    _Illum ("Illumination Mask", 2D) = "white" {}
    [NoScaleOffset] _BumpMap ("Normalmap", 2D) = "bump" {}
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 250

CGPROGRAM
#pragma surface surf MobileBlinnPhong exclude_path:prepass nolightmap noforwardadd halfasview interpolateview
#pragma target 3.0

inline fixed4 LightingMobileBlinnPhong (SurfaceOutput s, fixed3 lightDir, fixed3 halfDir, fixed atten)
{
    fixed diff = max (0, dot (s.Normal, lightDir));
    fixed nh = max (0, dot (s.Normal, halfDir));
    fixed spec = pow (nh, s.Specular*128) * s.Gloss;

    fixed4 c;
    c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
    UNITY_OPAQUE_ALPHA(c.a);
    return c;
}

sampler2D _MainTex;
sampler2D _Illum;
sampler2D _BumpMap;
half _Shininess;
half _Speed;
float4 _MainColor;
fixed _Emission;
float4 _EmissionColor;
half _Glossiness;
half _Wobbliness;

struct Input {
    float2 uv_MainTex;
    float2 uv_Illum;
};

void surf (Input IN, inout SurfaceOutput o) {

    float X = IN.uv_MainTex.x * 6 + _Time.y;
    float Y = IN.uv_MainTex.y * 6 + _Time.y;
    IN.uv_MainTex.x += cos(X + Y) * _Wobbliness * .05 * cos(Y);
    IN.uv_MainTex.y += sin(X + Y) * _Wobbliness * .05 * sin(Y);

    IN.uv_MainTex.x += _Time.y * _Speed;
    IN.uv_MainTex.y += _Time.y * _Speed;

    fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * _MainColor;
    o.Albedo = tex.rgb;

    o.Emission = lerp(tex.rgb * tex2D(_Illum, IN.uv_Illum).a, tex.rgb * _EmissionColor * tex2D(_Illum, IN.uv_Illum).a, _Emission) * _Emission;

    o.Gloss = tex.a * _Glossiness;
    o.Alpha = tex.a;
    o.Specular = _Shininess;
    o.Normal = UnpackNormal (tex2D(_BumpMap, IN.uv_MainTex));
}

ENDCG
}

FallBack "Mobile/VertexLit"
}