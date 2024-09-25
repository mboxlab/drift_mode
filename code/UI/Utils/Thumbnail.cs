public static class Thumbnail
{
	public static Dictionary<Model, Texture> Cache = new();

	private static Texture GenerateTexture( Model model )
	{
		if ( model is null || model.IsError ) return Texture.Transparent;

		SceneWorld sceneWorld = new();

		SceneCamera sceneCamera = new();
		sceneCamera.World = sceneWorld;
		sceneCamera.Rotation = Rotation.From( 0f, 45f, 0f );

		SceneModel sceneModel = new( sceneWorld, model, new() );
		sceneModel.Rotation = Rotation.From( 0f, 0f, 0f );

		BBox bounds = sceneModel.Bounds;
		Vector3 center = bounds.Center;
		float distance = bounds.Size.Length;

		sceneCamera.Position = center + sceneCamera.Rotation.Backward * distance * 1f;

		//SceneLight sceneLight = new SceneDirectionalLight( sceneWorld, Rotation.From( 45f, 45f, 0f ), Color.White );

		SceneLight sceneLight = new SceneLight( sceneWorld, sceneCamera.Position, distance * 2f, Color.White );

		Texture texture = Texture.CreateRenderTarget().WithSize( 128, 128 ).Create();
		Graphics.RenderToTexture( sceneCamera, texture );

		sceneLight.Delete();
		sceneCamera.Dispose();
		sceneModel.Delete();
		sceneWorld.Delete();

		return texture;
	}

	public static Texture Get( Model model )
	{
		if ( Cache.TryGetValue( model, out var texture ) )
			return texture;

		texture = GenerateTexture( model );
		Cache[ model ] = texture;

		return texture;
	}
}
