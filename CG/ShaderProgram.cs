using System.Drawing;
using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace CG
{
    //Classe que encapsula a compilação de shaders e link do shaderprogram
    internal class ShaderProgram
    {
        private GL gl;
        private uint program;

        public ShaderProgram(GL gl)
        {
            this.gl = gl;
        }

        //Carrega um shader a partir de strings contendo os códigos fonte de vertex e fragment shaders
        public bool LoadFromStrings(string vertSource, string fragSource)
        {
            uint vertShader = gl.CreateShader(GLEnum.VertexShader);
            gl.ShaderSource(vertShader, vertSource);
            gl.CompileShader(vertShader);
            string log = gl.GetShaderInfoLog(vertShader);
            if(!string.IsNullOrEmpty(log))
            {
                Console.WriteLine($"Falha no vertex shader: {log}");
                gl.DeleteShader(vertShader);
                return false;
            }

            uint fragShader = gl.CreateShader(GLEnum.FragmentShader);
            gl.ShaderSource(fragShader, fragSource);
            gl.CompileShader(fragShader);
            log = gl.GetShaderInfoLog(fragShader);
            if (!string.IsNullOrEmpty(log))
            {
                Console.WriteLine($"Falha no fragment shader: {log}");
                gl.DeleteShader(vertShader);
                gl.DeleteShader(fragShader);
                return false;
            }

            program = gl.CreateProgram();
            gl.AttachShader(program, vertShader);
            gl.AttachShader(program, fragShader);
            gl.LinkProgram(program);
            //Assim que os shaders são utilizados pra fazer o link do program, podemos descartá-los para evitar o uso indevido de memória
            gl.DeleteShader(vertShader);
            gl.DeleteShader(fragShader);

            log = gl.GetProgramInfoLog(program);
            if (!string.IsNullOrEmpty(log))
            {
                Console.WriteLine($"Falha no link do shader: {log}");
                program = 0;
                gl.DeleteProgram(program);
                return false;
            }

            return true;
        }

        public bool LoadFromFiles(string vertPath, string fragPath)
        {
            try
            {
                string vertStr = File.ReadAllText(vertPath);
                string fragStr = File.ReadAllText(fragPath);

                return LoadFromStrings(vertStr, fragStr);
            } catch {}
            return false;
        }

        public void Use()
        {
            gl.UseProgram(program);
        }

        public int GetAttribLocation(string name)
        {
            return gl.GetAttribLocation(program, name);
        }

        public int GetUniformLocation(string name)
        {
            return gl.GetUniformLocation(program, name);
        }

        public void SetInt(string name, int value)
        {
            Use();
            gl.Uniform1(GetUniformLocation(name), value);
        }

        public void SetFloat(string name, float value)
        {
            Use();
            gl.Uniform1(GetUniformLocation(name), value);
        }

        public void SetVector2(string name, Vector2 value)
        {
            Use();
            gl.Uniform2(GetUniformLocation(name), value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            Use();
            gl.Uniform3(GetUniformLocation(name), value);
        }

        public void SetVector4(string name, Vector4 value)
        {
            Use();
            gl.Uniform4(GetUniformLocation(name), value);
        }

        public void SetMatrix4(string name, Matrix4x4 value)
        {
            Use();
            unsafe
            {
                gl.UniformMatrix4(GetUniformLocation(name), 1, false, (float*)&value);
            }
        }
    }
}
