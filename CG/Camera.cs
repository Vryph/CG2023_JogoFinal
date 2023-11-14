using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace CG
{
    internal class Camera
    {
        public Transform transform;
        public float fieldOfView = 1.04719755f;
        private IWindow window;
        private GL gl;

        public Camera(GL gl, IWindow window)
        {
            this.gl = gl;
            this.window = window;
            transform = new Transform(gl);
        }

        public void Use(ShaderProgram program)
        {
            float aspectRatio = (float)window.Size.X / window.Size.Y;
            //matriz de view, muda a posição do nosso objeto em relação à câmera
            Matrix4x4 view = Matrix4x4.CreateLookAt(transform.position, transform.position + transform.Forward, new Vector3(0.0f, 1.0f, 0.0f));

            //matriz de projeção, transforma as coordenadas de mundo em coordenadas de tela
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, 0.1f, 100.0f);

            program.SetMatrix4("view", view);
            program.SetMatrix4("projection", projection);
            program.SetVector3("viewPosition", transform.position);
        }
    }
}