HEADER
{
    Description = "Template Shader for S&box";
    DevShader = true;
}

FEATURES
{
    #include "common/features.hlsl"
}

COMMON
{
	
    #define CUSTOM_MATERIAL_INPUTS
    #include "common/shared.hlsl"
}

struct VertexInput
{
    #include "common/vertexinput.hlsl"
};

struct PixelInput
{
    #include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
    {
        PixelInput o = ProcessVertex( i );
        return FinalizeVertex( o );
    }

}

PS
{
    #include "common/pixel.hlsl"
	#define CUSTOM_MATERIAL_INPUTS

	CreateTexture2D( g_ColorTexture ) < Attribute( "BaseTexture" ); Filter( BILINEAR ); AddressU( WRAP ); AddressV( WRAP ); AddressW( CLAMP ); SrgbRead( true ); >;


	float2 g_vTexCoordScale < UiType( Slider ); Range2(0,0,1,1); Default2( 1.0, 1.0 ); UiGroup( "Texture Coordinates,10/21" ); >;
	float g_vImpact < UiType( Slider ); Range(0,1); Default( .2); UiGroup( "Texture Coordinates,10/21" ); >;

	
	CreateInputTexture2D( Color, Srgb, 8, "", "_color", "Material,10/10", Default3( 1.0, 1.0, 1.0 ) );
	Texture2D g_tColor < Channel( RGB, Box( Color ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >; 
	
	
	CreateInputTexture2D( Snow, Srgb, 8, "", "_color", "Material,10/10", Default3( 1.0, 1.0, 1.0 ) );
	Texture2D g_tSnow < Channel( RGB, Box( Snow ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >; 
	
	
    float4 MainPs( PixelInput i ) : SV_Target0
    {
        Material m = Material::Init();


		m.Albedo = float3( 1, 1, 1 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		

		float3 color =  g_tColor.Sample( g_sAniso, i.vTextureCoords.xy );
		float3 snow_color =  g_tSnow.Sample( g_sAniso, i.vTextureCoords.xy );
		m.Albedo =  lerp(color,snow_color, g_vImpact);
		m.Opacity = 1;
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );

		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// for some toolvis shit
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
        m.TextureCoords = i.vTextureCoords.xy;
		
		return ShadingModelStandard::Shade( i, m );
	}
}
