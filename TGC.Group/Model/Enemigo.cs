using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Geometry;
using TGC.Core.SceneLoader;
using TGC.Core.Utils;
using TGC.Core.Shaders;


namespace TGC.Group.Model
{
    public enum Direccion
    {
        Sur = 0,
        Este = 1,
        Norte = 2,
        Oeste = 3,
    }

    public class Enemigo
    {
        private const int LONGITUD_SECTOR = 512;
        private float velocidad;
        private List<Point> recorrido;
        public TgcMesh representacion;
        private Direccion sentidoAnterior;
        private static readonly float[,] rotacionCardinal;
        private static Random random = new Random();

        static Enemigo()
        {
            rotacionCardinal = new float[4, 4];
            for (int i = 0; i < 4; i++)
            {
                rotacionCardinal[i, i] = 0;
            }
            rotacionCardinal[(int)Direccion.Sur, (int)Direccion.Este] = -FastMath.PI_HALF;
            rotacionCardinal[(int)Direccion.Este, (int)Direccion.Sur] = FastMath.PI_HALF;
            rotacionCardinal[(int)Direccion.Sur, (int)Direccion.Norte] = FastMath.PI;
            rotacionCardinal[(int)Direccion.Norte, (int)Direccion.Sur] = -FastMath.PI;
            rotacionCardinal[(int)Direccion.Sur, (int)Direccion.Oeste] = FastMath.PI_HALF;
            rotacionCardinal[(int)Direccion.Oeste, (int)Direccion.Sur] = -FastMath.PI_HALF;
            rotacionCardinal[(int)Direccion.Este, (int)Direccion.Norte] = -FastMath.PI_HALF;
            rotacionCardinal[(int)Direccion.Norte, (int)Direccion.Este] = FastMath.PI_HALF;
            rotacionCardinal[(int)Direccion.Este, (int)Direccion.Oeste] = -FastMath.PI;
            rotacionCardinal[(int)Direccion.Oeste, (int)Direccion.Este] = FastMath.PI;
            rotacionCardinal[(int)Direccion.Norte, (int)Direccion.Oeste] = -FastMath.PI_HALF;
            rotacionCardinal[(int)Direccion.Oeste, (int)Direccion.Norte] = FastMath.PI_HALF;
        }

        public Enemigo(TgcMesh mesh, float velocidad, Maze laberinto, Vector3 scale)
        {
            this.representacion = mesh;
            this.representacion.Scale = scale;
            this.velocidad = velocidad;
            this.recorrido = laberinto.FindPath(new Point(random.Next(0, laberinto.Width - 1), random.Next(0, laberinto.Height - 1)), 
                new Point(random.Next(0, laberinto.Width - 1), random.Next(0, laberinto.Height - 1)));
            this.sentidoAnterior = Direccion.Sur;
            Representar();
        }

        private void Representar()
        {
           
            var color = Color.Red;
            //representacion = TgcBox.fromSize(size, color);
            representacion.AutoTransformEnable = true;
            representacion.setColor(color);
            
            Posicionar(this.recorrido[0]);

        }

        public Direccion Sentido(Point actual, Point proximo)
        {
            if (actual.X == proximo.X)
            {
                // Movimiento este o oeste
                return actual.Y - proximo.Y < 0 ? Direccion.Norte : Direccion.Sur;
            } else
            {
                // Movimiento norte o sur
                return actual.X - proximo.X < 0 ? Direccion.Este : Direccion.Oeste;
            }
        }

        public void Mover(float tiempo)
        {
            Point posicionActual = PosicionActual();
            Direccion hacia = Sentido(posicionActual, Proximo(posicionActual));
            float posicionEnBloque = 0;
             // Si paso la mitad del sector que recorre.
            if (hacia != this.sentidoAnterior)
            {
                if (this.sentidoAnterior == Direccion.Sur || this.sentidoAnterior == Direccion.Norte)
                {
                    posicionEnBloque = FastMath.Abs(this.representacion.Position.Z) % LONGITUD_SECTOR;
                }
                else
                {
                    posicionEnBloque = FastMath.Abs(this.representacion.Position.X) % LONGITUD_SECTOR;
                }

                if (posicionEnBloque > LONGITUD_SECTOR / 2)
                {
                    Posicionar(posicionActual);
                } else
                {
                    hacia = this.sentidoAnterior;
                }
                
            }
            float tiempoAcotado = tiempo > 2 ? 2 : tiempo;
            float cantidad = tiempoAcotado * velocidad;
            if (hacia == Direccion.Sur)
            {
                representacion.move(0, 0, -cantidad);
            }
            if (hacia == Direccion.Norte)
            {
                representacion.move(0, 0, cantidad);
            }
            if (hacia == Direccion.Este)
            {
                representacion.move(cantidad, 0, 0);
            }
            if (hacia == Direccion.Oeste)
            {
                representacion.move(-cantidad, 0, 0);
            }
            
        }

        private Point Proximo(Point posicionActual)
        {
            Point proximo = new Point();
            for (int i = 0; i < this.recorrido.Count; i++)
            {
                if (posicionActual.Equals(this.recorrido[i]))
                {
                    if (i + 1 < this.recorrido.Count)
                    {
                        proximo = this.recorrido[i + 1];
                        break;
                    } else
                    {
                        throw new Exception("Fin de recorrido");
                    }
                }
            }
            return proximo;
        }

        private Point PosicionActual()
        {
            return new Point( (int) representacion.Position.X / LONGITUD_SECTOR, (int) representacion.Position.Z / LONGITUD_SECTOR);
        }

        public void Posicionar(Point donde)
        {
            // TODO Verificar que el recorrido posea al menos 2 elementos.
            Direccion hacia = Sentido(donde, Proximo(donde));
            
            float angulo = Enemigo.rotacionCardinal[(int)this.sentidoAnterior, (int)hacia];

            this.sentidoAnterior = hacia;

            var deltaX = (LONGITUD_SECTOR - representacion.BoundingBox.calculateSize().X) / 2;
            var deltaZ = (LONGITUD_SECTOR - representacion.BoundingBox.calculateSize().Z) / 2;

            Vector3 posicion = TraducirCoordenada(donde);
            if (hacia == Direccion.Sur || hacia == Direccion.Norte)
            {
                posicion.X = posicion.X + deltaX;
                posicion.Z = posicion.Z + deltaZ;
            } else
            {
                posicion.Z = posicion.Z + deltaX;
                posicion.X = posicion.X + deltaZ;
            }
            posicion.Y += 80;
            posicion.X += 85;
            posicion.Z += 85;
            var size = new Vector3(6, 6, 6);
            representacion.Scale = size;
            representacion.rotateY(angulo);
            representacion.Position = posicion;

        }

        private Vector3 TraducirCoordenada(Point coordenada)
        {
            return new Vector3(coordenada.X * LONGITUD_SECTOR, -40, coordenada.Y * LONGITUD_SECTOR);
        }

        public void Render(Effect efecto)
        {
            if (efecto != null)
            {
                representacion.Effect = efecto;
                representacion.Technique = TgcShaders.Instance.getTgcMeshTechnique(representacion.RenderType);
            }
            this.representacion.render();
        }

        public void Dispose()
        {
            representacion.dispose();
        }
    }
}
