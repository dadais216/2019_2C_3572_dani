using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Collision;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model
{
    public class Mostro
    {
        public TgcMesh mesh;
        public TGCVector3 pos = new TGCVector3(0, 0, 0);
        public TGCVector3 dir;

        TGCVector3 lookAt;
        TGCVector3 lookin;

        public int mode = 0;//0 ataque, 1 seguimiento, 2 busqueda

        float speed = 3500f;

        public Mostro()
        {
            mesh = Map.GetMeshFromScene("Esqueleto2-TgcScene.xml");


            g.mostro = this;
        }

        const float colissionLen = 200f;
        public float height = 1500f;

        public void render()
        {
            mesh.Render();


            TGCVector3 colDir = new TGCVector3(dir.X, 0, dir.Z);
            colDir.Normalize();
            colDir *= 350f;

            var cPos = pos + TGCVector3.Up * height;
            TgcLine.fromExtremes(cPos - colDir, cPos + colDir, Color.Red).Render();
            TGCVector3 cross = TGCVector3.Cross(colDir, TGCVector3.Up);
            TgcLine.fromExtremes(cPos - cross, cPos + cross, Color.Red).Render();


        }

        bool speedGoingUp=true;
        float acceleration = 200f;

        TGCVector3 obj;
        bool setObj = true;

        int sidePref = 1;
        float swithSideTimer = 0;
        public void update()
        {
            dir = TGCVector3.Empty;
            if (mode == 0)
            {
                //va hacia el jugador, la aceleracion salta entre + y - cada tanto
                speedToPlayer();
            }
            if (mode == 1)
            {
                //mientras se lo este mirando no ataca al jugador, se queda cerca
                if (g.camera.triangle.enclosesPoint(pos))
                {
                    if (setObj)
                    {
                        setObj = false;

                        if (speed > 3500f)
                        {
                            speed -= g.game.ElapsedTime * acceleration;
                        }


                        //aca estoy consiguiendo un valor en un radio del jugador, mas o menos en la direccion que mira
                        //seguro que hay una forma mejor de calcular esto
                        obj=g.camera.cameraRotatedTarget*100f 
                            + TGCVector3.Cross(g.camera.cameraRotatedTarget * 100f,TGCVector3.Up)*g.map.Random.Next(-10000,10000);
                        obj.Normalize();
                        obj *= 2000f+1000*g.hands.state;
                        obj += g.camera.eyePosition;

                    }
                    dir = obj - pos;
                    Logger.Log(dir.Length());
                    if (dir.Length() < 500f)
                    {
                        setObj = true;
                    }
                }
                else
                {
                    setObj = true;
                    speedToPlayer();
                    Logger.Log(":("+dir.Length().ToString());
                }

            }
            if(mode == 2)
            {
                //dir en el cielo, no se cambia hasta que se llegue
            }
            dir.Normalize();
            dir.Multiply(speed * (g.cameraSprites.squeletonHalfSpeed ? .5f : 1f) * g.game.ElapsedTime);//11000f

            if(!addaptDirIfColission())
            {
                swithSideTimer += g.game.ElapsedTime;
                if (swithSideTimer > 3f)
                {
                    swithSideTimer = 0;
                    sidePref = -sidePref;
                }


                lookAt = new TGCVector3(dir.X, 0, dir.Z);
                lookAt.Normalize();
                lookin = new TGCVector3(0, 0, -1);
            }
            else
            {
                setObj = true;
            }

            pos += dir;


            mesh.Transform =
                TGCMatrix.RotationAxis(TGCVector3.Cross(lookAt, lookin),
                                     -(float)Math.Acos(TGCVector3.Dot(lookAt, lookin)))
                * TGCMatrix.Scaling(TGCVector3.One * 30)
                * TGCMatrix.Translation(pos);
            //Logger.Log(rot);
        }

        private void speedToPlayer()
        {
            dir = g.camera.eyePosition - pos;

            if (speedGoingUp)
            {
                speed += g.game.ElapsedTime * acceleration;
                if (speed > 8500f)
                    speedGoingUp = false;
            }
            else
            {
                speed -= g.game.ElapsedTime * acceleration * 1.1f;
                if (speed < 4000f)
                {
                    speedGoingUp = true;
                }
            }

            if (dir.Length() < 300f)
            {
                g.game.gameState = 2;
            }
        }

        public bool addaptDirIfColission()
        {
            var chunk = g.chunks.fromCoordinates(pos);
            foreach (var mesh in chunk.meshes)
            {
                if (pointParallelipedXZColission(mesh.paralleliped))
                    return true;
            }
            foreach (var multimesh in chunk.multimeshes)
            {
                foreach (var par in multimesh.parallelipeds)
                {
                    if (pointParallelipedXZColission(par))
                        return true;
                }
            }
            return false;
        }

        public bool pointParallelipedXZColission(Parallelepiped par)
        {
            TGCVector3 colDir = new TGCVector3(dir.X, 0, dir.Z);
            colDir.Normalize();
            colDir *= 350f;

            var cPos = pos + TGCVector3.Up * height;

            var ray = new TgcRay();
            ray.Direction = colDir;
            ray.Origin = cPos;

            float len = colDir.Length(); //es cte, ver que es y ponerlo

            
            if (par.intersectRay(ray, out float t, out TGCVector3 q)&&t < len)
            {
                ray.Direction = TGCVector3.Cross(colDir, TGCVector3.Up)*sidePref;
                ray.Origin = cPos - ray.Direction;


                dir = TGCVector3.Cross(dir, TGCVector3.Up)*sidePref;
                if (par.intersectRay(ray, out t, out q)&&t < len)
                {
                    dir = -dir;
                    sidePref = -sidePref;
                }
                return true;
            }

            return false;//no hay nada enfrente


        }

    }
}