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
        private TgcText2D titulo;
        private TgcText2D instruccionesText1;
        private TgcText2D instruccionesText2;
        private TgcText2D instruccionesText3;
        private TgcText2D restartText;
        private TgcText2D loseText;
        private int paredesXY = 16; //potencia de 2
        private int paredesYZ = 16; //potencia de 2
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
        private int visibilityLen;//distancia de renderizado
        private int posiX;
        private int posfX;
        private int posiZ;
        private int posfZ;
        private double rangoDiagAngle;
        private bool optimizationEnabled;
        private List<Enemigo> enemigos = new List<Enemigo>();
        private TgcBox exitGate;

        private TgcBox ligthBox { get; set; }

        //Caja que se muestra en el ejemplo.
        //private TgcBox Box { get; set; }

        private Microsoft.DirectX.Direct3D.Effect efecto;

        private List<Tgc3dSound> sonidos;
        private TgcStaticSound loseSound;


        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aquí todo el código de inicialización: cargar modelos, texturas, estructuras de optimización, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {
            laberinto = new Maze(paredesXY, paredesYZ);
            obstaculos = new List<TgcBox>();
            collide = false;
            visibilityLen = 3;
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
            ligthIntensity = 50f;
            objCount = 0;
            candleCount = 0;
            keyCount = 0;

            //instrucciones al inicio del juego
            instruccionesText1 = new TgcText2D();
            instruccionesText1.Text = "Utiliza el mouse y W/A/S/D para moverte. Consigue 3 llaves para abrir la puerta y salir del laberinto.";
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

            restartText = new TgcText2D();
            restartText.Text = "Presiona R para reiniciar el juego";
            restartText.Position = new System.Drawing.Point(50 , 500);
            restartText.Color = Color.Orange;
            restartText.changeFont(new System.Drawing.Font(FontFamily.GenericMonospace,40,FontStyle.Bold));

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

            DirectSound.ListenerTracking = playerBBox;

            var esquletoSize = new Vector3(5,5,5);
            var candleSize = new Vector3(2, 2, 2);
            var keySize = new Vector3(2, 2, 2);
            var gateSize = new Vector3(anchoPared, altoPared, grosorPared * 2);
            var textura = TgcTexture.createTexture(MediaDir + "Puerta\\Textures\\Puerta.jpg");
            var exitPos = new Vector3(anchoPared * (paredesXY-0.5f), altoPared * 0.5f, anchoPared * paredesXY);
            exitGate = new TgcBox();
            exitGate.AutoTransformEnable = true;
            exitGate.Position = exitPos;
            exitGate.Size = gateSize;
            exitGate.setTexture(textura);
            exitGate.Enabled = true;

            var loader = new TgcSceneLoader();

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
                        if (random.Next(0,20) < 1){
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
                                loadMesh(MediaDir + "Vela\\Vela-TgcScene.xml", i, j);
                                currentScene[i, j].Meshes[0].AutoTransformEnable = true;
                                currentScene[i, j].Meshes[0].move(512 * i + 256, 150, 512 * j + 256);
                                currentScene[i, j].Meshes[0].Scale = keySize;
                                currentScene[i, j].Meshes[0].Rotation = new Vector3(0, random.Next(0, 360), 0);
                                keyAp[i, j] = true;
                                keyCount += 1;
                                velas.Add(new Vector2(i, j));
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
            CrearEnemigos();
        }

        private void CrearEnemigos()
        {
            // Elimino enemigos anteriores si existieran.
            foreach (var item in this.enemigos)
            {
                item.Dispose();
            }
            this.enemigos.Clear();
            TgcMesh enemigoMesh = new TgcSceneLoader().loadSceneFromFile(MediaDir + "EsqueletoHumano2\\Esqueleto2-TgcScene.xml").Meshes[0];
            enemigos.Add(new Enemigo(enemigoMesh, 420, this.laberinto.FindPath(new Point(random.Next(0,paredesXY-1), random.Next(0, paredesYZ - 1)), new Point(random.Next(0, paredesXY - 1), random.Next(0, paredesYZ - 1))), new Vector3(5, 5, 5)));

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
            if (optimizationEnabled)
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

            if (Input.keyPressed(Key.G))
            {
                camaraFps.GodMode();
                godMode = !godMode;
            }

            if ((lose && Input.keyPressed(Key.M)) || (camaraFps.LockCam && Input.keyPressed(Key.M)))
            {
                this.reset();
            }

            if ((lose && Input.keyPressed(Key.E)) || (!camaraFps.LockCam && Input.keyPressed(Key.E)))
            {
                Program.Terminate();
            }

            if (Input.keyPressed(Key.B)) bMode = !bMode;

            if (Input.keyPressed(Key.P)) ligthIntensity = 50f;

            if(ligthIntensity > 0 && !beggining && !win && !godMode)ligthIntensity -= 0.005f;

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
            }

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
                        ligthIntensity = 50f;
                        keyCount -= 1;
                        currentScene[i, j].Meshes[0].dispose();
                        keyAp[i, j] = false;
                        llaves.Remove(llave);
                        break;
                    }
                }


                //Si hubo colision, restaurar la posicion anterior


                //Hacer que la camara siga al personaje en su nueva posicion
                //camaraInterna.Target = personaje.Position;
            }

            //Si no se esta moviendo, activar animacion de Parado
            else
            {
                //personaje.playAnimation("Parado", true);
            }

            //Ajustar la posicion de la camara segun la colision con los objetos del escenario
            //ajustarPosicionDeCamara();

            //mover ligthBox
            ligthBox.Position = camaraFps.Position;

            //Capturar Input Mouse
            /*if (Input.buttonUp(Core.Input.TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                //Como ejemplo podemos hacer un movimiento simple de la cámara.
                //En este caso le sumamos un valor en Y
                Camara.SetCamera(Camara.Position + new Vector3(0, 100f, 0), Camara.LookAt);
                //Ver ejemplos de cámara para otras operaciones posibles.

                //Si superamos cierto Y volvemos a la posición original.
                if (Camara.Position.Y > 6000f)
                {
                    Camara.SetCamera(new Vector3(Camara.Position.X, 0f, Camara.Position.Z), Camara.LookAt);
                }
            }*/

            if (!lose && !win && !beggining)
            {
                List<Enemigo> aRemover = new List<Enemigo>();
                foreach (Enemigo enemigo in this.enemigos)
                {
                    try
                    {
                        enemigo.Mover(ElapsedTime);
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
        public override void Render()
        {
            System.Console.WriteLine(exitGate.Position);
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();

            D3DDevice.Instance.Device.Clear(Microsoft.DirectX.Direct3D.ClearFlags.Target, Color.Black, 1.0f, 0);

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

            /*if (!godMode)
            {
                DrawText.drawText("Con la tecla M se Reinicia el juego.", 0, 20, Color.OrangeRed);
                DrawText.drawText(
                    "Con esc, haciedno click izquierdo se controla la camara [Actual]: " + TgcParserUtils.printVector3(Camara.Position), 0, 30,
                    Color.OrangeRed);
                DrawText.drawText("Con la tecla G se activa modo dios.", 0, 40, Color.OrangeRed);
                DrawText.drawText("Con la tecla B puede visualizar los Bounding Box.", 0, 50, Color.OrangeRed);
                DrawText.drawText("Recogiendo velas se reestablece la intensidad de la luz( o presionando la tecla P).", 0, 60, Color.OrangeRed);
                DrawText.drawText("Intensidad de la luz: " + ligthIntensity, 0, 70, Color.OrangeRed);
                DrawText.drawText("Hay " + objCount + " esqueletos y " + candleCount + " velas disponibles.", 0, 80, Color.OrangeRed);
                DrawText.drawText("Hay " + keyCount + " llaves disponibles.", 0, 90, Color.OrangeRed);
            }
            else
            {
                DrawText.drawText("Con la tecla G se desactiva modo dios.", 0, 20, Color.OrangeRed);
                DrawText.drawText("Utiliza la tecla ESPACIO para elevarse, y CTRL para descender.", 0, 30, Color.OrangeRed);
                DrawText.drawText("En modo dios no hay deteccion de colisiones.", 0, 40, Color.OrangeRed);
                DrawText.drawText("Con la tecla B puede visualizar los Bounding Box.", 0, 50, Color.OrangeRed);
            }*/
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
            exitGate.render();
            PostRender();
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
                        currentScene[i, j].Meshes[0].Transform = Matrix.Scaling(new Vector3(200, 200, 200));
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
            exitGate.dispose();
            //Dispose de la caja.
            //Box.dispose();
            playerBBox.dispose();

            foreach (TgcBox pared in this.paredes)
            {
                pared.dispose();
            }
            loseSound.dispose();
            loseText.Dispose();
            restartText.Dispose();
            instruccionesText1.Dispose();
            instruccionesText3.render();
            instruccionesText2.render();
            titulo.Dispose();

            /*
            for (int i = 1; i < paredesXY; i++)
            {
                for (int j = 0; j < paredesYZ; j++)
                {
                    if (wallMatXY[i - 1, j])
                    {
                        ParedInternaXY[i - 1, j].dispose();
                    }
                    if (wallMatYZ[i - 1, j])
                    {
                        ParedInternaYZ[i - 1, j].dispose();
                    }
                }
            }

            for (int i = 0; i < paredesXY; i++)
            {
                ParedXY[i].dispose();
                ParedNXY[i].dispose();
            }
            for (int i = 0; i < paredesYZ; i++)
            {
                ParedYZ[i].dispose();
                ParedNYZ[i].dispose();
            }
            */

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
            if (!godMode)
            {
                camaraFps = new TgcFpsCamera(new Vector3(4850, 200, 220), 850f, 500f, true, Input);
                Camara = camaraFps;
                playerBBox.Position = Camara.Position;
                CrearEnemigos();
            }
        }
    }
}