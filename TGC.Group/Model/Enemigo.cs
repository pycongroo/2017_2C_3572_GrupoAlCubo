using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Geometry;
using TGC.Core.Utils;

namespace TGC.Group.Model
{
    public enum Direccion
    {
        Sur,
        Este,
        Oeste,
        Norte,
    }

    public class Enemigo
    {
        private const int LONGITUD_SECTOR = 512;
        private float velocidad;
        private List<Point> recorrido;
        private TgcBox representacion;
        private Direccion sentidoAnterior;

        public Enemigo(float velocidad, List<Point> recorrido)
        {
            this.velocidad = velocidad;
            this.recorrido = recorrido;
            Representar();
        }

        private void Representar()
        {
            var size = new Vector3(100, 200, 40);
            var color = Color.Red;
            representacion = TgcBox.fromSize(size, color);
            representacion.AutoTransformEnable = true;
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
            
            float angulo = 0;
            // TODO Corregir el ángulo a partir del sentido anterior. Por defecto toma el sur.
            if (hacia == Direccion.Sur) angulo = 0;
            if (hacia == Direccion.Norte) angulo = FastMath.PI;
            if (hacia == Direccion.Este) angulo = -FastMath.PI_HALF;
            if (hacia == Direccion.Oeste) angulo = FastMath.PI_HALF;

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


            representacion.rotateY(angulo);
            representacion.Position = posicion;
        }

        private Vector3 TraducirCoordenada(Point coordenada)
        {
            return new Vector3(coordenada.X * LONGITUD_SECTOR, 100, coordenada.Y * LONGITUD_SECTOR);
        }

        public void Render()
        {
            this.representacion.render();
        }
    }
}
