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
        public TgcMesh mesh;//me gustaria usar herencia en vez de esto pero ni ganas de ver como se hace
        //el tema esta en que sceneLoader me devuelve un mesh, y no se como mover eso a un meshc
        public Parallelepiped paralleliped;

        private TGCMatrix originalMesh;
        private TGCMatrix originalParalleliped;

        public void setOriginals()
        {
            originalMesh = mesh.Transform;
            originalParalleliped = paralleliped.Transform;
        }

        //usa este metodo para hacer transformaciones, todo lo que viene de tgcMesh no se usa
        public void transform(TGCMatrix matrix)
        {
            mesh.Transform = originalMesh*matrix;
            paralleliped.transform(originalParalleliped*matrix);

            //creo que lo que quiero hacer aca es sumar en vez de multiplicar
            //el tema es que las matrices originales son distintas, tengo que guardar una matriz que me permita
            //moverme entre esos dos espacios
        }
    }
}
