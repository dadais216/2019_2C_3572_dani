using System;
using System.Collections.Generic;
using System.Drawing;
using TGC.Core.Geometry;
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
            public TGCVector3 center;
            public Color color;
            public int lastDrawnFrame=-1; //asegura que no se dibuje 2 veces. Despues implementar lo mismo a nivel mesh

            public void render()
            {
                if (lastDrawnFrame != GameModel.actualFrame)
                {
                    lastDrawnFrame = GameModel.actualFrame;
                    foreach (Meshc meshc in meshes)
                    {
                        meshc.render();
                    }
                }
            }
        }
        public const float MapSquareRad = 60000f;//deberia ser lo mismo que terrain
        public const float chunkLen= 10000f;
        public const int chunksPerDim = ((int)MapSquareRad / (int)chunkLen)*2;
        public Chunk[,] chunks = new Chunk[chunksPerDim, chunksPerDim];


        Camera.Camera camera;

        public Chunks(Camera.Camera camera_)
        {
            camera = camera_;
            var random = new Random();

            for (int i=0;i<chunksPerDim;i++)
                for(int j = 0; j < chunksPerDim; j++)
                {
                    chunks[i, j] = new Chunk();
                    chunks[i, j].center = new TGCVector3((i+.5f) * chunkLen-MapSquareRad,0, (j+.5f) * chunkLen - MapSquareRad);

                    chunks[i, j].color=Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
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

        public struct int2
        {
            public int i;
            public int j;
        }
        public int2 toIndexSpace(TGCVector3 pos)
        {
            var ret = new int2();
            ret.i = (int)FastMath.Floor(pos.X / chunkLen) + chunksPerDim / 2;
            ret.j = (int)FastMath.Floor(pos.Z / chunkLen) + chunksPerDim / 2;
            return ret;
        }
        public void addVertexFall(Meshc meshc)
        {
            //esto solo funciona si se una sola deformacion continua. Si se hacen 2 una despues de la otra
            //puede que la primera deformacion haya separado los vertices por mas de un chunk, 
            //y haya chunks por los que pase el paralleliped que no se registren
            //por lo que me tengo que limitar a una sola deformacion, 
            //o a varias que no separen los vertices mas del largo de un chunk
            foreach(TGCVector3 vertex in meshc.paralleliped.transformedVertex)
            {
                Chunk meshList = fromCoordinates(vertex);
                if (meshList!=null&&!meshList.meshes.Contains(meshc))
                {
                    meshList.meshes.Add(meshc);
                }
            }
        }
        public void render()
        {
            
            foreach (Chunk chunk in chunks)
            {
                var p0 = new TGCVector3(chunk.center.X, -10000, chunk.center.Z);
                var p1 = new TGCVector3(chunk.center.X, 100000, chunk.center.Z);
                var line = TgcLine.fromExtremes(p0, p1, Color.Pink);
                line.Render();

                var s = toIndexSpace(camera.eyePosition);
                chunks[s.i, s.j].render();
                chunks[s.i+1, s.j].render();
                chunks[s.i+1, s.j+1].render();
                chunks[s.i, s.j+1].render();
                chunks[s.i-1, s.j+1].render();
                chunks[s.i-1, s.j].render();
                chunks[s.i-1, s.j-1].render();
                chunks[s.i, s.j-1].render();
                chunks[s.i+1, s.j-1].render();

                if (camera.triangle.enclosesPoint(chunk.center))
                {
                    chunk.render();
                }
            }




            //renderChunks();
            camera.triangle.render();





            //foreach (Chunk chunk in chunks)
            //{
            //        foreach (Meshc meshc in chunk.meshes)
            //        {
            //            meshc.transform(matriz);
            //            meshc.mesh.Render();
            //            //meshc.paralleliped.renderAsPolygons();
            //        }
            //}


            //foreach (Meshc meshc in fromCoordinates(camera.eyePosition).meshes)
            //{
            //    meshc.transform(matriz);
            //    meshc.mesh.Render();
            //    meshc.paralleliped.renderAsPolygons();
            //}
        }

        public void renderChunks()
        {
            
                var y = camera.eyePosition.Y + 1800f;
                var r = chunkLen/2;

                var poly = new TgcConvexPolygon();
                var face = new TGCVector3[4];
            foreach(var chunk in chunks)
            {
                var c = chunk.center;
                poly.Color = chunk.color;

                face[0] = new TGCVector3(c.X + r, y, c.Z + r);
                face[1] = new TGCVector3(c.X + r, y, c.Z - r);
                face[2] = new TGCVector3(c.X - r, y, c.Z - r);
                face[3] = new TGCVector3(c.X - r, y, c.Z + r);

                poly.BoundingVertices = face;
                poly.updateValues();
                poly.Render();
            }
            
        }
    }
}
