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
            public List<MultiMeshc> multimeshes = new List<MultiMeshc>();
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
                        //meshc.render();

                        if (meshc == g.camera.setToRemove)
                        {
                            Console.WriteLine("O-O");
                        }

                        if (g.map.isCandle(meshc))
                        {
                            //if (g.map.lightIndex >= Map.lightCount)//optimizacion, se podria dejar el branch de true
                            //{
                                //se priorizan las mas cercanas
                                //creo que es mas rapido buscar el maximo cada vez que mantener la lista ordenada, no sé
                                int maxIndex = 0;
                                float maxDistSq = TGCVector3.LengthSq(g.map.lightPosition[0] - g.camera.eyePosition);
                                for (int i = 1; i < Map.lightCount; i++)
                                {
                                    if (g.map.lightPosition[i] == meshc.position())
                                    //si la luz ya esta no volverla a poner
                                    //una luz puede estar mas de una vez porque se reutilizan las de frames previos cuando
                                    //en el triangulo actual hay menos de 9
                                    {
                                        goto loop;
                                    }
                                    var dist = TGCVector3.LengthSq(g.map.lightPosition[i] - g.camera.eyePosition);
                                    if (dist > maxDistSq)
                                    {
                                        maxDistSq = dist;
                                        maxIndex = i;
                                    }
                                }
                                if (TGCVector3.LengthSq(meshc.position() - g.camera.eyePosition) < maxDistSq)
                                    g.map.lightPosition[maxIndex] = meshc.position();
                            //}
                            //else
                            //{
                            //    g.map.lightPosition[g.map.lightIndex++] = meshc.position();
                            //}
                        }
                        loop:;
                        meshc.render();
                        //hay un bug bastante oscuro
                        //todas las luces se van hacia el jugador, porque de alguna manera eyePosition cae
                        //en todos los lightPositions. No sé por que. 
                        //si render esta antes de todo no pasa, no entiendo por que.
                        
                        //hay un bug con el bucle, la primera posicion no se compara con los meshc y genera duplicados
                        //eso no explica por que pasa lo otro igual

                        //pareciera que eyeposition se filtra por lo menos al primero porque de alguna manera se sigue
                        //accediendo al mesh, que ahora esta en la mano, aunque no se deberia porque se borro del chunk
                        //y se esta accediendo apartir del chunk

                        //una vela puede quedar duplicada por estar justo en 2 chunks, creo que esto no tiene nada que ver

                    }
                    foreach(MultiMeshc meshc in multimeshes)
                    {
                        meshc.render();
                    }
                }
                if(GameModel.debugChunks)
                    renderDebug();
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
                foreach (MultiMeshc meshc in multimeshes)
                {
                    meshc.renderDebugColission();
                }
            }
        }
        public const float MapSquareRad = 410000f;//deberia ser lo mismo que terrain
        public const float chunkLen= 10000f;
        public const int chunksPerDim = ((int)MapSquareRad / (int)chunkLen)*2;
        public Chunk[,] chunks = new Chunk[chunksPerDim, chunksPerDim];

        public Chunks()
        {
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
                    Console.WriteLine(ret.meshes.Count.ToString() + "    " + x.ToString() + "   " + z.ToString());

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
                Chunk c = fromCoordinates(vertex);
                if (c!=null&&!c.meshes.Contains(meshc))
                {
                    c.meshes.Add(meshc);
                }
            }
        }
        public void addVertexFall(Parallelepiped par, MultiMeshc meshc)
        //aca podria haber usado las templates pero las tengo que aprender a escribir
        // y son como las de scala y es mas rapido copiar y pegar
        {
            foreach (TGCVector3 vertex in par.transformedVertex)
            {
                Chunk c = fromCoordinates(vertex);
                if (c != null && !c.multimeshes.Contains(meshc))
                {
                    c.multimeshes.Add(meshc);
                }
            }
        }

        public void render()
        {

            //por ahora lo dejo aca a este codigo 
            //se separa la transformacion de el render porque la transformacion aplica a chunks que no estan siendo
            //renderizados tambien. Ahora se recorren todos, en la version final se van a actualizar algunos con 
            //distintas tranformaciones

            if (Meshc.matrizChange)
            {
                foreach (Chunk chunk in chunks)
                {
                    foreach(Meshc m in chunk.meshes)
                    {
                        m.transformColission();
                    }
                    foreach (MultiMeshc m in chunk.multimeshes)
                    {
                        m.transformColission();
                    }
                }
                Meshc.matrizChange = false;
            }




            var s = toIndexSpace(g.camera.eyePosition);
            chunks[s.i, s.j].render();
            chunks[s.i+1, s.j].render();
            chunks[s.i+1, s.j+1].render();
            chunks[s.i, s.j+1].render();
            chunks[s.i-1, s.j+1].render();
            chunks[s.i-1, s.j].render();
            chunks[s.i-1, s.j-1].render();
            chunks[s.i, s.j-1].render();
            chunks[s.i+1, s.j-1].render();

            int chunksInTriangle = 0;
            foreach (Chunk chunk in chunks)
            {
                if (g.camera.triangle.enclosesPoint(chunk.center))
                {
                    chunksInTriangle++;
                    chunk.render();
                }
            }
            //Console.WriteLine(chunksInTriangle.ToString());

            //Logger.Log(s.i.ToString() + "  " + s.j.ToString() + "  " + chunksPerDim);




            if(GameModel.debugChunks)
                g.camera.triangle.render();


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

    }
}
