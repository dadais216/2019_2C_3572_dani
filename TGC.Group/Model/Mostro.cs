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
        public TGCVector3 pos=new TGCVector3(0,0,0);
        
        public Mostro()
        {
            mesh= Map.GetMeshFromScene("Esqueleto2-TgcScene.xml");


            g.mostro = this;
        }

        public void render()
        {
            mesh.Render();
        }

        public void update()
        {
            var dir = g.camera.eyePosition - pos;           
            dir.Normalize();
            dir.Multiply(5500f*g.game.ElapsedTime);//11000f
            pos += dir;

            var lookAt = new TGCVector3(dir.X, 0, dir.Z);
            lookAt.Normalize();
            var lookin = new TGCVector3(0, 0, -1);

            mesh.Transform =
                TGCMatrix.RotationAxis(TGCVector3.Cross(lookAt, lookin),
                                     -(float)Math.Acos(TGCVector3.Dot(lookAt, lookin))) 
                * TGCMatrix.Scaling(TGCVector3.One * 30) 
                * TGCMatrix.Translation(pos);
            //Logger.Log(rot);
        }


    }
}
