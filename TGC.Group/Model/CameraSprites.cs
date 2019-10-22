﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.Text;

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

                g.map.precomputeCandlePolygonVertex();
                Meshc.matrizChange = true;//hack, igual eventualmente no voy a usar la matriz esta
                g.map.addCandles();
                return;
            }
            if (g.input.keyPressed(Microsoft.DirectX.DirectInput.Key.DownArrow))
            {
                selectorState= selectorState < 3?
                    (selectorState==2?2:selectorState+1):
                    (selectorState == 8 ? 8 : selectorState + 1);
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
                    case 7: debugVisualizations = !debugVisualizations;break;
                    case 8: selectorState = 2;break;
                }
                actualStateDraw = selectorState;
            }
            if (g.input.keyPressed(Microsoft.DirectX.DirectInput.Key.RightArrow))
            {
                if (selectorState == 5)
                {
                    candlesRequired = Math.Max(1, candlesRequired + 1);
                    candlesInMap = Math.Max(candlesRequired, candlesInMap);
                }
                if (selectorState == 6)
                    candlesInMap = Math.Max(1, candlesInMap + 50);
            }
            if (g.input.keyPressed(Microsoft.DirectX.DirectInput.Key.LeftArrow))
            {
                if (selectorState == 5)
                    candlesRequired=Math.Max(1,candlesRequired-1);
                if (selectorState == 6)
                    candlesInMap= Math.Max(candlesRequired, candlesInMap - 50);
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


        int selectorState = 0;
        int actualStateDraw = -1;
        public void initMenu()
        {
            text.Align = TgcText2D.TextAlign.LEFT;
            text.Color = Color.White;
            text.Text = "TITULO";
            text.Size = new Size(1000, 1000);
            text.Position = new Point(60, 60);
            text.changeFont(new Font("TimesNewRoman", 45, FontStyle.Bold));

            var commonFont = new Font("TimesNewRoman", 25, FontStyle.Bold);

            text2.Align = TgcText2D.TextAlign.LEFT;
            text2.Color = Color.White;
            text2.Text = "w para comerzar";
            text2.Size = new Size(1000, 1000);
            text2.Position = new Point(50, 120);
            text2.changeFont(commonFont);

            text3.Align = TgcText2D.TextAlign.LEFT;
            text3.Color = Color.White;
            text3.Text = "objetivo\ncontroles\ntrucos";
            text3.Size = new Size(1000, 1000);
            text3.Position = new Point(40, 450);
            text3.changeFont(commonFont);

            text4.Align = TgcText2D.TextAlign.LEFT;
            text4.Color = Color.White;
            text4.Text = "llevar 9 velas al centro de la iglesia";
            text4.Size = new Size(1000, 1000);
            text4.Position = new Point(340, 450);
            text4.changeFont(commonFont);

            text5.Align = TgcText2D.TextAlign.LEFT;
            text5.Color = Color.White;
            text5.Text = "wasd  - moverse\nshift  - correr\nh    - ???";
            text5.Size = new Size(1000, 1000);
            text5.Position = new Point(340, 450);
            text5.changeFont(commonFont);

            text6.Align = TgcText2D.TextAlign.LEFT;
            text6.Color = Color.White;
            text6.Text = "estamina infinita\nesqueleto mitad velocidad\ncantidad de velas requeridas:\nvelas en mapa:\nvisualizacion debug (z colisiones,x meshes,c chunks)\natras";
            text6.Size = new Size(1000, 1000);
            text6.Position = new Point(250, 450);
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
            candlesRequiredText.Position = new Point(710, 528);
            candlesRequiredText.changeFont(commonFont);

            candlesInMapText.Align = TgcText2D.TextAlign.LEFT;
            candlesInMapText.Color = Color.White;
            candlesInMapText.Size = new Size(1000, 1000);
            candlesInMapText.Position = new Point(490, 567);
            candlesInMapText.changeFont(commonFont);


        }

        public bool infiniteStamina = false;
        public bool squeletonHalfSpeed = false;
        public int candlesRequired = 9;
        public int candlesInMap = 500;
        public bool debugVisualizations = false;

        public void renderMenu()
        {
            text.render();
            text2.render();
            text3.render();

            if (selectorState < 3)
            {
                selector.Position = new Point(15, 450 + selectorState * 40);
            }
            else
            {
                selector.Position = new Point(205, 450 + (selectorState-3) * 39);
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
                    seleccionado.Position = new Point(225, 450);
                    seleccionado.render();
                }
                if (squeletonHalfSpeed)
                {
                    seleccionado.Position = new Point(225, 450+39);
                    seleccionado.render();
                }
                if (debugVisualizations)
                {
                    seleccionado.Position = new Point(225, 450 + 39*4);
                    seleccionado.render();
                }
                candlesRequiredText.Text = candlesRequired.ToString();
                candlesRequiredText.render();
                candlesInMapText.Text = candlesInMap.ToString();
                candlesInMapText.render();
            }


        }



    }
}