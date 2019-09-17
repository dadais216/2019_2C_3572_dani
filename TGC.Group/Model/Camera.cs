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
        private TGCVector3 eyePosition = Map.Origin;

        /// <summary>
        ///  Velocidad de movimiento
        /// </summary>
        public readonly float MovementSpeed = 1000f;

        /// <summary>
        ///  Velocidad de rotacion
        /// </summary>
        public readonly float RotationSpeed = 0.1f;

        public Map map;
        private TgcRay up;
        private TgcRay down;
        private TgcRay[] horizontal;
        private bool onGround=false;
        private float vSpeed=10;


        /// <summary>
        ///     Constructor de la camara a partir de un TgcD3dInput el cual ya tiene por default el eyePosition (0,0,0), el mouseCenter a partir del centro del a pantalla, RotationSpeed 1.0f,
        ///     MovementSpeed y JumpSpeed 500f, el directionView (0,0,-1)
        /// </summary>
        public Camera(TgcD3dInput input, Map map_)
        {
            Input = input;
            map = map_;

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


            //en la colision se asume que el centro del personaje nunca atraviesa la pared

            //rayos colision
            horizontal[0].Origin = eyePosition;
            horizontal[1].Origin = eyePosition;
            horizontal[2].Origin = eyePosition;
            horizontal[3].Origin = eyePosition;
            up.Origin = eyePosition;
            down.Origin = eyePosition;

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

            eyePosition += moveXZ * MovementSpeed * elapsedTime;

            var displacement = new TGCVector3(0, 0, 0);
            const float border = 100f;

            float t;
            TGCVector3 q;

            //se podrian tirar rayos en las diagonales para manejar mejor esquinas tambien.
            //se podria tirar rayos solo en las direcciones que me estoy moviendo
            //se podria tirar solo un rayo horizontal en la direccion que me muevo y sacar el
            //desplazamiento haciendo cuentas con la normal del triangulo
            foreach (var box in map.collisions)
            {
                foreach (TgcRay dir in horizontal)
                {
                    if (box.intersectRay(dir, out t, out q) && t < border)
                    {
                        displacement += -dir.Direction * (border - t);
                    }
                }


                if (Input.keyDown(Key.Space) && onGround)
                    vSpeed = 20f;

                if (vSpeed <= 0 && box.intersectRay(down, out t, out q) && t < border)
                {
                    if (t < border)
                        displacement += up.Direction * (border - t);
                    onGround = true;
                    vSpeed = 0f;
                }
                else
                {
                    if (!onGround)
                    {
                        vSpeed = Math.Max(vSpeed-elapsedTime * 1,-8);//gravedad
                    }

                    onGround = false;
                }

                displacement += up.Direction * vSpeed;
                Logger.Log(vSpeed);
                if (box.intersectRay(up, out t, out q) && t < border)
                {
                    displacement += -up.Direction * (border - t);
                    vSpeed = 0f;
                }

            }
                
            /*
            var minNotNull = new Func<float, float, float>((aS, bS) => {
                var a=Math.Abs(aS);
                var b = Math.Abs(bS);
                if (a == 0 && b == 0) return 0;
                if (a == 0) return b;
                if (b == 0) return a;
                if (a < b) return a;
                return b;
            });

            float minVal = minNotNull(minNotNull(displacement.X, displacement.Y),displacement.Z);
            if (Math.Abs(displacement.X) != minVal) displacement.X = 0;
            if (Math.Abs(displacement.Y) != minVal) displacement.Y = 0;
            if (Math.Abs(displacement.Z) != minVal) displacement.Z = 0;
            */
            //Logger.Log(displacement);


            eyePosition += displacement;

            


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