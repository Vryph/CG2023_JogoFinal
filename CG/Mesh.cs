using Silk.NET.OpenGL;

namespace CG
{
    internal class Mesh
    {
        GL gl;
        uint vbo;
        uint vao;
        uint ebo;
        uint indicesCount;

        public Mesh(GL gl, float[] meshData, uint[] indices)
        {
            this.gl = gl;

            //Pedimos para a OpenGL um buffer, um lugar para por dados
            //Em seguida colocamos esse novo buffer na "caixinha" de operação, para depois poder enviar os dados de nosso triângulo
            vbo = gl.GenBuffer();
            gl.BindBuffer(GLEnum.ArrayBuffer, vbo);
            gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)meshData.AsSpan(), GLEnum.StaticDraw);

            //Vertex array, associa atributos(variáveis com 'in') do vertex shader com buffers que criamos
            vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            //indicamos à OpenGL como associar os dados de posição e coordenada de textura de cada vértice
            unsafe {
                gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 8 * sizeof(float), (void*)0);
                gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 8 * sizeof(float), (void*)(sizeof(float) * 3));
                gl.VertexAttribPointer(2, 3, GLEnum.Float, false, 8 * sizeof(float), (void*)(sizeof(float) * 5));
            }
            gl.EnableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
            gl.EnableVertexAttribArray(2);

            ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices.AsSpan(), GLEnum.StaticDraw);

            indicesCount = (uint)indices.Length;
        }

        ~Mesh()
        {
            gl.DeleteBuffer(vao);
            gl.DeleteBuffer(ebo);
            gl.DeleteVertexArray(vao);
        }

        public void Draw(Transform transform, Material material, Camera camera)
        {
            material.Use();
            material.Program.SetMatrix4("model", transform.ModelMatrix);
            camera.Use(material.Program);
            gl.BindVertexArray(vao);
            unsafe
            {
                gl.DrawElements(PrimitiveType.Triangles, indicesCount, DrawElementsType.UnsignedInt, null);
            }
        }

        public static Mesh CreateCube(GL gl, float size)
        {
            //Array de floats que representa uma malha, contém informação de posição e coordenadas de textura de cada vértice
            float half = size / 2.0f;
            float[] meshData = new float[] {
                //posicao           //uv       //normal
                -half, -half, half, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f,//0
                 half, -half, half, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f,//1
                 half,  half, half, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f,//2
                -half,  half, half, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f,//3

                -half, -half, -half, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f,//4
                 half, -half, -half, 1.0f, 0.0f, 0.0f, 0.0f, -1.0f,//5
                 half,  half, -half, 1.0f, 1.0f, 0.0f, 0.0f, -1.0f,//6
                -half,  half, -half, 0.0f, 1.0f, 0.0f, 0.0f, -1.0f,//7

                 half, -half,  half, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f,//8
                 half, -half, -half, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f,//9
                 half,  half, -half, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f,//10
                 half,  half,  half, 0.0f, 1.0f, 1.0f, 0.0f, 0.0f,//11

                -half, -half,  half, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f,//12
                -half, -half, -half, 1.0f, 0.0f, -1.0f, 0.0f, 0.0f,//13
                -half,  half, -half, 1.0f, 1.0f, -1.0f, 0.0f, 0.0f,//14
                -half,  half,  half, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f,//15

                -half,  half,  half, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f,//16
                 half,  half,  half, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f,//17
                 half,  half, -half, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f,//18
                -half,  half, -half, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f,//19

                -half, -half,  half, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f,//20
                 half, -half,  half, 1.0f, 0.0f, 0.0f, -1.0f, 0.0f,//21
                 half, -half, -half, 1.0f, 1.0f, 0.0f, -1.0f, 0.0f,//22
                -half, -half, -half, 0.0f, 1.0f, 0.0f, -1.0f, 0.0f,//23
            };

            //Criamos um element buffer, para podermos especificar quais triângulos queremos desenhar
            uint[] indices = new uint[]
            {
                0, 1, 2,
                0, 2, 3,

                4, 6, 5,
                4, 7, 6,

                8, 9, 10,
                8, 10, 11,

                12, 14, 13,
                12, 15, 14,

                16, 17, 18,
                16, 18, 19,

                20, 22, 21,
                20, 23, 22,
            };

            return new Mesh(gl, meshData, indices);
        }
    }
}