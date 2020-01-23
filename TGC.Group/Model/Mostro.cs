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
        public TGCVector3 pos = new TGCVector3(0, 60000, 0);
        public TGCVector3 dir;

        TGCVector3 lookAt;
        TGCVector3 lookin;

        public int mode = 0;//0 ataque, 1 seguimiento, 2 busqueda

        float speed = 3500f;

        Tgc3dSound musica;

        public Mostro()
        {
            mesh = Map.GetMeshFromScene("Esqueleto2-TgcScene.xml");

            mesh.Effect = TGCShaders.Instance.LoadEffect(TGCShaders.Instance.CommonShadersPath+"esqueleto.fx");
            mesh.Technique = "DIFFUSE_MAP";


            mesh.Effect.SetValue("lightAttenuation", .2f);
            mesh.Effect.SetValue("spotLightAngleCos", FastMath.ToRad(0f));
            mesh.Effect.SetValue("spotLightExponent", .0f);
            mesh.Effect.SetValue("lightColor", ColorValue.FromColor(Color.White));

            mesh.Effect.SetValue("materialAmbientColor", ColorValue.FromColor(Color.White));
            mesh.Effect.SetValue("materialDiffuseColor", ColorValue.FromColor(Color.White));
            mesh.Effect.SetValue("materialSpecularColor", ColorValue.FromColor(Color.Black));
            mesh.Effect.SetValue("materialSpecularExp", .9f);

            //mesh.Effect = TGCShaders.Instance.TgcMeshSpotLightShader;
            //mesh.Technique = "DIFFUSE_MAP";

            musica = new Tgc3dSound(g.game.MediaDir + "tambo_tambo-la_cumbita.wav", pos, g.game.DirectSound.DsDevice);
            musica.MinDistance = 60f;

            g.game.DirectSound.Listener3d.Position = g.camera.eyePosition;
           
            musica.play(true);

            g.mostro = this;
        }

        const float colissionLen = 200f;
        public float height = 1500f;

        public void render()
        {
            TGCShaders.Instance.SetShaderMatrix(mesh.Effect,mesh.Transform);

            mesh.Effect.SetValue("lightIntensity", 150f + 500f * g.hands.state);
            mesh.Effect.SetValue("lightPosition", TGCVector3.Vector3ToFloat4Array(g.camera.eyePosition));
            mesh.Effect.SetValue("eyePosition", TGCVector3.Vector3ToFloat4Array(g.camera.eyePosition));
            mesh.Effect.SetValue("spotLightDir", TGCVector3.Vector3ToFloat3Array(g.camera.cameraRotatedTarget));
            mesh.Effect.SetValue("materialEmissiveColor", ColorValue.FromColor(Color.FromArgb(0, g.map.candlesPlaced * 2, 0, 0)));



            mesh.Render();

            if (g.cameraSprites.debugVisualizations)
            {
                TgcLine.fromExtremes(cPos, cPos + colDir, Color.Red).Render();
                TGCVector3 cross = TGCVector3.Cross(colDir, TGCVector3.Up);
                TgcLine.fromExtremes(cPos, cPos + cross * sidePref, Color.Green).Render();
            }
        }

        bool speedGoingUp = true;
        float acceleration = 200f;

        TGCVector3 obj;
        bool setObj = true;

        int sidePref = 1;
        float swithSideTimer = 0;

        TGCVector3 colDir;
        TGCVector3 cPos;
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
                if (g.camera.triangleNear.enclosesPoint(pos))
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
            if (mode == 2)
            {
                //dir en el cielo, no se cambia hasta que se llegue
            }
            dir.Normalize();
            dir.Multiply(speed * (g.cameraSprites.squeletonHalfSpeed ? .0f : 1f)
                               * (g.map.candlesPlaced==g.cameraSprites.candlesRequired ? .2f : 1f)
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


            mesh.Transform =
                TGCMatrix.RotationAxis(TGCVector3.Cross(lookAt, lookin),
                                     -(float)Math.Acos(TGCVector3.Dot(lookAt, lookin)))
                * TGCMatrix.Scaling(TGCVector3.One * 30)
                * TGCMatrix.Translation(pos);
            //Logger.Log(rot);

            musica.Position = pos;
            g.game.DirectSound.Listener3d.Position = g.camera.eyePosition;
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