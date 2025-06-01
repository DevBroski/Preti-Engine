using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace YourNamespace
{
    public class Shader : IDisposable
    {
        public int Handle { get; private set; }
        private readonly Dictionary<string, int> _uniformLocations = new();
        private bool _disposed = false;

        public Shader(string vertexPath, string fragmentPath)
        {
            string vertexSource = File.ReadAllText(vertexPath);
            string fragmentSource = File.ReadAllText(fragmentPath);

            int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource, vertexPath);
            int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource, fragmentPath);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int status);
            if (status == 0)
            {
                string info = GL.GetProgramInfoLog(Handle);
                GL.DeleteProgram(Handle);
                throw new Exception($"Program linking failed:\n{info}");
            }

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int numUniforms);
            for (int i = 0; i < numUniforms; i++)
            {
                string name = GL.GetActiveUniform(Handle, i, out _, out _);
                int location = GL.GetUniformLocation(Handle, name);
                _uniformLocations[name] = location;
            }
        }

        private int CompileShader(ShaderType type, string source, string filePath)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                throw new Exception(
                    $"Error compiling {type} ({filePath}):\n{info}\nSource:\n{source.Substring(0, Math.Min(300, source.Length))}...");
            }
            return shader;
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        public void SetInt(string name, int value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
                GL.Uniform1(location, value);
        }

        public void SetFloat(string name, float value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
                GL.Uniform1(location, value);
        }

        public void SetMatrix4(string name, Matrix4 value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
                GL.UniformMatrix4(location, false, ref value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
                GL.Uniform3(location, value);
        }

        public void SetVector4(string name, Vector4 value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
                GL.Uniform4(location, value);
        }

        public void SetBool(string name, bool value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
                GL.Uniform1(location, value ? 1 : 0);
        }

        // IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                GL.DeleteProgram(Handle);
                _disposed = true;
            }
        }

        ~Shader()
        {
            Dispose(false);
        }
    }
}
