﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.Text;
using TGC.Group.Form;

namespace TGC.Group.Model
{
    class CameraSprites
    {
        public CameraSprites()
        {
            x = GameForm.ActiveForm.Width / 30;
            y = GameForm.ActiveForm.Height / 30;
        }
        int x;
        int y;

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

        public void updateMenu()
        {
            if (g.input.keyDown(Microsoft.DirectX.DirectInput.Key.W))
            {
                pixels = pixelsBuf;
                g.game.gameState = 1;

                g.map.precomputeCandlePolygonVertex();
                g.map.addCandles();
                return;
            }
            if (g.input.keyPressed(Microsoft.DirectX.DirectInput.Key.DownArrow))
            {
                selectorState= selectorState < 3?
                    (selectorState==2?2:selectorState+1):
                    (selectorState == 10 ? 10 : selectorState + 1);
            }
            if (g.input.keyPressed(Microsoft.DirectX.DirectInput.Key.UpArrow))
            {
                selectorState = selectorState < 3 ?
                    (selectorState == 0 ? 0 : selectorState - 1) :
                    (selectorState == 3 ? 3 : selectorState - 1);
            }
            if (g.input.keyPressed(Microsoft.DirectX.DirectInput.Key.Z))
            {
                switch (selectorState)
                {
                    case 2: selectorState = 3;break;
                    case 3: infiniteStamina = !infiniteStamina;break;
                    case 4: squeletonHalfSpeed = !squeletonHalfSpeed;break;
                    case 7: inmunity = !inmunity; break;
                    case 8: debugVisualizations = !debugVisualizations;break;
                    case 10: selectorState = 2;break;
                }
                actualStateDraw = selectorState;
            }
            if (g.input.keyPressed(Microsoft.DirectX.DirectInput.Key.RightArrow))
            {
                if (selectorState == 5)
                {
                    candlesRequired += candlesRequired + 1;
                }
                if (selectorState == 6)
                    candlesInMap += 50;
                if (selectorState == 9)
                {
                    pixelsBuf = pixelsBuf==0? 300:Math.Max(0, pixelsBuf - 50);
                }
            }
            if (g.input.keyPressed(Microsoft.DirectX.DirectInput.Key.LeftArrow))
            {
                if (selectorState == 5)
                    candlesRequired=Math.Max(1,candlesRequired-1);
                if (selectorState == 6)
                    candlesInMap= Math.Max(candlesRequired, candlesInMap - 50);
                if (selectorState == 9)
                    pixelsBuf = pixelsBuf==300?0:Math.Min(300, pixelsBuf + 50);
            }
        }

        TgcText2D text = new TgcText2D();
        TgcText2D text2 = new TgcText2D();
        TgcText2D text3 = new TgcText2D();
        TgcText2D text4 = new TgcText2D();
        TgcText2D text5 = new TgcText2D();
        TgcText2D text6 = new TgcText2D();

        TgcText2D selector = new TgcText2D();
        TgcText2D seleccionado = new TgcText2D();

        TgcText2D candlesRequiredText = new TgcText2D();
        TgcText2D candlesInMapText = new TgcText2D();
        TgcText2D indieText = new TgcText2D();

        int selectorState = 0;
        int actualStateDraw = -1;
        public void initMenu()
        {

            //los sizes tambien deberian depender de x y pero eh

            text.Align = TgcText2D.TextAlign.LEFT;
            text.Color = Color.White;
            text.Text = "TITULO";
            text.Size = new Size(1000, 1000);
            //text.Position = new Point(60, 60);
            text.Position = new Point(2*x, 2* y);


            text.changeFont(new Font("TimesNewRoman", 45, FontStyle.Bold));

            var commonFont = new Font("TimesNewRoman", 25, FontStyle.Bold);

            text2.Align = TgcText2D.TextAlign.LEFT;
            text2.Color = Color.White;
            text2.Text = "w para comerzar";
            text2.Size = new Size(1000, 1000);
            //text2.Position = new Point(50, 120);
            text2.Position=new Point(2 * x, 4 * y);
            text2.changeFont(commonFont);

            text3.Align = TgcText2D.TextAlign.LEFT;
            text3.Color = Color.White;
            text3.Text = "objetivo\ncontroles\n";
            text3.Size = new Size(1000, 1000);
            //text3.Position = new Point(40, 450);
            text3.Position = new Point(1*x, 18*y);
            text3.changeFont(commonFont);

            text4.Align = TgcText2D.TextAlign.LEFT;
            text4.Color = Color.White;
            text4.Text = "llevar 9 velas al centro de la iglesia";
            text4.Size = new Size(1000, 1000);
            //text4.Position = new Point(340, 450);
            text4.Position = new Point(10*x, 18*y);
            text4.changeFont(commonFont);

            text5.Align = TgcText2D.TextAlign.LEFT;
            text5.Color = Color.White;
            text5.Text = "wasd espacio  - moverse\nshift  - correr\n";
            text5.Size = new Size(1000, 1000);
            //text5.Position = new Point(340, 450);
            text5.Position = new Point(10*x, 18*y);
            text5.changeFont(commonFont);

            text6.Align = TgcText2D.TextAlign.LEFT;
            text6.Color = Color.White;
            text6.Text = "estamina infinita\nesqueleto mitad velocidad\n"
                +"cantidad de velas requeridas:\nvelas en mapa:\ninmunidad\n" +
                "visualizacion debug (z colisiones,x meshes,c chunks)\nindie: \natras";
            text6.Size = new Size(1000, 1000);
            //text6.Position = new Point(250, 450);
            text6.Position = new Point(8*x, 18*y);
            text6.changeFont(commonFont);
            
            selector.Align = TgcText2D.TextAlign.LEFT;
            selector.Color = Color.White;
            selector.Text = ">";
            selector.Size = new Size(1000, 1000);
            selector.changeFont(commonFont);

            seleccionado.Align = TgcText2D.TextAlign.LEFT;
            seleccionado.Color = Color.White;
            seleccionado.Text = "o";
            seleccionado.Size = new Size(1000, 1000);
            seleccionado.changeFont(commonFont);

            candlesRequiredText.Align = TgcText2D.TextAlign.LEFT;
            candlesRequiredText.Color = Color.White;
            candlesRequiredText.Size = new Size(1000, 1000);
            //candlesRequiredText.Position = new Point(710, 528);
            candlesRequiredText.Position = new Point(19*x, floor(20.2*y));
            candlesRequiredText.changeFont(commonFont);

            candlesInMapText.Align = TgcText2D.TextAlign.LEFT;
            candlesInMapText.Color = Color.White;
            candlesInMapText.Size = new Size(1000, 1000);
            //candlesInMapText.Position = new Point(490, 567);
            candlesInMapText.Position = new Point(14*x, floor(21.4*y));
            candlesInMapText.changeFont(commonFont);

            indieText.Align = TgcText2D.TextAlign.LEFT;
            indieText.Color = Color.White;
            indieText.Size = new Size(1000, 1000);
            indieText.Position = new Point(12 * x, floor(24.8 * y));
            indieText.changeFont(commonFont);
        }

        public bool infiniteStamina = false;
        public bool squeletonHalfSpeed = false;
        public int candlesRequired = 9;
        public int candlesInMap = 90;
        public bool debugVisualizations = true;
        public bool inmunity = true;
        int pixelsBuf = 0;
        public int pixels = 0;

        Func<double, int> floor = d =>
        {
            return (int)Math.Floor(d);
        };
        public void renderMenu()
        {

            

            text.render();
            text2.render();
            text3.render();

            if (selectorState < 3)
            {
                selector.Position = new Point(floor(.5*x), floor(18*y + selectorState * y*1.15));
            }
            else
            {
                selector.Position = new Point(7*x, floor(18 * y + (selectorState-3) * y * 1.15));
            }
            selector.render();

            switch (actualStateDraw)
            {
            case -1: break;
            case 0: text4.render();break;
            case 1: text5.render();break;
            default: text6.render();break;
            }
            if (actualStateDraw >= 2)
            {
                if(infiniteStamina)
                {   
                    seleccionado.Position = new Point(floor(7.2 * x), 18*y);
                    seleccionado.render();
                }
                if (squeletonHalfSpeed)
                {
                    seleccionado.Position = new Point(floor(7.2*x), 18*y + floor(1.12*y));
                    seleccionado.render();
                }
                if (inmunity)
                {
                    seleccionado.Position = new Point(floor(7.2 * x), 18 * y + floor(1.12 * y)*4);
                    seleccionado.render();
                }
                if (debugVisualizations)
                {
                    seleccionado.Position = new Point(floor(7.2 * x), 18 * y + floor(1.12 * y) * 5);
                    seleccionado.render();
                }
                candlesRequiredText.Text = candlesRequired.ToString();
                candlesRequiredText.render();
                candlesInMapText.Text = candlesInMap.ToString();
                candlesInMapText.render();
                indieText.Text = pixelsBuf == 0 ? "no" : pixelsBuf.ToString();
                indieText.render();
            }


        }



    }
}
