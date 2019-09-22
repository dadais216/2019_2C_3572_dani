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
        private static TGCMatrix cameraRotation = TGCMatrix.RotationX(updownRot) * TGCMatrix.RotationY(leftrightRot);

        /// <summary>
        ///  Se traba la camara, se utiliza para ocultar el puntero del mouse y manejar la rotacion de la camara.
        /// </summary>
        private bool lockCam = true;

        /// <summary>
        ///     Posicion de la camara
        /// </summary>
        private TGCVector3 eyePosition = new TGCVector3(0,9500,0);

        /// <summary>
        ///  Velocidad de movimiento
        /// </summary>
        public readonly float MovementSpeed = 4000f;

        /// <summary>
        ///  Velocidad de rotacion
        /// </summary>
        public readonly float RotationSpeed = 0.1f;

        public Map map;
        private TgcSimpleTerrain terrain;
        private TgcRay up;
        private TgcRay down;
        private TgcRay[] horizontal;

        private bool onGround=false;
        private float vSpeed=10;
        private bool underRoof;


        /// <summary>
        ///     Constructor de la camara a partir de un TgcD3dInput el cual ya tiene por default el eyePosition (0,0,0), el mouseCenter a partir del centro del a pantalla, RotationSpeed 1.0f,
        ///     MovementSpeed y JumpSpeed 500f, el directionView (0,0,-1)
        /// </summary>
        public Camera(TgcD3dInput input, Map map_)
        {
            Input = input;
            map = map_;
            terrain = map.terrain;

            up = new TgcRay();
            up.Direction = new TGCVector3(0, 1, 0);
            down = new TgcRay();
            down.Direction = new TGCVector3(0, -1, 0);
            horizontal = new TgcRay[4] {new TgcRay(), new TgcRay(), new TgcRay(), new TgcRay() ,};
            horizontal[0].Direction = new TGCVector3(1, 0, 0);
            horizontal[1].Direction = new TGCVector3(0, 0, 1);
            horizontal[2].Direction = new TGCVector3(-1, 0, 0);
            horizontal[3].Direction = new TGCVector3(0, 0, -1);
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

            TGCVector3 eyePositionBefore = new TGCVector3(eyePosition.X, eyePosition.Y, eyePosition.Z);

            TGCVector3 moveXZ = TGCVector3.TransformNormal(inputMove, cameraRotation);
            moveXZ.Y = 0;
            moveXZ.Normalize();

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
                    vSpeed = 100f- Math.Max(vSpeed - elapsedTime * 100, -180);//gravedad;
                }
                else
                    vSpeed = -20f;//porque sino va pegando saltitos en la bajada
            }
            else
                vSpeed = Math.Max(vSpeed - elapsedTime * 100, -180);//gravedad
                //v=a*t
                //x=v*t
                //como tengo t al cuadrado no deberia usar elapsed porque dependeria de los fps
                //sigo con elapsed porque no note una diferencia importante
                //la solucion seria medir de forma absoluta desde el comienzo del salto
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
            int iters = (int)FastMath.Ceiling(dist/50f);

            Logger.Log(iters.ToString()+"   "+dist.ToString());

            iters = FastMath.Min(iters, 10);//dropear iteraciones en casos extremos, sino
                                            //el elapsedTime va a ser todavia mas grande en el proximo frame

            for (int i = 0; i < iters; i++)
            {
                eyePosition += new TGCVector3(moving.X/iters,moving.Y/iters,moving.Z/iters);

                horizontal[0].Origin = eyePosition;
                horizontal[1].Origin = eyePosition;
                horizontal[2].Origin = eyePosition;
                horizontal[3].Origin = eyePosition;
                up.Origin = eyePosition;
                down.Origin = eyePosition;

                foreach (var meshc in map.scene)
                {
                    var box = meshc.paralleliped;
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
                        //Logger.Log("hit");
                    }
                    else
                    {
                        //Logger.Log("fail");
                    }

                    if (box.intersectRay(up, out t, out q) && t < border)
                    {
                        underRoof = true;
                        if (onGround)//esta subiendo una pendiente hacia un techo, se pudre todo
                        {
                            eyePosition = eyePositionBefore;
                            goto setCamera;
                        }
                        displacement += -up.Direction * (border - t);
                        vSpeed = 0f;

                    }

                }

                //Logger.Log(vSpeed);


                //manejo de terreno. Se podria hacer colisionando rayos con triangulos, usando el mismo sistema que las
                //demas colisiones, y puede que quiera hacerlo si hago mas complejo, pero por ahora manejarlo como un
                //sistema aparte es simple y es mucho mas eficiente
                //en algunos lugares, como un barranco, voy a agregar colisiones con cajas invisibles para manejar eso mejor

                eyePosition += displacement;
            }
            var zxScale = Map.xzTerrainScale;
            var yScale = Map.yTerrainScale;
            var yOffset = Map.yTerrainOffset;

            var hm = terrain.HeightmapData;

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

            TGCVector3 cameraRotatedTarget = TGCVector3.TransformNormal(directionView, cameraRotation);
            TGCVector3 cameraFinalTarget = eyePosition + cameraRotatedTarget;
            // Se calcula el nuevo vector de up producido por el movimiento del update.
            var cameraOriginalUpVector = DEFAULT_UP_VECTOR;
            var cameraRotatedUpVector = TGCVector3.TransformNormal(cameraOriginalUpVector, cameraRotation);


            base.SetCamera(eyePosition, cameraFinalTarget, cameraRotatedUpVector);
        }

        /// <summary>
        ///     Cuando se elimina esto hay que desbloquear la camera
        /// </summary>
        ~Camera()
        {
            LockCam = false;
        }
    }
}