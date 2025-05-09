using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Parallas;

namespace Hiker.Terrain;

[Tool]
public partial class Terrain : Node3D
{
    [Export]
    public MeshInstance3D MeshInstance3D
    {
        get => _meshInstance3D;
        set { _meshInstance3D = value; GenerateMesh(); }
    }

    [Export]
    public Vector2I Size
    {
        get => _size;
        set { _size = value; GenerateMesh(); }
    }

    [Export]
    public float MaxHeight
    {
        get => _maxHeight;
        set { _maxHeight = value; GenerateMesh(); }
    }

    [Export]
    public Texture2D Heightmap
    {
        get => _heightmap;
        set { _heightmap = value; GenerateMesh(); }
    }

    private MeshInstance3D _meshInstance3D;
    private Vector2I _size = new Vector2I(100, 100);
    private float _maxHeight = 50f;
    private Texture2D _heightmap;

    private CollisionShape3D _collisionShape;

    public override void _Ready()
    {
        base._Ready();

        if (_collisionShape is null)
        {
            _collisionShape = new CollisionShape3D();
            AddChild(_collisionShape);
        }

        GenerateMesh();
    }

    private void GenerateMesh()
    {
        if (MeshInstance3D is null) return;
        var mesh = new ArrayMesh();

        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        List<Vector3> verts = [];
        List<Vector2> uvs = [];
        List<Vector3> normals = [];
        List<int> indices = [];

        var heightmapImage = Heightmap?.GetImage();

        for (int x = 0; x < Size.X; x++)
        for (int y = 0; y < Size.Y; y++)
        {
            float u = (float)x / (Size.X - 1);
            float v = (float)y / (Size.Y - 1);
            float height = heightmapImage?.GetPixel(
                MathUtil.FloorToInt(heightmapImage.GetWidth() * ((float)x / Size.X)),
                MathUtil.FloorToInt(heightmapImage.GetHeight() * ((float)y / Size.Y))
            ).Luminance ?? 0;
            var vert = new Vector3(
                x,
                height * MaxHeight,
                y
            );
            verts.Add(vert);
            normals.Add(Vector3.Up);
            uvs.Add(new Vector2(u, v));
        }

        for (int y = 0; y < (Size.X - 1); y++)
        for (int x = 0; x < (Size.Y - 1); x++)
        {
            int quad = y*Size.X + x;

            indices.Add(quad);
            indices.Add(quad+Size.X);
            indices.Add(quad+Size.X+1);

            indices.Add(quad);
            indices.Add(quad+Size.X+1);
            indices.Add(quad+1);
        }

        // Generate mesh data here

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        // No blendshapes, lods, or compression used.
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        var surfaceTool = new SurfaceTool();
        surfaceTool.CreateFrom(mesh, 0);
        surfaceTool.SetSmoothGroup(1);
        surfaceTool.GenerateNormals();
        mesh = surfaceTool.Commit();
        MeshInstance3D.Mesh = mesh;

        _collisionShape.Shape = MeshInstance3D.Mesh.CreateTrimeshShape();
    }
}
