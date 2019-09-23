using System;
using System.Collections.Generic;
using TGC.Core.Mathematica;
using static TGC.Group.Model.Chunks;

namespace TGC.Group.Model
{
    public class Chunks
    {
        public class Chunk
        {
            public List<Meshc> meshes = new List<Meshc>();
            //en la primera aproximacion le mando la meshc entera. Supongo que seria mejor tener todo aplanado
            //pero no sé hasta donde me deja c# hacer cosas
        }
        public const float MapSquareRad = 50000f;//deberia ser lo mismo que terrain
        public const float chunkLen= 10000f;
        public const int chunksPerDim = ((int)MapSquareRad / (int)chunkLen)*2;

        public Chunk[,] chunks = new Chunk[chunksPerDim, chunksPerDim];
        public Chunks()
        {
            for(int i=0;i<chunksPerDim;i++)
                for(int j = 0; j < chunksPerDim; j++)
                {
                    chunks[i, j] = new Chunk();
                }
        }
        public Chunk fromCoordinates(TGCVector3 pos, bool print=false)
        {

            var x = (int)FastMath.Floor(pos.X / chunkLen) + chunksPerDim / 2;
            var z = (int)FastMath.Floor(pos.Z / chunkLen) + chunksPerDim / 2;


            if (x >= 0 && z >= 0 && x < chunksPerDim && z < chunksPerDim)
                //este chequeo esta porque estoy boludeando con deformaciones,
                //en el juego final no creo que sea necesario
            {
                var ret = chunks[x, z];
                if(print)
                    Logger.Log(ret.meshes.Count.ToString() + "    " + x.ToString() + "   " + z.ToString());

                return ret;
            }
            return null;
        }
        public void addVertexFall(Meshc meshc)
        {
            foreach(TGCVector3 vertex in meshc.paralleliped.transformedVertex)
            {
                Chunk meshList = fromCoordinates(vertex);
                if (meshList!=null&&!meshList.meshes.Contains(meshc))
                {
                    meshList.meshes.Add(meshc);
                }
            }
        }
        public void render(TGCMatrix matriz)
        {
            foreach(Chunk chunk in chunks)
            {
                foreach(Meshc meshc in chunk.meshes)
                {
                    meshc.transform(matriz);
                    meshc.mesh.Render();
                    //meshc.paralleliped.renderAsPolygons();
                }
            }
        }
    }
}
