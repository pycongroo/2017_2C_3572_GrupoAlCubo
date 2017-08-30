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

namespace TGC.Group.Model
{
    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer m�s ejemplos chicos, en el caso de copiar para que se
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

        private TgcPlane Piso { get; set; }
        private TgcPlane[] ParedXY = new TgcPlane[10];
        private TgcPlane[] ParedNXY = new TgcPlane[10];
        private TgcPlane[] ParedYZ = new TgcPlane[10];
        private TgcPlane[] ParedNYZ = new TgcPlane[10];
        private TgcPlane[,] ParedInternaXY = new TgcPlane[9, 10];
        private TgcPlane[,] ParedInternaYZ = new TgcPlane[9, 10];
        private bool[,] wallMatXY = new bool[9, 10];
        private bool[,] wallMatYZ = new bool[9, 10];

        //Caja que se muestra en el ejemplo.
        private TgcBox Box { get; set; }
        
        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aqu� todo el c�digo de inicializaci�n: cargar modelos, texturas, estructuras de optimizaci�n, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {
            //Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;
            
            //Textura de la carperta Media. Game.Default es un archivo de configuracion (Game.settings) util para poner cosas.
            //Pueden abrir el Game.settings que se ubica dentro de nuestro proyecto para configurar.
            var pathTexturaCaja = MediaDir + Game.Default.TexturaCaja;
            var pathTexturaPiso = MediaDir + "rock_floor1.jpg";
            var pathTexturaPared = MediaDir + "brick1_1.jpg";
            var sizeParedXY = new Vector3(512, 512, 0);
            var sizeParedYZ = new Vector3(0, 512, 512);
            var sizePiso = new Vector3(5120, 0, 5120);

            //Cargamos una textura, tener en cuenta que cargar una textura significa crear una copia en memoria.
            //Es importante cargar texturas en Init, si se hace en el render loop podemos tener grandes problemas si instanciamos muchas.
            var texture = TgcTexture.createTexture(pathTexturaCaja);
            var texturePiso = TgcTexture.createTexture(pathTexturaPiso);
            var texturaPared = TgcTexture.createTexture(pathTexturaPared);

            Piso = new TgcPlane(new Vector3(0, 0, 0), sizePiso, TgcPlane.Orientations.XZplane, texturePiso);
            for (int i=0; i< 10; i++)
            {
                var posXY = new Vector3(0 + i*512, 0, 0);
                ParedXY[i] = new TgcPlane(posXY, sizeParedXY, TgcPlane.Orientations.XYplane, texturaPared);
                var posNXY = new Vector3(0 + i * 512, 0, 5120);
                ParedNXY[i] = new TgcPlane(posNXY, sizeParedXY, TgcPlane.Orientations.XYplane, texturaPared);
                var posYZ = new Vector3(0, 0, 0 + i * 512);
                ParedYZ[i] = new TgcPlane(posYZ, sizeParedYZ, TgcPlane.Orientations.YZplane, texturaPared);
                var posNYZ = new Vector3(5120, 0, 0 + i * 512);
                ParedNYZ[i] = new TgcPlane(posNYZ, sizeParedYZ, TgcPlane.Orientations.YZplane, texturaPared);
            }

            Random random = new Random();
            for (int i = 1; i < 10; i++)
            {
                for(int j = 0; j < 10; j++)
                {
                    var posXY = new Vector3(0 + i * 512, 0, 0 + j*512);
                    ParedInternaXY[i-1, j] = new TgcPlane(posXY, sizeParedXY, TgcPlane.Orientations.XYplane, texturaPared);
                    var posYZ = new Vector3(0 + j * 512, 0, 0 + i*512);
                    ParedInternaYZ[i-1, j] = new TgcPlane(posYZ, sizeParedYZ, TgcPlane.Orientations.YZplane, texturaPared);
                    //generacion de valores para aparicion de paredes
                    int valR = random.Next(0, 10);
                    wallMatXY[i-1, j] = (valR < 7);
                    valR = random.Next(0, 10);
                    wallMatYZ[i-1, j] = (valR < 7);
                }
            }
            //Creamos una caja 3D ubicada de dimensiones (5, 10, 5) y la textura como color.
            var size = new Vector3(100, 100, 100);
            //Construimos una caja seg�n los par�metros, por defecto la misma se crea con centro en el origen y se recomienda as� para facilitar las transformaciones.
            Box = TgcBox.fromSize(size, texture);
            //Posici�n donde quiero que este la caja, es com�n que se utilicen estructuras internas para las transformaciones.
            //Entonces actualizamos la posici�n l�gica, luego podemos utilizar esto en render para posicionar donde corresponda con transformaciones.
            Box.Position = new Vector3(50, 50, 50);
            
            //Suelen utilizarse objetos que manejan el comportamiento de la camara.
            //Lo que en realidad necesitamos gr�ficamente es una matriz de View.
            //El framework maneja una c�mara est�tica, pero debe ser inicializada.
            //Posici�n de la camara.
            var cameraPosition = new Vector3(6000, 1000, 6000);
            //Quiero que la camara mire hacia el origen (0,0,0).
            var lookAt = Vector3.Empty;
            var moveSpeed = 500f;
            var jumpSpeed = 200f;

            Camara = new TgcFpsCamera(cameraPosition, moveSpeed, jumpSpeed, Input);
            //Configuro donde esta la posicion de la camara y hacia donde mira.
            //Camara.SetCamera(cameraPosition, lookAt);
            //Internamente el framework construye la matriz de view con estos dos vectores.
            //Luego en nuestro juego tendremos que crear una c�mara que cambie la matriz de view con variables como movimientos o animaciones de escenas.
        }

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la l�gica de computo del modelo, as� como tambi�n verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        public override void Update()
        {
            PreUpdate();

            //Capturar Input Mouse
            /*if (Input.buttonUp(Core.Input.TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                //Como ejemplo podemos hacer un movimiento simple de la c�mara.
                //En este caso le sumamos un valor en Y
                Camara.SetCamera(Camara.Position + new Vector3(0, 100f, 0), Camara.LookAt);
                //Ver ejemplos de c�mara para otras operaciones posibles.

                //Si superamos cierto Y volvemos a la posici�n original.
                if (Camara.Position.Y > 6000f)
                {
                    Camara.SetCamera(new Vector3(Camara.Position.X, 0f, Camara.Position.Z), Camara.LookAt);
                }
            }*/
        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aqu� todo el c�digo referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>
        public override void Render()
        {
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones seg�n nuestra conveniencia.
            PreRender();

            //Dibuja un texto por pantalla
            DrawText.drawText("Con la tecla F se dibuja el bounding box.", 0, 20, Color.OrangeRed);
            DrawText.drawText(
                "Con clic izquierdo subimos la camara [Actual]: " + TgcParserUtils.printVector3(Camara.Position), 0, 30,
                Color.OrangeRed);
            Piso.render();
            for (int i = 0; i < 10; i++)
            {
                ParedXY[i].render();
                ParedNXY[i].render();
                ParedYZ[i].render();
                ParedNYZ[i].render();
            }
            for (int i = 1; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (wallMatXY[i-1, j])
                    {
                        ParedInternaXY[i - 1, j].render();
                    }
                    if (wallMatYZ[i-1, j])
                    {
                        ParedInternaYZ[i - 1, j].render();
                    }
                }
            }
            //Piso2.render();
            //Piso3.render();
            //Siempre antes de renderizar el modelo necesitamos actualizar la matriz de transformacion.
            //Debemos recordar el orden en cual debemos multiplicar las matrices, en caso de tener modelos jer�rquicos, tenemos control total.
            Box.Transform = Matrix.Scaling(Box.Scale) *
                            Matrix.RotationYawPitchRoll(Box.Rotation.Y, Box.Rotation.X, Box.Rotation.Z) *
                            Matrix.Translation(Box.Position);
            //A modo ejemplo realizamos toda las multiplicaciones, pero aqu� solo nos hacia falta la traslaci�n.
            //Finalmente invocamos al render de la caja
            Box.render();

            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        /// <summary>
        ///     Se llama cuando termina la ejecuci�n del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gr�ficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            //Dispose de la caja.
            Box.dispose();
        }
    }
}