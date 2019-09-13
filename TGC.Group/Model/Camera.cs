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
        private readonly TGCVector3 directionView = GameModel.South;


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
        private TGCVector3 eyePosition = GameModel.Origin;

        /// <summary>
        ///  Velocidad de movimiento
        /// </summary>
        public readonly float MovementSpeed = 1000f;

        /// <summary>
        ///  Velocidad de rotacion
        /// </summary>
        public readonly float RotationSpeed = 0.1f;

        /// <summary>
        ///  Velocidad de Salto
        /// </summary>
        public float JumpSpeed = 500f;

        private List<TgcBoundingAxisAlignBox> boxes;
        private TgcBoundingAxisAlignBox boundingBox;


        /// <summary>
        ///     Constructor de la camara a partir de un TgcD3dInput el cual ya tiene por default el eyePosition (0,0,0), el mouseCenter a partir del centro del a pantalla, RotationSpeed 1.0f,
        ///     MovementSpeed y JumpSpeed 500f, el directionView (0,0,-1)
        /// </summary>
        public Camera(TgcD3dInput input, List<TgcBoundingAxisAlignBox> boxes_)
        {
            Input = input;
            boxes = boxes_;//no tengo claro si estoy haciendo una copia o pasando puntero. Espero que sea lo segundo

            boundingBox = new TgcBoundingAxisAlignBox(new TGCVector3(-50, -50, -50), new TGCVector3(50, 50, 50));
            //creo que esta descentrada, por lo menos eso parece cuando se usan cajas muy chicas
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

        private static readonly Dictionary<Key, TGCVector3> keysDirections = new Dictionary<Key, TGCVector3>
        {
            {Key.W, GameModel.South},
            {Key.S, GameModel.North},
            {Key.A, GameModel.East},
            {Key.D, GameModel.West},
            {Key.Space, GameModel.Up},
            {Key.LeftShift, GameModel.Down}
        };

        struct vecIndex
        {
            public int index;
            public float val;

            public vecIndex(int v, float x)
            {
                index = v;
                val = x;
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

            //Exit
            if (Input.keyPressed(Key.Escape) || Input.keyPressed(Key.BackSpace))
            {
                throw new ApplicationException();
            }

            var moveVector = TGCVector3.Empty;
            { //codigo de camara que vuela
                if (lockCam)
                {
                    leftrightRot -= -Input.XposRelative * RotationSpeed;
                    updownRot -= Input.YposRelative * RotationSpeed;
                    // Se actualiza matrix de rotacion, para no hacer este calculo cada vez y solo cuando en verdad es necesario.
                    cameraRotation = TGCMatrix.RotationX(updownRot) * TGCMatrix.RotationY(leftrightRot);
                    Cursor.Position = mouseCenter;
                }

                foreach (var kvp in keysDirections)
                {
                    //esto tiene el bug de que moverse en diagonal es mas rapido
                    if (Input.keyDown(kvp.Key))
                        moveVector += kvp.Value * MovementSpeed;
                }
            }

            // Calculamos la nueva posicion del ojo segun la rotacion actual de la camara.
            for (int i = 1; i < 2; i++)
            {
                TGCVector3 step = new TGCVector3(moveVector.X,moveVector.Y,moveVector.Z);
                eyePosition += TGCVector3.TransformNormal( step*elapsedTime, cameraRotation);
                boundingBox.transform(TGCMatrix.Translation(eyePosition));
                //colision

                foreach (var box in boxes)//eventualmente se va a tener que acelerar supongo
                {
                    var result = TgcCollisionUtils.classifyBoxBox(boundingBox, box);
                    if (result == TgcCollisionUtils.BoxBoxResult.Adentro)
                    {
                        Logger.Log("no papa la re bardeaste");
                    }
                    else if (result == TgcCollisionUtils.BoxBoxResult.Atravesando)
                    {
                        TGCVector3 displacement = new TGCVector3(0, 0, 0); //asigno aca porque c# me obliga lenguaje de mierda en c esto no pasaba
                                                                           //puede que haya una forma mejor de hacer esto, abriendo el codigo de boundingBox
                        var cameraCenter = boundingBox.calculateBoxCenter();
                        var cameraRadius = boundingBox.calculateAxisRadius().X;//es un cuadrado
                        var boxCenter = box.calculateBoxCenter();
                        var boxRadius = box.calculateAxisRadius();
                        if (boxCenter.X - boxRadius.X < cameraCenter.X + cameraRadius
                            && boxCenter.X - boxRadius.X > cameraCenter.X - cameraRadius)
                        {
                            displacement.X += (boxCenter.X - boxRadius.X) - (cameraCenter.X + cameraRadius);
                            Logger.Log("X-");
                        }
                        if (boxCenter.X + boxRadius.X > cameraCenter.X - cameraRadius
                           && boxCenter.X + boxRadius.X < cameraCenter.X + cameraRadius)
                        {
                            displacement.X += (boxCenter.X + boxRadius.X) - (cameraCenter.X - cameraRadius);
                            Logger.Log("X+");
                        }
                        if (boxCenter.Y - boxRadius.Y < cameraCenter.Y + cameraRadius
                           && boxCenter.Y - boxRadius.Y > cameraCenter.Y - cameraRadius)
                        {
                            displacement.Y += (boxCenter.Y - boxRadius.Y) - (cameraCenter.Y + cameraRadius);
                            Logger.Log("Y-");
                        }
                        if (boxCenter.Y + boxRadius.Y > cameraCenter.Y - cameraRadius
                           && boxCenter.Y + boxRadius.Y < cameraCenter.Y + cameraRadius)
                        {
                            displacement.Y += (boxCenter.Y + boxRadius.Y) - (cameraCenter.Y - cameraRadius);
                            Logger.Log("Y+");
                        }
                        if (boxCenter.Z - boxRadius.Z < cameraCenter.Z + cameraRadius
                           && boxCenter.Z - boxRadius.Z > cameraCenter.Z - cameraRadius)
                        {
                            displacement.Z += (boxCenter.Z - boxRadius.Z) - (cameraCenter.Z + cameraRadius);
                            Logger.Log("Z-");
                        }
                        if (boxCenter.Z + boxRadius.Z > cameraCenter.Z - cameraRadius
                           && boxCenter.Z + boxRadius.Z < cameraCenter.Z + cameraRadius)
                        {
                            displacement.Z += (boxCenter.Z + boxRadius.Z) - (cameraCenter.Z - cameraRadius);
                            Logger.Log("Z+");
                        }

                        //solo mantener eje menor != 0. Es una heuristica para manejar esquinas
                        //osea, de los desplazamientos hacer solo el menos brusco
                        {
                            var nums = new List<vecIndex>();
                            nums.Add(new vecIndex(0, displacement.X));
                            nums.Add(new vecIndex(1, displacement.Y));
                            nums.Add(new vecIndex(2, displacement.Z));
                            nums.RemoveAll(n => n.val == 0);
                            if (nums.Count != 0)
                            {
                                //if (nums.Count == 2)
                                //{
                                //    Logger.Log("X: " + displacement.X.ToString() + "Y: " + displacement.Y.ToString() + "Z: " + displacement.Z.ToString());
                                //}

                                nums.Sort((a, b) => Math.Abs(a.val) < Math.Abs(b.val) ? -1 : 1);
                                if (nums[0].index == 0)
                                {
                                    displacement.Y = 0; displacement.Z = 0;
                                }
                                else if (nums[0].index == 1)
                                {
                                    displacement.X = 0; displacement.Z = 0;
                                }
                                else
                                {
                                    displacement.X = 0; displacement.Y = 0;
                                }

                                //if (nums.Count == 2)
                                //{
                                //    Logger.Log("=> X: " + displacement.X.ToString() + "Y: " + displacement.Y.ToString() + "Z: " + displacement.Z.ToString());
                                //}
                            }
                        }

                        eyePosition += displacement * 1.1f;
                        //break;
                    }
                }
            }
            //Logger.Log("c+ " + (cameraCenter.Y + cameraRadius).ToString() + " c- " + (cameraCenter.Y - cameraRadius).ToString());

            // Calculamos el target de la camara, segun su direccion inicial y las rotaciones en screen space x,y.
            var cameraRotatedTarget = TGCVector3.TransformNormal(directionView, cameraRotation);
            var cameraFinalTarget = eyePosition + cameraRotatedTarget;

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