using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model
{
    public class Mostro
    {
        public TgcMesh mesh;
        public Mostro()
        {
            mesh= Map.GetMeshFromScene("Esqueleto2-TgcScene.xml");
            mesh.Transform = TGCMatrix.Scaling(TGCVector3.One * 30);


            g.mostro = this;
        }

        public void render()
        {
            mesh.Render();
        }

        public void update()
        {
            TGCVector3 pos = new TGCVector3(mesh.Transform.M41, mesh.Transform.M42, mesh.Transform.M43);
            var dir = g.camera.eyePosition - pos;
            dir.Normalize();
            dir.Multiply(200f*g.game.ElapsedTime);//11000f
            var newTransform = mesh.Transform;//no se por que no me deja cambiar el de mesh directamente c# de mierda
            newTransform.M41 += dir.X;
            newTransform.M42 += dir.Y;
            newTransform.M43 += dir.Z;
            mesh.Transform = newTransform;
        }


    }
}
