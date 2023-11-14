using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace CG
{
    class Transform
    {
        public Vector3 position = new Vector3(0, 0, 0);
        public Vector3 scale = new Vector3(1, 1, 1);
        public Vector3 rotation = new Vector3(0, 0, 0);

        public Matrix4x4 ModelMatrix
        {
            get
            {
                Matrix4x4 model = Matrix4x4.CreateScale(scale);
                model *= Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
                model *= Matrix4x4.CreateTranslation(position);

                return model;
            }
        }

        public Vector3 Forward
        {
            get
            {
                Vector3 vec = new Vector3(0, 0, -1);
                return Vector3.Transform(vec, Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z));
            }
        }

        public Vector3 Right
        {
            get
            {
                Vector3 vec = new Vector3(1, 0, 0);
                return Vector3.Transform(vec, Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z));
            }
        }

        public Vector3 Up
        {
            get
            {
                Vector3 vec = new Vector3(0, 1, 0);
                return Vector3.Transform(vec, Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z));
            }
        }

        private GL gl;
        public Transform(GL gl)
        {
            this.gl = gl;
        }
    }
}
