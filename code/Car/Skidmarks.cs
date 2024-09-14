
using System;
using Sandbox;

namespace DM.Car;

public sealed class Skidmarks : Component
{
	[Property] public Material SkidmarksMaterial { get; set; }

	const int MAX_MARKS = 2048; // Max number of marks total for everyone together
	const float MARK_WIDTH = 0.35f; // Width of the skidmarks. Should match the width of the wheels
	const float GROUND_OFFSET = 0.02f;  // Distance above surface in metres
	const float MIN_DISTANCE = 0.25f; // Distance between skid texture sections in metres. Bigger = better performance, less smooth
	const float MIN_SQR_DISTANCE = MIN_DISTANCE * MIN_DISTANCE;
	const float MAX_OPACITY = 1.0f; // Max skidmark opacity

	// Info for each mark created. Needed to generate the correct mesh
	class MarkSection
	{
		public Vector3 Pos = Vector3.Zero;
		public Vector3 Normal = Vector3.Zero;
		public Vector4 Tangent = Vector4.Zero;
		public Vector3 Posl = Vector3.Zero;
		public Vector3 Posr = Vector3.Zero;
		public Color32 Colour;
		public int LastIndex;
	};

	private int markIndex;
	private MarkSection[] skidmarks;
	private Mesh marksMesh;
	//MeshRenderer mr;
	//MeshFilter mf;
	private ModelBuilder ModelBuilder = new();
	[Property] private ModelRenderer ModelRenderer { get; set; }

	//private List<Vector3> vertices;
	private List<Vertex> vertices;
	private VertexAttribute[] vertexes;
	//private Vector3[] normals;
	//private Vector4[] tangents;
	//private Color32[] colors;
	//private Vector2[] uvs;
	//private int[] triangles;

	private bool meshUpdated;
	private bool haveSetBounds;
	private VertexBuffer Buffer = new();
	private Color32 black = Color.Black;

	protected override void OnStart()
	{
		base.OnStart();
		VertexBuffer vb = new();
		Vertex v = vb.Default;
		vb.Add( v );
	
		Mesh m = new();
		
		m.CreateBuffers( vb );

		Model mod = new ModelBuilder().AddMesh( m ).Create();

		ModelRenderer.Model = mod;

	}

	protected override void OnUpdate()
	{



	}
	// Function called by the wheel that's skidding. Sets the colour and intensity of the skidmark section.
	public int AddSkidMark( Vector3 pos, Vector3 normal, Color32 colour, int lastIndex )
	{

		MarkSection lastSection = null;

		Vector3 distAndDirection = Vector3.Zero;

		Vector3 newPos = pos + normal * GROUND_OFFSET;
		if ( lastIndex != -1 )
		{
			lastSection = skidmarks[lastIndex];
			distAndDirection = newPos - lastSection.Pos;
			if ( distAndDirection.LengthSquared < MIN_SQR_DISTANCE )
			{
				return lastIndex;
			}
			// Fixes an awkward bug:
			// - Car draws skidmark, e.g. index 50 with last index 40.
			// - Skidmark markIndex loops around, and other car overwrites index 50
			// - Car draws skidmark, e.g. index 45. Last index was 40, but now 40 is different, changed by someone else.
			// This makes sure we ignore the last index if the distance looks wrong
			if ( distAndDirection.LengthSquared > MIN_SQR_DISTANCE * 10 )
			{
				lastIndex = -1;
				lastSection = null;
			}
		}
		colour.a = (byte)(colour.a * MAX_OPACITY);

		MarkSection curSection = skidmarks[markIndex];

		curSection.Pos = newPos;
		curSection.Normal = normal;
		curSection.Colour = colour;
		curSection.LastIndex = lastIndex;

		if ( lastSection != null )
		{
			Vector3 xDirection = Vector3.Cross( distAndDirection, normal ).Normal;
			curSection.Posl = curSection.Pos + xDirection * MARK_WIDTH * 0.5f;
			curSection.Posr = curSection.Pos - xDirection * MARK_WIDTH * 0.5f;
			curSection.Tangent = new Vector4( xDirection.x, xDirection.y, xDirection.z, 1 );

			if ( lastSection.LastIndex == -1 )
			{
				lastSection.Tangent = curSection.Tangent;
				lastSection.Posl = curSection.Pos + xDirection * MARK_WIDTH * 0.5f;
				lastSection.Posr = curSection.Pos - xDirection * MARK_WIDTH * 0.5f;
			}
		}

		UpdateSkidmarksMesh();

		int curIndex = markIndex;
		if ( lastIndex == -1 )
		{
			markIndex = (markIndex - 1 + MAX_MARKS) % MAX_MARKS;
		}
		else
		{
			markIndex = ++markIndex % MAX_MARKS;
		}

		return curIndex;
	}

	// Update part of the mesh for the current markIndex
	void UpdateSkidmarksMesh()
	{
		MarkSection curr = skidmarks[markIndex];

		// Nothing to connect to yet
		if ( curr.LastIndex == -1 ) return;

		MarkSection last = skidmarks[curr.LastIndex];
		Vertex vertex1 = vertices[markIndex * 4 + 0];
		Vertex vertex2 = vertices[markIndex * 4 + 1];
		Vertex vertex3 = vertices[markIndex * 4 + 2];
		Vertex vertex4 = vertices[markIndex * 4 + 3];

		vertex1.Position = last.Posl;
		vertex2.Position = last.Posr;
		vertex3.Position = curr.Posl;
		vertex4.Position = curr.Posl;

		vertex1.Normal = last.Normal;
		vertex2.Normal = last.Normal;
		vertex3.Normal = curr.Normal;
		vertex4.Normal = curr.Normal;

		vertex1.Tangent = last.Tangent;
		vertex2.Tangent = last.Tangent;
		vertex3.Tangent = curr.Tangent;
		vertex4.Tangent = curr.Tangent;

		vertex1.Color = last.Colour;
		vertex2.Color = last.Colour;
		vertex2.Color = curr.Colour;
		vertex4.Color = curr.Colour;


		vertex1.TexCoord0 = new Vector2( 0, 0 );
		vertex2.TexCoord0 = new Vector2( 1, 0 );
		vertex3.TexCoord0 = new Vector2( 0, 1 );
		vertex4.TexCoord0 = new Vector2( 1, 1 );

		//triangles[markIndex * 6 + 0] = markIndex * 4 + 0;
		//triangles[markIndex * 6 + 2] = markIndex * 4 + 1;
		//triangles[markIndex * 6 + 1] = markIndex * 4 + 2;

		//triangles[markIndex * 6 + 3] = markIndex * 4 + 2;
		//triangles[markIndex * 6 + 5] = markIndex * 4 + 1;
		//triangles[markIndex * 6 + 4] = markIndex * 4 + 3;

		meshUpdated = true;
	}
}
