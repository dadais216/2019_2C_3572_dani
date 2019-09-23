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
        public static bool matrizChange;//eventualmente cambiar por el mecanismo que use para determinar cuando hacer
        //vertexfall de un meshc particular


        public TgcMesh mesh;//me gustaria usar herencia en vez de esto pero ni ganas de ver como se hace
        //el tema esta en que sceneLoader me devuelve un mesh, y no se como mover eso a un meshc
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

        //con parallelepiped voy a mantener copias porque los necesito transformados 2 veces por frame,
        //en la colision y el render

        public TGCMatrix originalMesh;
        
        //eventualmente voy a tener una lista de colisiones en vez de una sola
        public TGCMatrix meshToParalleliped;

        //usa este metodo para hacer transformaciones, todo lo que viene de tgcMesh no se usa
        //me parece que en la version final voy a preferir tener solo la transformacion y modificarla de a poco,
        //porque voy a querer tener transformaciones distintas para cada cosa y seria mas comodo
        public void transform(TGCMatrix matrix)
        {
#if true

            mesh.Transform = matrix*originalMesh;
            paralleliped.transform(meshToParalleliped*matrix*originalMesh);

            if (matrizChange)
            {
                chunks.addVertexFall(this);
            }
#else
            mesh.Transform = originalMesh*matrix;
            paralleliped.transform(originalParalleliped*matrix);
#endif
        }

        
        
    }
}
