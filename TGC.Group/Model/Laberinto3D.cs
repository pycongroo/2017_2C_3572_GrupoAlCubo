using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using System.Collections.Generic;
using TGC.Core.Geometry;
using TGC.Core.Shaders;
using TGC.Core.Textures;
using TGC.Group.Model.MazeGenerator;

namespace TGC.Group.Model
{
    public class Laberinto3D
    {
        public static readonly int DimensionPorSector = 512;
        private GameModel gameModel;
        private Effect efecto = TgcShaders.Instance.TgcMeshPointLightShader;
        private Maze laberinto;
        private TgcPlane piso;
        private TgcPlane techo;
        private static readonly int ParedHorizontal = 1;
        private static readonly int ParedVertical = 2;
        private List<TgcBox> paredes = new List<TgcBox>();

        public Laberinto3D(GameModel gameModel, Maze laberinto)
        {
            this.gameModel = gameModel;
            this.laberinto = laberinto;
            this.Init();
        }

        private void Init()
        {
            var size = new Vector3(Laberinto3D.DimensionPorSector * laberinto.Width, 20, 
                Laberinto3D.DimensionPorSector * laberinto.Height);
            var textura = TgcTexture.createTexture(gameModel.MediaDir + "rock_floor2.jpg");
            this.piso = new TgcPlane(new Vector3(0, 0, 0), size, TgcPlane.Orientations.XZplane, textura);
            this.techo = new TgcPlane(new Vector3(0, Laberinto3D.DimensionPorSector - 1, 0), size, TgcPlane.Orientations.XZplane, textura);
            FabricarParedes();
            
        }

        public TgcBox CrearPared(int orientacion)
        {
            float largo = Laberinto3D.DimensionPorSector;
            float alto = Laberinto3D.DimensionPorSector;
            float ancho = 50;
            var size = orientacion == ParedVertical ? new Vector3(largo, alto, ancho) : 
                new Vector3(ancho, alto, largo);
            var textura = TgcTexture.createTexture(gameModel.MediaDir + "brick1_1.jpg");
            return TgcBox.fromSize(size, textura);

        }

        public void UbicarPared(TgcBox pared, CellState posicion, Point punto)
        {
            Vector3 ubicacion = new Vector3(0, 0, 0);
            if (posicion == CellState.Top)
            {
                ubicacion = new Vector3(punto.X * Laberinto3D.DimensionPorSector,
                    0.5f * Laberinto3D.DimensionPorSector, (punto.Y  + 0.5f) * Laberinto3D.DimensionPorSector);
            }
            if (posicion == CellState.Left)
            {
                ubicacion = new Vector3((punto.X + 0.5f) * Laberinto3D.DimensionPorSector,
                    0.5f * Laberinto3D.DimensionPorSector, punto.Y * Laberinto3D.DimensionPorSector);
            }
            if (posicion == CellState.Bottom)
            {
                ubicacion = new Vector3(punto.X * Laberinto3D.DimensionPorSector,
                    0.5f * Laberinto3D.DimensionPorSector, (punto.Y + 0.5f) * Laberinto3D.DimensionPorSector);
            }
            if (posicion == CellState.Right)
            {
                ubicacion = new Vector3((punto.X + 0.5f) * Laberinto3D.DimensionPorSector,
                    0.5f * Laberinto3D.DimensionPorSector, punto.Y * Laberinto3D.DimensionPorSector);
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
                        pared = CrearPared(Laberinto3D.ParedHorizontal);
                        UbicarPared(pared, CellState.Top, new Point(y, x));
                        paredes.Add(pared);

                    }
                    if (this.laberinto[x, y].HasFlag(CellState.Left))
                    {
                        pared = CrearPared(Laberinto3D.ParedVertical);
                        UbicarPared(pared, CellState.Left, new Point(y, x));
                        paredes.Add(pared);

                    }
                    
                }

            }
            for (var x = 0; x < this.laberinto.Width; x++)
            {
                pared = CrearPared(Laberinto3D.ParedHorizontal);
                UbicarPared(pared, CellState.Bottom, new Point(this.laberinto.Height, x));
                paredes.Add(pared);
            }
            for (var y = 0; y < this.laberinto.Height; y++)
            {
                pared = CrearPared(Laberinto3D.ParedVertical);
                UbicarPared(pared, CellState.Right, new Point(y, this.laberinto.Width));
                paredes.Add(pared);
            }

        }
        public void Render()
        {
            this.piso.Effect = efecto;
            this.piso.Technique = TgcShaders.Instance.getTgcMeshTechnique(this.piso.toMesh("piso").RenderType);
            this.piso.render();
            this.techo.Effect = efecto;
            this.techo.Technique = TgcShaders.Instance.getTgcMeshTechnique(this.techo.toMesh("techo").RenderType);
            this.techo.render();
            foreach (TgcBox pared in this.paredes)
            {
                pared.Transform = this.gameModel.transformBox(pared);
                pared.Effect = efecto;
                pared.Technique = TgcShaders.Instance.getTgcMeshTechnique(pared.toMesh("pared").RenderType);
                pared.render();
            }
        }
    }

}
