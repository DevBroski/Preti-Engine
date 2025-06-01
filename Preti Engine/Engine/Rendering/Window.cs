using Dear_ImGui_Sample; // Assuming this provides ImGuiController
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework; // For MouseButton and Keys
using System; // For MathF.Floor and Math.Abs
using System.Collections.Generic; // Make sure List is recognized

namespace VoxelEditor
{
    public class Window : GameWindow
    {
        private ImGuiController _imGuiController;
        private Camera _camera;

        private enum BrushShape { Cube }
        private BrushShape _brushShape = BrushShape.Cube;
        private float _brushSize = 1.0f;

        private readonly List<(BrushShape shape, Vector3 position, float size)> _placedBrushes = new();

        private Matrix4 _projection;
        private bool _wasMouseDownLastFrame = false;

        // --- Grid resources ---
        private int _gridVao, _gridVbo, _gridVertexCount;
        private const float GridMin = -500; // 1000x1000 grid
        private const float GridMax = 500;

        // --- Cube mesh and shader resources ---
        private int _cubeVao, _cubeVbo, _cubeEbo;
        private int _shaderProgram, _uMvp, _uColor;

        private readonly float[] _cubeVertices = {
            // Back face
            -0.5f,-0.5f,-0.5f, // 0
             0.5f,-0.5f,-0.5f, // 1
             0.5f, 0.5f,-0.5f, // 2
            -0.5f, 0.5f,-0.5f, // 3
            // Front face
            -0.5f,-0.5f, 0.5f, // 4
             0.5f,-0.5f, 0.5f, // 5
             0.5f, 0.5f, 0.5f, // 6
            -0.5f, 0.5f, 0.5f  // 7
        };
        private readonly uint[] _cubeIndices = {
            // Front face
            4, 5, 6,
            6, 7, 4,
            // Back face
            1, 0, 3,
            3, 2, 1,
            // Right face
            5, 1, 2,
            2, 6, 5,
            // Left face
            0, 4, 7,
            7, 3, 0,
            // Top face
            3, 2, 6,
            6, 7, 3,
            // Bottom face
            0, 1, 5,
            5, 4, 0
        };


        public Window(GameWindowSettings gws, NativeWindowSettings nws)
            : base(gws, nws)
        {
            Title = "Simple Voxel Editor"; // Set a window title
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            _imGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
            ImGui.StyleColorsDark();

            // Initialize camera with aspect ratio and an initial position/look direction
            _camera = new Camera(new Vector3(0, 10, 10), -30, -90, Size.X / (float)Size.Y);

            // Grab the mouse by default for camera control
            CursorState = CursorState.Grabbed;

            _projection = _camera.GetProjectionMatrix(); // Initial projection matrix

            // --- Setup grid mesh ---
            List<float> gridVerts = new List<float>();
            for (int i = (int)GridMin; i <= (int)GridMax; i += 10) // Draw grid lines every 10 units for a large grid
            {
                // X lines
                gridVerts.Add(i); gridVerts.Add(0); gridVerts.Add(GridMin);
                gridVerts.Add(i); gridVerts.Add(0); gridVerts.Add(GridMax);
                // Z lines
                gridVerts.Add(GridMin); gridVerts.Add(0); gridVerts.Add(i);
                gridVerts.Add(GridMax); gridVerts.Add(0); gridVerts.Add(i);
            }
            _gridVertexCount = gridVerts.Count / 3;
            _gridVao = GL.GenVertexArray();
            _gridVbo = GL.GenBuffer();
            GL.BindVertexArray(_gridVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _gridVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, gridVerts.Count * sizeof(float), gridVerts.ToArray(), BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.BindVertexArray(0);

            // --- Setup cube mesh ---
            _cubeVao = GL.GenVertexArray();
            _cubeVbo = GL.GenBuffer();
            _cubeEbo = GL.GenBuffer();
            GL.BindVertexArray(_cubeVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _cubeVertices.Length * sizeof(float), _cubeVertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _cubeIndices.Length * sizeof(uint), _cubeIndices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.BindVertexArray(0);

            // --- Setup shader ---
            _shaderProgram = CreateShaderProgram();
            _uMvp = GL.GetUniformLocation(_shaderProgram, "uMVP");
            _uColor = GL.GetUniformLocation(_shaderProgram, "uColor");
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
            _projection = _camera.GetProjectionMatrix();
            _imGuiController.WindowResized(ClientSize.X, ClientSize.Y);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            // Toggle mouse grab/ungrab with Escape key
            if (KeyboardState.IsKeyPressed(Keys.Escape))
            {
                if (CursorState == CursorState.Grabbed)
                    CursorState = CursorState.Normal;
                else
                    CursorState = CursorState.Grabbed;
            }

            // Only update camera if mouse is grabbed
            if (CursorState == CursorState.Grabbed)
            {
                _camera.Update(e, KeyboardState, MouseState);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Matrix4 view = _camera.GetViewMatrix();

            // Draw grid
            GL.UseProgram(_shaderProgram);
            Matrix4 gridMvp = Matrix4.Identity * view * _projection;
            GL.UniformMatrix4(_uMvp, false, ref gridMvp);
            GL.Uniform4(_uColor, 0.3f, 0.3f, 0.3f, 1.0f);
            GL.BindVertexArray(_gridVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertexCount);
            GL.BindVertexArray(0);

            // Draw placed brushes
            foreach (var brush in _placedBrushes)
            {
                if (brush.shape == BrushShape.Cube)
                    DrawCube(brush.position, brush.size, view, _projection, new Vector3(0.7f, 0.7f, 1.0f), 1.0f);
            }

            // Draw brush preview
            // Only draw preview if mouse is grabbed and not interacting with ImGui
            bool wantCaptureMouse = ImGui.GetIO().WantCaptureMouse;
            if (CursorState == CursorState.Grabbed && !wantCaptureMouse && TryGetBrushPlacement(out Vector3 previewPos))
            {
                // Enable blending for transparency
                GL.Enable(EnableCap.Blend);
                // Standard alpha blending: new_color = source_alpha * source_color + (1 - source_alpha) * destination_color
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                DrawCube(previewPos, _brushSize, view, _projection, new Vector3(1.0f, 1.0f, 0.3f), 0.5f);

                // Disable blending after drawing transparent objects if no other transparent objects will be drawn
                GL.Disable(EnableCap.Blend);
            }

            // --- ImGui UI ---
            _imGuiController.Update(this, (float)e.Time);

            ImGui.Begin("Brush Tools");
            if (ImGui.RadioButton("Cube", _brushShape == BrushShape.Cube))
                _brushShape = BrushShape.Cube;
            ImGui.SliderFloat("Brush Size", ref _brushSize, 0.2f, 3.0f, "%.1f");
            ImGui.Text("Left-click in the viewport to place a block.");
            ImGui.Text($"Camera Position: {_camera.Position.X:F1}, {_camera.Position.Y:F1}, {_camera.Position.Z:F1}");
            ImGui.Text($"Camera Pitch: {_camera.Pitch:F1}, Yaw: {_camera.Yaw:F1}");
            ImGui.Text($"Mouse Grabbed: {(CursorState == CursorState.Grabbed ? "Yes" : "No")}");
            ImGui.Text("Press ESC to toggle mouse grab for UI interaction.");
            ImGui.End();

            // ImGui.ShowDemoWindow(); // Uncomment to see the ImGui demo window

            _imGuiController.Render();
            SwapBuffers();

            // --- Handle brush placement ---
            bool mouseDown = MouseState.IsButtonDown(MouseButton.Left);
            // Only place brush if mouse is grabbed and not interacting with ImGui
            if (CursorState == CursorState.Grabbed && !wantCaptureMouse && mouseDown && !_wasMouseDownLastFrame)
            {
                if (TryGetBrushPlacement(out Vector3 placePos))
                {
                    _placedBrushes.Add((_brushShape, placePos, _brushSize));
                }
            }
            _wasMouseDownLastFrame = mouseDown;
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            _imGuiController?.Dispose();
            // Delete all OpenGL resources
            GL.DeleteVertexArray(_cubeVao);
            GL.DeleteBuffer(_cubeVbo);
            GL.DeleteBuffer(_cubeEbo);
            GL.DeleteVertexArray(_gridVao);
            GL.DeleteBuffer(_gridVbo);
            GL.DeleteProgram(_shaderProgram);
        }

        // --- Accurate mouse picking and grid snapping ---
        private bool TryGetBrushPlacement(out Vector3 snapped)
        {
            var mouse = MouseState.Position;
            float ndcX = (2.0f * mouse.X) / Size.X - 1.0f;
            float ndcY = 1.0f - (2.0f * mouse.Y) / Size.Y; // Y-axis inversion for NDC

            // These declarations MUST be directly within the TryGetBrushPlacement method
            // and not inside any nested if/else or other blocks
            Vector4 nearPoint = new Vector4(ndcX, ndcY, -1.0f, 1.0f); // Point on the near plane
            Vector4 farPoint = new Vector4(ndcX, ndcY, 1.0f, 1.0f);  // Point on the far plane

            // Calculate View-Projection matrix
            // Note: OpenTK's Matrix4 multiplication is column-major compatible,
            // meaning `A * B` applies `B` then `A`. So `View * Projection` becomes `Projection * View` when applied to a vector.
            Matrix4 viewProj = _camera.GetViewMatrix() * _projection;
            Matrix4 invProjView = viewProj.Inverted();

            // Transform NDC points back to World Space
            Vector4 nearWorld = Vector4.Transform(nearPoint, invProjView);
            Vector4 farWorld = Vector4.Transform(farPoint, invProjView);

            // Perform perspective divide
            nearWorld /= nearWorld.W;
            farWorld /= farWorld.W;

            // Define the ray in world coordinates
            Vector3 rayOrigin = new Vector3(nearWorld.X, nearWorld.Y, nearWorld.Z);
            Vector3 rayDir = Vector3.Normalize(new Vector3(farWorld.X, farWorld.Y, farWorld.Z) - rayOrigin);

            // Intersect with y=0 plane (the grid plane)
            // If ray is parallel to the plane (or very close), no intersection
            if (Math.Abs(rayDir.Y) < 1e-6f)
            {
                snapped = default;
                return false;
            }
            // Calculate parameter 't' where ray intersects y=0 plane
            float t = -rayOrigin.Y / rayDir.Y;
            if (t < 0) // Intersection is behind the ray origin
            {
                snapped = default;
                return false;
            }
            Vector3 hit = rayOrigin + rayDir * t; // Calculate the hit point in world space

            // Snap to nearest grid cell
            float cellSize = 1.0f; // Each grid cell is 1x1 unit
            // Floor and add 0.5f to round to nearest integer, then multiply by cell size
            float snappedX = MathF.Floor(hit.X / cellSize + 0.5f) * cellSize;
            float snappedZ = MathF.Floor(hit.Z / cellSize + 0.5f) * cellSize;
            snapped = new Vector3(snappedX, 0, snappedZ); // Y-coordinate is 0 for placement on the grid

            // Only allow placement within grid boundaries
            if (snappedX < GridMin || snappedX > GridMax || snappedZ < GridMin || snappedZ > GridMax)
                return false;
            return true;
        }

        // --- Draw a cube using modern OpenGL ---
        private void DrawCube(Vector3 position, float size, Matrix4 view, Matrix4 proj, Vector3 color, float alpha)
        {
            GL.UseProgram(_shaderProgram);
            Matrix4 model = Matrix4.CreateScale(size) * Matrix4.CreateTranslation(position);
            Matrix4 mvp = model * view * proj; // Model * View * Projection
            GL.UniformMatrix4(_uMvp, false, ref mvp);
            GL.Uniform4(_uColor, color.X, color.Y, color.Z, alpha);

            GL.BindVertexArray(_cubeVao);
            GL.DrawElements(PrimitiveType.Triangles, _cubeIndices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0); // Unbind program after drawing to avoid accidental state changes
        }

        // --- Minimal shader (MVP, color) ---
        private int CreateShaderProgram()
        {
            string vert = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
uniform mat4 uMVP;
void main()
{
    gl_Position = uMVP * vec4(aPosition, 1.0);
}";
            string frag = @"
#version 330 core
uniform vec4 uColor;
out vec4 FragColor;
void main()
{
    FragColor = uColor;
}";
            int v = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(v, vert);
            GL.CompileShader(v);
            // Check for vertex shader compile errors
            GL.GetShader(v, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(v);
                Console.WriteLine($"Vertex Shader Compile Error: {infoLog}");
            }

            int f = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(f, frag);
            GL.CompileShader(f);
            // Check for fragment shader compile errors
            GL.GetShader(f, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(f);
                Console.WriteLine($"Fragment Shader Compile Error: {infoLog}");
            }

            int prog = GL.CreateProgram();
            GL.AttachShader(prog, v);
            GL.AttachShader(prog, f);
            GL.LinkProgram(prog);
            // Check for program linking errors
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(prog);
                Console.WriteLine($"Shader Program Link Error: {infoLog}");
            }

            GL.DetachShader(prog, v);
            GL.DetachShader(prog, f);
            GL.DeleteShader(v);
            GL.DeleteShader(f);
            return prog;
        }
    }
}