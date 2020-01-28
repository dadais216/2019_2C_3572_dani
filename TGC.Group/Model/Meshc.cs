using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Group.Model;

namespace TGC.Group
{
    //idealmente deberia hacer mi propia mesh para no arrastrar un monton de cosas que no uso
    public class Meshc
    {
        public TgcMesh mesh;
        public Parallelepiped paralleliped;


        //no tengo claro si es mas rapido tener un mesh propio en cada meshc o 
        //tener uno solo e ir aplicando transformaciones.
        //Por lo que probe, con 200 arboles antes de hacer ninguna optimizacion:
        //un mesh en cada meshc, transformaciones: 33fps
        //uno solo, transformaciones: 33fps
        //un mesh en cada meshc, sin transformaciones: 33fps
        //parece ser que da lo mismo
        //creo que prefiero tirarme por tener un solo mesh porque en teoria deberia ser mas rapido, 
        //y no estoy ganando ningun beneficio por tener copias. No parece importar mucho igual


        public TGCMatrix originalMesh;
        public TGCMatrix deformation;
        //probar guardar originalMesh*deformation tambien, para no calcularlo por cada mesh en render

        public int lastFrameDrawn = -1;


        //usa este metodo para hacer transformaciones, todo lo que viene de tgcMesh no se usa
        //me parece que en la version final voy a preferir tener solo la transformacion y modificarla de a poco,
        //porque voy a querer tener transformaciones distintas para cada cosa y seria mas comodo

        static public TGCMatrix multMatrix(float f,TGCMatrix m)//lo ideal seria tener algo inplace y simd pero bueno
        {
            var r=new TGCMatrix();
            r.M11 = m.M11 * f;
            r.M12 = m.M12 * f;
            r.M13 = m.M13 * f;
            r.M21 = m.M21 * f;
            r.M22 = m.M22 * f;
            r.M23 = m.M23 * f;
            r.M31 = m.M31 * f;
            r.M32 = m.M32 * f;
            r.M33 = m.M33 * f;

            r.M42 = m.M42 * f;
            return r;
        }

        public void deform()
        {
            paralleliped.transform(multMatrix(g.map.deforming, deformation) + originalMesh);

            //como las cosas se deforman despacio es seguro no actualizar los chunks por un tiempo relativamente largo
            if(g.game.actualFrame%180==0)//sería preferible que no se hagan todos en un mismo frame, pero es
                //dificil testear la velocidad y preferiria no agregar otra variable para esto sin saber como afecta
                g.chunks.addVertexFall(this);
        }
        public void renderAndDeform()
        {
            if (lastFrameDrawn != g.game.actualFrame)
            {
                lastFrameDrawn = g.game.actualFrame;
                if (GameModel.debugMeshes)
                {
                    mesh.Transform = multMatrix(g.map.deforming, deformation) + originalMesh;//mesh se transforma siempre porque se comparte
                    mesh.Render();
                }

                deform();
            }

        }

        //solo valido despues de actualizar transform
        public TGCVector3 position()
        {
            return new TGCVector3(mesh.Transform.M41,mesh.Transform.M42,mesh.Transform.M43);
            //return new TGCVector3(originalMesh.M41,originalMesh.M42,originalMesh.M43);
        }
        public void renderDebugColission()
        {
            paralleliped.renderAsPolygons();
        }
    }
    public class MultiMeshc
    {
        //necesito tener otro tipo de mesh para manejar estructuras que contengan varios mesh y colisiones
        //estaba entre hacer otro tipo, usar polimorfismo o hacer que todos los meshc contengan varios mesh y colisiones
        //el polimorfismo seria mas limpio pero probablemente sea lento
        //en el tercero tener que usar un bucle for para cada meshc comun, que son la mayoria, tambien. 
        //Creo que tener 2 tipos aparte va a ser lo mas rapido, aunque es lo mas feo

        //una desventaja de tener multimesh como una estructura toda junta es que se dibuja toda o no se dibuja nada,
        //no creo que tenga mucha importancia
        //una ventaja es que la carga es mas simple, tengo una cosa con todas las mesh y colisiones. Manejar una transformacion
        //coordinada tambien va a ser mas comodo

        public TgcMesh[] meshes;
        public Parallelepiped[] parallelipeds;

        public TGCMatrix originalMesh;
        public TGCMatrix deformation;

        public int lastFrameDrawn = -1;

        public void deform()
        {
            foreach (var paralleliped in parallelipeds)
            {
                paralleliped.transform(Meshc.multMatrix(g.map.deforming, deformation) + originalMesh);
                if (g.game.actualFrame % 180 == 0)
                    g.chunks.addVertexFall(paralleliped, this);
            }
        }
        public void render()
        {
            if (lastFrameDrawn != g.game.actualFrame)
            {
                lastFrameDrawn = g.game.actualFrame;
                if (GameModel.debugMeshes)
                {
                    foreach (var mesh in meshes)
                    {
                        mesh.Transform = Meshc.multMatrix(g.map.deforming, deformation) + originalMesh;//mesh se transforma siempre porque se comparte

                        mesh.Render();
                    }
                }
                deform();

                if (false) //@todo agregar boton
                {
                    foreach (var par in parallelipeds)
                    {
                        Action<TGCVector3,Color> drawLine = (v,c) =>
                         {
                             var p1 = v * 1; p1.Y = -40000;
                             var p2 = v * 1; p2.Y = 4000;

                             var line = TgcLine.fromExtremes(p1, p2);
                             line.Color = c;
                             line.updateValues();
                             line.Render();
                         };

                        foreach(var vertex in par.transformedVertex)
                        {
                            drawLine(vertex,Color.Gold);
                        }

                        Action<int, int, float> drawExtraVertexFall = (v1, v2, w1) =>
                        {
                            float w2 = 1f - w1;
                            var vertex = new TGCVector3(par.transformedVertex[v1].X * w1 + par.transformedVertex[v2].X * w2,
                                                      0,
                                                      par.transformedVertex[v1].Z * w1 + par.transformedVertex[v2].Z * w2);
                            drawLine(vertex,Color.BlanchedAlmond);
                        };

                        drawExtraVertexFall(0, 4, .5f);
                        drawExtraVertexFall(0, 1, .5f);
                        drawExtraVertexFall(1, 5, .5f);
                        drawExtraVertexFall(4, 5, .5f);//@optim puede que tirar vertex del piso no sea necesario, ver al final

                        drawExtraVertexFall(2, 6, .5f);
                        drawExtraVertexFall(2, 3, .5f);
                        drawExtraVertexFall(3, 7, .5f);
                        drawExtraVertexFall(6, 7, .5f);

                        drawExtraVertexFall(2, 7, .5f);
                        drawExtraVertexFall(2, 7, .25f);
                        drawExtraVertexFall(2, 7, .75f);

                        drawExtraVertexFall(4, 1, .5f);
                        drawExtraVertexFall(4, 1, .25f);
                        drawExtraVertexFall(4, 1, .75f);
                    }
                }
            }
        }
    }
}
