using Silk.NET.OpenGL;
using StbImageSharp;

namespace CG
{
    internal class TextureConfig
    {
        public List<Tuple<TextureParameterName, int>> parameters = new List<Tuple<TextureParameterName, int>>();

        public void AddParameter(TextureParameterName parameterName, int parameter)
        {
            parameters.Add(Tuple.Create(parameterName, parameter));
        }

        public void Apply(GL gl, uint textureId)
        {
            foreach(var param in parameters)
            {
                gl.TextureParameter(textureId, param.Item1, param.Item2);
            }
        }
    }

    internal class Texture
    {
        private GL gl;
        private uint textureId;
        public Texture(GL gl, string path, TextureConfig config)
        {
            this.gl = gl;

            //carregamos nossa imagem do disco
            StbImage.stbi_set_flip_vertically_on_load(1);
            ImageResult result = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

            textureId = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, textureId);

            //enviamos os dados de textura para a open gl
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)result.Width, (uint)result.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (ReadOnlySpan<byte>)result.Data.AsSpan());

            config.Apply(gl, textureId);
            gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void SetParameter(TextureParameterName parameterName, int parameter)
        {
            gl.BindTexture(TextureTarget.Texture2D, textureId);
            gl.TextureParameter(textureId, parameterName, parameter);
        }

        public void Bind(int textureUnit)
        {
            gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
            gl.BindTexture(TextureTarget.Texture2D, textureId);
        }

        ~Texture()
        {
            gl.DeleteTexture(textureId);
        }
    }
}