using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.DirectX.Direct3D;
using TGC.Core.BoundingVolumes;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Terrain;
using TGC.Core.Textures;
using TGC.Group.Model.Camera;

namespace TGC.Group.Model
{
    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {
        public int actualFrame=0;

        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;

            g.game = this;

        }


        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aquí todo el código de inicialización: cargar modelos, texturas, estructuras de optimización, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {
            g.input = Input;
            g.cameraSprites = new CameraSprites();
            g.cameraSprites.initMenu();
            Camara =new Camera.Camera();
            new Map();
        }

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la lógica de computo del modelo, así como también verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        /// 

        public static TGCMatrix matriz;

        public static bool debugColission = false;
        public static bool debugChunks = false;
        public static bool debugMeshes = true;

        public int gameState = 0;//0 menu inicio, 1 juego, 2 muerte


        public override void Update()
        {
            PreUpdate();

            if (ElapsedTime > .5f)
                ElapsedTime = .5f;//cuando estoy debuggeando a veces el tiempo salta un monton, el esqueleto sale del mapa y crashea el juego

            actualFrame++;

            if (gameState==0)
            {
                g.cameraSprites.updateMenu();

            }
            else if(gameState==1)
            {
                /*
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.Y) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.U) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.I) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.H) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.J) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.K) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.B) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.N) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.M) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.D1) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.D2) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.D3) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.D4) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.D5) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.D6) ||
                    Input.keyDown(Microsoft.DirectX.DirectInput.Key.D7))
                    Meshc.matrizChange = true;

                float sgn = .5f;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift))
                    sgn = -.5f;

                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.Y)) matriz.M11 += ElapsedTime * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.U)) matriz.M12 += ElapsedTime * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.I)) matriz.M13 += ElapsedTime * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.H)) matriz.M21 += ElapsedTime * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.J)) matriz.M22 += ElapsedTime * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.K)) matriz.M23 += ElapsedTime * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.B)) matriz.M31 += ElapsedTime * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.N)) matriz.M32 += ElapsedTime * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.M)) matriz.M33 += ElapsedTime * sgn;

                //ni idea de por que la traslacion es tan lenta
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D1)) matriz.M41 += ElapsedTime * 100 * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D2)) matriz.M42 += ElapsedTime * 100 * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D3)) matriz.M43 += ElapsedTime * 100 * sgn;

                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D4)) matriz.M14 += 0.1f * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D5)) matriz.M24 += 0.1f * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D6)) matriz.M34 += 0.1f * sgn;
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D7)) matriz.M44 += 0.1f * sgn;
                */
                if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.K))
                    g.map.deforming+= Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift)? -5f:5f;

                if (g.cameraSprites.debugVisualizations)
                {
                    if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.Z))
                    {
                        debugColission = !debugColission;
                    }
                    else if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.X))
                    {
                        debugMeshes = !debugMeshes;
                    }
                    else if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.C))
                    {
                        debugChunks = !debugChunks;
                    }
                }
                g.mostro.update();
                g.hands.updateCandle();
                g.map.updateCandlePlace();
            }

            PostUpdate();
        }

        Shadow shadow = new Shadow();
        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aquí todo el código referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>

        public override void Render()
        {
            ClearTextures();

            shadow.render();

            BeginRenderScene();
            g.map.shader.SetValue("shadowTexture",g.shadow.tex);

            g.terrain.technique = "DIFFUSEWITHSHADOW";

            
            if (gameState==0)
            {
                g.cameraSprites.renderMenu();
            }

            g.map.Render();
            g.mostro.render();

            if (gameState == 1)
            {
                g.cameraSprites.renderStaminaBar();
                g.hands.renderCandles();
            }
            RenderAxis();
            RenderFPS();
            
            EndRenderScene();
        }

        /// <summary>
        ///     Se llama cuando termina la ejecución del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gráficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {

        }

        
    }
}