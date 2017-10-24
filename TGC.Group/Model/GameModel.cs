using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
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

        private int paredesXY = 16; //potencia de 2
        private int paredesYZ = 16; //potencia de 2
        private TgcScene[,] currentScene;
        private List<Vector2> velas;
        private List<Vector2> esqueletos;
        private bool[,] skeletonsAp;
        private bool[,] candleAp;
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
        private Random random;
        //booleanos para pruebas
        private bool bTrue = true;
        private bool bFalse = false;
        private TgcFpsCamera camaraFps;
        //private TgcArrow lookingArrow { get; set; }
        private float ligthIntensity;
        private int objCount;
        private int candleCount;
        private int visibilityLen;//distancia de renderizado
        private int posiX;
        private int posfX;
        private int posiZ;
        private int posfZ;
        private double rangoDiagAngle;
        private bool optimizationEnabled;

        private TgcBox ligthBox { get; set; }

        //Caja que se muestra en el ejemplo.
        //private TgcBox Box { get; set; }

        private Microsoft.DirectX.Direct3D.Effect efecto;
        
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
            velas = new List<Vector2>();
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
            /*
            for (int i=0; i< paredesXY; i++)
            {
                var posXY = new Vector3((i+0.5f)*anchoPared, 0.5f*altoPared, 0);
                ParedXY[i] = TgcBox.fromSize(sizeParedXY, texturaPared);
                ParedXY[i].Position = posXY;
                obstaculos.Add(ParedXY[i]);
                var posNXY = new Vector3((i + 0.5f) * anchoPared, 0.5f * altoPared, paredesXY*anchoPared);
                ParedNXY[i] = TgcBox.fromSize(sizeParedXY, texturaPared);
                ParedNXY[i].Position = posNXY;
                obstaculos.Add(ParedNXY[i]);
            }
            for (int i = 0; i < paredesYZ; i++)
            {
                var posYZ = new Vector3(0, 0.5f * altoPared, (i + 0.5f) * anchoPared);
                ParedYZ[i] = TgcBox.fromSize(sizeParedYZ, texturaPared);
                ParedYZ[i].Position = posYZ;
                obstaculos.Add(ParedYZ[i]);
                var posNYZ = new Vector3(paredesYZ * anchoPared, 0.5f * altoPared, (i + 0.5f) * anchoPared);
                ParedNYZ[i] = TgcBox.fromSize(sizeParedYZ, texturaPared);
                ParedNYZ[i].Position = posNYZ;
                obstaculos.Add(ParedNYZ[i]);
            }
            */
            random = new Random();
            /*
            for (int i = 1; i < paredesXY; i++)
            {
                for(int j = 0; j < paredesYZ; j++)
                {
                    var posXY = new Vector3((j + .5f) * anchoPared, .5f * altoPared, i * anchoPared); 
                    ParedInternaXY[i-1, j] = TgcBox.fromSize(sizeParedXY, texturaPared);
                    ParedInternaXY[i - 1, j].Position = posXY;
                    wallMatXY[i - 1, j] = bTrue;
                    var posYZ = new Vector3(i* anchoPared, .5f * altoPared, (j+.5f) * anchoPared);
                    ParedInternaYZ[i-1, j] = TgcBox.fromSize(sizeParedYZ, texturaPared);
                    ParedInternaYZ[i - 1, j].Position = posYZ;
                    wallMatYZ[i - 1, j] = bTrue;
                }
            }
            int posX = paredesXY / 2;
            int posZ = paredesYZ / 2;
            int cant = paredesXY;
            //Console.WriteLine("PRE GEN: \nposX:" + posX + "\nposZ:" + posZ + "\nlen:" + cant);
            genLab(posX -1, posZ -1, cant);

            for (int i = 1; i < paredesXY; i++)
            {
                for (int j = 0; j < paredesYZ; j++)
                {
                    if (wallMatXY[i - 1, j])
                    {
                        obstaculos.Add(ParedInternaXY[i - 1, j]);
                    }
                    if (wallMatYZ[i - 1, j])
                    {
                        obstaculos.Add(ParedInternaYZ[i - 1, j]);
                    }
                }
            }
            */
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

            var esquletoSize = new Vector3(5,5,5);
            var candleSize = new Vector3(2, 2, 2);

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

                    if (this.laberinto[x, y].HasFlag(CellState.Top))
                    {
                        pared = CrearPared(ParedHorizontal);
                        UbicarPared(pared, CellState.Top, new Point(y, x));
                        paredes.Add(pared);

                    }
                    if (this.laberinto[x, y].HasFlag(CellState.Left))
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
            }
            for (var y = 0; y < this.laberinto.Height; y++)
            {
                pared = CrearPared(ParedVertical);
                UbicarPared(pared, CellState.Right, new Point(y, this.laberinto.Width));
                paredes.Add(pared);
            }

        }

        /*
        private void genLab(int posX, int posZ, int len)
        {
            //Console.WriteLine("Values: \nposX:" + posX + "\nposZ:" + posZ + "\nlen:" + len);
            int rn;
            if (len > 2)
            {
                //Console.WriteLine("genero Lab");
                //Console.WriteLine("Values: \nposX:"+ posX+"\nposZ:"+posZ+"\nlen:"+len);
                if (random.Next(0, 2) == 1)
                {   
                    //Console.WriteLine("Doble Z");
                    if (random.Next(0, 2) == 0)
                    {
                        //Console.WriteLine("rango X: (" + (posX + 1 - len / 2) + ", " + (posX + 1) + ")");
                        rn = random.Next(posX + 1 - len / 2, posX + 1);
                        //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                        wallMatYZ[posZ, rn] = bFalse;
                        //Console.WriteLine(ParedInternaYZ[posZ + 1, rn].Position);
                    } else
                    {
                        //Console.WriteLine("rango X: (" + (posX + 1) + ", " + (posX + 1 + len/2) + ")");
                        rn = random.Next(posX + 1, posX + 1 + len/2);
                        //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                        wallMatYZ[posZ, rn] = bFalse;
                        //Console.WriteLine(ParedInternaYZ[posZ, rn].Position);
                    }
                    //Console.WriteLine("Rango Z: (" + (posZ +1 - len / 2) + ", " + (posZ+1) + ")");
                    rn = random.Next(posZ +1 - len / 2, posZ + 1);
                    //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                    wallMatXY[posX ,rn] = bFalse;
                    //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                    //Console.WriteLine("Rango Z: (" + (posZ + 1) + ", " + (posZ + 1 + len/2) + ")");
                    rn = random.Next(posZ + 1, posZ + 1 + len/2);
                    //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                    wallMatXY[posX, rn] = bFalse;
                    //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                }
                else
                {
                    //Console.WriteLine("Doble X");
                    if (random.Next(0, 2)==0)
                    {
                        //Console.WriteLine("Rango Z: (" + (posZ + 1 - len / 2) + ", " + (posZ + 1) + ")");
                        rn = random.Next(posZ + 1 - len / 2, posZ + 1);
                        //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                        wallMatXY[posX, rn] = bFalse;
                        //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                    } else
                    {
                        //Console.WriteLine("Rango Z: (" + (posZ + 1) + ", " + (posZ + 1 + len/2) + ")");
                        rn = random.Next(posZ + 1, posZ + 1 + len/2);
                        //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                        wallMatXY[posX, rn] = bFalse;
                        //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                    }
                    //Console.WriteLine("rango X: (" + (posX + 1 - len / 2) + ", " + (posX + 1) + ")");
                    rn = random.Next(posX + 1 - len / 2, posX + 1);
                    //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                    wallMatYZ[posZ, rn] = bFalse;
                    //Console.WriteLine(ParedInternaYZ[posZ, rn].Position);
                    //Console.WriteLine("rango X: (" + (posX + 1) + ", " + (posX + 1 + len/2) + ")");
                    rn = random.Next(posX + 1, posX + 1 + len / 2);
                    //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                    wallMatYZ[posZ, rn] = bFalse;
                    //Console.WriteLine(ParedInternaYZ[posZ, rn].Position);
                }
                //Console.WriteLine("************************");
                int nextX = posX;
                int nextZ = posZ;
                //Console.WriteLine("PRE SUB GEN: \nnextX:" + (nextX + len / 4) + "\nnextZ:" + (nextZ + len / 4) + "\nlen:" + len/2);
                genLab(nextX + len / 4, nextZ + len / 4, len / 2);
                //Console.WriteLine("PRE SUB GEN: \nnextX:" + (nextX + len / 4) + "\nnextZ:" + (nextZ - len / 4) + "\nlen:" + len / 2);
                genLab(nextX + len / 4, nextZ - len / 4, len / 2);
                //Console.WriteLine("PRE SUB GEN: \nnextX:" + (nextX - len / 4) + "\nnextZ:" + (nextZ + len / 4) + "\nlen:" + len / 2);
                genLab(nextX - len / 4, nextZ + len / 4, len / 2);
                //Console.WriteLine("PRE SUB GEN: \nnextX:" + (nextX - len / 4) + "\nnextZ:" + (nextZ - len / 4) + "\nlen:" + len / 2);
                genLab(nextX - len / 4, nextZ - len / 4, len / 2);
                //Console.WriteLine("Fin Lab");
            } else
            {
                switch (random.Next(0, 4))
                {
                    case 0:
                        //Console.WriteLine("Caso 0");
                        //0
                        //Console.WriteLine("rango X: (" + (posX + 1 - len / 2) + ", " + (posX + 1) + ")");
                        rn = random.Next(posX + 1 - len / 2, posX + 1);
                        //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                        wallMatYZ[posZ, rn] = bFalse;
                        //Console.WriteLine(ParedInternaYZ[posZ + 1, rn].Position);
                        //1
                        //Console.WriteLine("rango X: (" + (posX + 1) + ", " + (posX + 1 + len / 2) + ")");
                        rn = random.Next(posX + 1, posX + 1 + len / 2);
                        //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                        wallMatYZ[posZ, rn] = bFalse;
                        //Console.WriteLine(ParedInternaYZ[posZ, rn].Position);
                        //2
                        //Console.WriteLine("Rango Z: (" + (posZ + 1 - len / 2) + ", " + (posZ + 1) + ")");
                        rn = random.Next(posZ + 1 - len / 2, posZ + 1);
                        //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                        wallMatXY[posX, rn] = bFalse;
                        //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                        break;
                    case 1:
                        //Console.WriteLine("Caso 1");
                        //1
                        //Console.WriteLine("rango X: (" + (posX + 1) + ", " + (posX + 1 + len / 2) + ")");
                        rn = random.Next(posX + 1, posX + 1 + len / 2);
                        //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                        wallMatYZ[posZ, rn] = bFalse;
                        //Console.WriteLine(ParedInternaYZ[posZ, rn].Position);
                        //2
                        //Console.WriteLine("Rango Z: (" + (posZ + 1 - len / 2) + ", " + (posZ + 1) + ")");
                        rn = random.Next(posZ + 1 - len / 2, posZ + 1);
                        //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                        wallMatXY[posX, rn] = bFalse;
                        //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                        //3
                        //Console.WriteLine("Rango Z: (" + (posZ + 1) + ", " + (posZ + 1 + len / 2) + ")");
                        rn = random.Next(posZ + 1, posZ + 1 + len / 2);
                        //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                        wallMatXY[posX, rn] = bFalse;
                        //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                        break;
                    case 2:
                        //Console.WriteLine("Caso 2");
                        //0
                        //Console.WriteLine("rango X: (" + (posX + 1 - len / 2) + ", " + (posX + 1) + ")");
                        rn = random.Next(posX + 1 - len / 2, posX + 1);
                        //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                        wallMatYZ[posZ, rn] = bFalse;
                        //Console.WriteLine(ParedInternaYZ[posZ, rn].Position);
                        //2
                        //Console.WriteLine("Rango Z: (" + (posZ + 1 - len / 2) + ", " + (posZ + 1) + ")");
                        rn = random.Next(posZ + 1 - len / 2, posZ + 1);
                        //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                        wallMatXY[posX, rn] = bFalse;
                        //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                        //3
                        //Console.WriteLine("Rango Z: (" + (posZ + 1) + ", " + (posZ + 1 + len / 2) + ")");
                        rn = random.Next(posZ + 1, posZ + 1 + len / 2);
                        //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                        wallMatXY[posX, rn] = bFalse;
                        //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                        break;
                    case 3:
                        //Console.WriteLine("Caso 3");
                        //0
                        //Console.WriteLine("rango X: (" + (posX + 1 - len / 2) + ", " + (posX + 1) + ")");
                        rn = random.Next(posX + 1 - len / 2, posX + 1);
                        //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                        wallMatYZ[posZ, rn] = bFalse;
                        //Console.WriteLine(ParedInternaYZ[posZ + 1, rn].Position);
                        //1
                        //Console.WriteLine("rango X: (" + (posX + 1) + ", " + (posX + 1 + len / 2) + ")");
                        rn = random.Next(posX + 1, posX + 1 + len / 2);
                        //Console.WriteLine("RN X: (" + posZ + ", " + rn + ")");
                        wallMatYZ[posZ, rn] = bFalse;
                        //Console.WriteLine(ParedInternaYZ[posZ, rn].Position);
                        //3
                        //Console.WriteLine("Rango Z: (" + (posZ + 1) + ", " + (posZ + 1 + len / 2) + ")");
                        rn = random.Next(posZ + 1, posZ + 1 + len / 2);
                        //Console.WriteLine("RN Z: (" + posX + ", " + rn + ")");
                        wallMatXY[posX, rn] = bFalse;
                        //Console.WriteLine(ParedInternaXY[posX, rn].Position);
                        break;
                }
                //Console.WriteLine("----------------------------------------");
            }
        }
        */

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la lógica de computo del modelo, así como también verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        public override void Update()
        {
            PreUpdate();
            if (optimizationEnabled)
            {
                Vector3 dirView = camaraFps.LookAt - camaraFps.Position;
                float tan = dirView.Z / dirView.X;
                System.Console.WriteLine("Tan: " + tan);
                double anguloVista = Math.Atan2(dirView.Z, dirView.X) * (180 / Math.PI);
                System.Console.WriteLine("Angle Tan2: " + anguloVista);
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

            if (Input.keyPressed(Key.M))
            {
                this.reset();
            }

            if (Input.keyPressed(Key.B)) bMode = !bMode;

            if (Input.keyPressed(Key.P)) ligthIntensity = 50f;

            if(ligthIntensity > 0)ligthIntensity -= 0.02f;

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
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();

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
            if (!godMode)
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
            }
            else
            {
                DrawText.drawText("Con la tecla G se desactiva modo dios.", 0, 20, Color.OrangeRed);
                DrawText.drawText("Utiliza la tecla ESPACIO para elevarse, y CTRL para descender.", 0, 30, Color.OrangeRed);
                DrawText.drawText("En modo dios no hay deteccion de colisiones.", 0, 40, Color.OrangeRed);
                DrawText.drawText("Con la tecla B puede visualizar los Bounding Box.", 0, 50, Color.OrangeRed);
            }
            Piso.Effect = efecto;
            Piso.Technique = TgcShaders.Instance.getTgcMeshTechnique(Piso.toMesh("piso").RenderType);
            Piso.render();
            Techo.Effect = efecto;
            Techo.Technique = TgcShaders.Instance.getTgcMeshTechnique(Techo.toMesh("techo").RenderType);
            Techo.render();
            /*
            for (int i = 0; i < paredesXY; i++)
            {
                ParedXY[i].Transform = transformBox(ParedXY[i]);
                ParedXY[i].Effect = efecto;
                auxMesh = ParedXY[i].toMesh("paredXY");
                ParedXY[i].Technique = TgcShaders.Instance.getTgcMeshTechnique(auxMesh.RenderType);
                ParedXY[i].render();
                auxMesh.dispose();
                if (bMode) ParedXY[i].BoundingBox.render();
                ParedNXY[i].Transform = transformBox(ParedNXY[i]);
                ParedNXY[i].Effect = efecto;
                auxMesh = ParedNXY[i].toMesh("paredNXY");
                ParedNXY[i].Technique = TgcShaders.Instance.getTgcMeshTechnique(auxMesh.RenderType);
                ParedNXY[i].render();
                auxMesh.dispose();
                if (bMode) ParedNXY[i].BoundingBox.render();
            }
            for (int i = 0; i < paredesYZ; i++)
            {
                ParedYZ[i].Transform = transformBox(ParedYZ[i]);
                ParedYZ[i].Effect = efecto;
                auxMesh = ParedYZ[i].toMesh("paredYZ");
                ParedYZ[i].Technique = TgcShaders.Instance.getTgcMeshTechnique(auxMesh.RenderType);
                ParedYZ[i].render();
                auxMesh.dispose();
                if (bMode) ParedYZ[i].BoundingBox.render();
                ParedNYZ[i].Transform = transformBox(ParedNYZ[i]);
                ParedNYZ[i].Effect = efecto;
                auxMesh = ParedNYZ[i].toMesh("paredNYZ");
                ParedNYZ[i].Technique = TgcShaders.Instance.getTgcMeshTechnique(auxMesh.RenderType);
                ParedNYZ[i].render();
                auxMesh.dispose();
                if (bMode) ParedNYZ[i].BoundingBox.render();
            }
            */
            //renderGrid(posX, posZ);
            renderGrid();

            //for (int i = 0; i < paredesXY; i++)
            //{
            //    for (int j = 0; j < paredesYZ; j++)
            //    {
            //        if (skeletonsAp[i, j])
            //        {
            //            currentScene[i, j].Meshes[0].Transform = Matrix.Scaling(new Vector3(100,100,100));
            //            currentScene[i, j].Meshes[0].Effect = efecto;
            //            currentScene[i, j].Meshes[0].Technique = TgcShaders.Instance.getTgcMeshTechnique(currentScene[i, j].Meshes[0].RenderType);
            //            currentScene[i, j].Meshes[0].render();
            //        }
            //        if (candleAp[i, j])
            //        {
            //            currentScene[i, j].Meshes[0].Transform = Matrix.Scaling(new Vector3(100, 100, 100));
            //            currentScene[i, j].Meshes[0].Effect = efecto;
            //            currentScene[i, j].Meshes[0].Technique = TgcShaders.Instance.getTgcMeshTechnique(currentScene[i, j].Meshes[0].RenderType);
            //            currentScene[i, j].Meshes[0].render();
            //        }
            //    }
            //}

            //Piso2.render();
            //Piso3.render();
            //Siempre antes de renderizar el modelo necesitamos actualizar la matriz de transformacion.
            //Debemos recordar el orden en cual debemos multiplicar las matrices, en caso de tener modelos jerárquicos, tenemos control total.
            /*Box.Transform = Matrix.Scaling(Box.Scale) *
                            Matrix.RotationYawPitchRoll(Box.Rotation.Y, Box.Rotation.X, Box.Rotation.Z) *
                            Matrix.Translation(Box.Position);*/
            playerBBox.Transform = transformBox(playerBBox);
            /* playerBBox.Transform = Matrix.Scaling(Box.Scale) *
                             Matrix.RotationYawPitchRoll(Box.Rotation.Y, Box.Rotation.X, Box.Rotation.Z) *
                             Matrix.Translation(Box.Position);-*/
            if (bMode) playerBBox.BoundingBox.render();
            //A modo ejemplo realizamos toda las multiplicaciones, pero aquí solo nos hacia falta la traslación.
            //Finalmente invocamos al render de la caja
            //Box.render();
            //Box.BoundingBox.render();
             
            //currentScene.Meshes[0].render();
            
            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        public void renderGrid()
        {
            //System.Console.WriteLine("Position : (" + posPX + ", " + posPZ + ")");
            TgcMesh auxMesh = null;
            //obtener ind X min y max

            System.Console.WriteLine("X : [" + posiX + ", " + posfX + "]");
            System.Console.WriteLine("Z : [" + posiZ + ", " + posfZ + "]");
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

            /*
            for (int i = posiX; i < posfX - 1; i++)
            {
                for (int j = posiZ; j < posfZ; j++)
                {
                    if (wallMatYZ[i, j])
                    {
                        //System.Console.WriteLine("X:(" + i + "," + j + ")");
                        ParedInternaYZ[i, j].Transform = transformBox(ParedInternaYZ[i, j]);
                        ParedInternaYZ[i, j].Effect = efecto;
                        auxMesh = ParedInternaYZ[i, j].toMesh("paredInternaXY");
                        ParedInternaYZ[i, j].Technique = TgcShaders.Instance.getTgcMeshTechnique(auxMesh.RenderType);
                        ParedInternaYZ[i, j].render();
                        auxMesh.dispose();
                        if (bMode) ParedInternaYZ[i, j].BoundingBox.render();
                        //DecoWallXY[i - 1, j].render();
                    }
                }
            }
            for (int i = posiZ + 1; i < posfZ; i++)
            {
                for (int j = posiX; j < posfX; j++)
                {
                    if (wallMatXY[i - 1, j])
                    {
                        //System.Console.WriteLine("Z:(" + (i - 1) + "," + j + ")");
                        ParedInternaXY[i - 1, j].Transform = transformBox(ParedInternaXY[i - 1, j]);
                        ParedInternaXY[i - 1, j].Effect = efecto;
                        auxMesh = ParedInternaYZ[i - 1, j].toMesh("paredInternaYZ");
                        ParedInternaXY[i - 1, j].Technique = TgcShaders.Instance.getTgcMeshTechnique(auxMesh.RenderType);
                        ParedInternaXY[i - 1, j].render();
                        auxMesh.dispose();
                        if (bMode) ParedInternaXY[i - 1, j].BoundingBox.render();
                        //DecoWallYZ[i - 1, j].render();
                    }
                }
            }
            */
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

            foreach (TgcBox pared in this.paredes)
            {
                pared.dispose();
            }
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
                }
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

        private Matrix transformSpehre( TgcSphere aBox)
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
            }
        }
    }
}