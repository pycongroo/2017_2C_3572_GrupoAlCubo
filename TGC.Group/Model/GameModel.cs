using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Geometry;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Core.Utils;
using TGC.Core.Example;
using TGC.Group.Camera;
using System;
using System.Collections.Generic;
using TGC.Core.Collision;
using TGC.Core.Fog;
using TGC.Core.Shaders;
using TGC.Core.Sound;
using TGC.Core.Text;

namespace TGC.Group.Model
{
    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {
        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
        }

        private float time;
        private float distance2nearEnemy;

        private Microsoft.DirectX.Direct3D.Surface depthStencil; // Depth-stencil buffer
        private Microsoft.DirectX.Direct3D.Surface depthStencilOld;
        private Microsoft.DirectX.Direct3D.Effect postEffect;
        private Microsoft.DirectX.Direct3D.Surface pOldRT;
        private Microsoft.DirectX.Direct3D.Texture renderTarget2D;
        private Microsoft.DirectX.Direct3D.VertexBuffer screenQuadVB;

        private TgcText2D titulo;
        private TgcText2D instruccionesText1;
        private TgcText2D instruccionesText2;
        private TgcText2D instruccionesText3;
        private TgcText2D restartText;
        private TgcText2D loseText;
        private TgcText2D winText;
        private int paredesXY = 32; //potencia de 2
        private int paredesYZ = 32; //potencia de 2
        private TgcScene[,] currentScene;
        private List<Vector2> velas;
        private List<Vector2> llaves;
        private List<Vector2> esqueletos;
        private bool[,] skeletonsAp;
        private bool[,] candleAp;
        private bool[,] keyAp;
        private TgcPlane Piso { get; set; }
        private TgcPlane Techo { get; set; }
        private TgcBox playerBBox { get; set; }
        private bool godMode;
        private bool bMode;
        private bool paused;
        //private TgcBox[] ParedXY;
        //private TgcBox[] ParedNXY;
        //private TgcBox[] ParedYZ;
        //private TgcBox[] ParedNYZ;
        private TgcPlane[,] DecoWallXY;
        private TgcPlane[,] DecoWallYZ;
        //private TgcBox[,] ParedInternaXY;
        //private TgcBox[,] ParedInternaYZ;
        //private bool[,] wallMatXY;
        //private bool[,] wallMatYZ;
        private Maze laberinto;
        private static readonly int ParedHorizontal = 1;
        private static readonly int ParedVertical = 2;
        private List<TgcBox> paredes = new List<TgcBox>();
        private float anchoPared = 512;
        private float altoPared = 512;
        private float grosorPared = 50;
        private List<TgcBox> obstaculos;
        private bool collide;
        private bool enemColl;
        private bool beggining;
        private bool lose;
        private bool win;
        private bool puertaTouch;
        private TgcText2D puertaText;
        private Random random;
        //booleanos para pruebas
        private bool bTrue = true;
        private bool bFalse = false;
        private TgcFpsCamera camaraFps;
        //private TgcArrow lookingArrow { get; set; }
        private float ligthIntensity;
        private int objCount;
        private int candleCount;
        private int keyCount;
        private int totalKeys;
        private int visibilityLen;//distancia de renderizado
        private int posiX;
        private int posfX;
        private int posiZ;
        private int posfZ;
        private double rangoDiagAngle;
        private bool optimizationEnabled;
        private List<Enemigo> enemigos = new List<Enemigo>();
        private TgcScene salida;
        private Vector3 exitPos;
        private int minKeys;

        private TgcBox ligthBox { get; set; }

        //Caja que se muestra en el ejemplo.
        //private TgcBox Box { get; set; }

        private Microsoft.DirectX.Direct3D.Effect efecto;

        private List<Tgc3dSound> sonidos;
        private TgcStaticSound loseSound;
        private TgcStaticSound winSound;
        private bool winsoundplayed;

        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aquí todo el código de inicialización: cargar modelos, texturas, estructuras de optimización, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {
            distance2nearEnemy = 9999999999f;
            time = 0f;
            //Se crean 2 triangulos (o Quad) con las dimensiones de la pantalla con sus posiciones ya transformadas
            // x = -1 es el extremo izquiedo de la pantalla, x = 1 es el extremo derecho
            // Lo mismo para la Y con arriba y abajo
            // la Z en 1 simpre
            Microsoft.DirectX.Direct3D.CustomVertex.PositionTextured[] screenQuadVertices =
            {
                new Microsoft.DirectX.Direct3D.CustomVertex.PositionTextured(-1, 1, 1, 0, 0),
                new Microsoft.DirectX.Direct3D.CustomVertex.PositionTextured(1, 1, 1, 1, 0),
                new Microsoft.DirectX.Direct3D.CustomVertex.PositionTextured(-1, -1, 1, 0, 1),
                new Microsoft.DirectX.Direct3D.CustomVertex.PositionTextured(1, -1, 1, 1, 1)
            };
            //vertex buffer de los triangulos
            screenQuadVB = new Microsoft.DirectX.Direct3D.VertexBuffer(typeof(Microsoft.DirectX.Direct3D.CustomVertex.PositionTextured),
                4, D3DDevice.Instance.Device, Microsoft.DirectX.Direct3D.Usage.Dynamic | Microsoft.DirectX.Direct3D.Usage.WriteOnly,
                Microsoft.DirectX.Direct3D.CustomVertex.PositionTextured.Format, Microsoft.DirectX.Direct3D.Pool.Default);
            screenQuadVB.SetData(screenQuadVertices, 0, Microsoft.DirectX.Direct3D.LockFlags.None);

            //Creamos un Render Targer sobre el cual se va a dibujar la pantalla
            renderTarget2D = new Microsoft.DirectX.Direct3D.Texture(D3DDevice.Instance.Device,
                D3DDevice.Instance.Device.PresentationParameters.BackBufferWidth
                , D3DDevice.Instance.Device.PresentationParameters.BackBufferHeight, 1, Microsoft.DirectX.Direct3D.Usage.RenderTarget,
                Microsoft.DirectX.Direct3D.Format.X8R8G8B8, Microsoft.DirectX.Direct3D.Pool.Default);

            //Creamos un DepthStencil que debe ser compatible con nuestra definicion de renderTarget2D.
            depthStencil =
                D3DDevice.Instance.Device.CreateDepthStencilSurface(
                    D3DDevice.Instance.Device.PresentationParameters.BackBufferWidth,
                    D3DDevice.Instance.Device.PresentationParameters.BackBufferHeight,
                    Microsoft.DirectX.Direct3D.DepthFormat.D24S8, Microsoft.DirectX.Direct3D.MultiSampleType.None, 0, true);
            depthStencilOld = D3DDevice.Instance.Device.DepthStencilSurface;
            //Cargar shader con efectos de Post-Procesado
            postEffect = TgcShaders.loadEffect(ShadersDir + "PostProcess.fx");

            //Configurar Technique dentro del shader
            postEffect.Technique = "CustomTechnique";

            //algoritmo de juego
            laberinto = new Maze(paredesXY, paredesYZ);
            obstaculos = new List<TgcBox>();
            collide = false;
            visibilityLen = 4;
            rangoDiagAngle = 10;
            optimizationEnabled = true;
            currentScene = new TgcScene[paredesXY, paredesYZ];
            skeletonsAp = new bool[paredesXY, paredesYZ];
            candleAp = new bool[paredesXY, paredesYZ];
            keyAp = new bool[paredesXY, paredesYZ];
            velas = new List<Vector2>();
            llaves = new List<Vector2>();
            esqueletos = new List<Vector2>();
            //ParedXY = new TgcBox[paredesXY];
            //ParedNXY = new TgcBox[paredesXY];
            //ParedYZ = new TgcBox[paredesYZ];
            //ParedNYZ = new TgcBox[paredesYZ];
            DecoWallXY = new TgcPlane[paredesXY -1, paredesXY];
            DecoWallYZ = new TgcPlane[paredesYZ -1, paredesYZ];
            //ParedInternaXY = new TgcBox[paredesXY - 1, paredesXY];
            //ParedInternaYZ = new TgcBox[paredesYZ - 1, paredesYZ];
            //wallMatXY = new bool[paredesXY - 1, paredesXY];
            //wallMatYZ = new bool[paredesYZ - 1, paredesYZ];
            //playerBBox = new TgcSphere(125,texturapiso,new Vector3(0,0,0));
            //Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;
            godMode = false;
            bMode = false;
            paused = false;
            ligthIntensity = 50f;
            objCount = 0;
            candleCount = 0;
            keyCount = 0;
            totalKeys = 0;
            puertaTouch = false;
            winsoundplayed = false;
            minKeys = 15;

            //instrucciones al inicio del juego
            instruccionesText1 = new TgcText2D();
            instruccionesText1.Text = "Utiliza el mouse y W/A/S/D para moverte. Consigue " + minKeys + " logos para abrir la puerta y salir del laberinto.";
            instruccionesText1.Position = new System.Drawing.Point(5,210);
            instruccionesText1.Color = Color.Red;
            instruccionesText1.changeFont(new System.Drawing.Font(FontFamily.GenericMonospace, 35, FontStyle.Regular));

            instruccionesText2 = new TgcText2D();
            instruccionesText2.Text = "Si tu luz se acaba, pierdes. Cruzarte con el esqueleto andante disminuira tu luz. Puedes recargar tu luz recolectando Velas.";
            instruccionesText2.Position = new System.Drawing.Point(5, 370);
            instruccionesText2.Color = Color.Green;
            instruccionesText2.changeFont(new System.Drawing.Font(FontFamily.GenericMonospace, 35, FontStyle.Regular));

            instruccionesText3 = new TgcText2D();
            instruccionesText3.Text = "Presiona SPACE para comenzar.";
            instruccionesText3.Position = new System.Drawing.Point(50, 520);
            instruccionesText3.Color = Color.Blue;
            instruccionesText3.changeFont(new System.Drawing.Font(FontFamily.GenericMonospace, 35, FontStyle.Regular));

            titulo = new TgcText2D();
            titulo.Text = "DREADMAZE";
            titulo.Position = new System.Drawing.Point(40, 80);
            titulo.Color = Color.Yellow;
            titulo.changeFont(new System.Drawing.Font(FontFamily.GenericMonospace,80, FontStyle.Regular));

            loseText = new TgcText2D();
            loseText.Text = "GAME OVER";
            loseText.Position = new System.Drawing.Point(50, 300);
            loseText.Color = Color.Red;
            loseText.changeFont(new System.Drawing.Font(FontFamily.GenericMonospace, 80, FontStyle.Regular));

            winText = new TgcText2D();
            winText.Text = "Felicitaciones! Has logrado escapar!";
            winText.Position = new System.Drawing.Point(50, 300);
            winText.Color = Color.YellowGreen;
            winText.changeFont(new System.Drawing.Font(FontFamily.GenericMonospace, 80, FontStyle.Regular));

            restartText = new TgcText2D();
            restartText.Text = "Presiona R para reiniciar el juego";
            restartText.Position = new System.Drawing.Point(50 , 500);
            restartText.Color = Color.Orange;
            restartText.changeFont(new System.Drawing.Font(FontFamily.GenericMonospace,40,FontStyle.Bold));

            puertaText = new TgcText2D();
            puertaText.Text = "Necesitas " + minKeys + " logos para abrir la puerta!";
            puertaText.Position = new System.Drawing.Point(50, 500);
            puertaText.Color = Color.DarkCyan;
            puertaText.changeFont(new System.Drawing.Font(FontFamily.GenericMonospace, 40, FontStyle.Bold));

            //Textura de la carperta Media. Game.Default es un archivo de configuracion (Game.settings) util para poner cosas.
            //Pueden abrir el Game.settings que se ubica dentro de nuestro proyecto para configurar.
            var pathTexturaCaja = MediaDir + Game.Default.TexturaCaja;
            var pathTexturaPiso = MediaDir + "rock_floor2.jpg";
            //var pathTexturaPared = MediaDir + "brick1_1.jpg";
            var pathTexturaDeco = MediaDir + "cartelera2.jpg";
            var sizeDecoXY = new Vector3(300, 300, 0);
            var sizeDecoYZ = new Vector3(0, 300, 300);
            //var sizeParedXY = new Vector3(anchoPared, altoPared, grosorPared);
            //var sizeParedYZ = new Vector3(grosorPared, altoPared, anchoPared);
            var sizePiso = new Vector3(512*paredesXY, 20, 512*paredesYZ);
            var relDecoPosXY = new Vector3(100, 100, 10);
            var relDecoPosYZ = new Vector3(10, 100, 100);

            //Cargamos una textura, tener en cuenta que cargar una textura significa crear una copia en memoria.
            //Es importante cargar texturas en Init, si se hace en el render loop podemos tener grandes problemas si instanciamos muchas.
            var texture = TgcTexture.createTexture(pathTexturaCaja);
            var texturePiso = TgcTexture.createTexture(pathTexturaPiso);
            var texturaTecho = TgcTexture.createTexture(pathTexturaPiso);
            //var texturaPared = TgcTexture.createTexture(pathTexturaPared);
            var texturaDeco = TgcTexture.createTexture(pathTexturaDeco);

            efecto = TgcShaders.Instance.TgcMeshPointLightShader;
            //efecto = TgcShaders.Instance.TgcMeshSpotLightShader;

            Piso = new TgcPlane(new Vector3(0, 0, 0), sizePiso, TgcPlane.Orientations.XZplane, texturePiso);
            Techo = new TgcPlane(new Vector3(0, 511, 0), sizePiso, TgcPlane.Orientations.XZplane, texturaTecho);
            FabricarParedes();
            obstaculos.AddRange(this.paredes);
            
            random = new Random();
            //Creamos una caja 3D ubicada de dimensiones (5, 10, 5) y la textura como color.
            var size = new Vector3(100, 100, 100);
            //Construimos una caja según los parámetros, por defecto la misma se crea con centro en el origen y se recomienda así para facilitar las transformaciones.
            //Box = TgcBox.fromSize(size, texture);
            //Posición donde quiero que este la caja, es común que se utilicen estructuras internas para las transformaciones.
            //Entonces actualizamos la posición lógica, luego podemos utilizar esto en render para posicionar donde corresponda con transformaciones.
            //Box.Position = new Vector3(512 * posX, 100, 512 * posZ);
            
            //Suelen utilizarse objetos que manejan el comportamiento de la camara.
            //Lo que en realidad necesitamos gráficamente es una matriz de View.
            //El framework maneja una cámara estática, pero debe ser inicializada.
            //Posición de la camara.
            var cameraPosition = new Vector3(100, 200, 220);
            //playerBBox.Position = cameraPosition;
            playerBBox = new TgcBox();
            playerBBox = TgcBox.fromSize(cameraPosition, new Vector3(80,80,80));
            //Quiero que la camara mire hacia el origen (0,0,0).
            var lookAt = Vector3.Empty;
            var moveSpeed = 850f;
            var jumpSpeed = 500f;
            enemColl = false;
            lose = false;
            beggining = true;
            win = false;

            sonidos = new List<Tgc3dSound>();
            Tgc3dSound sound;

            sound = new Tgc3dSound(MediaDir + "sound\\viento helado.wav", Vector3.Empty, DirectSound.DsDevice);
            sound.MinDistance = 220f;
            sound.play(true);
            sonidos.Add(sound);
            sound = new Tgc3dSound(MediaDir + "sound\\risa infantil.wav", Vector3.Empty, DirectSound.DsDevice);
            sound.MinDistance = 2500f;
            sonidos.Add(sound);

            loseSound = new TgcStaticSound();
            loseSound.loadSound(MediaDir + "sound\\risa de maníaco.wav",DirectSound.DsDevice);

            winSound = new TgcStaticSound();
            winSound.loadSound(MediaDir + "sound\\puerta, abrir.wav", DirectSound.DsDevice);

            DirectSound.ListenerTracking = playerBBox;

            var esquletoSize = new Vector3(5,5,5);
            var candleSize = new Vector3(2, 2, 2);
            var keySize = new Vector3(2, 2, 2);
            var gateSize = new Vector3(10, 7, 28);
            var textura = TgcTexture.createTexture(MediaDir + "Puerta\\Textures\\Puerta.jpg");
            //exitPos = new Vector3(anchoPared * (paredesXY-0.5f), altoPared * 0.5f, anchoPared * paredesXY);
            exitPos = new Vector3(random.Next(0,this.laberinto.Height) + 200 ,10, random.Next(0, this.laberinto.Height));
            
            var loader = new TgcSceneLoader();

            salida = loader.loadSceneFromFile(MediaDir + "Puerta\\Puerta-TgcScene.xml");
            salida.Meshes[0].AutoTransformEnable = true;
            salida.Meshes[0].Position = exitPos;
            salida.Meshes[0].Scale = gateSize;
            //salida.Meshes[0].Enabled = true;

            for (int i = 0; i < paredesXY; i++)
            {
                for (int j = 0; j < paredesYZ; j++)
                {
                    if (random.Next(0, 10) < 1) {
                        loadMesh(MediaDir + "EsqueletoHumano\\Esqueleto-TgcScene.xml",i,j);
                        //No recomendamos utilizar AutoTransform, en juegos complejos se pierde el control. mejor utilizar Transformaciones con matrices.
                        currentScene[i, j].Meshes[0].AutoTransformEnable = true;
                        //Desplazarlo
                        currentScene[i, j].Meshes[0].move(512 * i + 256, 0, 512 * j + 256);
                        currentScene[i, j].Meshes[0].Scale = esquletoSize;
                        currentScene[i, j].Meshes[0].Rotation = new Vector3(0, random.Next(0, 360), 0);
                        skeletonsAp[i, j] = true;
                        objCount += 1;
                        esqueletos.Add(new Vector2(i, j));
                    }
                    else
                    {
                        skeletonsAp[i, j] = false;
                        if (random.Next(0,20) < 2){
                            loadMesh(MediaDir + "Vela\\Vela-TgcScene.xml",i,j);
                            currentScene[i, j].Meshes[0].AutoTransformEnable = true;
                            currentScene[i, j].Meshes[0].move(512 * i + 256, 150, 512 * j + 256);
                            currentScene[i, j].Meshes[0].Scale = candleSize;
                            currentScene[i, j].Meshes[0].Rotation = new Vector3(0, random.Next(0, 360), 0);
                            candleAp[i, j] = true;
                            candleCount += 1;
                            velas.Add(new Vector2(i, j));
                        }
                        else
                        {
                            candleAp[i, j] = false;

                            if (random.Next(0, 20) < 1)
                            {
                                loadMesh(MediaDir + "LogoTGC\\LogoTGC-TgcScene.xml", i, j);
                                currentScene[i, j].Meshes[0].AutoTransformEnable = true;
                                currentScene[i, j].Meshes[0].move(512 * i + 256, 150, 512 * j + 256);
                                currentScene[i, j].Meshes[0].Scale = keySize;
                                currentScene[i, j].Meshes[0].Rotation = new Vector3(0, random.Next(0, 360), 0);
                                keyAp[i, j] = true;
                                totalKeys += 1;
                                llaves.Add(new Vector2(i, j));
                            }
                            else
                            {
                                keyAp[i, j] = false;
                            }
                        }
                    }
                }
            }

            //fija la camara en la dimension Y en true. Por el momento si se activa no se puede saltar ni agacharse ( seria necesario en nuestro juego?)
            var fixCamY = true;

            camaraFps = new TgcFpsCamera(cameraPosition, moveSpeed, jumpSpeed, fixCamY, Input);
            Camara = camaraFps;
            //Configuro donde esta la posicion de la camara y hacia donde mira.
            //Camara.SetCamera(cameraPosition, lookAt);
            //Internamente el framework construye la matriz de view con estos dos vectores.
            //Luego en nuestro juego tendremos que crear una cámara que cambie la matriz de view con variables como movimientos o animaciones de escenas.

            ligthBox = TgcBox.fromSize(cameraPosition, new Vector3(20,20,20));
            var start = 0;
            var end = 9;
            while (start < end) {
                CrearEnemigos();
                sonidos.Add(sound);
                start++;
            }
        }

        private void CrearEnemigos()
        {
            // Elimino enemigos anteriores si existieran.
            TgcMesh enemigoMesh = new TgcSceneLoader().loadSceneFromFile(MediaDir + "EsqueletoHumano2\\Esqueleto2-TgcScene.xml").Meshes[0];
            enemigos.Add(new Enemigo(enemigoMesh, 420, this.laberinto, new Vector3(5, 5, 5)));

        }

        public TgcBox CrearPared(int orientacion)
        {
            float largo = 512;
            float alto = 512;
            float ancho = 50;
            var size = orientacion == ParedVertical ? new Vector3(largo, alto, ancho) :
                new Vector3(ancho, alto, largo);
            var textura = TgcTexture.createTexture(MediaDir + "brick1_1.jpg");
            return TgcBox.fromSize(size, textura);

        }

        public void UbicarPared(TgcBox pared, CellState posicion, Point punto)
        {
            Vector3 ubicacion = new Vector3(0, 0, 0);
            if (posicion == CellState.Top || posicion == CellState.Bottom)
            {
                ubicacion = new Vector3(punto.X * 512, 0.5f * 512, (punto.Y + 0.5f) * 512);
            }
            if (posicion == CellState.Left || posicion == CellState.Right)
            {
                ubicacion = new Vector3((punto.X + 0.5f) * 512, 0.5f * 512, punto.Y * 512);
            }
            pared.Position = ubicacion;
        }

        public void FabricarParedes()
        {
            TgcBox pared = null;
            for (var y = 0; y < this.laberinto.Height; y++)
            {
                for (var x = 0; x < this.laberinto.Width; x++)
                {

                    if (this.laberinto[x, y].HasFlag(CellState.Top) && y>0)
                    {
                        pared = CrearPared(ParedHorizontal);
                        UbicarPared(pared, CellState.Top, new Point(y, x));
                        paredes.Add(pared);

                    }
                    if (this.laberinto[x, y].HasFlag(CellState.Left) && x>0)
                    {
                        pared = CrearPared(ParedVertical);
                        UbicarPared(pared, CellState.Left, new Point(y, x));
                        paredes.Add(pared);

                    }

                }

            }
            for (var x = 0; x < this.laberinto.Width; x++)
            {
                pared = CrearPared(ParedHorizontal);
                UbicarPared(pared, CellState.Bottom, new Point(this.laberinto.Height, x));
                paredes.Add(pared);
                pared = CrearPared(ParedHorizontal);
                UbicarPared(pared, CellState.Top, new Point(0, x));
                paredes.Add(pared);
            }
            for (var y = 0; y < this.laberinto.Height; y++)
            {
                pared = CrearPared(ParedVertical);
                UbicarPared(pared, CellState.Right, new Point(y, this.laberinto.Width));
                paredes.Add(pared);
                pared = CrearPared(ParedVertical);
                UbicarPared(pared, CellState.Left, new Point(y, 0));
                paredes.Add(pared);
            }

        }

        public bool estaEnElMapa()
        {
            bool estaEnRangoX = camaraFps.Position.X > 0 && camaraFps.Position.X < paredesXY * anchoPared;
            bool estaEnRangoY = camaraFps.Position.Y > 0 && camaraFps.Position.Y < paredesYZ * anchoPared;
            //Console.WriteLine(estaEnRangoX);
            //Console.WriteLine(estaEnRangoY);
            return estaEnRangoX && estaEnRangoY;
        }

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la lógica de computo del modelo, así como también verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        public override void Update()
        {
            PreUpdate();


            if (beggining)
            {
                if (Input.keyPressed(Key.Space))
                {
                    beggining = false;
                    camaraFps.LockCam = true;
                    camaraFps.playing = true;
                }
            }

            if (lose)
            {
                loseSound.play();
                //lose = false;
            }
            if (optimizationEnabled && estaEnElMapa())
            {
                Vector3 dirView = camaraFps.LookAt - camaraFps.Position;
                float tan = dirView.Z / dirView.X;
                //System.Console.WriteLine("Tan: " + tan);
                double anguloVista = Math.Atan2(dirView.Z, dirView.X) * (180 / Math.PI);
                //System.Console.WriteLine("Angle Tan2: " + anguloVista);
                genRanges((int)(camaraFps.Position.X / anchoPared), (int)(camaraFps.Position.Z / anchoPared), anguloVista);
            } else
            {
                posiX = 0;
                posfX = paredesXY;
                posiZ = 0;
                posfZ = paredesYZ;
            }

            //var currentCameraPos = Camara.Position;
            //playerBBox.Position = currentCameraPos;

            var moving = false;

            //Adelante
            if (Input.keyDown(Key.W) || Input.keyDown(Key.A) || Input.keyDown(Key.S) || Input.keyDown(Key.D))
            {
                moving = true;
            }

            if (Input.keyPressed(Key.G) && !beggining && !win && !paused && !lose)
            {
                camaraFps.GodMode();
                godMode = !godMode;
            }

            if ((lose && Input.keyPressed(Key.M)) || (camaraFps.LockCam && Input.keyPressed(Key.M)))
            {
                this.reset();
            }

            /*if ((lose && Input.keyPressed(Key.E)) || (!camaraFps.LockCam && Input.keyPressed(Key.E)))
            {
                Program.Terminate();
            }*/

            if (Input.keyPressed(Key.B)) bMode = !bMode;

            if (Input.keyPressed(Key.P)) ligthIntensity = 50f;

            if (Input.keyPressed(Key.Escape) && !beggining && !win && !lose) paused = !paused;

            if ((lose || paused || win) && Input.keyPressed(Key.R) && !godMode && !beggining) reset();

            if(ligthIntensity > 0 && !beggining && !win && !godMode && !paused)ligthIntensity -= 0.005f;

            if (godMode && Input.keyPressed(Key.UpArrow))
            {
                keyCount += 1;
            }

            if (godMode && Input.keyPressed(Key.DownArrow))
            {
                keyCount -= 1;
            }
            float auxDist = 9999999999f;
            var count = 0;
            foreach(Enemigo enemigo in enemigos)
            {
                bool result = false;
                result = TgcCollisionUtils.testAABBAABB(playerBBox.BoundingBox, enemigo.representacion.BoundingBox);
                if (result && enemColl)
                {
                    ligthIntensity -= 20;
                    enemColl = false;
                }
                if (!result) enemColl = true;
                sonidos[count].Position = enemigo.representacion.Position;
                count++;
                float tDist = (enemigo.representacion.Position - playerBBox.Position).Length();
                if (tDist < auxDist)
                {
                    auxDist = tDist;
                }
            }
            distance2nearEnemy = auxDist;
            //Console.WriteLine("Distancia enemigo mas cercano");
            //Console.WriteLine(distance2nearEnemy);
            if (random.Next(0, 10000) < 1) {
                sonidos[count].Position = new Vector3(random.Next(0,8000), 200, random.Next(0, 8000));
                sonidos[count].play(false);
            }

            if (ligthIntensity <= 0) lose = true;

            if (moving && !godMode)
            {
                var lastPos = playerBBox.Position;
                var currentCameraPos = camaraFps.Position;
                playerBBox.Position = currentCameraPos;
                //Detectar colisiones
                var currCollide = false;
                foreach (var obstaculo in obstaculos)
                {
                    bool result;
                    result = TgcCollisionUtils.testAABBAABB(playerBBox.BoundingBox, obstaculo.BoundingBox);
                    
                    if (result)
                    {
                        currCollide = true;
                        break;
                    }
                }

                if (collide != currCollide)
                {
                    collide = currCollide;
                    camaraFps.UpdateCollision(collide,lastPos);
                }

                if(currCollide)
                {
                    currentCameraPos = camaraFps.Position;
                    playerBBox.Position = currentCameraPos;

                }
                foreach(var vela in velas)
                {
                    var i =  (int)vela.X;
                    var j = (int)vela.Y;
                    if (TgcCollisionUtils.testAABBAABB(playerBBox.BoundingBox, currentScene[i, j].Meshes[0].BoundingBox))
                    {
                        ligthIntensity = 50f;
                        candleCount -= 1;
                        currentScene[i, j].Meshes[0].dispose();
                        candleAp[i, j] = false;
                        velas.Remove(vela);
                        break;
                    }
                }
                foreach (var llave in llaves)
                {
                    var i = (int)llave.X;
                    var j = (int)llave.Y;
                    if (TgcCollisionUtils.testAABBAABB(playerBBox.BoundingBox, currentScene[i, j].Meshes[0].BoundingBox))
                    {
                        //ligthIntensity = 50f;
                        keyCount += 1;
                        totalKeys -= 1;
                        currentScene[i, j].Meshes[0].dispose();
                        keyAp[i, j] = false;
                        llaves.Remove(llave);
                        break;
                    }
                }

                puertaTouch = TgcCollisionUtils.testAABBAABB(playerBBox.BoundingBox, salida.Meshes[0].BoundingBox);

                if (puertaTouch && keyCount >= minKeys)
                {
                    win = true;
                    camaraFps.LockCam = false;
                    camaraFps.playing = false;
                }
            }

            //Si no se esta moviendo, activar animacion de Parado
            else
            {
                //personaje.playAnimation("Parado", true);
            }

            ligthBox.Position = camaraFps.Position;

            if (!lose && !win && !beggining && !paused)
            {
                List<Enemigo> aRemover = new List<Enemigo>();
                foreach (Enemigo enemigo in this.enemigos)
                {
                    try
                    {
                        enemigo.Mover(ElapsedTime, camaraFps.Position);
                    }
                    catch (Exception e)
                    {
                        aRemover.Add(enemigo);

                    }

                }

                foreach (var item in aRemover)
                {
                    item.Dispose();
                    this.enemigos.Remove(item);
                    CrearEnemigos();
                }
            }
            
            
        }

        private void genRanges(int posPX, int posPZ, double angleView)
        {
            //System.Console.WriteLine("Position : (" + posPX + ", " + posPZ + ")");
            //obtener ind X min y max

            if (posPX - visibilityLen < 0)
            {
                posiX = 0;
                posfX = posPX + (visibilityLen + 1);
            }
            else
            {
                if (posPX + visibilityLen >= paredesXY)
                {
                    posiX = posPX - (visibilityLen + 1);
                    posfX = paredesXY;
                }
                else
                {
                    posfX = posPX + (visibilityLen + 1);
                    posiX = posPX - visibilityLen;
                }
            }

            if (posPZ - visibilityLen < 0)
            {
                posiZ = 0;
                posfZ = posPZ + (visibilityLen + 1);
            }
            else
            {
                if (posPZ + visibilityLen >= paredesYZ)
                {
                    posiZ = posPZ - (visibilityLen + 1);
                    posfZ = paredesYZ;
                }
                else
                {
                    posfZ = posPZ + (visibilityLen + 1);
                    posiZ = posPZ - visibilityLen;
                }
            }
            //System.Console.WriteLine("Pre angle-optimization:");
            //System.Console.WriteLine("X : [" + posiX + ", " + posfX + "]");
            //System.Console.WriteLine("Z : [" + posiZ + ", " + posfZ + "]");
            if (angleView>=(-45 + rangoDiagAngle) && angleView < (45 - rangoDiagAngle))
            {
                //System.Console.WriteLine("R");
                posiX = posPX;
            } else if (angleView>=(45 - rangoDiagAngle) && angleView < (45 + rangoDiagAngle))
            {
                //System.Console.WriteLine("R+U");
                posiX = posPX;
                posiZ = posPZ;
            } else if (angleView >= (45 + rangoDiagAngle) && angleView < (135 - rangoDiagAngle))
            {
                //System.Console.WriteLine("U");
                posiZ = posPZ;
            } else if (angleView >= (135 - rangoDiagAngle) && angleView < (135 + rangoDiagAngle))
            {
                //System.Console.WriteLine("L+U");
                posiZ = posPZ;
                posfX = posPX + 1;
            } else if (angleView >= (135 + rangoDiagAngle) || angleView < (-135 - rangoDiagAngle))
            {
                //System.Console.WriteLine("L");
                posfX = posPX + 1;
            } else if (angleView >= (-135 - rangoDiagAngle) && angleView < (-135 + rangoDiagAngle))
            {
                //System.Console.WriteLine("L+D");
                posfX = posPX + 1;
                posfZ = posPZ + 1;
            } else if (angleView >= (-135 + rangoDiagAngle) && angleView < (-45 - rangoDiagAngle))
            {
                //System.Console.WriteLine("D");
                posfZ = posPZ + 1;
            } else if (angleView >= (-45 - rangoDiagAngle) && angleView < (-45 + rangoDiagAngle))
            {
                //System.Console.WriteLine("R+D");
                posfZ = posPZ + 1;
                posiX = posPX;
            }
            //System.Console.WriteLine("Post angle-optimization:");
            //System.Console.WriteLine("X : [" + posiX + ", " + posfX + "]");
            //System.Console.WriteLine("Z : [" + posiZ + ", " + posfZ + "]");
            //System.Console.WriteLine("------------------------------------------------");
        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aquí todo el código referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>
        public void Render_juego()
        {
            //System.Console.WriteLine(exitGate.Position);
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            //PreRender();

            //D3DDevice.Instance.Device.Clear(Microsoft.DirectX.Direct3D.ClearFlags.Target, Color.Black, 1.0f, 0);

            if (puertaTouch && !win)
            {
                puertaText.render();
            }
            //TgcMesh auxMesh = null;
            //var ligthDir = Camara.LookAt;
            //ligthDir.Normalize();
            efecto.SetValue("lightColor", Color.LightYellow.ToArgb());
            efecto.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(ligthBox.Position));
            efecto.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
            //efecto.SetValue("spotLightDir", TgcParserUtils.vector3ToFloat3Array(ligthDir));
            efecto.SetValue("lightIntensity", ligthIntensity);
            efecto.SetValue("lightAttenuation", 0.29f);
            //efecto.SetValue("spotLightAngleCos", FastMath.ToRad(18));
            //efecto.SetValue("spotLightExponent", 11f);
            if (paused)
            {
                DrawText.drawText(" Hay " + enemigos.Count + " Enemigos. Con G ingresa en modo dios. La salida esta en la pos " + exitPos, 600, 300, Color.OrangeRed);
            }
            //Cargar variables de shader de Material. El Material en realidad deberia ser propio de cada mesh. Pero en este ejemplo se simplifica con uno comun para todos
            if (!godMode)
            {
                efecto.SetValue("materialEmissiveColor", Color.Black.ToArgb());
            } else
            {
                efecto.SetValue("materialEmissiveColor", Color.White.ToArgb());
            }
            efecto.SetValue("materialAmbientColor", Color.GhostWhite.ToArgb());
            efecto.SetValue("materialDiffuseColor", Color.White.ToArgb());
            efecto.SetValue("materialSpecularColor", Color.White.ToArgb());
            efecto.SetValue("materialSpecularExp", 10f);

            
            Piso.Effect = efecto;
            Piso.Technique = TgcShaders.Instance.getTgcMeshTechnique(Piso.toMesh("piso").RenderType);
            Piso.render();
            Techo.Effect = efecto;
            Techo.Technique = TgcShaders.Instance.getTgcMeshTechnique(Techo.toMesh("techo").RenderType);
            Techo.render();
            
            //renderGrid(posX, posZ);
            

            renderGrid();

            
            playerBBox.Transform = transformBox(playerBBox);
           
            if (bMode) playerBBox.BoundingBox.render();
            

            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            foreach(Enemigo enemigo in this.enemigos) 
            {
                
                enemigo.Render(efecto);
            }
            
            //PostRender();
        }

        public override void Render()
        {
            ClearTextures();

            //Cargamos el Render Targer al cual se va a dibujar la escena 3D. Antes nos guardamos el surface original
            //En vez de dibujar a la pantalla, dibujamos a un buffer auxiliar, nuestro Render Target.
            pOldRT = D3DDevice.Instance.Device.GetRenderTarget(0);
            var pSurf = renderTarget2D.GetSurfaceLevel(0);
            D3DDevice.Instance.Device.SetRenderTarget(0, pSurf);
            // Probar de comentar esta linea, para ver como se produce el fallo en el ztest
            // por no soportar usualmente el multisampling en el render to texture (en nuevas placas de video)
            D3DDevice.Instance.Device.DepthStencilSurface = depthStencil;
            D3DDevice.Instance.Device.Clear(Microsoft.DirectX.Direct3D.ClearFlags.Target | Microsoft.DirectX.Direct3D.ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            //Dibujamos la escena comun, pero en vez de a la pantalla al Render Target
            drawSceneToRenderTarget(D3DDevice.Instance.Device);

            //Liberar memoria de surface de Render Target
            pSurf.Dispose();

            //Si quisieramos ver que se dibujo, podemos guardar el resultado a una textura en un archivo para debugear su resultado (ojo, es lento)
            //TextureLoader.Save(this.ShadersDir + "render_target.bmp", ImageFileFormat.Bmp, renderTarget2D);

            //Ahora volvemos a restaurar el Render Target original (osea dibujar a la pantalla)
            D3DDevice.Instance.Device.SetRenderTarget(0, pOldRT);
            D3DDevice.Instance.Device.DepthStencilSurface = depthStencilOld;

            //Luego tomamos lo dibujado antes y lo combinamos con una textura con efecto de alarma
            drawPostProcess(D3DDevice.Instance.Device, ElapsedTime);
        }

        /// <summary>
        ///     Dibujamos toda la escena pero en vez de a la pantalla, la dibujamos al Render Target que se cargo antes.
        ///     Es como si dibujaramos a una textura auxiliar, que luego podemos utilizar.
        /// </summary>
        private void drawSceneToRenderTarget(Microsoft.DirectX.Direct3D.Device d3dDevice)
        {
            //Arrancamos el renderizado. Esto lo tenemos que hacer nosotros a mano porque estamos en modo CustomRenderEnabled = true
            d3dDevice.BeginScene();

            //Dibujamos todos los meshes del escenario
            //foreach (var m in meshes)
            //{
            //    m.Render();
            //}
            Render_juego();

            //Terminamos manualmente el renderizado de esta escena. Esto manda todo a dibujar al GPU al Render Target que cargamos antes
            d3dDevice.EndScene();
        }

        /// <summary>
        ///     Se toma todo lo dibujado antes, que se guardo en una textura, y se combina con otra textura, que en este ejemplo
        ///     es para generar un efecto de alarma.
        ///     Se usa un shader para combinar ambas texturas y lograr el efecto de alarma.
        /// </summary>
        private void drawPostProcess(Microsoft.DirectX.Direct3D.Device d3dDevice, float elapsedTime)
        {
            //Arrancamos la escena
            d3dDevice.BeginScene();

            //Cargamos para renderizar el unico modelo que tenemos, un Quad que ocupa toda la pantalla, con la textura de todo lo dibujado antes
            d3dDevice.VertexFormat = Microsoft.DirectX.Direct3D.CustomVertex.PositionTextured.Format;
            d3dDevice.SetStreamSource(0, screenQuadVB, 0);

            postEffect.Technique = "CustomTechnique";
            time += elapsedTime;
            float intensidad;
            if (distance2nearEnemy < 2000)
            {
                intensidad = 40f*(1f - distance2nearEnemy/2000);
            } else
            {
                intensidad = 0;
                //intensidad = 40f * (1f - distance2nearEnemy / 2000);
            }
            //Cargamos parametros en el shader de Post-Procesado
            postEffect.SetValue("render_target2D", renderTarget2D);
            postEffect.SetValue("time", time);
            postEffect.SetValue("intensidad", intensidad);

            //Limiamos la pantalla y ejecutamos el render del shader
            d3dDevice.Clear(Microsoft.DirectX.Direct3D.ClearFlags.Target | Microsoft.DirectX.Direct3D.ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            postEffect.Begin(Microsoft.DirectX.Direct3D.FX.None);
            postEffect.BeginPass(0);
            d3dDevice.DrawPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleStrip, 0, 2);
            postEffect.EndPass();
            postEffect.End();

            //Terminamos el renderizado de la escena
            RenderFPS();
            //Dibuja un texto por pantalla

            if (beggining)
            {
                instruccionesText1.render();
                instruccionesText2.render();
                instruccionesText3.render();
                titulo.render();
            }


            if (lose)
            {
                loseText.render();
                restartText.render();
            }

            if (win)
            {
                winText.render();
                restartText.render();
                if (!winsoundplayed)
                {
                    winSound.play();
                    winsoundplayed = true;
                }
            }

            if (!beggining && !win && !lose)
            {
                DrawText.drawText("Logos adquiridos: " + keyCount, 1200, 650, Color.Yellow);
                DrawText.drawText("Nivel de luz: " + Math.Truncate(ligthIntensity * 100 / 50) + "%", 50, 650, Color.Yellow);
            }

            if (godMode)
            {
                DrawText.drawText("logos disponibles: " + totalKeys, 1200, 660, Color.OrangeRed);
                DrawText.drawText("Velas disponibles: " + candleCount, 1200, 670, Color.OrangeRed);
                DrawText.drawText("Con la tecla P resetea la intensidad de la luz. Con las flechas arriba y abajo aumenta o decrese su conteo de logos.", 10, 40, Color.OrangeRed);
            }

            RenderAxis();
            d3dDevice.EndScene();
            d3dDevice.Present();
        }

        public void renderGrid()
        {
            //System.Console.WriteLine("Position : (" + posPX + ", " + posPZ + ")");
            TgcMesh auxMesh = null;
            //obtener ind X min y max

            //System.Console.WriteLine("X : [" + posiX + ", " + posfX + "]");
            //System.Console.WriteLine("Z : [" + posiZ + ", " + posfZ + "]");
            foreach (TgcBox pared in this.paredes)
            {
                Point cuadrante = new Point((int) pared.Position.X / 512, (int) pared.Position.Z / 512);
                if (cuadrante.X >= posiX && cuadrante.X <= posfX && cuadrante.Y >= posiZ && cuadrante.Y <= posfZ)
                {
                    pared.Transform = transformBox(pared);
                    pared.Effect = efecto;
                    auxMesh = pared.toMesh("pared");
                    pared.Technique = TgcShaders.Instance.getTgcMeshTechnique(auxMesh.RenderType);
                    pared.render();
                    auxMesh.dispose();
                    if (bMode) pared.BoundingBox.render();
                }
            }

            Point puertaPoint = new Point((int)salida.Meshes[0].Position.X / 512, (int)salida.Meshes[0].Position.Z / 512);
            if (puertaPoint.X >= posiX && puertaPoint.X <= posfX && puertaPoint.Y >= posiZ && puertaPoint.Y <= posfZ)
            {
                salida.Meshes[0].Effect = efecto;
                salida.Meshes[0].Technique = TgcShaders.Instance.getTgcMeshTechnique(salida.Meshes[0].RenderType);
                salida.renderAll();
            }

            for (int i = posiX; i < posfX; i++)
            {
                for (int j = posiZ; j < posfZ; j++)
                {
                    if (skeletonsAp[i, j])
                    {
                        currentScene[i, j].Meshes[0].Transform = Matrix.Scaling(new Vector3(100, 100, 100));
                        currentScene[i, j].Meshes[0].Effect = efecto;
                        currentScene[i, j].Meshes[0].Technique = TgcShaders.Instance.getTgcMeshTechnique(currentScene[i, j].Meshes[0].RenderType);
                        currentScene[i, j].Meshes[0].render();
                    }
                    if (candleAp[i, j])
                    {
                        currentScene[i, j].Meshes[0].Transform = Matrix.Scaling(new Vector3(100, 100, 100));
                        currentScene[i, j].Meshes[0].Effect = efecto;
                        currentScene[i, j].Meshes[0].Technique = TgcShaders.Instance.getTgcMeshTechnique(currentScene[i, j].Meshes[0].RenderType);
                        currentScene[i, j].Meshes[0].render();
                    }
                    if (keyAp[i, j])
                    {
                        currentScene[i, j].Meshes[0].Transform = Matrix.Scaling(new Vector3(80, 200, 80));
                        currentScene[i, j].Meshes[0].Effect = efecto;
                        currentScene[i, j].Meshes[0].Technique = TgcShaders.Instance.getTgcMeshTechnique(currentScene[i, j].Meshes[0].RenderType);
                        currentScene[i, j].Meshes[0].render();
                    }
                }
            }
        }

        /// <summary>
        ///     Se llama cuando termina la ejecución del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gráficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            //Dispose de la caja.
            //Box.dispose();
            playerBBox.dispose();
            salida.disposeAll();
            foreach (TgcBox pared in this.paredes)
            {
                pared.dispose();
            }
            loseSound.dispose();
            loseText.Dispose();
            winSound.dispose();
            restartText.Dispose();
            instruccionesText1.Dispose();
            instruccionesText3.render();
            instruccionesText2.render();
            titulo.Dispose();
            puertaText.Dispose();            

            for (int i = 0; i < paredesXY; i++)
            {
                for (int j = 0; j < paredesYZ; j++)
                {
                   if(skeletonsAp[i,j]) currentScene[i, j].Meshes[0].dispose();
                   if(candleAp[i,j]) currentScene[i, j].Meshes[0].dispose(); 
                   if(keyAp[i, j]) currentScene[i, j].Meshes[0].dispose();
                }
            }
            foreach (Enemigo enemigo in enemigos)
            {
                enemigo.Dispose();
            }
            this.enemigos.Clear();

        }

        /// <summary>
        ///     Carga una malla estatica de formato TGC
        /// </summary>
        private void loadMesh(string path, int i, int j)
        {
            //Dispose de escena anterior
            if (currentScene[i,j] != null)
            {
                currentScene[i,j].disposeAll();
            }

            //Cargar escena con herramienta TgcSceneLoader
            var loader = new TgcSceneLoader();
            currentScene[i,j] = loader.loadSceneFromFile(path);
            
        }

        private Matrix transformBox(TgcBox aBox)
        {
            return Matrix.Scaling(aBox.Scale) *
                            Matrix.RotationYawPitchRoll(aBox.Rotation.Y, aBox.Rotation.X, aBox.Rotation.Z) *
                            Matrix.Translation(aBox.Position);
        }

        private void reset()
        {
            /*camaraFps = new TgcFpsCamera(new Vector3(4850, 200, 220), 850f, 500f, true, Input);
            Camara = camaraFps;
            playerBBox.Position = Camara.Position;
            beggining = true;
            lose = false;
            win = false;
            paused = false;
            keyCount = 0;*/
            this.Dispose();
            this.Init();
            //ligthIntensity = 40;
                       
        }
    }
}