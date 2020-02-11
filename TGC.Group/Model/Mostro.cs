using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectSound;
using TGC.Core.Collision;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Shaders;
using TGC.Core.Sound;


namespace TGC.Group.Model
{
    public class Mostro
    {
        public TgcMesh mesh;
        public TGCVector3 pos = new TGCVector3(0, 60000, 0);//60000
        public TGCVector3 dir;

        TGCVector3 lookAt;
        TGCVector3 lookin;

        public int mode = 0;//0 ataque, 1 seguimiento, 2 busqueda

        float speed = 3500f;

        Tgc3dSound musica;

        public Mostro()
        {
            mesh = Map.GetMeshFromScene("Esqueleto2-TgcScene.xml");

            musica = new Tgc3dSound(g.game.MediaDir + "tambo_tambo-la_cumbita.wav", pos, g.game.DirectSound.DsDevice);
            musica.MinDistance = 60f;

            g.game.DirectSound.Listener3d.Position = g.camera.eyePosition;

            musica.play(true);

            g.mostro = this;


            mode=3;
        }

        const float colissionLen = 200f;
        public float height = 1500f;

        public void render()
        {
            updateMesh();

            mesh.Effect = Meshc.actualShader;
            mesh.Effect.SetValue("type", 2);
            mesh.Technique = Meshc.actualTechnique;

            mesh.Render();

            if (g.cameraSprites.debugVisualizations)
            {
                TgcLine.fromExtremes(cPos, cPos + colDir, Color.Red).Render();
                TGCVector3 cross = TGCVector3.Cross(colDir, TGCVector3.Up);
                TgcLine.fromExtremes(cPos, cPos + cross * sidePref, Color.Green).Render();

                var lightObj = new TGCVector3(g.mostro.lightObj.X, g.mostro.flyHeight, g.mostro.lightObj.Y);
                var lightObjObj = new TGCVector3(g.mostro.lightObjObj.X, g.mostro.flyHeight, g.mostro.lightObjObj.Y);
                TgcLine.fromExtremes(lightObj-TGCVector3.Up*10000f, lightObj + TGCVector3.Up * 10000f, Color.Green).Render();
                TgcLine.fromExtremes(lightObjObj - TGCVector3.Up * 10000f, lightObjObj + TGCVector3.Up * 10000f, Color.Red).Render();
            }


        }

        bool speedGoingUp = true;
        float acceleration = 200f;

        TGCVector3 obj;
        bool setObj = true;

        int sidePref = 1;
        float swithSideTimer = 0;

        public TGCVector3 colDir;
        public TGCVector3 cPos;


        public TGCVector2 lightObj;
        public TGCVector2 lightObjObj;
        public float flyHeight;
        public float timeInView=0;

        public void update()
        {
            dir = TGCVector3.Empty;
            if (mode == 0)
            {
                //va hacia el jugador, la aceleracion salta entre + y - cada tanto
                speedToPlayer();
            }
            else if (mode == 1)
            {
                //mientras se lo este mirando no ataca al jugador, se queda cerca
                //@todo chequeo especial, antes se usaba los triangulos de la camara
                if (true)
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
                        obj = g.camera.cameraRotatedTarget * 100f
                            + TGCVector3.Cross(g.camera.cameraRotatedTarget * 100f, TGCVector3.Up) * g.map.Random.Next(-10000, 10000);
                        obj.Normalize();
                        obj *= 2000f + 1000 * g.hands.state;
                        obj += g.camera.eyePosition;

                    }
                    dir = obj - pos;
                    if (dir.Length() < 500f)
                    {
                        setObj = true;
                    }
                }
                else
                {
                    setObj = true;
                    speedToPlayer();
                }

            }
            else if (mode == 3)
            {
                //@todo se puede optimizar guardando la direccion de movimiento y una cantidad de pasos en vez
                //de tener objobj y calcular cada frame
                var lightMove = lightObjObj - lightObj;
                if (lightMove.Length() < 1000f)
                {
                    lightObjObj = new TGCVector2();
                    var rnd = g.map.Random;


                    lightObjObj.X = (float)rnd.NextDouble();
                    lightObjObj.Y = (float)rnd.NextDouble();
                    lightObjObj.Normalize();
                    lightObjObj *= (float)rnd.NextDouble() * 150000f;
                    lightObjObj += new TGCVector2(g.camera.eyePosition.X,g.camera.eyePosition.Z);

                    lightMove = lightObjObj - lightObj;
                }
                lightMove.Normalize();
                lightMove *= 5000f;
                lightObj += lightMove*g.game.ElapsedTime;

                flyHeight = g.camera.terrainHeight(pos) + 2500f;
                dir.X = lightObj.X-pos.X;
                dir.Y =  flyHeight - pos.Y;
                dir.Z = lightObj.Y-pos.Z;


                var sightLine = new TGCVector2(dir.X,dir.Z);
                var playerLine = new TGCVector2(g.camera.eyePosition.X - pos.X, g.camera.eyePosition.Z - pos.Z);
                sightLine.Normalize();playerLine.Normalize();
                if (TGCVector2.Dot(sightLine, playerLine) >= .96)
                {
                    var sight = g.camera.Position - pos;
                    //detectar colisiones
                    timeInView += g.game.ElapsedTime;
                    if (timeInView > 1)
                    {
                        mode=0;
                    }
                }
                else
                {
                    timeInView = 0;
                }


            }

            Console.WriteLine(mode);

            dir.Normalize();
            dir.Multiply(speed * (g.cameraSprites.squeletonHalfSpeed ? .0f : 1f)
                               * (g.map.candlesPlaced == g.cameraSprites.candlesRequired ? .2f : 1f)
                               * g.game.ElapsedTime);//11000f

            colDir = new TGCVector3(dir.X, 0, dir.Z);
            colDir.Normalize();
            colDir *= 1000;

            //colDir.X = Math.Max(colDir.X, dir.X);
            //colDir.Z = Math.Max(colDir.Z, dir.Z);//puede que sea demasiado precavido esto


            cPos = pos + TGCVector3.Up * height;



            var chunk = g.chunks.fromCoordinates(pos);

            float len = colDir.Length();
            float t;
            TGCVector3 q;//esta porque c# me obliga

            var ray = new TgcRay();
            ray.Direction = colDir;
            ray.Origin = cPos;


            var intersecRay = new Func<bool>(() =>
            {
                foreach (var mesh in chunk.meshes)
                {
                    if (mesh.paralleliped.intersectRay(ray, out t, out q) && t < len)
                        return true;
                }
                foreach (var multimesh in chunk.multimeshes)
                {
                    foreach (var par in multimesh.parallelipeds)
                    {
                        if (par.intersectRay(ray, out t, out q) && t < len)
                            return true;
                    }
                }
                return false;
            });


            if (intersecRay())
            {
                swithSideTimer = 0f;
                setObj = true;

                ray.Direction = TGCVector3.Cross(colDir, TGCVector3.Up) * sidePref;
                if (intersecRay())
                {
                    ray.Direction = -ray.Direction;
                    if (intersecRay())
                    {
                        dir = -dir;
                    }
                    else
                    {
                        dir = -TGCVector3.Cross(dir, TGCVector3.Up) * sidePref;
                    }
                    sidePref = -sidePref;
                }
                else
                {
                    dir = TGCVector3.Cross(dir, TGCVector3.Up) * sidePref;
                }

            }
            else
            {
                //si no hay nada enfrente mirar al jugador
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


            pos += dir;
            //Logger.Log(rot);

            musica.Position = pos;
            g.game.DirectSound.Listener3d.Position = g.camera.eyePosition;
        }

        private void updateMesh()
        {
            mesh.Transform =
                            TGCMatrix.RotationAxis(TGCVector3.Cross(lookAt, lookin),
                                                 -(float)Math.Acos(TGCVector3.Dot(lookAt, lookin)))
                            * TGCMatrix.Scaling(TGCVector3.One * 30)
                            * TGCMatrix.Translation(pos);
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

            if (dir.Length() < 300f && !g.cameraSprites.inmunity)
            {
                g.game.gameState = 2;
            }
        }




    }
}