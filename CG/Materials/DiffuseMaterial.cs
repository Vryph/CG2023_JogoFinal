using System.Numerics;
using Silk.NET.OpenGL;

namespace CG
{
    class DiffuseMaterial : Material
    {
        public Vector3 objectColor;
        public Vector3 ambientLightColor;
        public Vector3 directionalLightDir;
        public Vector3 directionalLightColor;
        public float specularIntensity;
        public DiffuseMaterial(ShaderProgram program, GL gl) : base(program, gl)
        {
            objectColor = new Vector3(0.7f);
            ambientLightColor = new Vector3(0.1f);
            directionalLightDir = new Vector3(0, -1, -0.3f);
            directionalLightDir = new Vector3(0.8f);
            specularIntensity = 0.5f;
        }

        protected override void InternalUse()
        {
            Program.SetVector3("objectColor", objectColor);
            Program.SetVector3("directionalLightDir", directionalLightDir);
            Program.SetVector3("directionalLightColor", directionalLightColor);
            Program.SetVector3("ambientLightColor", ambientLightColor);
            Program.SetFloat("specularIntensity", specularIntensity);
        }
    }
}