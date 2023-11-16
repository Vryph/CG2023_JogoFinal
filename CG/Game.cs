using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace CG
{
    //A classe Game vai ser o nosso ponto de entrada para inicializar e utilizar a OpenGL
    internal class Game
    {
        IWindow window;//Esta é uma Interface que representa uma janela, é nesta janela que vamos desenhar cores e formatos logo mais
        IInputContext input;
        GL gl;
        ShaderProgram program;
        ShaderProgram diffuseProgram;
        Mesh mesh;
        Texture texture1;
        Texture texture2;
        Camera camera;
        Transform transform1;
        Transform transform2;
        Transform transform3;
        TexturedMaterial material1;
        TexturedMaterial material2;
        DiffuseMaterial diffuseMaterial;

        //Variaveis do Jogo
        int gamePhase = 0;
        int menuButton = 0;
        float minRangeX = 0.6f;
        float maxRangeX = 2.0f;
        float minRangeY = -1.4f;
        float maxRangeY = 1.2f;
        float minRotation = -6.2825f;
        float maxRotation = 6.2825f;
        float winTimer = 0.0f;
        bool lockY = false;
        bool lockX = false;
        bool lockRotation = false;
        float points = 0.0f;
        float gameTimer = 0.0f;
        Random rng = new Random();

        public Game()
        {
            Window.PrioritizeSdl();
            WindowOptions options = WindowOptions.Default;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "Jogo de Imitação Wow";
            window = Window.Create(options);//Aqui nós pedimos para a Silk.Net criar uma janela, com as opções padrão(altura/largura/etc)

            //o Método IWindow.Run que chamamos mais tarde só para de executar quando fechamos a janela, por isso precisamos especificar Métodos
            //que queremos utilizar em cada ponto de nosso programa. Nesse caso estamos pedindo que a Silk.Net chame internamente o método Render
            //da nossa classe Game
            window.Render += Render;
            window.Update += Update;
            //Para que possamos chamar funções da OpenGL, precisamos primeiro inicializar a nossa janela, mas sem ainda chamar o Run
            window.Initialize();

            input = window.CreateInput();
            foreach (var keyboard in input.Keyboards)
            {
                keyboard.KeyDown += KeyDown;
                keyboard.KeyUp += KeyUp;
            }
            foreach (var mouse in input.Mice)
            {
                mouse.Click += MouseClick;
            }
            //Cada sistema tem diferentes funções que equivalem às funções da OpenGL, aqui nós carregamos essas funções para o nosso sitema atual
            gl = GL.GetApi(window);

            gl.Enable(EnableCap.DepthTest);//O depth test faz com que objetos mais próximos à câmera escondam os demais com o uso do depth buffer
            gl.Enable(EnableCap.CullFace);//O culling de faces evita que desenhemos faces que não estão viradas para a posição da câmera

            mesh = Mesh.CreateCube(gl, 1.0f);

            //Vertex shader, determina como nossas coordenadas serão transformadas em coordenadas de tela
            string vert = @"
            #version 430 core
            layout(location = 0)in vec3 vert_Position;
            layout(location = 1)in vec2 vert_TexCoord;
            layout(location = 2)in vec3 vert_Normal;

            layout(location = 2)uniform mat4 model;
            layout(location = 0)uniform mat4 view;
            layout(location = 1)uniform mat4 projection;

            out vec2 frag_TexCoord;
            out vec3 frag_Normal;
            out vec3 frag_Position;

            void main() {
                frag_TexCoord = vert_TexCoord;
                frag_Normal = mat3(transpose(inverse(model))) * vert_Normal;
                frag_Position = vec3(model * vec4(vert_Position, 1.0));
                gl_Position = projection * view * model * vec4(vert_Position, 1.0);
            }
            ";

            //Fragment shader, determina a cor que cada pixel em nosso triângulo terá
            string fragTexture = @"
            #version 430 core
            in vec2 frag_TexCoord;

            uniform sampler2D mainTexture;

            out vec4 out_Color;
            void main() {
                out_Color = texture(mainTexture, frag_TexCoord);
            }
            ";

            //Fragment shader para material de diffuse. Por fazer cálculos de luz, ele precisa receber vários inputs a mais que um shader comum.
            string fragDiffuse = @"
            #version 430 core
            in vec3 frag_Normal;
            in vec3 frag_Position;

            uniform vec3 viewPosition;

            uniform vec3 ambientLightColor;
            uniform vec3 directionalLightDir;
            uniform vec3 directionalLightColor;
            uniform float specularIntensity;

            uniform vec3 objectColor;

            out vec4 out_Color;
            void main() {
                vec3 diffuse = directionalLightColor * max(dot(frag_Normal, normalize(-directionalLightDir)), 0.0);

                vec3 viewDir = normalize(viewPosition - frag_Position);
                vec3 reflectDir = reflect(normalize(directionalLightDir), frag_Normal);
                float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
                vec3 specular = spec * specularIntensity * directionalLightColor;

                out_Color = vec4(objectColor * (specular + diffuse + ambientLightColor), 1.0);
            }
            ";

            //Uso da classe ShaderProgram facilita a compilação de shaders
            program = new ShaderProgram(gl);
            program.LoadFromStrings(vert, fragTexture);
            program.Use();

            diffuseProgram = new ShaderProgram(gl);
            diffuseProgram.LoadFromStrings(vert, fragDiffuse);

            //determinamos como a textura vai se comportar em determinados casos
            TextureConfig textureConfig = new TextureConfig();
            textureConfig.AddParameter(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            textureConfig.AddParameter(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            textureConfig.AddParameter(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            textureConfig.AddParameter(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            //Carregamento de 2 texturas
            texture1 = new Texture(gl, "./img2.jpg", textureConfig);
            texture2 = new Texture(gl, "./img2.jpg", textureConfig); ;

            camera = new Camera(gl, window);
            camera.transform.position.Z = 4.0f;

            transform1 = new Transform(gl);
            transform1.position.X = 2.0f;
            transform1.position.Z = 0f;
            transform2 = new Transform(gl);
            transform2.position.X = -2.0f;
            transform2.position.Z = 0f;
            transform3 = new Transform(gl);
            transform3.scale.Y = 10.0f;
            transform3.scale.X = 0.05f;

            material1 = new TexturedMaterial(program, gl);
            material1.texture = texture1;
            material2 = new TexturedMaterial(program, gl);
            material2.texture = texture2;
            diffuseMaterial = new DiffuseMaterial(diffuseProgram, gl);
            diffuseMaterial.objectColor = new Vector3(0f, 0f, 0f);
            diffuseMaterial.directionalLightDir = new Vector3(0, -0.5f, -1f);
            diffuseMaterial.directionalLightColor = new Vector3(0.7f, 0.7f, 0.5f);
            diffuseMaterial.ambientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
        }

        public void Run()
        {
            window.Run();
        }

        private void KeyDown(IKeyboard keyboard, Key key, int i)
        {
            if (key == Key.Z)
            {
                transform3.position.Y -= 2.0f;
            }
            if (key == Key.Enter && gamePhase == 0)
            {
                if (menuButton == 0)
                {
                    RandomizeCube();
                    gamePhase = 1;
                    gameTimer += 60.0f;
                }
            }
            if (gamePhase == 1)
            {
                if (key == Key.A)
                {
                    if (transform1.position.X >= minRangeX && lockX == false) { transform1.position.X -= 0.2f; winTimer = 1.5f; }
                }
                if (key == Key.D)
                {
                    if (transform1.position.X <= maxRangeX && lockX == false) { transform1.position.X += 0.2f; winTimer = 1.5f; }
                }
                if (key == Key.W)
                {
                    if (transform1.position.Y <= maxRangeY && lockY == false) { transform1.position.Y += 0.2f; winTimer = 1.5f; }
                }
                if (key == Key.S)
                {
                    if (transform1.position.Y >= minRangeY && lockY == false) { transform1.position.Y -= 0.2f; winTimer = 1.5f; }
                }
                if (key == Key.R)
                {
                    RandomizeCube();
                }
            }
        }

        private void KeyUp(IKeyboard keyboard, Key key, int i)
        {
            if (key == Key.Z)
            {
            }
        }

        private void MouseClick(IMouse mouse, MouseButton button, Vector2 position)
        {
            if (button == MouseButton.Left)
            {
            }
        }

        float time = 0.0f;//tempo passado desde o início do jogo

        //Função de Update. Nela implementamos a lógica frame a frame do nosso jogo.
        private void Update(double delta)
        {
            time += (float)delta;

            foreach (var keyboard in input.Keyboards)
            {
                if (gamePhase == 1)
                {
                    //Controles da rotação do Cubo
                    if (keyboard.IsKeyPressed(Key.E))
                    {
                        if (transform1.rotation.X < maxRotation && lockRotation == false) { transform1.rotation.X += 0.03f; winTimer = 1.5f; }
                    }
                    if (keyboard.IsKeyPressed(Key.Q))
                    {
                        if (transform1.rotation.X > minRotation && lockRotation == false) { transform1.rotation.X -= 0.03f; winTimer = 1.5f; }
                    }
                }

            }

            if (gamePhase == 1)
            {
                gameTimer -= (float)delta * 2;
                if (winTimer > 0.1)
                {
                    winTimer -= (float)delta;
                    Console.WriteLine($"{transform1.position.X} ; {transform2.position.X} ; {(float)System.Math.Round(transform1.rotation.X % 6.2825f - transform2.rotation.X % 6.2825f, 3)} ; {gameTimer} ; {points}");
                }
                if (winTimer < 0.1f)
                {
                    double rotationDifference = (double)System.Math.Round(Math.Abs(transform1.rotation.X - transform2.rotation.X) % 6.2825, 3);
                    if (transform1.position.Y - transform2.position.Y >= -0.1f && transform1.position.Y - transform2.position.Y <= 0.1f)
                    {
                        lockY = true;
                        if (transform1.position.X - transform2.position.X >= 2.78f && transform1.position.X - transform2.position.X <= 2.81f)
                        {
                            if (rotationDifference <= 0.18 && rotationDifference >= -0.18)
                            {
                                points++;
                                gameTimer += 6.0f;
                                Console.WriteLine("Ganhou!!!");
                                RandomizeCube();
                            }
                            else if (rotationDifference <=  -6.03f || rotationDifference >= 6.03f)
                            {
                                points++;
                                gameTimer += 6.0f;
                                Console.WriteLine("Ganhou!!!");
                                RandomizeCube();
                            }
                        }

                    }

                    //Trava o objeto quando na posição certa
                    if (transform1.position.X - transform2.position.X >= 2.78f && transform1.position.X - transform2.position.X <= 2.81f) { lockX = true; }
                    else { lockX = false; }
                    if (rotationDifference <= 0.18 && rotationDifference >= -0.18) { lockRotation = true; }
                    else if (rotationDifference <= -6.03f || rotationDifference >= 6.03f) { lockRotation = true; }
                    else { lockRotation = false; }

                }

                if (gameTimer <= 0.05f)
                {
                    Console.Clear();
                    gamePhase = 0;
                }
            }
        }

        //Função de Render, que roda a cada frame do jogo. Nela, limpamos a tela e depois desenhamos todas as malhas que queremos
        private unsafe void Render(double delta)
        {

            if (gamePhase == 0)
            {
                gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            }
            else if (gamePhase == 1)
            {
                gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
                gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);//limpamos as cores e o depth buffer da tela

                mesh.Draw(transform1, material1, camera);
                mesh.Draw(transform2, material2, camera);
                mesh.Draw(transform3, diffuseMaterial, camera);

            }
        }

        public void RandomizeCube()
        {

            double randX = (double)rng.Next(3, 12) * -0.2;
            transform2.position.X = (float)randX;
            double randY = ((double)rng.Next(15) - 7) * 0.2;
            transform2.position.Y = (float)randY;
            double randRotation = (rng.NextDouble() * 6.2528f);
            transform2.rotation.X = (float)randRotation;

            lockY = false;
            lockRotation = false;
            lockX = false;
        }
    }
}
