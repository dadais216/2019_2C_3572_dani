using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.DirectX.DirectInput;
using TGC.Core.BoundingVolumes;
using TGC.Core.Camara;
using TGC.Core.Collision;
using TGC.Core.Direct3D;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Terrain;

namespace TGC.Group.Model.Camera
{
    /// <summary>
    ///     Camara en primera persona que utiliza matrices de rotacion, solo almacena las rotaciones en updown y costados.
    ///     Ref: http://www.riemers.net/eng/Tutorials/XNA/Csharp/Series4/Mouse_camera.php
    ///     Autor: Rodrigo Garcia.
    /// </summary>
    public class Camera : TgcCamera
    {
        /// <summary>
        ///  Centro del mouse 2D para ocultarlo
        /// </summary>
        private readonly Point mouseCenter = new Point(D3DDevice.Instance.Device.Viewport.Width / 2, D3DDevice.Instance.Device.Viewport.Height / 2);

        /// <summary>
        ///  Direction view se calcula a partir de donde se quiere ver con la camara inicialmente. por defecto se ve en -Z.
        /// </summary>
        private readonly TGCVector3 directionView = Map.South;


        private static float leftrightRot = FastMath.PI_HALF;

        //No hace falta la base ya que siempre es la misma, la base se arma segun las rotaciones de esto costados y updown.
        /// <summary>
        ///
        /// </summary>
        private static float updownRot = -FastMath.PI / 10.0f;

        /// <summary>
        ///  Se mantiene la matriz rotacion para no hacer este calculo cada vez.
        /// </summary>
        public static TGCMatrix cameraRotation = TGCMatrix.RotationX(updownRot) * TGCMatrix.RotationY(leftrightRot);

        /// <summary>
        ///  Se traba la camara, se utiliza para ocultar el puntero del mouse y manejar la rotacion de la camara.
        /// </summary>
        private bool lockCam = true;

        /// <summary>
        ///     Posicion de la camara
        /// </summary>
        public TGCVector3 eyePosition = new TGCVector3(-0, 200, -49000);
        public TGCVector3 cameraRotatedTarget;

        /// <summary>
        ///  Velocidad de movimiento
        /// </summary>
        public readonly float MovementSpeed = 12000f;

        /// <summary>
        ///  Velocidad de rotacion
        /// </summary>
        public readonly float RotationSpeed = 0.1f;


        private TgcRay up;
        private TgcRay down;
        private TgcRay[] horizontal;

        private bool onGround = false;
        private float vSpeed = 10;

        private bool underRoof;
        private TGCVector3 eyePositionOffRoof;


        

        /// <summary>
        ///     Constructor de la camara a partir de un TgcD3dInput el cual ya tiene por default el eyePosition (0,0,0), el mouseCenter a partir del centro del a pantalla, RotationSpeed 1.0f,
        ///     MovementSpeed y JumpSpeed 500f, el directionView (0,0,-1)
        /// </summary>
        public Camera(TgcD3dInput input)
        {
            Input = input;

            up = new TgcRay();
            up.Direction = new TGCVector3(0, 1, 0);
            down = new TgcRay();
            down.Direction = new TGCVector3(0, -1, 0);
            horizontal = new TgcRay[4] { new TgcRay(), new TgcRay(), new TgcRay(), new TgcRay(), };
            horizontal[0].Direction = new TGCVector3(1, 0, 0);
            horizontal[1].Direction = new TGCVector3(0, 0, 1);
            horizontal[2].Direction = new TGCVector3(-1, 0, 0);
            horizontal[3].Direction = new TGCVector3(0, 0, -1);

            g.camera = this;
            g.hands=new Hands();
        }

        private TgcD3dInput Input { get; }

        /// <summary>
        ///  Condicion para trabar y destrabar la camara y ocultar el puntero de mouse.
        /// </summary>
        public bool LockCam {
            get => lockCam;
            set {
                if (!lockCam && value)
                {
                    Cursor.Position = mouseCenter;

                    Cursor.Hide();
                }
                if (lockCam && !value)
                    Cursor.Show();
                lockCam = value;
            }
        }




        /// <summary>
        ///     Realiza un update de la camara a partir del elapsedTime, actualizando Position,LookAt y UpVector.
        ///     Presenta movimientos basicos a partir de input de teclado W, A, S, D, Espacio, Control y rotraciones con el mouse.
        /// </summary>
        /// <param name="elapsedTime"></param>



        public override void UpdateCamera(float elapsedTime)
        {
            //Lock camera
            if (Input.keyPressed(Key.L))
            {
                LockCam = !lockCam;
            }

            var inputMove = TGCVector3.Empty;
            if (lockCam)
            {
                leftrightRot -= -Input.XposRelative * RotationSpeed;
                updownRot -= Input.YposRelative * RotationSpeed;
                // Se actualiza matrix de rotacion, para no hacer este calculo cada vez y solo cuando en verdad es necesario.
                cameraRotation = TGCMatrix.RotationX(updownRot) * TGCMatrix.RotationY(leftrightRot);
                Cursor.Position = mouseCenter;
            }





            if (Input.keyDown(Key.W))
                inputMove += Map.South;
            if (Input.keyDown(Key.S))
                inputMove += Map.North;
            if (Input.keyDown(Key.A))
                inputMove += Map.East;
            if (Input.keyDown(Key.D))
                inputMove += Map.West;

            

            TGCVector3 moveXZ = TGCVector3.TransformNormal(inputMove, cameraRotation);
            moveXZ.Y = 0;
            moveXZ.Normalize();


            if (Input.keyDown(Key.Q))
                moveXZ += Map.South * 32;

            TGCVector3 moving = moveXZ * MovementSpeed * elapsedTime;

            var displacement = new TGCVector3(0, 0, 0);
            const float border = 100f;

            float t;
            TGCVector3 q;

            bool onBox=false;
            //salto
            if (onGround)
            {
                if (Input.keyDown(Key.Space) && !underRoof)
                {
                    vSpeed = 100f- Math.Max(-elapsedTime * 100, -180);
                }
                else
                    vSpeed = 0f;
                //antes estaba en -20 porque sino va pegando saltitos en la bajada
                //ahora lo dejo en 0 porque causa mucha vibracion con bajos fps
            }
            else
                vSpeed = Math.Max(vSpeed - elapsedTime * 100, -180);//gravedad
                //v=a*t
                //x=v*t
                //como tengo t al cuadrado no deberia usar elapsed porque dependeria de los fps
                //sigo con elapsed porque no note una diferencia importante
                //la solucion seria medir de forma absoluta desde el comienzo del salto


            if(!underRoof)
                eyePositionOffRoof= new TGCVector3(eyePosition.X, eyePosition.Y, eyePosition.Z);
            underRoof = false;




            moving += up.Direction* vSpeed*elapsedTime * 100;

            //rayos colision

            //se podrian tirar rayos en las diagonales para manejar mejor esquinas tambien.
            //se podria tirar rayos solo en las direcciones ortogonales que me estoy moviendo
            //se podria tirar solo un rayo horizontal en la direccion que me muevo y sacar el
            //desplazamiento haciendo cuentas con la normal del triangulo

            //en la colision se asume que el centro del personaje nunca atraviesa la pared

            //solo se mueve cierta distancia por iteracion. Esto hace que no se atraviesen las cosas
            //con fps bajos va a correr todavia mas lento pero va a ser consistente
            float dist=moving.Length();
            int iters = (int)FastMath.Ceiling(dist/45f);

            //Logger.Log(iters.ToString()+"   "+dist.ToString());

            var step = moving * (1 / (float)iters);

            iters = FastMath.Min(iters, 10);//dropear iteraciones en casos extremos, sino
                                            //el elapsedTime va a ser todavia mas grande en el proximo frame


            for (int i = 0; i < iters; i++)
            {
                eyePosition += step;

                horizontal[0].Origin = eyePosition;
                horizontal[1].Origin = eyePosition;
                horizontal[2].Origin = eyePosition;
                horizontal[3].Origin = eyePosition;
                up.Origin = eyePosition;
                down.Origin = eyePosition;

                bool doGoto = false;
                var handleRays = new Action<Parallelepiped>((box) =>
                {
                    foreach (TgcRay dir in horizontal)
                    {
                        if (box.intersectRay(dir, out t, out q) && t < border)
                        {
                            displacement += -dir.Direction * (border - t);
                        }
                    }

                    if (vSpeed <= 0 && box.intersectRay(down, out t, out q) && t < border)
                    {
                        if (t < border)
                            displacement += up.Direction * (border - t);
                        onBox = true;
                    }

                    if (box.intersectRay(up, out t, out q) && t < border * 3)
                    {
                        underRoof = true;
                        if (t < border)
                        {
                            if (onGround)//esta subiendo una pendiente hacia un techo, se pudre todo
                            {
                                eyePosition += new TGCVector3(eyePositionOffRoof.X - eyePosition.X, 0f, eyePositionOffRoof.Z - eyePosition.Z);
                                //no es la mejor solucion porque causa mucho temblor, pero bueno
                                doGoto=true;//salto el codigo de terreno porque puede hacer subir la camara
                                return;
                            }
                            displacement += -up.Direction * (border - t);
                            vSpeed = 0f;
                        }

                    }
                });
                var chunk = g.chunks.fromCoordinates(eyePosition, false);
                Meshc setToRemove = null;
                foreach (var meshc in chunk.meshes)
                {
                    var box = meshc.paralleliped;

                    if (g.map.isCandle(meshc))
                    {
                        if (Map.pointParallelipedXZColission(box, eyePosition))
                        {
                            setToRemove = meshc;
                        }

                    }
                    else
                    {
                        handleRays(box);
                        if (doGoto)
                            goto setCamera;//C# no se banca gotos en lambdas
                    }

                }

                if (setToRemove != null)
                {
                    if(g.hands.maybePickCandle())
                        chunk.meshes.Remove(setToRemove);
                }

                foreach (var meshc in chunk.multimeshes)
                {
                    foreach(var box in meshc.parallelipeds)
                    {
                        handleRays(box);
                        if (doGoto)
                            goto setCamera;//C# no se banca gotos en lambdas
                    }
                }


                eyePosition += displacement;
            }
                //manejo de terreno. Se podria hacer colisionando rayos con triangulos, usando el mismo sistema que las
                //demas colisiones, y puede que quiera hacerlo si hago mas complejo, pero por ahora manejarlo como un
                //sistema aparte es simple y es mucho mas eficiente
                //en algunos lugares, como un barranco, voy a agregar colisiones con cajas invisibles para manejar eso mejor
            
    
            var zxScale = Map.xzTerrainScale;
            var yScale = Map.yTerrainScale;
            var yOffset = Map.yTerrainOffset;

            var hm = g.terrain.HeightmapData;

            float x = eyePosition.X / zxScale;
            float z = eyePosition.Z / zxScale;

            int hx = (int)FastMath.Floor(x) + hm.GetLength(0) / 2;
            int hz = (int)FastMath.Floor(z) + hm.GetLength(1) / 2;

            //interpolacion bilineal
            float Yx0z0 = (hm[hx, hz] - yOffset) *yScale;
            float Yx1z0 = (hm[hx+1, hz] - yOffset) * yScale;
            float Yx0z1 = (hm[hx, hz+1] - yOffset) * yScale;
            float Yx1z1 = (hm[hx+1, hz+1] - yOffset) * yScale;

            float dx = x - FastMath.Floor(x);
            float dz = z - FastMath.Floor(z);

            float interpolz0 = Yx0z0 * (1 - dx) + Yx1z0 * dx;
            float interpolz1 = Yx0z1 * (1 - dx) + Yx1z1 * dx;
            float interpol = interpolz0 * (1 - dz) + interpolz1 * dz;

            if (eyePosition.Y <= interpol + 500)
            {
                eyePosition.Y = interpol + 500;
                onGround = true;
            }
            else
            {
                onGround = onBox;
            }


        setCamera:

            cameraRotatedTarget = TGCVector3.TransformNormal(directionView, cameraRotation);
            TGCVector3 cameraFinalTarget = eyePosition + cameraRotatedTarget;
            // Se calcula el nuevo vector de up producido por el movimiento del update.
            var cameraOriginalUpVector = DEFAULT_UP_VECTOR;
            var cameraRotatedUpVector = TGCVector3.TransformNormal(cameraOriginalUpVector, cameraRotation);

            base.SetCamera(eyePosition, cameraFinalTarget, cameraRotatedUpVector);
            //base.SetCamera(eyePosition, cameraRotatedTarget, cameraRotatedUpVector);

            //Logger.Log(eyePosition);
            //Logger.Log(map.chunks.fromCoordinates(eyePosition, false).center.X.ToString()+"  "+ map.chunks.fromCoordinates(eyePosition, false).center.Y.ToString());


            // Y esta seteado para visualizarlo en debug nomas
            var eyeUp = new TGCVector3(eyePosition.X, eyePosition.Y + 10f, eyePosition.Z);
            triangle.a = eyeUp;

            var lineVec = (cameraFinalTarget - eyePosition) * 200000f;
            var Pend = lineVec + eyePosition;
            Pend.Y = eyeUp.Y;

            var lineBack = TGCVector3.Cross(lineVec, new TGCVector3(0, 1, 0)) * .7f;
            var PBeg = -lineBack + Pend;
            var PendBack = lineBack + Pend;
            PendBack.Y = eyeUp.Y;

            triangle.b = PBeg;
            triangle.c = PendBack;




        }


        public class Triangle
        {
            public TGCVector3 a, b, c; //deberian ser TGCVector2 pero por conveniencia
            public void render()
            {
                var line = TgcLine.fromExtremes(a, (b + c) * .5f, Color.Red);
                var back = TgcLine.fromExtremes(b, c, Color.Green);
                var leg1 = TgcLine.fromExtremes(a, b, Color.Yellow);
                var leg2 = TgcLine.fromExtremes(a, c, Color.Yellow);

                line.Render();
                back.Render();
                leg1.Render();
                leg2.Render();
            }

            public bool enclosesPoint(TGCVector3 p)
            {
                //lo que se me habia ocurrido es usar producto interno entre a y b y despues entre a y p, viendo si el angulo es menor al de ab
                //y repetir para los otros 2 vertices. Pero puede que tenga problemas en los signos y ni ganas de verlo ahora

                //esto lo saque de internet
                float s1 = c.Z - a.Z;
                float s2 = c.X - a.X;
                float s3 = b.Z - a.Z;
                float s4 = p.Z - a.Z;

                float w1 = (a.X * s1 + s4 * s2 - p.X * s1) / (s3 * s2 - (b.X - a.X) * s1);
                float w2 = (s4 - w1 * s3) / s1;

                return w1 >= 0 && w2 >= 0 && (w1 + w2) <= 1;
            }
        }
        public Triangle triangle = new Triangle();






        /// <summary>
        ///     Cuando se elimina esto hay que desbloquear la camera
        /// </summary>
        ~Camera()
        {
            LockCam = false;
        }
    }
}