using System;
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

    [Export] public CollisionShape3D CollisionShape3D { get; set; }

    [Export]
    public int HeightmapResolution
    {
        get => _heightmapResolution;
        set { _heightmapResolution = value; GenerateMesh(); }
    }

    [Export]
    public int Size
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
    private int _size = 100;
    private int _heightmapResolution = 100;
    private float _maxHeight = 50f;
    private Texture2D _heightmap;

    public override void _Ready()
    {
        base._Ready();

        GenerateMesh();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        switch ((long)what)
        {
            case NotificationEditorPreSave:
                MeshInstance3D?.SetMesh(null);
                CollisionShape3D?.SetShape(null);
                break;
            case NotificationEditorPostSave:
                GenerateMesh();
                break;
        }
    }

    private void GenerateMesh()
    {
        if (MeshInstance3D is null) return;
        if (CollisionShape3D is null) return;
        var mesh = new ArrayMesh();

        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        List<Vector3> verts = [];
        List<Vector2> uvs = [];
        List<Vector3> normals = [];
        List<int> indices = [];

        var heightmapImage = Heightmap?.GetImage();
        heightmapImage?.Convert(Image.Format.Rf);

        for (int x = 0; x < HeightmapResolution; x++)
        for (int y = 0; y < HeightmapResolution; y++)
        {
            float u = (float)x / (HeightmapResolution - 1);
            float v = (float)y / (HeightmapResolution - 1);
            float height = heightmapImage?.GetPixel(
                MathUtil.FloorToInt(heightmapImage.GetWidth() * ((float)x / HeightmapResolution)),
                MathUtil.FloorToInt(heightmapImage.GetHeight() * ((float)y / HeightmapResolution))
            ).R ?? 0;
            var vert = new Vector3(
                u * Size,
                height * MaxHeight,
                v * Size
            );
            verts.Add(vert);
            normals.Add(Vector3.Up);
            uvs.Add(new Vector2(u, v));
        }

        for (int y = 0; y < (HeightmapResolution - 1); y++)
        for (int x = 0; x < (HeightmapResolution - 1); x++)
        {
            int quad = y * HeightmapResolution + x;

            indices.Add(quad);
            indices.Add(quad + HeightmapResolution);
            indices.Add(quad + HeightmapResolution + 1);

            indices.Add(quad);
            indices.Add(quad + HeightmapResolution + 1);
            indices.Add(quad + 1);
        }

        // Generate mesh data here

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        // No blendshapes, lods, or compression used.
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        var surfaceTool = new SurfaceTool();
        surfaceTool.SetSmoothGroup(0);
        surfaceTool.AppendFrom(mesh, 0, Transform3D.Identity);
        surfaceTool.GenerateNormals();
        surfaceTool.GenerateTangents();
        mesh = surfaceTool.Commit(mesh);
        MeshInstance3D.Mesh = mesh;

        CollisionShape3D.Position = new Vector3(Size * 0.5f, 0f, Size * 0.5f);
        var terrainCollider = new HeightMapShape3D();
        if (heightmapImage is not null)
        {
            terrainCollider.UpdateMapDataFromImage(heightmapImage, 0f, MaxHeight);
            CollisionShape3D.SetScale(
                new Vector3(
                    (float)Size / heightmapImage.GetWidth(),
                    1f,
                    (float)Size / heightmapImage.GetHeight()
                )
            );
        }
        CollisionShape3D.SetShape(terrainCollider);
    }
}
