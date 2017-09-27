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

        private int paredesXY = 20;
        private int paredesYZ = 20;
        private TgcScene[,] currentScene;
        private bool[,] skeletonsAp;
        private TgcPlane Piso { get; set; }
        private TgcPlane Techo { get; set; }
        private TgcBox playerBBox { get; set; }
        private TgcBox[] ParedXY;
        private TgcBox[] ParedNXY;
        private TgcBox[] ParedYZ;
        private TgcBox[] ParedNYZ;
        private TgcPlane[,] DecoWallXY;
        private TgcPlane[,] DecoWallYZ;
        private TgcBox[,] ParedInternaXY;
        private TgcBox[,] ParedInternaYZ;
        private bool[,] wallMatXY;
        private bool[,] wallMatYZ;
        private float anchoPared = 512;
        private float altoPared = 512;
        private float grosorPared = 50;
        private List<TgcBox> obstaculos;
        private bool collide;
        //private TgcArrow lookingArrow { get; set; }

        //Caja que se muestra en el ejemplo.
        private TgcBox Box { get; set; }
        
        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aquí todo el código de inicialización: cargar modelos, texturas, estructuras de optimización, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {
            obstaculos = new List<TgcBox>();
            collide = false;
            currentScene = new TgcScene[paredesXY, paredesYZ];
            skeletonsAp = new bool[paredesXY, paredesYZ];
            ParedXY = new TgcBox[paredesXY];
            ParedNXY = new TgcBox[paredesXY];
            ParedYZ = new TgcBox[paredesYZ];
            ParedNYZ = new TgcBox[paredesYZ];
            DecoWallXY = new TgcPlane[paredesXY -1, paredesXY];
            DecoWallYZ = new TgcPlane[paredesYZ -1, paredesYZ];
            ParedInternaXY = new TgcBox[paredesXY - 1, paredesXY];
            ParedInternaYZ = new TgcBox[paredesYZ - 1, paredesYZ];
            wallMatXY = new bool[paredesXY - 1, paredesXY];
            wallMatYZ = new bool[paredesYZ - 1, paredesYZ];
            //playerBBox = new TgcSphere(125,texturapiso,new Vector3(0,0,0));
        //Device de DirectX para crear primitivas.
        var d3dDevice = D3DDevice.Instance.Device;
            
            //Textura de la carperta Media. Game.Default es un archivo de configuracion (Game.settings) util para poner cosas.
            //Pueden abrir el Game.settings que se ubica dentro de nuestro proyecto para configurar.
            var pathTexturaCaja = MediaDir + Game.Default.TexturaCaja;
            var pathTexturaPiso = MediaDir + "rock_floor2.jpg";
            var pathTexturaPared = MediaDir + "brick1_1.jpg";
            var pathTexturaDeco = MediaDir + "cartelera2.jpg";
            var sizeDecoXY = new Vector3(300, 300, 0);
            var sizeDecoYZ = new Vector3(0, 300, 300);
            var sizeParedXY = new Vector3(anchoPared, altoPared, grosorPared);
            var sizeParedYZ = new Vector3(grosorPared, altoPared, anchoPared);
            var sizePiso = new Vector3(512*paredesXY, 20, 512*paredesYZ);
            var relDecoPosXY = new Vector3(100, 100, 10);
            var relDecoPosYZ = new Vector3(10, 100, 100);

            //Cargamos una textura, tener en cuenta que cargar una textura significa crear una copia en memoria.
            //Es importante cargar texturas en Init, si se hace en el render loop podemos tener grandes problemas si instanciamos muchas.
            var texture = TgcTexture.createTexture(pathTexturaCaja);
            var texturePiso = TgcTexture.createTexture(pathTexturaPiso);
            var texturaTecho = TgcTexture.createTexture(pathTexturaPiso);
            var texturaPared = TgcTexture.createTexture(pathTexturaPared);
            var texturaDeco = TgcTexture.createTexture(pathTexturaDeco);

            Piso = new TgcPlane(new Vector3(0, 0, 0), sizePiso, TgcPlane.Orientations.XZplane, texturePiso);
            Techo = new TgcPlane(new Vector3(0, 511, 0), sizePiso, TgcPlane.Orientations.XZplane, texturaTecho);
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
            Random random = new Random();
            for (int i = 1; i < paredesXY; i++)
            {
                for(int j = 0; j < paredesYZ; j++)
                {
                    var posXY = new Vector3((i+.5f) * anchoPared , .5f*altoPared, j*anchoPared);
                    ParedInternaXY[i-1, j] = TgcBox.fromSize(sizeParedXY, texturaPared);
                    ParedInternaXY[i - 1, j].Position = posXY;
                    obstaculos.Add(ParedInternaXY[i - 1, j]);
                    DecoWallXY[i - 1, j] = new TgcPlane(posXY+relDecoPosXY, sizeDecoXY, TgcPlane.Orientations.XYplane, texturaDeco);
                    var posYZ = new Vector3(j * anchoPared, .5f*altoPared, (i+.5f)*anchoPared);
                    ParedInternaYZ[i-1, j] = TgcBox.fromSize(sizeParedYZ, texturaPared);
                    ParedInternaYZ[i - 1, j].Position = posYZ;
                    obstaculos.Add(ParedInternaYZ[i - 1, j]);
                    DecoWallYZ[i - 1, j] = new TgcPlane(posYZ+relDecoPosYZ, sizeDecoYZ, TgcPlane.Orientations.YZplane, texturaDeco);
                    //generacion de valores para aparicion de paredes
                    int valR = random.Next(0, 10);
                    wallMatXY[i-1, j] = (valR < 7);
                    valR = random.Next(0, 10);
                    wallMatYZ[i-1, j] = (valR < 7);
                }
            }
            //Creamos una caja 3D ubicada de dimensiones (5, 10, 5) y la textura como color.
            var size = new Vector3(100, 100, 100);
            //Construimos una caja según los parámetros, por defecto la misma se crea con centro en el origen y se recomienda así para facilitar las transformaciones.
            Box = TgcBox.fromSize(size, texture);
            //Posición donde quiero que este la caja, es común que se utilicen estructuras internas para las transformaciones.
            //Entonces actualizamos la posición lógica, luego podemos utilizar esto en render para posicionar donde corresponda con transformaciones.
            Box.Position = new Vector3(50, 50, 50);
            
            //Suelen utilizarse objetos que manejan el comportamiento de la camara.
            //Lo que en realidad necesitamos gráficamente es una matriz de View.
            //El framework maneja una cámara estática, pero debe ser inicializada.
            //Posición de la camara.
            var cameraPosition = new Vector3(4850, 200, 220);
            //playerBBox.Position = cameraPosition;
            playerBBox = new TgcBox();
            playerBBox = TgcBox.fromSize(cameraPosition, new Vector3(25,25,25));
            //Quiero que la camara mire hacia el origen (0,0,0).
            var lookAt = Vector3.Empty;
            var moveSpeed = 850f;
            var jumpSpeed = 500f;

            var esquletoSize = new Vector3(5,5,5);

            for (int i = 0; i < paredesXY; i++)
            {
                for (int j = 0; j < paredesYZ; j++)
                {
                    loadMesh(MediaDir + "EsqueletoHumano\\Esqueleto-TgcScene.xml", i, j);
                    //No recomendamos utilizar AutoTransform, en juegos complejos se pierde el control. mejor utilizar Transformaciones con matrices.
                    currentScene[i, j].Meshes[0].AutoTransformEnable = true;
                    //Desplazarlo
                    currentScene[i, j].Meshes[0].move(512 * i + 256, 0, 512 * j + 256);
                    currentScene[i, j].Meshes[0].Scale = esquletoSize;
                    currentScene[i, j].Meshes[0].Rotation = new Vector3(0,random.Next(0,360),0);
                    skeletonsAp[i, j] = (random.Next(0, 10) < 2);
                }
            }

            //fija la camara en la dimension Y en true. Por el momento si se activa no se puede saltar ni agacharse ( seria necesario en nuestro juego?)
            var fixCamY = true;

            Camara = new TgcFpsCamera(cameraPosition, moveSpeed, jumpSpeed, fixCamY, Input);
            //Configuro donde esta la posicion de la camara y hacia donde mira.
            //Camara.SetCamera(cameraPosition, lookAt);
            //Internamente el framework construye la matriz de view con estos dos vectores.
            //Luego en nuestro juego tendremos que crear una cámara que cambie la matriz de view con variables como movimientos o animaciones de escenas.
        }

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la lógica de computo del modelo, así como también verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        public override void Update()
        {
            PreUpdate();

            //var currentCameraPos = Camara.Position;
            //playerBBox.Position = currentCameraPos;
            
            ////for(var i = 0; i < 4; i++) {
            //    if (intersectBtoB(playerBBox, ParedInternaXY[0, 0])) { 
                
            //                currentCameraPos.X += 50;
                        
            //        Camara.SetCamera(currentCameraPos,Camara.LookAt, Camara.UpVector);
            //        //playerBBox.Position = currentCameraPos;
            //    }
            ////}
            
            var velocidadCaminar = 400f;
            var velocidadRotacion = 120f;

            //Calcular proxima posicion de personaje segun Input
            var moveForward = 0f;
            var relPosX = new Vector3(0, 0, 0);
            var relPosY = new Vector3(0, 0, 0);
            var moveSideRight = 0f;
            float rotate = 0;
            var moving = false;

            //Adelante
            if (Input.keyDown(Key.W))
            {
                moveForward = -velocidadCaminar;
                relPosX = new Vector3(-velocidadCaminar, 0, 0);
                moving = true;
            }

            //Atras
            if (Input.keyDown(Key.S))
            {
                moveForward = velocidadCaminar;
                relPosX = new Vector3(velocidadCaminar, 0, 0);
                moving = true;
            }

            //Derecha
            if (Input.keyDown(Key.D))
            {
                moveSideRight = velocidadCaminar;
                relPosY = new Vector3(0, 0, -velocidadCaminar);
                moving = true;
            }

            //Izquierda
            if (Input.keyDown(Key.A))
            {
                moveSideRight = -velocidadCaminar;
                relPosY = new Vector3(0, 0, velocidadCaminar);
                moving = true;
            }

            //Si hubo rotacion
            //if (rotating)
            //{
            //    //Rotar personaje y la camara, hay que multiplicarlo por el tiempo transcurrido para no atarse a la velocidad el hardware
            //    var rotAngle = Geometry.DegreeToRadian(rotate * ElapsedTime);
            //    personaje.rotateY(rotAngle);
            //    camaraInterna.rotateY(rotAngle);
            //}

            //Si hubo desplazamiento
            if (moving)
            {
                //Activar animacion de caminando
                //personaje.playAnimation("Caminando", true);

                //Aplicar movimiento hacia adelante o atras segun la orientacion actual del Mesh
                var lastPos = playerBBox.Position;

                //La velocidad de movimiento tiene que multiplicarse por el elapsedTime para hacerse independiente de la velocida de CPU
                //Ver Unidad 2: Ciclo acoplado vs ciclo desacoplado
                //playerBBox.moveOrientedY(moveForward * ElapsedTime);
                playerBBox.Position += relPosX * ElapsedTime;
                playerBBox.Position += relPosY * ElapsedTime;

                //Detectar colisiones
                var collide = false;
                foreach (var obstaculo in obstaculos)
                {
                    var result = TgcCollisionUtils.classifyBoxBox(playerBBox.BoundingBox, obstaculo.BoundingBox);
                    if (result == TgcCollisionUtils.BoxBoxResult.Adentro ||
                        result == TgcCollisionUtils.BoxBoxResult.Atravesando)
                    {
                        collide = true;
                        break;
                    }
                }

                //Si hubo colision, restaurar la posicion anterior
                if (collide)
                {
                    playerBBox.Position = lastPos;
                }

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

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aquí todo el código referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>
        public override void Render()
        {
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();

            //Dibuja un texto por pantalla
            DrawText.drawText("Con la tecla F se dibuja el bounding box.", 0, 20, Color.OrangeRed);
            DrawText.drawText(
                "Con clic izquierdo subimos la camara [Actual]: " + TgcParserUtils.printVector3(Camara.Position), 0, 30,
                Color.OrangeRed);
            Piso.render();
            Techo.render();
            for (int i = 0; i < paredesXY; i++)
            {
                ParedXY[i].Transform = transformBox(ParedXY[i]);
                ParedXY[i].render();
                ParedNXY[i].Transform = transformBox(ParedNXY[i]);
                ParedNXY[i].render();
            }
            for (int i = 0; i < paredesYZ; i++)
            {
                ParedYZ[i].Transform = transformBox(ParedYZ[i]);
                ParedYZ[i].render();
                ParedNYZ[i].Transform = transformBox(ParedNYZ[i]);
                ParedNYZ[i].render();
            }
            for (int i = 1; i < paredesXY; i++)
            {
                for (int j = 0; j < paredesYZ; j++)
                {
                    if (wallMatXY[i-1, j])
                    {
                        ParedInternaXY[i - 1, j].Transform = transformBox(ParedInternaXY[i - 1, j]);
                        ParedInternaXY[i - 1, j].render();
                        //DecoWallXY[i - 1, j].render();
                    }
                    if (wallMatYZ[i-1, j])
                    {
                        ParedInternaYZ[i - 1, j].Transform = transformBox(ParedInternaYZ[i - 1, j]);
                        ParedInternaYZ[i - 1, j].render();
                        //DecoWallYZ[i - 1, j].render();
                    }
                }
            }
            for (int i = 0; i < paredesXY; i++)
            {
                for (int j = 0; j < paredesYZ; j++)
                {
                    if (skeletonsAp[i, j])
                    {
                        currentScene[i, j].Meshes[0].Transform = Matrix.Scaling(new Vector3(100,100,100));
                        currentScene[i, j].Meshes[0].render();
                    }
                }
            }

            //Piso2.render();
            //Piso3.render();
            //Siempre antes de renderizar el modelo necesitamos actualizar la matriz de transformacion.
            //Debemos recordar el orden en cual debemos multiplicar las matrices, en caso de tener modelos jerárquicos, tenemos control total.
            Box.Transform = Matrix.Scaling(Box.Scale) *
                            Matrix.RotationYawPitchRoll(Box.Rotation.Y, Box.Rotation.X, Box.Rotation.Z) *
                            Matrix.Translation(Box.Position);
            playerBBox.Transform = transformBox(playerBBox);
           /* playerBBox.Transform = Matrix.Scaling(Box.Scale) *
                            Matrix.RotationYawPitchRoll(Box.Rotation.Y, Box.Rotation.X, Box.Rotation.Z) *
                            Matrix.Translation(Box.Position);-*/
            playerBBox.BoundingBox.render();
            //A modo ejemplo realizamos toda las multiplicaciones, pero aquí solo nos hacia falta la traslación.
            //Finalmente invocamos al render de la caja
            Box.render();
            Box.BoundingBox.render();
             
            //currentScene.Meshes[0].render();
            
            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        /// <summary>
        ///     Se llama cuando termina la ejecución del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gráficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            //Dispose de la caja.
            Box.dispose();
            playerBBox.dispose();

            /*for (int i = 1; i < paredesXY; i++)
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
            }*/

                        for (int i = 0; i < paredesXY; i++)
                        {
                            for (int j = 0; j < paredesYZ; j++)
                            {
                                currentScene[i, j].Meshes[0].dispose();
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

        private Boolean intersectStoB(TgcSphere sphere, TgcBox box)
        {
            var bbox = box.BoundingBox;
            var bsphere = sphere.BoundingSphere;
            //punto mas cercano al centro de la esfera
            var x = Math.Max(bbox.PMin.X, Math.Min(bsphere.Center.X, bbox.PMax.X));
            var y = Math.Max(bbox.PMin.Y, Math.Min(bsphere.Center.Y, bbox.PMax.Y));
            var z = Math.Max(bbox.PMin.Z, Math.Min(bsphere.Center.Z, bbox.PMax.Z));

            //verificar si el punto esta dentro de la esfera
            var distance = Math.Sqrt((x - bsphere.Center.X) * (x - bsphere.Center.X) +
                                     (y - bsphere.Center.Y) * (y - bsphere.Center.Y) +
                                     (z - bsphere.Center.Z) * (z - bsphere.Center.Z));
            return distance < bsphere.Radius;
        }

        private Boolean intersectBtoB(TgcBox boxA, TgcBox boxB)
        {
            var a = boxA.BoundingBox;
            var b = boxB.BoundingBox;
            return ((a.PMin.X <= b.PMax.X && a.PMax.X >= b.PMin.X) &&
                   (a.PMin.Y <= b.PMax.Y && a.PMax.Y >= b.PMin.Y) &&
                   (a.PMin.Z <= b.PMax.Z && a.PMax.Z >= b.PMin.Z));
        }
    }
}