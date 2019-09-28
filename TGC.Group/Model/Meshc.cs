using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Group.Model;

namespace TGC.Group
{
    //idealmente deberia hacer mi propia mesh para no arrastrar un monton de cosas que no uso
    public class Meshc
    {
        public static Chunks chunks;
        public static bool matrizChange = true;//eventualmente cambiar por el mecanismo que use para determinar cuando hacer
        //vertexfall de un meshc particular


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


        public int lastFrameDrawn = -1;
        public int lastFrameColissionT = -1;


        //usa este metodo para hacer transformaciones, todo lo que viene de tgcMesh no se usa
        //me parece que en la version final voy a preferir tener solo la transformacion y modificarla de a poco,
        //porque voy a querer tener transformaciones distintas para cada cosa y seria mas comodo



        public void transformColission()
        {
            if (matrizChange && lastFrameColissionT != GameModel.actualFrame)
            {
                lastFrameColissionT = GameModel.actualFrame;
                paralleliped.transform(GameModel.matriz * originalMesh);
                chunks.addVertexFall(this);
            }
        }
        public void render()
        {
            if (lastFrameDrawn != GameModel.actualFrame)
            {
                lastFrameDrawn = GameModel.actualFrame;
                if (GameModel.debugMeshes)
                {
                    mesh.Transform = GameModel.matriz * originalMesh;//mesh se transforma siempre porque se comparte
                    mesh.Render();
                }

                if (GameModel.debugColission)
                    paralleliped.renderAsPolygons();
            }

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

        public int lastFrameDrawn = -1;
        public int lastFrameColissionT = -1;

        public void transformColission()
        {
            if (Meshc.matrizChange && lastFrameColissionT != GameModel.actualFrame)
            {
                lastFrameColissionT = GameModel.actualFrame;
                foreach (var paralleliped in parallelipeds)
                {
                    paralleliped.transform(GameModel.matriz * originalMesh);
                    Meshc.chunks.addVertexFall(paralleliped, this);
                }
            }
        }
        public void render()
        {
            if(lastFrameDrawn != GameModel.actualFrame)
            {
                lastFrameDrawn = GameModel.actualFrame;
                if (GameModel.debugMeshes)
                {
                    foreach (var mesh in meshes)
                    {
                        mesh.Transform = GameModel.matriz * originalMesh;//mesh se transforma siempre porque se comparte
                        mesh.Render();
                    }
                }
                if (GameModel.debugColission)
                    foreach (var paralleliped in parallelipeds)
                        paralleliped.renderAsPolygons();
            }

        }
    }
}
