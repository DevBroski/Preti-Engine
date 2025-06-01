using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework; // For KeyboardState and MouseState
using System; // For MathHelper

namespace VoxelEditor
{
    public class Camera
    {
        // Camera properties
        public Vector3 Position { get; set; }
        public Vector3 Front { get; private set; } // The direction the camera is looking
        public Vector3 Up { get; private set; }    // The 'up' direction of the camera
        public Vector3 Right { get; private set; } // The 'right' direction of the camera

        private float _pitch; // Rotation around the X axis (up/down look)
        private float _yaw;   // Rotation around the Y axis (left/right look)

        public float AspectRatio { get; set; }
        public float Fov { get; set; } = MathHelper.DegreesToRadians(45f);
        public float NearPlane { get; set; } = 0.1f;
        public float FarPlane { get; set; } = 1000f;

        // Mouse tracking for looking around
        private bool _firstMove = true;
        private Vector2 _lastMousePos;

        // Camera controls
        private const float CameraSpeed = 10.0f; // Units per second
        private const float Sensitivity = 0.2f;  // Mouse sensitivity

        public Camera(Vector3 position, float pitch, float yaw, float aspectRatio)
        {
            Position = position;
            _pitch = pitch;
            _yaw = yaw;
            AspectRatio = aspectRatio;

            // Initialize camera vectors
            UpdateCameraVectors();
        }

        // Clamp Pitch to prevent camera flipping
        public float Pitch
        {
            get => _pitch;
            set
            {
                // Clamp the pitch value to be between -89 and 89 degrees to prevent camera flip
                _pitch = MathHelper.Clamp(value, -89.0f, 89.0f);
                UpdateCameraVectors();
            }
        }

        // Yaw doesn't need clamping (can rotate 360 degrees)
        public float Yaw
        {
            get => _yaw;
            set
            {
                _yaw = value;
                UpdateCameraVectors();
            }
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(Fov, AspectRatio, NearPlane, FarPlane);
        }

        public void Update(FrameEventArgs e, KeyboardState keyboardState, MouseState mouseState)
        {
            // Handle mouse input for looking around
            if (_firstMove)
            {
                _lastMousePos = new Vector2(mouseState.X, mouseState.Y);
                _firstMove = false;
            }
            else
            {
                // Calculate the mouse delta
                var deltaX = mouseState.X - _lastMousePos.X;
                var deltaY = mouseState.Y - _lastMousePos.Y;
                _lastMousePos = new Vector2(mouseState.X, mouseState.Y);

                // Apply sensitivity
                Yaw += deltaX * Sensitivity;
                Pitch -= deltaY * Sensitivity; // Invert Y-axis for typical camera controls
            }

            // Handle keyboard input for movement
            float actualCameraSpeed = CameraSpeed * (float)e.Time;

            if (keyboardState.IsKeyDown(Keys.W)) // Forward
            {
                Position += Front * actualCameraSpeed;
            }
            if (keyboardState.IsKeyDown(Keys.S)) // Backward
            {
                Position -= Front * actualCameraSpeed;
            }
            if (keyboardState.IsKeyDown(Keys.A)) // Strafe Left
            {
                Position -= Right * actualCameraSpeed;
            }
            if (keyboardState.IsKeyDown(Keys.D)) // Strafe Right
            {
                Position += Right * actualCameraSpeed;
            }
            if (keyboardState.IsKeyDown(Keys.Space)) // Move Up
            {
                Position += Up * actualCameraSpeed;
            }
            if (keyboardState.IsKeyDown(Keys.LeftShift)) // Move Down
            {
                Position -= Up * actualCameraSpeed;
            }
        }

        // Recalculates the Front, Right, and Up vectors based on the current Yaw and Pitch
        private void UpdateCameraVectors()
        {
            // Calculate new Front vector
            Front = new Vector3(
                (float)Math.Cos(MathHelper.DegreesToRadians(Yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(Pitch)),
                (float)Math.Sin(MathHelper.DegreesToRadians(Pitch)),
                (float)Math.Sin(MathHelper.DegreesToRadians(Yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(Pitch))
            );
            Front = Vector3.Normalize(Front);

            // Recalculate Right and Up vectors
            // Right is perpendicular to Front and World Up (0, 1, 0)
            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
            // Up is perpendicular to Front and Right
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));
        }
    }
}