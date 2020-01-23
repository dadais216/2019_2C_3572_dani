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


        public static float leftrightRot = 2.803196f;

        //No hace falta la base ya que siempre es la misma, la base se arma segun las rotaciones de esto costados y updown.
        /// <summary>
        ///
        /// </summary>
        private static float updownRot = 0.5277546f;

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
        public TGCVector3 eyePosition = new TGCVector3(-0, -10800, -45000);
        public TGCVector3 cameraRotatedTarget;

        /// <summary>
        ///  Velocidad de movimiento
        /// </summary>
        public readonly float MovementSpeed = 7000f;
        public float stamina = 5000f;

        /// <summary>
        ///  Velocidad de rotacion
        /// </summary>
        public readonly float RotationSpeed = 0.1f;

        public TgcRay down,horx,horz;

        private bool onGround = false;
        private float vSpeed = 10;

        private bool underRoof;
        private TGCVector3 eyePositionOffRoof;


        

        /// <summary>
        ///     Constructor de la camara a partir de un TgcD3dInput el cual ya tiene por default el eyePosition (0,0,0), el mouseCenter a partir del centro del a pantalla, RotationSpeed 1.0f,
        ///     MovementSpeed y JumpSpeed 500f, el directionView (0,0,-1)
        /// </summary>
        public Camera()
        {
            down = new TgcRay();
            horx = new TgcRay();
            horz = new TgcRay();
            down.Direction = new TGCVector3(0, -1, 0);
            horx.Direction = new TGCVector3(1, 0, 0);
            horz.Direction = new TGCVector3(0, 0, 1);


            g.camera = this;
            g.hands=new Hands();
            

        }

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

            if (g.game.gameState!= 1)
                goto setCamera;
            //Lock camera
            if (g.input.keyPressed(Key.L))
            {
                LockCam = !lockCam;
            }

            var inputMove = TGCVector3.Empty;
            if (lockCam)
            {
                leftrightRot -= -g.input.XposRelative * RotationSpeed;
                updownRot -= g.input.YposRelative * RotationSpeed;
                // Se actualiza matrix de rotacion, para no hacer este calculo cada vez y solo cuando en verdad es necesario.
                cameraRotation = TGCMatrix.RotationX(updownRot) * TGCMatrix.RotationY(leftrightRot);
                Cursor.Position = mouseCenter;
            }

            if (g.input.keyDown(Key.W))
                inputMove += Map.South;
            if (g.input.keyDown(Key.S))
                inputMove += Map.North;
            if (g.input.keyDown(Key.A))
                inputMove += Map.East;
            if (g.input.keyDown(Key.D))
                inputMove += Map.West;

            

            TGCVector3 moveXZ = TGCVector3.TransformNormal(inputMove, cameraRotation);
            moveXZ.Y = 0;
            moveXZ.Normalize();



            TGCVector3 moving = moveXZ * MovementSpeed * elapsedTime;
            if (g.input.keyDown(Key.LeftShift) && onGround && stamina>500f)
                moving *= 3f;

            stamina -= moving.Length()/40f;
            stamina = Math.Min(stamina+ (MovementSpeed * elapsedTime)/(g.cameraSprites.infiniteStamina? 1f:35f), 5000f);




            float t;
            TGCVector3 q;

            bool onBox=false;
            //salto
            if (onGround)
            {
                if (g.input.keyDown(Key.Space) && !underRoof && stamina>1000f)
                {
                    stamina -= 1000f;
                    vSpeed = 100f- Math.Max(-elapsedTime * 100, -380);
                }
                else
                    vSpeed = 0f;
                //antes estaba en -20 porque sino va pegando saltitos en la bajada
                //ahora lo dejo en 0 porque causa mucha vibracion con bajos fps
            }
            else
                vSpeed = Math.Max(vSpeed - elapsedTime * 100, -480);//gravedad
                //v=a*t
                //x=v*t
                //como tengo t al cuadrado no deberia usar elapsed porque dependeria de los fps
                //sigo con elapsed porque no note una diferencia importante
                //la solucion seria medir de forma absoluta desde el comienzo del salto


            if(!underRoof)
                eyePositionOffRoof= new TGCVector3(eyePosition.X, eyePosition.Y, eyePosition.Z);
            underRoof = false;




            moving += -down.Direction* vSpeed*elapsedTime * 100;

            //rayos colision

            //tirar solo un rayo horizontal en la direccion que me muevo y sacar el
            //desplazamiento haciendo cuentas con la normal del triangulo no andaria si me estoy moviendo
            //a una esquina, porque nomas reconoceria una de las paredes


            //lo que hice durante casi todo el desarrollo es tirar 6 rayos en cada direccion ortogonal y 
            //desplazar segun estos. Despues lo baje a 3, haciendo lo mismo.
            //el problema de esto es que no maneja bien las esquinas. En un frame puede estar justo afuera
            //y en el proximo justo adentro. No es normal que pase pero con un par de intentos se logra.
            //desviar un poco los rayos no termina de solucionar el problema, si hay una caja mas o menos alineada
            //atraviesa igual

            //lo que voy a hacer es tirar un rayo en la orientacion que se esta moviendo, otro ortogonal a ese.
            //puede que sea suficiente.
            //Bueno probe hacer eso y el rayo diagonal puede justo pasar entre las dos paredes sin tocar ninguna,
            //y ademas el movimiento se sentia un poco mas raro.

            //la ultima solucion que se me ocurrio es que no haya esquinas, hacer que una pared sobresalga un poco.
            //ya que estoy con eso no hay necesidad de tirar rayos especiales, voy a volver a ortogonales




            //en la colision se asume que el centro del personaje nunca atraviesa la pared

            //solo se mueve cierta distancia por iteracion. Esto hace que no se atraviesen las cosas.
            //con fps bajos va a correr todavia mas lento pero va a ser consistente
            float dist=moving.Length();
            int iters = (int)FastMath.Ceiling(dist/45f);


            var step = moving * (1 / (float)iters);

            iters = FastMath.Min(iters, 20);//dropear iteraciones en casos extremos, sino
                                            //el elapsedTime va a ser todavia mas grande en el proximo frame

            var displacement = new TGCVector3(0, 0, 0);
            const float border = 100f;

            for (int i = 0; i < iters; i++)
            {
                eyePosition += step;
                horx.Origin = new TGCVector3(eyePosition.X - border, eyePosition.Y, eyePosition.Z);
                horz.Origin = new TGCVector3(eyePosition.X, eyePosition.Y, eyePosition.Z - border);
                var ceilignH = 3 * border;
                down.Origin = new TGCVector3(eyePosition.X, eyePosition.Y + ceilignH, eyePosition.Z);

                bool doGoto = false;
                var handleRays = new Action<Parallelepiped>((box) =>
                {
                    var handleHor = new Action<TgcRay>((ray) =>
                      {
                        //lo malo un rayo por direccion en vez de dos es que si tengo 2 paredes
                        //casi paralelas y me voy acercando a la interseccion voy a atravesar una
                        //de las dos, porque la colision frena con la primera. Se podría cambiar 
                        //intersecRay para que siga pero creo que nunca va a aparecer algo asi en el mapa.
                        //Podria pasar si dejo que los arboles roten.
                        if (box.intersectRay(ray, out t, out q) && t < 2 * border)
                          {
                              if (t < border)
                              {
                                  displacement += ray.Direction * t;
                              }
                              else
                              {
                                  displacement += -ray.Direction * (2*border - t);
                              }
                          }
                      });
                    handleHor(horx);
                    handleHor(horz);

                    var cameraHeight = 9f*border;

                    if (box.intersectRay(down, out t, out q) && t < cameraHeight+ceilignH)
                    {
                        if (t < ceilignH)
                        {
                            underRoof = true;
                            if (t < border)
                            {
                                if (onGround)//esta subiendo una pendiente hacia un techo, se pudre todo
                                {
                                    eyePosition += new TGCVector3(eyePositionOffRoof.X - eyePosition.X, 0f, eyePositionOffRoof.Z - eyePosition.Z);
                                    //no es la mejor solucion porque causa mucho temblor, pero bueno
                                    doGoto = true;//salto el codigo de terreno porque puede hacer subir la camara
                                    return;
                                }
                                displacement += down.Direction * (border - t);
                                vSpeed = 0f;
                            }
                        }
                        else if(vSpeed <= 0)
                        {
                            //t<cameraHeight+ceilingH
                            displacement += -down.Direction * (cameraHeight+ceilignH - t);
                            onBox = true;
                        }
                    }
                });
                var chunk = g.chunks.fromCoordinates(eyePosition);
                Meshc setToRemove = null;
                foreach (var meshc in chunk.meshes)
                {
                    var box = meshc.paralleliped;

                    if (g.map.isCandle(meshc))
                    {
                        if (g.map.pointParallelipedXZColission(box, eyePosition,100f))
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
                    if(g.hands.maybePickCandle(setToRemove))
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
            TGCVector3 cameraFinalTarget;
            if (g.game.gameState != 2)
            {
                cameraRotatedTarget = TGCVector3.TransformNormal(directionView, cameraRotation);
                cameraFinalTarget = eyePosition + cameraRotatedTarget;
            }
            else
            {
                var mostroDir = g.mostro.pos + TGCVector3.Up * g.mostro.height;
                mostroDir.Normalize(); //se normaliza por el largo del lineVec
                cameraFinalTarget =  eyePosition - mostroDir;
            }
            var cameraOriginalUpVector = DEFAULT_UP_VECTOR;
            var cameraRotatedUpVector = TGCVector3.TransformNormal(cameraOriginalUpVector, cameraRotation);
        
            base.SetCamera(eyePosition, cameraFinalTarget, cameraRotatedUpVector); //cambiar por TGCVector3.Up cuando restringa la camara
            //base.SetCamera(eyePosition, cameraRotatedTarget, cameraRotatedUpVector);

            //Logger.Log(eyePosition);
            //Logger.Log(map.chunks.fromCoordinates(eyePosition, false).center.X.ToString()+"  "+ map.chunks.fromCoordinates(eyePosition, false).center.Y.ToString());

            
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