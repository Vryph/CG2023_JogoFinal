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
        Texture textureMenu;
        Camera camera;
        Transform transform1;
        Transform transform2;
        Transform transform3;
        Transform transformTimer;
        Transform transformMenu;
        Transform transformSignal;
        TexturedMaterial material1;
        TexturedMaterial material2;
        TexturedMaterial materialMenu;
        DiffuseMaterial diffuseMaterial;
        DiffuseMaterial redDiffuseMaterial;
        DiffuseMaterial greenDiffuseMaterial;

        //Variaveis do Jogo
        int gamePhase = 0;
        int menuButton = 0;
        int menuPrint = 0;
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
        float lastScore = 0;
        float highScore = 0;
        float gameTimer = 0.0f;
        float scoreTimer = 0.0f;
        int attempts = 0;
        bool signal = false;
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
            texture2 = new Texture(gl, "./img2.jpg", textureConfig);
            textureMenu = new Texture(gl, "./img3.jpg", textureConfig);

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
            transformTimer = new Transform(gl);
            transformTimer.scale.Z = 0.1f;
            transformTimer.scale.Y = 0.1f;
            transformTimer.position.Y += 1.6f;
            transformTimer.position.Z += 1.0f;
            transformSignal = new Transform(gl);
            transformSignal.scale /= 5;
            transformSignal.position.X += 2.9f;
            transformSignal.position.Y += 2.1f;
            transformSignal.rotation.Y += 0.7f;
            //Menu
            transformMenu = new Transform(gl);
            transformMenu.scale *= 3;
            transformMenu.scale.X += 2;



            material1 = new TexturedMaterial(program, gl);
            material1.texture = texture1;
            material2 = new TexturedMaterial(program, gl);
            material2.texture = texture2;
            diffuseMaterial = new DiffuseMaterial(diffuseProgram, gl);
            diffuseMaterial.objectColor = new Vector3(0f, 0f, 0f);
            redDiffuseMaterial = new DiffuseMaterial(diffuseProgram, gl);
            redDiffuseMaterial.objectColor = new Vector3(1f, 0f, 0f);
            redDiffuseMaterial.directionalLightDir = new Vector3(0, 0.6f, -1.0f);
            redDiffuseMaterial.directionalLightColor = new Vector3(1f, 1f, 1f);
            redDiffuseMaterial.ambientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            greenDiffuseMaterial = new DiffuseMaterial (diffuseProgram, gl);
            greenDiffuseMaterial.objectColor = new Vector3(0f, 1f, 0f);
            greenDiffuseMaterial.directionalLightDir = new Vector3(0, 0.6f, -1.0f);
            greenDiffuseMaterial.directionalLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            greenDiffuseMaterial.ambientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            //Menu
            materialMenu = new TexturedMaterial(program, gl);
            materialMenu.texture = textureMenu;
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
                    if (transform1.position.X >= minRangeX && lockX == false) { transform1.position.X -= 0.2f; winTimer = 0.6f; }
                }
                if (key == Key.D)
                {
                    if (transform1.position.X <= maxRangeX && lockX == false) { transform1.position.X += 0.2f; winTimer = 0.6f; }
                }
                if (key == Key.W)
                {
                    if (transform1.position.Y <= maxRangeY && lockY == false) { transform1.position.Y += 0.2f; winTimer = 0.6f; }
                }
                if (key == Key.S)
                {
                    if (transform1.position.Y >= minRangeY && lockY == false) { transform1.position.Y -= 0.2f; winTimer = 0.6f; }
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
                        if (transform1.rotation.X < maxRotation && lockRotation == false) { transform1.rotation.X += 0.03f; winTimer = 0.6f; }
                    }
                    if (keyboard.IsKeyPressed(Key.Q))
                    {
                        if (transform1.rotation.X > minRotation && lockRotation == false) { transform1.rotation.X -= 0.03f; winTimer = 0.6f; }
                    }
                }

            }

            if (gamePhase == 1)
            {
                gameTimer -= (float)delta;
                if (winTimer > 0.1)
                {
                    scoreTimer += (float)delta;
                    winTimer -= (float)delta;
                }
                if (winTimer < 0.1f)
                {
                    signal = true;
                    double rotationDifference = (double)System.Math.Round(Math.Abs(transform1.rotation.X - transform2.rotation.X) % 6.2825, 3);
                    if (transform1.position.Y - transform2.position.Y >= -0.1f && transform1.position.Y - transform2.position.Y <= 0.1f)
                    {
                        lockY = true;
                        if (transform1.position.X - transform2.position.X >= 2.78f && transform1.position.X - transform2.position.X <= 2.81f)
                        {
                            if (rotationDifference <= 0.18 && rotationDifference >= -0.18)
                            {
                                float tempPoints = (float)System.Math.Round(100 - (scoreTimer * 10));
                                if (tempPoints < 15) { points += 15; tempPoints = 15; }
                                else { points += tempPoints; }
                                gameTimer += 6.0f;
                                Console.WriteLine($"Posição Correta! Você recebeu {tempPoints} seu total agora é de {points}.");
                                RandomizeCube();
                            }
                            else if (rotationDifference <=  -6.03f || rotationDifference >= 6.03f)
                            {
                                float tempPoints = (float)System.Math.Round(100 - (scoreTimer * 10));
                                if(tempPoints < 15) { points += 15; tempPoints = 15; }
                                else{ points += tempPoints; }
                                gameTimer += 6.0f;
                                Console.WriteLine($"Posição Correta! Você recebeu {tempPoints} seu total agora é de {points}.");
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
                else { signal = false; }

                if (gameTimer <= 0.05f)
                {
                    attempts++;
                    lastScore = points;
                    if(lastScore > highScore) { highScore = lastScore; }
                    menuPrint = 0;
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

                mesh.Draw(transformMenu, materialMenu, camera);
                while(menuPrint < 1)
                {
                    Console.Clear();
                    Console.WriteLine("--------------------------------- JOGO DE IMITAÇÃO CG ---------------------------------");
                    Console.WriteLine("                               Aperte Enter para Iniciar                               ");
                    Console.WriteLine(); Console.WriteLine();
                    Console.WriteLine("PONTUAÇÕES: ");
                    Console.WriteLine($"High Score: {highScore}    -  Last Score: {lastScore}");
                    Console.WriteLine($"Número de tentativas: {attempts}");
                    Console.WriteLine(); Console.WriteLine();
                    Console.WriteLine("CONTROLES: ");
                    Console.WriteLine("W - A - S - D: Mover o Cubo.");
                    Console.WriteLine("Q - E: Rotacionar o Cubo.");
                    Console.WriteLine("R: Randomizar o Cubo-Alvo.");
                    Console.WriteLine(); Console.WriteLine();
                    Console.WriteLine("Instruções: ");
                    Console.WriteLine(" - Seu Objetivo é colocar o Cubo da direita na mesma posição que o Cubo da Esquerda está.");
                    Console.WriteLine(" - Seja rápido, caso o timer zerar o jogo acaba.");
                    Console.WriteLine(" - Sua velocidade também afeta quantos pontos você irá receber.");
                    Console.WriteLine("-----------------------------------------------------------------------------------------------");
                    Console.WriteLine(" - Para a posição do cubo ser considerada é necessário esperar um pequeno buffer.");
                    Console.WriteLine(" - Quando o buffer não estiver em efeito um sinal verde vai aparecer no canto da tela, indicando que uma posição certa estaria travada.");
                    Console.WriteLine(" - Quando os elementos(X, Y e Rotação) estiverem corretos seus controles serão desabilitados, travando o Cubo na posição correta.");
                    Console.WriteLine(" - Utilize a orientação das texturas para colocar o Cubo na rotação correta.");
                    Console.WriteLine(" - Duas das Faces frontais do Cubo são iguais, nos casos em que não é possível orientar o Cubo pelas outras faces e a rotação parecer não travar, tente a outra face idêntica.");
                    Console.WriteLine(" - O Console irá mostrar sua pontuação.");
                    Console.WriteLine(); Console.WriteLine();
                    Console.WriteLine("OBRIGADO POR JOGAR");

                    menuPrint++;
                }

            }
            else if (gamePhase == 1)
            {
                gl.ClearColor(0.85f, 0.85f, 0.85f, 1.0f);
                gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);//limpamos as cores e o depth buffer da tela

                mesh.Draw(transform1, material1, camera);
                mesh.Draw(transform2, material2, camera);
                mesh.Draw(transform3, diffuseMaterial, camera);
                if (signal) { mesh.Draw(transformSignal, greenDiffuseMaterial, camera); }
                mesh.Draw(transformTimer, redDiffuseMaterial, camera);
                transformTimer.scale.X = gameTimer * 0.03f;

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
            scoreTimer = 0;
        }
    }
}
