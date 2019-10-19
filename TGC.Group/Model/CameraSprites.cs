using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;

namespace TGC.Group.Model
{
    class CameraSprites
    {
        public void renderStaminaBar()
        {
            //dibujo un poligono enfrente de la camara
            //es medio choto porque lo hago pasar por todas las projecciones y eso, seria mejor dibujar un sprite directamente
            //pero tgccore parece no tener nada de eso y va a ser mas rapido programar esto que ver como renderizar un sprite

            var bar = new TgcConvexPolygon();
            var vertex=new TGCVector3[4];

            var forward = g.camera.cameraRotatedTarget * 5f;
            var forwardPos = g.camera.eyePosition + forward;

            var right = TGCVector3.Cross(g.camera.UpVector, forward) *.1f;
            var down = TGCVector3.Cross(right, forward)*.1f;
            forwardPos += right * 4f + down *7.5f;


            var lenght = g.camera.stamina / 5000f * 1.5f;

            vertex[0] = forwardPos;
            vertex[1] = forwardPos + right * lenght;
            vertex[2] = forwardPos + right * lenght + down * 0.3f;
            vertex[3] = forwardPos + down * 0.3f;

            bar.BoundingVertices = vertex;
            bar.Color = Color.White;
            bar.updateValues();

            bar.Render();

        }

        public bool gameStart=true;
        public void updateMenu()
        {
            if (g.input.keyDown(Microsoft.DirectX.DirectInput.Key.W))
            {
                gameStart = false;
                return;
            }


        }

        public void renderMenu()
        {

        }



    }
}
