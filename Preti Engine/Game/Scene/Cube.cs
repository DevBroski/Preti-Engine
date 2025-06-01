using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

public class Cube
{
    private readonly float[] _vertices = {
        // Position           // Color
        -0.5f, -0.5f, -0.5f,  1f, 0f, 0f, // 0
         0.5f, -0.5f, -0.5f,  0f, 1f, 0f, // 1
         0.5f,  0.5f, -0.5f,  0f, 0f, 1f, // 2
        -0.5f,  0.5f, -0.5f,  1f, 1f, 0f, // 3
        -0.5f, -0.5f,  0.5f,  1f, 0f, 1f, // 4
         0.5f, -0.5f,  0.5f,  0f, 1f, 1f, // 5
         0.5f,  0.5f,  0.5f,  1f, 1f, 1f, // 6
        -0.5f,  0.5f,  0.5f,  0f, 0f, 0f  // 7
    };

    private readonly uint[] _indices = {
        // Back face
        0, 1, 2, 2, 3, 0,
        // Front face
        4, 5, 6, 6, 7, 4,
        // Left face
        0, 3, 7, 7, 4, 0,
        // Right face
        1, 5, 6, 6, 2, 1,
        // Bottom face
        0, 1, 5, 5, 4, 0,
        // Top face
        3, 2, 6, 6, 7, 3
    };

    private int _vao, _vbo, _ebo;

    public Cube()
    {
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

        // Position attribute
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Color attribute
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
    }

    public void Draw()
    {
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
    }
}
