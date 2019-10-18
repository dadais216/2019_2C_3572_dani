using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Mathematica;

namespace TGC.Group.Model
{
    class Hands
    {
        public int state = 0;//0 nada 1 mano derecha 2 ambas manos

        //estas variables estan para hacer la animacion de levantar la vela si es que la hago,
        //por ahora lo dejo para el final
        //TGCVector3 rightPos = new TGCVector3(-25f, -10f, -25f);
        //TGCVector3 leftPos  = new TGCVector3(0, 0, 0);

        //con los estados y si las pos.y>k se sabe todo lo necesario

        public void renderCandles()
        {
            /*
            if(rightPos.Y> k)
                //vela en campo visual, dependiendo del estado ver si poner shaders y si moverla adentro o afuera de la pantalla
            //lo mismo para la izquierda, y esta lo de pasar la vela a la otra mano si se apaga la derecha
             */

            var forwardPos = g.camera.eyePosition + g.camera.cameraRotatedTarget * 10f;
            if (state > 0)
            {
                var rotatedPos= forwardPos 
                            + TGCVector3.Cross(g.camera.eyePosition - forwardPos, TGCVector3.Up) * .4f
                            + TGCVector3.Down * 4f;

                //Camera.Camera.cameraRotation * iria al principio, pero sacandole los elementos en y
                //o construir una matriz de rotacion segun algo
                g.map.candleMesh.Transform = TGCMatrix.Scaling(.1f, .2f, .1f) * TGCMatrix.Translation(rotatedPos);
                //lo de mirar hacia arriba y que levante la vela no fue intencional pero queda bien
                g.map.candleMesh.Render();
            }
            if (state == 2)
            {
                var rotatedPos = forwardPos
                            + TGCVector3.Cross(g.camera.eyePosition - forwardPos, TGCVector3.Up) * -.4f
                            + TGCVector3.Down * 4f;

                //Camera.Camera.cameraRotation * iria al principio, pero sacandole los elementos en y
                //o construir una matriz de rotacion segun algo
                g.map.candleMesh.Transform = TGCMatrix.Scaling(.1f, .2f, .1f) * TGCMatrix.Translation(rotatedPos);
                //lo de mirar hacia arriba y que levante la vela no fue intencional pero queda bien
                g.map.candleMesh.Render();
            }
        }

        public void updateCandle()
        {
            //esto es para las animaciones que por ahora no
            /*
            TGCVector3 leftMov=new TGCVector3(0,0,0);
            TGCVector3 rightMov=new TGCVector3(0,0,0);
            
            switch (state)
            {
                case 0:
                    if (leftPos.Y > -10f)
                        leftMov.Y -= 10f;
                    if (rightPos.Y > -10f)
                        rightPos.Y -= 10f;
                    break;
                //se tiene que mover hacia adentro del personaje para no desaparecer visiblemente
                case 1:
                    if (leftPos.Y > -10f)
                        leftMov.Y -= 10f;
                    if (rightPos.Y < 80f)
                        rightPos.Y += 10f;
                    if (rightPos.X < 80f) //hubo un giro
                        rightPos.X += 10f; 
                    break;
                case 2:
                    if (leftPos.Y < 80f)
                        leftMov.Y += 10f;
                    if (rightPos.Y < 80f)
                        rightPos.Y += 10f;
                    if (rightPos.X < 80f) //por las dudas
                        rightPos.X += 10f;
                    break;
            }

            rightPos += rightMov * 10f * g.game.ElapsedTime;
            leftPos += leftMov * 10f * g.game.ElapsedTime;
            */
        }

        public bool maybePickCandle()
        {
            if (state == 2)
                return false;
            state++;
            return true;
        }

        public void killLeft()
        {
            state = 1;
        }
        public void killRight()
        {
            state = 1;
            //var temp = rightPos * 1f;//osea una copia
            //leftPos = rightPos;
            //rightPos = leftPos;
        }

    }
}
