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
        }

        public void Render()
        {
            this.piso.Effect = efecto;
            this.piso.Technique = TgcShaders.Instance.getTgcMeshTechnique(this.piso.toMesh("piso").RenderType);
            this.piso.render();
            this.techo.Effect = efecto;
            this.techo.Technique = TgcShaders.Instance.getTgcMeshTechnique(this.techo.toMesh("techo").RenderType);
            this.techo.render();
        }
    }

}
