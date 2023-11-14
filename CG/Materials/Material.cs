using Silk.NET.OpenGL;

namespace CG
{
    class Material
    {
        private ShaderProgram program;
        public ShaderProgram Program => program;
        protected GL gl;

        public Material(ShaderProgram program, GL gl)
        {
            this.program = program;
            this.gl = gl;
        }

        public virtual void Use()
        {
            program.Use();
            InternalUse();
        }

        protected virtual void InternalUse() {}
    }
}