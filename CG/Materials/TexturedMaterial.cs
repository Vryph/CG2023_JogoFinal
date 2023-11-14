using Silk.NET.OpenGL;

namespace CG
{
    class TexturedMaterial : Material
    {
        public Texture? texture;
        public TexturedMaterial(ShaderProgram program, GL gl) : base(program, gl)
        {
        }

        protected override void InternalUse()
        {
            Program.SetInt("mainTexture", 0);
            if(texture != null)
            {
                texture?.Bind(0);
            }
            else
            {
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, 0);
            }
        }
    }
}