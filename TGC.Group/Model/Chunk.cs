using System;
using System.Collections.Generic;
using System.Drawing;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using static TGC.Group.Model.Chunks;

namespace TGC.Group.Model
{
    public class Chunks
    {
        public class Chunk
        {
            public List<Meshc> meshes = new List<Meshc>();
            public List<MultiMeshc> multimeshes = new List<MultiMeshc>();
            //nunca se da en el juego que haya mas de un multimesh por chunk asi que esto podría no ser una lista
            public List<Parallelepiped> parallelepipedsOfMultimesh = new List<Parallelepiped>();
            //solo guardo los parallelipepeds del multimesh que estan en este chunk, para no colisionar con los demas
            //al pedo


            public TGCVector3 center;
            public Color color;
            public int lastDrawnFrame = -1; //asegura que no se dibuje 2 veces. Despues implementar lo mismo a nivel mesh

            public void render()
            {
                if (lastDrawnFrame != g.game.actualFrame)
                {
                    lastDrawnFrame = g.game.actualFrame;
                    foreach (Meshc meshc in meshes)
                    {
                        meshc.renderAndDeform();
                        //se tiene que renderizar primero porque se esta usando el Transform de mesh, que se comparte
                        //con todas las velas

                        if (g.map.isCandle(meshc))
                        {
                            g.map.maybeLightCandleAt(meshc.position());
                        }

                        meshc.deform();
                    }
                    foreach (MultiMeshc meshc in multimeshes)
                    {
                        meshc.renderAndDeform();
                    }
                }
            }
            public void renderForShadow()
            {
                foreach (Meshc meshc in meshes)
                {
                    g.shadow.renderMesh(meshc);
                }
                foreach (MultiMeshc meshc in multimeshes)
                {
                    
                    g.shadow.renderMesh(meshc);
                }
            }
            public void renderDebug()
            {
                var y = g.camera.eyePosition.Y + 6800f;
                var r = chunkLen / 2;

                var poly = new TgcConvexPolygon();
                var face = new TGCVector3[4];


                poly.Color = color;

                face[0] = new TGCVector3(center.X + r, y, center.Z + r);
                face[1] = new TGCVector3(center.X + r, y, center.Z - r);
                face[2] = new TGCVector3(center.X - r, y, center.Z - r);
                face[3] = new TGCVector3(center.X - r, y, center.Z + r);

                poly.BoundingVertices = face;
                poly.updateValues();
                poly.Render();
            }
            public void renderDebugColission()
            {
                foreach (Meshc meshc in meshes)
                {
                    meshc.renderDebugColission();
                }
                foreach (Parallelepiped par in parallelepipedsOfMultimesh)
                {
                    par.renderAsPolygons();
                }
            }
        }
        public const float MapSquareRad = 410000f;//deberia ser lo mismo que terrain
        public const float chunkLen = 10000f;
        public const int chunksPerDim = ((int)MapSquareRad / (int)chunkLen) * 2;
        public Chunk[,] chunks = new Chunk[chunksPerDim, chunksPerDim];

        public Chunks()
        {
            var random = new Random();

            for (int i = 0; i < chunksPerDim; i++)
                for (int j = 0; j < chunksPerDim; j++)
                {
                    chunks[i, j] = new Chunk();
                    chunks[i, j].center = new TGCVector3((i + .5f) * chunkLen - MapSquareRad, 0, (j + .5f) * chunkLen - MapSquareRad);

                    chunks[i, j].color = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
                }
        }
        public Chunk fromCoordinates(TGCVector3 pos)
        {

            var x = (int)FastMath.Floor(pos.X / chunkLen) + chunksPerDim / 2;
            var z = (int)FastMath.Floor(pos.Z / chunkLen) + chunksPerDim / 2;


            if (x >= 0 && z >= 0 && x < chunksPerDim && z < chunksPerDim)
            //este chequeo esta porque estoy boludeando con deformaciones,
            //en el juego final no creo que sea necesario
            {
                var ret = chunks[x, z];

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
            foreach (TGCVector3 vertex in meshc.paralleliped.transformedVertex)
            {
                Chunk c = fromCoordinates(vertex);
                if (c != null && !c.meshes.Contains(meshc))
                {
                    c.meshes.Add(meshc);
                }
            }
        }
        public void addVertexFall(Parallelepiped par, MultiMeshc meshc)
        {
            Action<TGCVector3> addToChunk = v =>
            {
                Chunk c = fromCoordinates(v);
                if (c != null)
                {
                    if (!c.multimeshes.Contains(meshc))
                    {
                        c.multimeshes.Add(meshc);
                        c.parallelepipedsOfMultimesh.Add(par);
                    }
                    else
                    {
                        if (!c.parallelepipedsOfMultimesh.Contains(par))
                        {
                            c.parallelepipedsOfMultimesh.Add(par);
                        }
                    }
                }
            
            };
            foreach (TGCVector3 vertex in par.transformedVertex)
            {
                addToChunk(vertex);
            }

            Action<int, int,float> extraVertex = (v1, v2, w1) =>
             {
                 float w2 = 1f - w1;
                 var vertex = new TGCVector3(par.transformedVertex[v1].X * w1 + par.transformedVertex[v2].X * w2,
                                           0,
                                           par.transformedVertex[v1].Z * w1 + par.transformedVertex[v2].Z * w2);
                 addToChunk(vertex);
            };
            extraVertex(0, 4, .5f);
            extraVertex(0, 1, .5f);
            extraVertex(1, 5, .5f);
            extraVertex(4, 5, .5f);//@optim puede que tirar vertex del piso no sea necesario, ver al final

            extraVertex(2, 6, .5f);
            extraVertex(2, 3, .5f);
            extraVertex(3, 7, .5f);
            extraVertex(6, 7, .5f);

            extraVertex(2, 7, .5f);
            extraVertex(2, 7, .25f);
            extraVertex(2, 7, .75f);

            extraVertex(4, 1, .5f);
            extraVertex(4, 1, .25f);
            extraVertex(4, 1, .75f);



        }

        public void render()
        {

            //por ahora lo dejo aca a este codigo 
            //se separa la transformacion de el render porque la transformacion aplica a chunks que no estan siendo
            //renderizados tambien. Ahora se recorren todos, en la version final se van a actualizar algunos con 
            //distintas tranformaciones


            int chunksRendered = 0;

            var eyeDir = g.camera.cameraRotatedTarget;
            eyeDir.Y = 0;//ignorar y
            eyeDir.Normalize();

            var ortogDir = TGCVector3.Cross(eyeDir,TGCVector3.Up);


            //la idea inicial es tener un generar una cuadricula del tamaño de chunk con estos 2 vectores como base
            //y por cada interseccion conseguir el chunk y renderizarlo
            //funciona si los puntos estan alineados con los chunks, si no lo estan puede haber 2 puntos por chunk
            //o ninguno.
            //si se avanza por chunkLen * 1 / sqrt( 2 ) se asegura que no va a haber agujeros, pero se va a estar
            //tocando los chunks con 2 puntos casi siempre, pero bueno. Para no renderizar 2 veces controlo que
            //el ultimo frame de renderizado no sea el actual. 


            //el factor de avance podría estar en funcion de que tan alineado se esta, siendo 1 si se esta alineado
            //y 1/sqrt(2) si se esta totalmente desalineado.
            //float distFactor = 0.707f + (float)Math.Abs(Math.Cos(Camera.Camera.leftrightRot * 2f))* (1f-0.707f);

            float distFactor = 0.707f;

            Action<int, int> drawHor = (along, side) =>
             {
                 for (int j = -side; j <= side; j++)
                 {
                     var pos = eyeDir * chunkLen * along * distFactor
                             + ortogDir * chunkLen * j * distFactor
                             + g.camera.eyePosition;


                     Chunk c = fromCoordinates(pos);
                     if (c != null)
                     {
                         c.render();
                         chunksRendered++;
                         if (GameModel.debugChunks)
                         {
                             var p1 = pos * 1; p1.Y = -40000;
                             var p2 = pos * 1; p2.Y = 4000;

                             var line = TgcLine.fromExtremes(p1, p2);
                             line.Color = Color.BlanchedAlmond;
                             line.Render();
                             c.renderDebug();
                         }
                     }
                 }
             };

            //@todo agregar version hd que suma mas

            //@todo se podrían dibujar los chunks lejanos solo si hay alguna luz ahi, teniendo en cuenta
            //tambien los entremedios

            drawHor(-2,1);
            drawHor(-1, 2);
            int cutOut = 8;
            for(int i = 0; i < cutOut; i++)
            {
                drawHor(i, (i+1)/2+1);
            }
            for (int i = cutOut; i < 18; i++)
            {
                drawHor(i, 6);
            }


            /*var size = 9;
            for(float i=-size;i<size+1;i+=.707f)
                for(float j = -size; j < size+1; j+=.707f)
                {
                    var pos = eyeDir * chunkLen * i + ortogDir * chunkLen * j;// + g.camera.eyePosition;

                    var p1 = pos * 1;p1.Y = -40000;
                    var p2 = pos * 1;p2.Y = 4000;

                    var line = TgcLine.fromExtremes(p1, p2);
                    line.Color=Color.BlanchedAlmond;
                    line.Render();



                    Chunk c = fromCoordinates(pos);
                    if (c != null)
                    {
                        c.render();
                        chunksRendered++;
                    }
                }*/



            //Console.WriteLine(chunksRendered.ToString());






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

        public void renderForShadow()
        {
            var dir = g.camera.eyePosition - g.mostro.pos;

            int2 index = toIndexSpace(g.camera.eyePosition);

            if (index.i == 0) index.i++;
            if (index.j == 0) index.j++;
            if (index.i == chunksPerDim) index.i--;
            if (index.j == chunksPerDim) index.j--;

            //@todo 4 chunks en vez de 9

            chunks[index.i-1, index.j-1].renderForShadow();
            chunks[index.i-1, index.j  ].renderForShadow();
            chunks[index.i-1, index.j+1].renderForShadow();
            chunks[index.i  , index.j-1].renderForShadow();
            chunks[index.i  , index.j  ].renderForShadow();
            chunks[index.i  , index.j+1].renderForShadow();
            chunks[index.i+1, index.j-1].renderForShadow();
            chunks[index.i+1, index.j  ].renderForShadow();
            chunks[index.i+1, index.j+1].renderForShadow();


        }

    }
}
