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
        private Texture renderTarget;
        private Surface depthStencil;//no sé si lo necesito
        private Effect postProcess;
        private VertexBuffer vertexBuffer;
        public override void Init()
        {
            g.input = Input;
            g.cameraSprites = new CameraSprites();
            g.cameraSprites.initMenu();
            Camara =new Camera.Camera();
            new Map();
            new Shadow();


            //copy paste de distorciones.cs, ni idea
            Device d3dDevice = D3DDevice.Instance.Device;
            depthStencil = d3dDevice.CreateDepthStencilSurface(d3dDevice.PresentationParameters.BackBufferWidth, 
                d3dDevice.PresentationParameters.BackBufferHeight, DepthFormat.D24S8, MultiSampleType.None, 0, true);

            renderTarget = new Texture(d3dDevice, 
                d3dDevice.PresentationParameters.BackBufferWidth, 
                d3dDevice.PresentationParameters.BackBufferHeight, 
                1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);

            postProcess = Effect.FromFile(d3dDevice, 
                ShadersDir + "postProcess.fx", null, null, 
                ShaderFlags.PreferFlowControl, null,out string compilationErrors);
            if (postProcess == null)
            {
                throw new Exception("Error al cargar shader. Errores: " + compilationErrors);
            }

            postProcess.SetValue("renderTarget", renderTarget);
            postProcess.SetValue("screen_dx", d3dDevice.PresentationParameters.BackBufferWidth);
            postProcess.SetValue("screen_dy", d3dDevice.PresentationParameters.BackBufferHeight);

            CustomVertex.PositionTextured[] vertices = new CustomVertex.PositionTextured[]
            {
                new CustomVertex.PositionTextured( -1, 1, 1, 0,0),
                new CustomVertex.PositionTextured(1,  1, 1, 1,0),
                new CustomVertex.PositionTextured(-1, -1, 1, 0,1),
                new CustomVertex.PositionTextured(1,-1, 1, 1,1)
            };
            vertexBuffer = new VertexBuffer(typeof(CustomVertex.PositionTextured), 4, 
                d3dDevice, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionTextured.Format, Pool.Default);
            vertexBuffer.SetData(vertices, 0, LockFlags.None);
        }

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la lógica de computo del modelo, así como también verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        /// 

        public static bool debugColission = false;
        public static bool debugChunks = false;
        public static bool debugMeshes = true;
        public static bool debugSqueleton = false;

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
                    } else if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.V))
                    {
                        debugSqueleton = !debugSqueleton;
                    }
                }
                g.mostro.update();
                g.hands.updateCandle();
                g.map.updateCandlePlace();
            }

            PostUpdate();
        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aquí todo el código referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>

        public override void Render()
        {
            ClearTextures();
            if (g.mostro.mode == 3)
            {
                Meshc.actualShader = g.shadow.shader;
                Meshc.actualTechnique = "RenderShadow";

                g.shadow.render();
                actualFrame++;

                Meshc.actualShader = g.map.shader;
                Meshc.actualTechnique = "DIFFUSEWITHSHADOW";
            }
            else
            {
                Meshc.actualShader = g.map.shader;
                Meshc.actualTechnique = "DIFFUSE_MAP";
            }
#if true


            Device d3dDevice = D3DDevice.Instance.Device;
            Surface pOldRT=null; Surface pOldDS=null;
            if (g.cameraSprites.pixels != 0)
            {
                pOldRT = d3dDevice.GetRenderTarget(0);
                d3dDevice.SetRenderTarget(0, renderTarget.GetSurfaceLevel(0));
                pOldDS = d3dDevice.DepthStencilSurface;
                d3dDevice.DepthStencilSurface = depthStencil;
            }
            d3dDevice.BeginScene();
            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            if (gameState == 0)
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

            if (g.cameraSprites.pixels != 0)
            {
                d3dDevice.EndScene();

                d3dDevice.DepthStencilSurface = pOldDS;
                d3dDevice.SetRenderTarget(0, pOldRT);

                d3dDevice.BeginScene();

                d3dDevice.VertexFormat = CustomVertex.PositionTextured.Format;
                d3dDevice.SetStreamSource(0, vertexBuffer, 0);
                postProcess.SetValue("renderTarget", renderTarget);
                postProcess.SetValue("pixels", g.cameraSprites.pixels);

                d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
                postProcess.Begin(FX.None);
                postProcess.BeginPass(0);
                d3dDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                postProcess.EndPass();
                postProcess.End();
            }
            d3dDevice.EndScene();
            d3dDevice.Present();
#endif
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